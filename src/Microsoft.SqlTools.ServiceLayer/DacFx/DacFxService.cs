//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    class DacFxService
    {
        private static ConnectionService connectionService = null;
        private SqlTaskManager sqlTaskManagerInstance = null;
        private static readonly Lazy<DacFxService> instance = new Lazy<DacFxService>(() => new DacFxService());
        private readonly Lazy<ConcurrentDictionary<string, DacFxOperation>> operations =
            new Lazy<ConcurrentDictionary<string, DacFxOperation>>(() => new ConcurrentDictionary<string, DacFxOperation>());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DacFxService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ExportRequest.Type, this.HandleExportRequest);
            serviceHost.SetRequestHandler(ImportRequest.Type, this.HandleImportRequest);
            serviceHost.SetRequestHandler(ExtractRequest.Type, this.HandleExtractRequest);
            serviceHost.SetRequestHandler(DeployRequest.Type, this.HandleDeployRequest);
            serviceHost.SetRequestHandler(GenerateDeployScriptRequest.Type, this.HandleGenerateDeployScriptRequest);
            serviceHost.SetRequestHandler(GenerateDeployPlanRequest.Type, this.HandleGenerateDeployPlanRequest);
            serviceHost.SetRequestHandler(SchemaCompareRequest.Type, this.HandleSchemaCompareRequest);
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleExportRequest(ExportParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    ExportOperation operation = new ExportOperation(parameters, connInfo);
                    await ExecuteOperation(operation, parameters, "Export bacpac", requestContext);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to import a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleImportRequest(ImportParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    ImportOperation operation = new ImportOperation(parameters, connInfo);
                    await ExecuteOperation(operation, parameters, "Import bacpac", requestContext);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to extract a dacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleExtractRequest(ExtractParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    ExtractOperation operation = new ExtractOperation(parameters, connInfo);
                    await ExecuteOperation(operation, parameters, "Extract dacpac", requestContext);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to deploy a dacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleDeployRequest(DeployParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    DeployOperation operation = new DeployOperation(parameters, connInfo);
                    await ExecuteOperation(operation, parameters, "Deploy dacpac", requestContext);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to generate deploy script
        /// </summary>
        /// <returns></returns>
        public async Task HandleGenerateDeployScriptRequest(GenerateDeployScriptParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    GenerateDeployScriptOperation operation = new GenerateDeployScriptOperation(parameters, connInfo);
                    SqlTask sqlTask = null;
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Generate script", operation, ConnectionServiceInstance);

                    // want to show filepath in task history instead of server and database
                    metadata.ServerName = parameters.ScriptFilePath;
                    metadata.DatabaseName = string.Empty;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new DacFxResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = string.Empty
                    });
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to generate deploy plan
        /// </summary>
        /// <returns></returns>
        public async Task HandleGenerateDeployPlanRequest(GenerateDeployPlanParams parameters, RequestContext<GenerateDeployPlanRequestResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    GenerateDeployPlanOperation operation = new GenerateDeployPlanOperation(parameters, connInfo);
                    operation.Execute(parameters.TaskExecutionMode);

                    await requestContext.SendResult(new GenerateDeployPlanRequestResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = string.Empty,
                        Report = operation.DeployReport
                    });
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
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
                        parameters.sourceEndpointInfo.OwnerUri,
                        out sourceConnInfo);
                ConnectionServiceInstance.TryFindConnection(
                    parameters.targetEndpointInfo.OwnerUri,
                    out targetConnInfo);

                SchemaCompareOperation operation = new SchemaCompareOperation(parameters, sourceConnInfo, targetConnInfo);
                operation.Execute(parameters.TaskExecutionMode);

                await requestContext.SendResult(new SchemaCompareResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = string.Empty,
                    AreEqual = operation.ComparisonResult.IsEqual,
                    Differences = operation.Differences
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private async Task ExecuteOperation(DacFxOperation operation, DacFxParams parameters, string taskName, RequestContext<DacFxResult> requestContext)
        {
            SqlTask sqlTask = null;
            TaskMetadata metadata = TaskMetadata.Create(parameters, taskName, operation, ConnectionServiceInstance);

            // put appropriate database name since connection passed was to master
            metadata.DatabaseName = parameters.DatabaseName;

            sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

            await requestContext.SendResult(new DacFxResult()
            {
                OperationId = operation.OperationId,
                Success = true,
                ErrorMessage = string.Empty
            });
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

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
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

        /// <summary>
        /// For testing purpose only
        /// </summary>
        /// <param name="operation"></param>
        internal void PerformOperation(DacFxOperation operation)
        {
            operation.Execute(TaskExecutionMode.Execute);
        }
    }
}
