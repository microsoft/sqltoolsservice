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
            serviceHost.SetRequestHandler(DacFxExportRequest.Type, this.HandleExportRequest);
            serviceHost.SetRequestHandler(DacFxImportRequest.Type, this.HandleImportRequest);
            serviceHost.SetRequestHandler(DacFxExtractRequest.Type, this.HandleExtractRequest);
            serviceHost.SetRequestHandler(DacFxDeployRequest.Type, this.HandleDeployRequest);
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleExportRequest(DacFxExportParams parameters, RequestContext<DacFxExportResult> requestContext)
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
                    DacFxExportOperation operation = new DacFxExportOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Export bacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name since connection passed was to master
                    metadata.DatabaseName = parameters.DatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                //else
                //{
                //    response.Result = false;
                //}

                    await requestContext.SendResult(new DacFxExportResult()
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
        public async Task HandleImportRequest(DacFxImportParams parameters, RequestContext<DacFxImportResult> requestContext)
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
                    DacFxImportOperation operation = new DacFxImportOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Import bacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name so that it shows imported database's name rather than master
                    metadata.DatabaseName = parameters.TargetDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new DacFxImportResult()
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
        public async Task HandleExtractRequest(DacFxExtractParams parameters, RequestContext<DacFxExtractResult> requestContext)
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
                    DacFxExtractOperation operation = new DacFxExtractOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Extract dacpac", operation, ConnectionServiceInstance);
                    // put appropriate database name since connection passed was to master
                    metadata.DatabaseName = parameters.DatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new DacFxExtractResult()
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
        public async Task HandleDeployRequest(DacFxDeployParams parameters, RequestContext<DacFxDeployResult> requestContext)
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
                    DacFxDeployOperation operation = new DacFxDeployOperation(parameters, sqlConn);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(parameters, "Deploy dacpac", operation, ConnectionServiceInstance);

                    // put appropriate database name so that it shows deployed database's name rather than master
                    metadata.DatabaseName = parameters.TargetDatabaseName;

                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                    await requestContext.SendResult(new DacFxDeployResult()
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
