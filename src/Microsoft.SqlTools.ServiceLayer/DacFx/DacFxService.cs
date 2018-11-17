//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
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
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleExportRequest(ExportParams parameters, RequestContext<ExportResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Export");
                    ExportOperation operation = new ExportOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Export bacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name since connection passed was to master
                    metadata.DatabaseName = parameters.SourceDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new ExportResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = ""
                    });
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
        public async Task HandleImportRequest(ImportParams parameters, RequestContext<ImportResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Import");
                    ImportOperation operation = new ImportOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Import bacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name so that it shows imported database's name rather than master
                    metadata.DatabaseName = parameters.TargetDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new ImportResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = ""
                    });
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
        public async Task HandleExtractRequest(ExtractParams parameters, RequestContext<ExtractResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Extract");
                    ExtractOperation operation = new ExtractOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Extract dacpac", operation, ConnectionServiceInstance);
                    // put appropriate database name since connection passed was to master
                    metadata.DatabaseName = parameters.SourceDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new ExtractResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = ""
                    });
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
        public async Task HandleDeployRequest(DeployParams parameters, RequestContext<DeployResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                if (connInfo != null)
                {
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Deploy");
                    DeployOperation operation = new DeployOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Deploy dacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name so that it shows deployed database's name rather than master
                    metadata.DatabaseName = parameters.TargetDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new DeployResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = ""
                    });
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
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
