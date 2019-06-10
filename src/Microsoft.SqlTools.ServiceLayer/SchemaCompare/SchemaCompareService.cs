//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCopmare
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
            serviceHost.SetRequestHandler(SchemaCompareRequest.Type, this.HandleSchemaCompareRequest);
            serviceHost.SetRequestHandler(SchemaCompareCancellationRequest.Type, this.HandleSchemaCompareCancelRequest);
            serviceHost.SetRequestHandler(SchemaCompareGenerateScriptRequest.Type, this.HandleSchemaCompareGenerateScriptRequest);
            serviceHost.SetRequestHandler(SchemaComparePublishChangesRequest.Type, this.HandleSchemaComparePublishChangesRequest);
            serviceHost.SetRequestHandler(SchemaCompareIncludeExcludeNodeRequest.Type, this.HandleSchemaCompareIncludeExcludeNodeRequest);
            serviceHost.SetRequestHandler(SchemaCompareGetDefaultOptionsRequest.Type, this.HandleSchemaCompareGetDefaultOptionsRequest);
        }

        /// <summary>
        /// Handles schema compare request
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareRequest(SchemaCompareParams parameters, RequestContext<SchemaCompareResult> requestContext)
        {
            try
            {
                ConnectionInfo sourceConnInfo;
                ConnectionInfo targetConnInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.SourceEndpointInfo.OwnerUri,
                        out sourceConnInfo);
                ConnectionServiceInstance.TryFindConnection(
                    parameters.TargetEndpointInfo.OwnerUri,
                    out targetConnInfo);

                Task schemaCompareTask = Task.Run(async () =>
                {
                    SchemaCompareOperation operation = null;

                    try
                    {
                        operation = new SchemaCompareOperation(parameters, sourceConnInfo, targetConnInfo);
                        currentComparisonCancellationAction.Value[operation.OperationId] = operation.Cancel;
                        operation.Execute(parameters.TaskExecutionMode);

                        // add result to dictionary of results
                        schemaCompareResults.Value[operation.OperationId] = operation.ComparisonResult;

                        await requestContext.SendResult(new SchemaCompareResult()
                        {
                            OperationId = operation.OperationId,
                            Success = true,
                            ErrorMessage = operation.ErrorMessage,
                            AreEqual = operation.ComparisonResult.IsEqual,
                            Differences = operation.Differences
                        });
                    }
                    catch(Exception e)
                    {
                        await requestContext.SendResult(new SchemaCompareResult()
                        {
                            OperationId = operation != null ? operation.OperationId : null,
                            Success = false,
                            ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                        });
                    }
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles schema compare cancel request
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareCancelRequest(SchemaCompareCancelParams parameters, RequestContext<ResultStatus> requestContext)
        {
            try
            {
                Action cancelAction = null;
                if (currentComparisonCancellationAction.Value.TryRemove(parameters.OperationId, out cancelAction))
                {
                    if(cancelAction != null)
                    {
                        cancelAction.Invoke();
                        await requestContext.SendResult(new ResultStatus()
                        {
                            Success = true,
                            ErrorMessage = null
                        });
                    }
                }
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = SR.SchemaCompareSessionNotFound
                });

            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request for schema compare generate deploy script
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaCompareGenerateScriptRequest(SchemaCompareGenerateScriptParams parameters, RequestContext<ResultStatus> requestContext)
        {
            SchemaCompareGenerateScriptOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                operation = new SchemaCompareGenerateScriptOperation(parameters, compareResult);
                SqlTask sqlTask = null;
                TaskMetadata metadata = new TaskMetadata();
                metadata.TaskOperation = operation;
                metadata.TaskExecutionMode = parameters.TaskExecutionMode;
                metadata.ServerName = parameters.TargetServerName;
                metadata.DatabaseName = parameters.TargetDatabaseName;
                metadata.Name = SR.GenerateScriptTaskName;

                sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = operation.ErrorMessage
                });
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        /// <summary>
        /// Handles request for schema compare publish changes script
        /// </summary>
        /// <returns></returns>
        public async Task HandleSchemaComparePublishChangesRequest(SchemaComparePublishChangesParams parameters, RequestContext<ResultStatus> requestContext)
        {
            SchemaComparePublishChangesOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                operation = new SchemaComparePublishChangesOperation(parameters, compareResult);
                SqlTask sqlTask = null;
                TaskMetadata metadata = new TaskMetadata();
                metadata.TaskOperation = operation;
                metadata.ServerName = parameters.TargetServerName;
                metadata.DatabaseName = parameters.TargetDatabaseName;
                metadata.Name = SR.PublishChangesTaskName;

                sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = operation.ErrorMessage
                });
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        public async Task HandleSchemaCompareIncludeExcludeNodeRequest(SchemaCompareNodeParams parameters, RequestContext<ResultStatus> requestContext)
        {
            SchemaCompareIncludeExcludeNodeOperation operation = null;
            try
            {
                SchemaComparisonResult compareResult = schemaCompareResults.Value[parameters.OperationId];
                operation = new SchemaCompareIncludeExcludeNodeOperation(parameters, compareResult);

                operation.Execute(parameters.TaskExecutionMode);
                
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = operation.ErrorMessage
                });
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new ResultStatus()
                {
                    Success = false,
                    ErrorMessage = operation == null ? e.Message : operation.ErrorMessage,
                });
            }
        }

        public async Task HandleSchemaCompareGetDefaultOptionsRequest(SchemaCompareGetOptionsParams parameters, RequestContext<SchemaCompareOptionsResult> requestContext)
        {
            try
            {
                // this does not need to be an async operation since this only creates and resturn the default opbject
                DeploymentOptions options = new DeploymentOptions();

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

        private SqlTaskManager SqlTaskManagerInstance
        {
            get
            {
                if (sqlTaskManagerInstance == null)
                {
                    sqlTaskManagerInstance = SqlTaskManager.Instance;
                }
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
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }
    }
}
