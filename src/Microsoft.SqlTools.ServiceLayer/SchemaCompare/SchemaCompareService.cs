// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Main class for SchemaCompare service
    /// </summary>
    class SchemaCompareService
    {
        private static ConnectionService connectionService = null;
        private SqlTaskManager sqlTaskManagerInstance = null;
        private static readonly Lazy<SchemaCompareService> instance = new Lazy<SchemaCompareService>(() => new SchemaCompareService());
        private Lazy<ConcurrentDictionary<string, SchemaComparisonResult>> schemaCompareResults =
            new Lazy<ConcurrentDictionary<string, SchemaComparisonResult>>(() => new ConcurrentDictionary<string, SchemaComparisonResult>());
        private Lazy<ConcurrentDictionary<string, Action>> currentComparisonCancellationAction =
            new Lazy<ConcurrentDictionary<string, Action>>(() => new ConcurrentDictionary<string, Action>());

        // For testability
        internal Task CurrentSchemaCompareTask;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SchemaCompareService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(SchemaCompareRequest.Type, this.HandleSchemaCompareRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareCancellationRequest.Type, this.HandleSchemaCompareCancelRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareGenerateScriptRequest.Type, this.HandleSchemaCompareGenerateScriptRequest, true);
            serviceHost.SetRequestHandler(SchemaComparePublishDatabaseChangesRequest.Type, this.HandleSchemaComparePublishDatabaseChangesRequest, true);
            serviceHost.SetRequestHandler(SchemaComparePublishProjectChangesRequest.Type, this.HandleSchemaComparePublishProjectChangesRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareIncludeExcludeNodeRequest.Type, this.HandleSchemaCompareIncludeExcludeNodeRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareIncludeExcludeAllNodesRequest.Type, this.HandleSchemaCompareIncludeExcludeAllNodesRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareOpenScmpRequest.Type, this.HandleSchemaCompareOpenScmpRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareSaveScmpRequest.Type, this.HandleSchemaCompareSaveScmpRequest, true);
        }

        /// <summary>
        /// Handles schema compare request
        /// </summary>
        public Task HandleSchemaCompareRequest(SchemaCompareParams parameters, RequestContext<SchemaCompareResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    // ServiceLayer SchemaCompareParams extends CoreContracts.SchemaCompareParams directly — no conversion needed
                    operation = new CoreOps.SchemaCompareOperation(parameters, connectionProvider);
                    currentComparisonCancellationAction.Value[operation.OperationId] = operation.Cancel;
                    operation.Execute();

                    schemaCompareResults.Value[operation.OperationId] = operation.ComparisonResult;

                    await requestContext.SendResult(new SchemaCompareResult()
                    {
                        OperationId = operation.OperationId,
                        Success = operation.ComparisonResult.IsValid,
                        ErrorMessage = operation.ErrorMessage,
                        AreEqual = operation.ComparisonResult.IsEqual,
                        Differences = operation.Differences
                    });

                    Action cancelAction = null;
                    currentComparisonCancellationAction.Value.TryRemove(operation.OperationId, out cancelAction);
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to compare schema. Error: " + e);
                    await requestContext.SendResult(new SchemaCompareResult()
                    {
                        OperationId = operation != null ? operation.OperationId : null,
                        Success = false,
                        ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                    });
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles schema compare cancel request
        /// </summary>
        public async Task HandleSchemaCompareCancelRequest(SchemaCompareCancelParams parameters, RequestContext<ResultStatus> requestContext)
        {
            Action cancelAction = null;
            if (currentComparisonCancellationAction.Value.TryRemove(parameters.OperationId, out cancelAction))
            {
                if (cancelAction != null)
                {
                    cancelAction.Invoke();
                    await requestContext.SendResult(new ResultStatus()
                    {
                        Success = true,
                        ErrorMessage = null
                    });
                }
            }
            else
            {
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = SR.SchemaCompareSessionNotFound
                });
            }
        }

        /// <summary>
        /// Handles request for schema compare generate deploy script
        /// </summary>
        public async Task HandleSchemaCompareGenerateScriptRequest(SchemaCompareGenerateScriptParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareGenerateScriptOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                // ServiceLayer SchemaCompareGenerateScriptParams extends CoreContracts version — pass directly
                coreOp = new CoreOps.SchemaCompareGenerateScriptOperation(parameters, compareResult);

                var adapter = new SchemaCompareTaskAdapter(
                    execute: () => coreOp.Execute(),
                    cancel: () => coreOp.Cancel(),
                    getError: () => coreOp.ErrorMessage
                );

                coreOp.ScriptHandler = new VsCodeScriptHandler(() => adapter.SqlTask);

                TaskMetadata metadata = new TaskMetadata();
                metadata.TaskOperation = adapter;
                metadata.TaskExecutionMode = parameters.TaskExecutionMode;
                metadata.ServerName = parameters.TargetServerName;
                metadata.DatabaseName = parameters.TargetDatabaseName;
                metadata.Name = SR.GenerateScriptTaskName;

                SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = coreOp.ErrorMessage
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to generate schema compare script. Error: " + e);
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request for schema compare publish database changes
        /// </summary>
        public async Task HandleSchemaComparePublishDatabaseChangesRequest(SchemaComparePublishDatabaseChangesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaComparePublishDatabaseChangesOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                // ServiceLayer SchemaComparePublishDatabaseChangesParams extends CoreContracts version — pass directly
                coreOp = new CoreOps.SchemaComparePublishDatabaseChangesOperation(parameters, compareResult);

                var adapter = new SchemaCompareTaskAdapter(
                    execute: () => coreOp.Execute(),
                    cancel: () => coreOp.Cancel(),
                    getError: () => coreOp.ErrorMessage
                );

                TaskMetadata metadata = new TaskMetadata
                {
                    TaskOperation = adapter,
                    ServerName = parameters.TargetServerName,
                    DatabaseName = parameters.TargetDatabaseName,
                    Name = SR.PublishChangesTaskName
                };

                SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = coreOp.ErrorMessage
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to publish schema compare database changes. Error: " + e);
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request for schema compare publish project changes
        /// </summary>
        public async Task HandleSchemaComparePublishProjectChangesRequest(SchemaComparePublishProjectChangesParams parameters, RequestContext<SchemaComparePublishProjectResult> requestContext)
        {
            CoreOps.SchemaComparePublishProjectChangesOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                // ServiceLayer SchemaComparePublishProjectChangesParams extends CoreContracts version — pass directly
                coreOp = new CoreOps.SchemaComparePublishProjectChangesOperation(parameters, compareResult);

                var adapter = new SchemaCompareTaskAdapter(
                    execute: () => coreOp.Execute(),
                    cancel: () => coreOp.Cancel(),
                    getError: () => coreOp.ErrorMessage
                );

                TaskMetadata metadata = new TaskMetadata
                {
                    TaskOperation = adapter,
                    TargetLocation = parameters.TargetProjectPath,
                    Name = SR.PublishChangesTaskName
                };

                SqlTask sqlTask = SqlTaskManagerInstance.CreateTask<SqlTask>(metadata);
                await sqlTask.RunAsync();

                await requestContext.SendResult(new SchemaComparePublishProjectResult()
                {
                    ChangedFiles = coreOp.PublishResult.ChangedFiles,
                    AddedFiles = coreOp.PublishResult.AddedFiles,
                    DeletedFiles = coreOp.PublishResult.DeletedFiles,
                    Success = true,
                    ErrorMessage = coreOp.ErrorMessage
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to publish schema compare project changes. Error: " + e);
                await requestContext.SendResult(new SchemaComparePublishProjectResult()
                {
                    ChangedFiles = Array.Empty<string>(),
                    AddedFiles = Array.Empty<string>(),
                    DeletedFiles = Array.Empty<string>(),
                    Success = false,
                    ErrorMessage = coreOp?.ErrorMessage ?? e.Message
                });
            }
        }

        /// <summary>
        /// Handles request for include/exclude node in Schema compare result
        /// </summary>
        public async Task HandleSchemaCompareIncludeExcludeNodeRequest(SchemaCompareNodeParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeNodeOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                // ServiceLayer SchemaCompareNodeParams extends CoreContracts version — pass directly
                operation = new CoreOps.SchemaCompareIncludeExcludeNodeOperation(parameters, compareResult);
                operation.Execute();

                if (operation.Success)
                {
                    schemaCompareResults.Value[parameters.OperationId] = operation.ComparisonResult;
                }

                await requestContext.SendResult(new SchemaCompareIncludeExcludeResult()
                {
                    Success = operation.Success,
                    ErrorMessage = operation.ErrorMessage,
                    AffectedDependencies = operation.AffectedDependencies,
                    BlockingDependencies = operation.BlockingDependencies
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to select compare schema result node. Error: " + e);
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request for include/exclude all nodes in Schema compare result
        /// </summary>
        public async Task HandleSchemaCompareIncludeExcludeAllNodesRequest(SchemaCompareIncludeExcludeAllNodesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeAllNodesOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                // ServiceLayer SchemaCompareIncludeExcludeAllNodesParams extends CoreContracts version — pass directly
                operation = new CoreOps.SchemaCompareIncludeExcludeAllNodesOperation(parameters, compareResult);
                operation.Execute();

                if (operation.Success)
                {
                    schemaCompareResults.Value[parameters.OperationId] = operation.ComparisonResult;
                }

                await requestContext.SendResult(new SchemaCompareIncludeExcludeAllNodesResult()
                {
                    Success = operation.Success,
                    ErrorMessage = operation.ErrorMessage,
                    AllIncludedOrExcludedDifferences = operation.AllIncludedOrExcludedDifferences
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to select compare schema result node. Error: " + e);
                await requestContext.SendResult(new SchemaCompareIncludeExcludeAllNodesResult()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles schema compare open SCMP request
        /// </summary>
        public async Task HandleSchemaCompareOpenScmpRequest(SchemaCompareOpenScmpParams parameters, RequestContext<SchemaCompareOpenScmpResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOpenScmpOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    // SchemaCompareOpenScmpParams is identical in both namespaces (only FilePath) — use core type directly
                    var coreParams = new CoreContracts.SchemaCompareOpenScmpParams
                    {
                        FilePath = parameters.FilePath
                    };

                    operation = new CoreOps.SchemaCompareOpenScmpOperation(coreParams, connectionProvider);
                    operation.Execute();

                    // The core result type IS the ServiceLayer result type (SchemaCompareOpenScmpResult
                    // already uses CoreContracts types for its properties), so send directly.
                    await requestContext.SendResult(operation.Result);
                }
                catch (Exception e)
                {
                    await requestContext.SendResult(new SchemaCompareOpenScmpResult()
                    {
                        Success = false,
                        ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                    });
                }
            });
        }

        /// <summary>
        /// Handles schema compare save SCMP request
        /// </summary>
        public Task HandleSchemaCompareSaveScmpRequest(SchemaCompareSaveScmpParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareSaveScmpOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    // ServiceLayer SchemaCompareSaveScmpParams extends CoreContracts version — pass directly
                    operation = new CoreOps.SchemaCompareSaveScmpOperation(parameters, connectionProvider);
                    operation.Execute();

                    await requestContext.SendResult(new ResultStatus()
                    {
                        Success = true,
                        ErrorMessage = operation.ErrorMessage,
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save scmp file. Error: " + e);
                    await requestContext.SendResult(new ResultStatus()
                    {
                        Success = false,
                        ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                    });
                }
            });
            return Task.CompletedTask;
        }

        private SqlTaskManager SqlTaskManagerInstance
        {
            get
            {
                sqlTaskManagerInstance ??= SqlTaskManager.Instance;
                return sqlTaskManagerInstance;
            }
            set
            {
                sqlTaskManagerInstance = value;
            }
        }

        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                connectionService ??= ConnectionService.Instance;
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }
    }
}