//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        private Lazy<ConcurrentDictionary<string, SchemaComparison>> schemaComparisons =
            new Lazy<ConcurrentDictionary<string, SchemaComparison>>(() => new ConcurrentDictionary<string, SchemaComparison>());
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
        /// <returns></returns>
        public Task HandleSchemaCompareRequest(SchemaCompareParams parameters, RequestContext<SchemaCompareResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    operation = new CoreOps.SchemaCompareOperation(parameters, connectionProvider);
                    currentComparisonCancellationAction.Value[operation.OperationId] = operation.Cancel;
                    operation.Execute();

                    // add result to dictionary of results
                    schemaCompareResults.Value[operation.OperationId] = operation.ComparisonResult;
                    schemaComparisons.Value[operation.OperationId] = operation.Comparison;

                    await requestContext.SendResult(new SchemaCompareResult()
                    {
                        OperationId = operation.OperationId,
                        Success = operation.ComparisonResult.IsValid,
                        ErrorMessage = operation.ErrorMessage,
                        AreEqual = operation.ComparisonResult.IsEqual,
                        Differences = operation.Differences
                    });

                    // clean up cancellation action now that the operation is complete (using try remove to avoid exception)
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
        /// <returns></returns>
        public async Task HandleSchemaCompareCancelRequest(CoreContracts.SchemaCompareCancelParams parameters, RequestContext<ResultStatus> requestContext)
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
        /// <returns></returns>
        public async Task HandleSchemaCompareGenerateScriptRequest(SchemaCompareGenerateScriptParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareGenerateScriptOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                coreOp = new CoreOps.SchemaCompareGenerateScriptOperation(parameters, compareResult);

                var adapter = new SchemaCompareTaskAdapter(
                    execute: () => coreOp.Execute(),
                    cancel: () => coreOp.Cancel(),
                    getError: () => coreOp.ErrorMessage
                );

                // Wire up script handler now that adapter is available
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
        /// Handles request for schema compare publish database changes script
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaComparePublishDatabaseChangesRequest(SchemaComparePublishDatabaseChangesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaComparePublishDatabaseChangesOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                schemaComparisons.Value.TryGetValue(parameters.OperationId, out SchemaComparison schemaComparison);

                coreOp = new CoreOps.SchemaComparePublishDatabaseChangesOperation(parameters, compareResult, schemaComparison);

                var adapter = new SchemaCompareTaskAdapter(
                    execute: () => coreOp.Execute(),
                    cancel: () => coreOp.Cancel(),
                    getError: () => coreOp.ErrorMessage
                );

                coreOp.ProgressChanged += (sender, e) =>
                {
                    if (adapter.SqlTask != null && e?.Status != null)
                    {
                        adapter.SqlTask.AddMessage(e.Status, SqlTaskStatus.InProgress);
                    }
                };

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
        /// <returns></returns>
        public async Task HandleSchemaComparePublishProjectChangesRequest(SchemaComparePublishProjectChangesParams parameters, RequestContext<SchemaComparePublishProjectResult> requestContext)
        {
            CoreOps.SchemaComparePublishProjectChangesOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

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
                Logger.Error("Failed to publish schema compare database changes. Error: " + e);
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
        /// Handles request for exclude incude node in Schema compare result
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareIncludeExcludeNodeRequest(SchemaCompareNodeParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeNodeOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                operation = new CoreOps.SchemaCompareIncludeExcludeNodeOperation(parameters, compareResult);
                operation.Execute();

                // update the comparison result if the include/exclude was successful
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

        // <summary>
        /// Handles request for exclude incude all nodes in Schema compare result
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareIncludeExcludeAllNodesRequest(SchemaCompareIncludeExcludeAllNodesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeAllNodesOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];

                operation = new CoreOps.SchemaCompareIncludeExcludeAllNodesOperation(parameters, compareResult);
                operation.Execute();

                // update the comparison result if the include/exclude was successful
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
        /// <returns></returns>
        public async Task HandleSchemaCompareOpenScmpRequest(CoreContracts.SchemaCompareOpenScmpParams parameters, RequestContext<SchemaCompareOpenScmpResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOpenScmpOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);

                    operation = new CoreOps.SchemaCompareOpenScmpOperation(parameters, connectionProvider);
                    operation.Execute();

                    // Map core result back to ServiceLayer format (adds ConnectionDetails for wire protocol)
                    await requestContext.SendResult(FromCoreOpenScmpResult(operation.Result));
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
        /// <returns></returns>
        public Task HandleSchemaCompareSaveScmpRequest(SchemaCompareSaveScmpParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareSaveScmpOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
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

        #region Type Mapping Helpers

        /// <summary>
        /// Maps SqlCore SchemaCompareEndpointInfo to ServiceLayer format, adding ConnectionDetails
        /// for database endpoints parsed from SCMP files.
        /// </summary>
        private static SchemaCompareEndpointInfo ToServiceLayerEndpoint(CoreContracts.SchemaCompareEndpointInfo coreEndpoint)
        {
            if (coreEndpoint == null)
            {
                return null;
            }

            var endpointInfo = new SchemaCompareEndpointInfo
            {
                EndpointType = coreEndpoint.EndpointType,
                ProjectFilePath = coreEndpoint.ProjectFilePath,
                TargetScripts = coreEndpoint.TargetScripts,
                DataSchemaProvider = coreEndpoint.DataSchemaProvider,
                PackageFilePath = coreEndpoint.PackageFilePath,
                DatabaseName = coreEndpoint.DatabaseName,
                OwnerUri = coreEndpoint.OwnerUri,
                ServerName = coreEndpoint.ServerName,
                UserName = coreEndpoint.UserName,
                ConnectionString = coreEndpoint.ConnectionString,
                ExtractTarget = coreEndpoint.ExtractTarget
            };

            // For database endpoints from SCMP files, construct ConnectionDetails from the parsed info
            if (coreEndpoint.EndpointType == CoreContracts.SchemaCompareEndpointType.Database && coreEndpoint.ConnectionString != null)
            {
                endpointInfo.ConnectionDetails = ConnectionServiceInstance.ParseConnectionString(coreEndpoint.ConnectionString);
                endpointInfo.ConnectionDetails.ConnectionString = coreEndpoint.ConnectionString;
                endpointInfo.DatabaseName = endpointInfo.ConnectionDetails.DatabaseName;
            }

            return endpointInfo;
        }

        /// <summary>
        /// Maps SqlCore SchemaCompareOpenScmpResult to ServiceLayer format
        /// </summary>
        private static SchemaCompareOpenScmpResult FromCoreOpenScmpResult(CoreContracts.SchemaCompareOpenScmpResult coreResult)
        {
            return new SchemaCompareOpenScmpResult
            {
                Success = coreResult.Success,
                ErrorMessage = coreResult.ErrorMessage,
                SourceEndpointInfo = ToServiceLayerEndpoint(coreResult.SourceEndpointInfo),
                TargetEndpointInfo = ToServiceLayerEndpoint(coreResult.TargetEndpointInfo),
                OriginalTargetName = coreResult.OriginalTargetName,
                OriginalTargetConnectionString = coreResult.OriginalTargetConnectionString,
                DeploymentOptions = coreResult.DeploymentOptions,
                ExcludedSourceElements = coreResult.ExcludedSourceElements,
                ExcludedTargetElements = coreResult.ExcludedTargetElements
            };
        }

        #endregion

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
