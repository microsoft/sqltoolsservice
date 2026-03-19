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
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Main class for SchemaCompare service.
    /// Uses host-agnostic operations from SqlCore with VSCode/ADS adapters.
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
            serviceHost.SetRequestHandler(SchemaCompareGetDefaultOptionsRequest.Type, this.HandleSchemaCompareGetDefaultOptionsRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareOpenScmpRequest.Type, this.HandleSchemaCompareOpenScmpRequest, true);
            serviceHost.SetRequestHandler(SchemaCompareSaveScmpRequest.Type, this.HandleSchemaCompareSaveScmpRequest, true);
        }

        #region Type mapping helpers

        /// <summary>
        /// Maps ServiceLayer SchemaCompareEndpointInfo (wire format) to SqlCore SchemaCompareEndpointInfo.
        /// </summary>
        private static CoreOps.Contracts.SchemaCompareEndpointInfo ToCore(Contracts.SchemaCompareEndpointInfo svc)
        {
            if (svc == null) return null;
            return new CoreOps.Contracts.SchemaCompareEndpointInfo
            {
                EndpointType = svc.EndpointType,
                ProjectFilePath = svc.ProjectFilePath,
                TargetScripts = svc.TargetScripts,
                DataSchemaProvider = svc.DataSchemaProvider,
                PackageFilePath = svc.PackageFilePath,
                DatabaseName = svc.DatabaseName,
                OwnerUri = svc.OwnerUri,
                ExtractTarget = svc.ExtractTarget
            };
        }

        /// <summary>
        /// Maps SqlCore SchemaCompareEndpointInfo back to ServiceLayer wire format.
        /// Constructs ConnectionDetails from the parsed connection string properties.
        /// </summary>
        private static Contracts.SchemaCompareEndpointInfo FromCore(CoreOps.Contracts.SchemaCompareEndpointInfo core)
        {
            if (core == null) return null;
            var result = new Contracts.SchemaCompareEndpointInfo
            {
                EndpointType = core.EndpointType,
                ProjectFilePath = core.ProjectFilePath,
                TargetScripts = core.TargetScripts,
                DataSchemaProvider = core.DataSchemaProvider,
                PackageFilePath = core.PackageFilePath,
                DatabaseName = core.DatabaseName,
                OwnerUri = core.OwnerUri,
                ExtractTarget = core.ExtractTarget
            };

            // Construct ConnectionDetails from the core endpoint info fields
            if (!string.IsNullOrEmpty(core.ConnectionString) || !string.IsNullOrEmpty(core.ServerName))
            {
                result.ConnectionDetails = new ConnectionDetails
                {
                    ServerName = core.ServerName,
                    UserName = core.UserName,
                    DatabaseName = core.DatabaseName,
                    ConnectionString = core.ConnectionString
                };
            }

            return result;
        }

        /// <summary>
        /// Maps ServiceLayer SchemaCompareParams to SqlCore SchemaCompareParams.
        /// </summary>
        private static CoreOps.Contracts.SchemaCompareParams ToCoreParams(Contracts.SchemaCompareParams svc)
        {
            return new CoreOps.Contracts.SchemaCompareParams
            {
                OperationId = svc.OperationId,
                SourceEndpointInfo = ToCore(svc.SourceEndpointInfo),
                TargetEndpointInfo = ToCore(svc.TargetEndpointInfo),
                DeploymentOptions = svc.DeploymentOptions
            };
        }

        #endregion

        /// <summary>
        /// Handles schema compare request
        /// </summary>
        public Task HandleSchemaCompareRequest(Contracts.SchemaCompareParams parameters, RequestContext<SchemaCompareResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOperation operation = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    var coreParams = ToCoreParams(parameters);
                    operation = new CoreOps.SchemaCompareOperation(coreParams, connectionProvider);
                    currentComparisonCancellationAction.Value[operation.OperationId] = operation.Cancel;
                    operation.Execute();

                    // add result to dictionary of results
                    schemaCompareResults.Value[operation.OperationId] = operation.ComparisonResult;

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
                var coreParams = new CoreOps.Contracts.SchemaCompareGenerateScriptParams
                {
                    OperationId = parameters.OperationId,
                    TargetServerName = parameters.TargetServerName,
                    TargetDatabaseName = parameters.TargetDatabaseName
                };

                // Create adapter for SqlTask bridge, then wire up the script handler
                SchemaCompareTaskAdapter adapter = null;
                var scriptHandler = new VsCodeScriptHandler(() => adapter?.SqlTask);
                coreOp = new CoreOps.SchemaCompareGenerateScriptOperation(coreParams, compareResult, scriptHandler);
                adapter = new SchemaCompareTaskAdapter(
                    () => coreOp.Execute(),
                    () => coreOp.Cancel(),
                    () => coreOp.ErrorMessage
                );

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
                var coreParams = new CoreOps.Contracts.SchemaComparePublishDatabaseChangesParams
                {
                    OperationId = parameters.OperationId,
                    TargetServerName = parameters.TargetServerName,
                    TargetDatabaseName = parameters.TargetDatabaseName
                };
                coreOp = new CoreOps.SchemaComparePublishDatabaseChangesOperation(coreParams, compareResult);
                var adapter = new SchemaCompareTaskAdapter(
                    () => coreOp.Execute(),
                    () => coreOp.Cancel(),
                    () => coreOp.ErrorMessage
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
                var coreParams = new CoreOps.Contracts.SchemaComparePublishProjectChangesParams
                {
                    OperationId = parameters.OperationId,
                    TargetProjectPath = parameters.TargetProjectPath,
                    TargetFolderStructure = parameters.TargetFolderStructure
                };
                coreOp = new CoreOps.SchemaComparePublishProjectChangesOperation(coreParams, compareResult);
                var adapter = new SchemaCompareTaskAdapter(
                    () => coreOp.Execute(),
                    () => coreOp.Cancel(),
                    () => coreOp.ErrorMessage
                );

                TaskMetadata metadata = new()
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
        public async Task HandleSchemaCompareIncludeExcludeNodeRequest(Contracts.SchemaCompareNodeParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeNodeOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                var coreParams = new CoreOps.Contracts.SchemaCompareNodeParams
                {
                    OperationId = parameters.OperationId,
                    DiffEntry = parameters.DiffEntry,
                    IncludeRequest = parameters.IncludeRequest
                };
                coreOp = new CoreOps.SchemaCompareIncludeExcludeNodeOperation(coreParams, compareResult);

                coreOp.Execute();

                // update the comparison result if the include/exclude was successful
                if (coreOp.Success)
                {
                    schemaCompareResults.Value[parameters.OperationId] = coreOp.ComparisonResult;
                }

                await requestContext.SendResult(new SchemaCompareIncludeExcludeResult()
                {
                    Success = coreOp.Success,
                    ErrorMessage = coreOp.ErrorMessage,
                    AffectedDependencies = coreOp.AffectedDependencies,
                    BlockingDependencies = coreOp.BlockingDependencies
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to select compare schema result node. Error: " + e);
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request for include/exclude all nodes in Schema compare result
        /// </summary>
        public async Task HandleSchemaCompareIncludeExcludeAllNodesRequest(Contracts.SchemaCompareIncludeExcludeAllNodesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            CoreOps.SchemaCompareIncludeExcludeAllNodesOperation coreOp = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                var coreParams = new CoreOps.Contracts.SchemaCompareIncludeExcludeAllNodesParams
                {
                    OperationId = parameters.OperationId,
                    IncludeRequest = parameters.IncludeRequest
                };
                coreOp = new CoreOps.SchemaCompareIncludeExcludeAllNodesOperation(coreParams, compareResult);

                coreOp.Execute();

                // update the comparison result if the include/exclude was successful
                if (coreOp.Success)
                {
                    schemaCompareResults.Value[parameters.OperationId] = coreOp.ComparisonResult;
                }

                await requestContext.SendResult(new SchemaCompareIncludeExcludeAllNodesResult()
                {
                    Success = coreOp.Success,
                    ErrorMessage = coreOp.ErrorMessage,
                    AllIncludedOrExcludedDifferences = coreOp.AllIncludedOrExcludedDifferences
                });
            }
            catch (Exception e)
            {
                Logger.Error("Failed to select compare schema result node. Error: " + e);
                await requestContext.SendResult(new SchemaCompareIncludeExcludeAllNodesResult()
                {
                    Success = false,
                    ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request to create default deployment options as per DacFx
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareGetDefaultOptionsRequest(SchemaCompareGetOptionsParams parameters, RequestContext<SchemaCompareOptionsResult> requestContext)
        {
            try
            {
                // this does not need to be an async operation since this only creates and returns the default object
                CoreOps.Contracts.DeploymentOptions options = CoreOps.Contracts.DeploymentOptions.GetDefaultSchemaCompareOptions();

                await requestContext.SendResult(new SchemaCompareOptionsResult()
                {
                    DefaultDeploymentOptions = options,
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new SchemaCompareOptionsResult()
                {
                    DefaultDeploymentOptions = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }

        /// <summary>
        /// Handles schema compare open SCMP request
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareOpenScmpRequest(SchemaCompareOpenScmpParams parameters, RequestContext<SchemaCompareOpenScmpResult> requestContext)
        {
            CurrentSchemaCompareTask = Task.Run(async () =>
            {
                CoreOps.SchemaCompareOpenScmpOperation coreOp = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    var coreParams = new CoreOps.Contracts.SchemaCompareOpenScmpParams
                    {
                        FilePath = parameters.FilePath
                    };
                    coreOp = new CoreOps.SchemaCompareOpenScmpOperation(coreParams, connectionProvider);
                    coreOp.Execute();

                    // Convert SqlCore result to ServiceLayer wire-format result
                    var coreResult = coreOp.Result;
                    await requestContext.SendResult(new SchemaCompareOpenScmpResult()
                    {
                        Success = coreResult.Success,
                        ErrorMessage = coreResult.ErrorMessage,
                        DeploymentOptions = coreResult.DeploymentOptions,
                        SourceEndpointInfo = FromCore(coreResult.SourceEndpointInfo),
                        TargetEndpointInfo = FromCore(coreResult.TargetEndpointInfo),
                        OriginalTargetName = coreResult.OriginalTargetName,
                        OriginalTargetConnectionString = coreResult.OriginalTargetConnectionString,
                        ExcludedSourceElements = coreResult.ExcludedSourceElements,
                        ExcludedTargetElements = coreResult.ExcludedTargetElements
                    });
                }
                catch (Exception e)
                {
                    await requestContext.SendResult(new SchemaCompareOpenScmpResult()
                    {
                        Success = false,
                        ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
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
                CoreOps.SchemaCompareSaveScmpOperation coreOp = null;

                try
                {
                    var connectionProvider = new VsCodeConnectionProvider(ConnectionServiceInstance);
                    var coreParams = new CoreOps.Contracts.SchemaCompareSaveScmpParams
                    {
                        OperationId = parameters.OperationId,
                        SourceEndpointInfo = ToCore(parameters.SourceEndpointInfo),
                        TargetEndpointInfo = ToCore(parameters.TargetEndpointInfo),
                        DeploymentOptions = parameters.DeploymentOptions,
                        ScmpFilePath = parameters.ScmpFilePath,
                        ExcludedSourceObjects = parameters.ExcludedSourceObjects,
                        ExcludedTargetObjects = parameters.ExcludedTargetObjects
                    };
                    coreOp = new CoreOps.SchemaCompareSaveScmpOperation(coreParams, connectionProvider);
                    coreOp.Execute();

                    await requestContext.SendResult(new ResultStatus()
                    {
                        Success = true,
                        ErrorMessage = coreOp.ErrorMessage,
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save scmp file. Error: " + e);
                    await requestContext.SendResult(new SchemaCompareResult()
                    {
                        OperationId = coreOp != null ? coreOp.OperationId : null,
                        Success = false,
                        ErrorMessage = coreOp == null ? e.Message : coreOp.ErrorMessage,
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
