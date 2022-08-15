//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlServer.Dac.Model;
using DacTableDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.TableDesigner;

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
        /// <see cref="ConcurrentDictionary{String, TSqlModel}"/> that maps project uri to model
        /// </summary>
        public Lazy<ConcurrentDictionary<string, TSqlModel>> projectModels =
            new Lazy<ConcurrentDictionary<string, TSqlModel>>(() => new ConcurrentDictionary<string, TSqlModel>());

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
            serviceHost.SetRequestHandler(GetOptionsFromProfileRequest.Type, this.HandleGetOptionsFromProfileRequest);
            serviceHost.SetRequestHandler(ValidateStreamingJobRequest.Type, this.HandleValidateStreamingJobRequest);
            serviceHost.SetRequestHandler(GetDefaultPublishOptionsRequest.Type, this.HandleGetDefaultPublishOptionsRequest);
            serviceHost.SetRequestHandler(ParseTSqlScriptRequest.Type, this.HandleParseTSqlScriptRequest);
            serviceHost.SetRequestHandler(GenerateTSqlModelRequest.Type, this.HandleGenerateTSqlModelRequest);
            serviceHost.SetRequestHandler(GetObjectsFromTSqlModelRequest.Type, this.HandleGetObjectsFromTSqlModelRequest);
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, DacFxOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Handles request to export a bacpac
        /// </summary>
        /// <returns></returns>
        public Task HandleExportRequest(ExportParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                ExportOperation operation = new ExportOperation(parameters, connInfo);
                ExecuteOperation(operation, parameters, SR.ExportBacpacTaskName, requestContext);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles request to import a bacpac
        /// </summary>
        /// <returns></returns>
        public async Task HandleImportRequest(ImportParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                ImportOperation operation = new ImportOperation(parameters, connInfo);
                ExecuteOperation(operation, parameters, SR.ImportBacpacTaskName, requestContext);
            }
        }

        /// <summary>
        /// Handles request to extract a dacpac
        /// </summary>
        /// <returns></returns>
        public Task HandleExtractRequest(ExtractParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                // Set connection details database name to ensure the connection string gets created correctly for DW(extract doesn't work if connection is to master)
                connInfo.ConnectionDetails.DatabaseName = parameters.DatabaseName;
                ExtractOperation operation = new ExtractOperation(parameters, connInfo);
                string taskName = parameters.ExtractTarget == DacExtractTarget.DacPac ? SR.ExtractDacpacTaskName : SR.ProjectExtractTaskName;
                ExecuteOperation(operation, parameters, taskName, requestContext);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles request to deploy a dacpac
        /// </summary>
        /// <returns></returns>
        public Task HandleDeployRequest(DeployParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                DeployOperation operation = new DeployOperation(parameters, connInfo);
                ExecuteOperation(operation, parameters, SR.DeployDacpacTaskName, requestContext);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles request to generate deploy script
        /// </summary>
        /// <returns></returns>
        public async Task HandleGenerateDeployScriptRequest(GenerateDeployScriptParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
            if (connInfo != null)
            {
                GenerateDeployScriptOperation operation = new GenerateDeployScriptOperation(parameters, connInfo);
                SqlTask sqlTask = null;
                TaskMetadata metadata = new TaskMetadata();
                metadata.TaskOperation = operation;
                metadata.TaskExecutionMode = parameters.TaskExecutionMode;
                metadata.ServerName = connInfo.ConnectionDetails.ServerName;
                metadata.DatabaseName = parameters.DatabaseName;
                metadata.Name = SR.GenerateScriptTaskName;

                sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);

                await requestContext.SendResult(new DacFxResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
        }

        /// <summary>
        /// Handles request to generate deploy plan
        /// </summary>
        /// <returns></returns>
        public async Task HandleGenerateDeployPlanRequest(GenerateDeployPlanParams parameters, RequestContext<GenerateDeployPlanRequestResult> requestContext)
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

        /// <summary>
        /// Handles request to get the options from a publish profile
        /// </summary>
        /// <returns></returns>
        public async Task HandleGetOptionsFromProfileRequest(GetOptionsFromProfileParams parameters, RequestContext<DacFxOptionsResult> requestContext)
        {
            DeploymentOptions options = null;
            if (parameters.ProfilePath != null)
            {
                DacProfile profile = DacProfile.Load(parameters.ProfilePath);
                if (profile.DeployOptions != null)
                {
                    options = DeploymentOptions.GetDefaultPublishOptions();
                    await options.InitializeFromProfile(profile.DeployOptions, parameters.ProfilePath);
                }
            }

            await requestContext.SendResult(new DacFxOptionsResult()
            {
                DeploymentOptions = options,
                Success = true,
                ErrorMessage = string.Empty,
            });
        }

        /// <summary>
        /// Handles request to validate an ASA streaming job
        /// </summary>
        /// <returns></returns>
        public async Task HandleValidateStreamingJobRequest(ValidateStreamingJobParams parameters, RequestContext<ValidateStreamingJobResult> requestContext)
        {
            ValidateStreamingJobOperation operation = new ValidateStreamingJobOperation(parameters);
            ValidateStreamingJobResult result = operation.ValidateQuery();

            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Handles request to create default publish options for DacFx
        /// </summary>
        /// <returns></returns>
        public async Task HandleGetDefaultPublishOptionsRequest(GetDefaultPublishOptionsParams parameters, RequestContext<DacFxOptionsResult> requestContext)
        {
            try
            {
                // this does not need to be an async operation since this only creates and returns the default object
                DeploymentOptions options = DeploymentOptions.GetDefaultPublishOptions();

                await requestContext.SendResult(new DacFxOptionsResult()
                {
                    DeploymentOptions = options,
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new DacFxOptionsResult()
                {
                    DeploymentOptions = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }

        public async Task HandleParseTSqlScriptRequest(ParseTSqlScriptRequestParams requestParams, RequestContext<ParseTSqlScriptResult> requestContext)
        {
            var script = System.IO.File.ReadAllText(requestParams.FilePath);
            await requestContext.SendResult(new ParseTSqlScriptResult()
            {
                ContainsCreateTableStatement = DacTableDesigner.ScriptContainsCreateTableStatements(script, requestParams.DatabaseSchemaProvider)
            });
        }

        public async Task HandleGenerateTSqlModelRequest(GenerateTSqlModelParams requestParams, RequestContext<bool> requestContext)
        {
            try
            {
                GenerateTSqlModelOperation operation = new GenerateTSqlModelOperation(requestParams);
                TSqlModel model = operation.GenerateTSqlModel();

                projectModels.Value[operation.Parameters.ProjectUri] = model;
                await requestContext.SendResult(true);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleGetObjectsFromTSqlModelRequest(GetObjectsFromTSqlModelParams requestParams, RequestContext<TSqlObjectInfo[]> requestContext)
        {
            try
            {
                var model = projectModels.Value[requestParams.ProjectUri];
                GetObjectsFromTSqlModelOperation operation = new GetObjectsFromTSqlModelOperation(requestParams, model);
                TSqlObjectInfo[] objectInfos = operation.GetObjectsFromTSqlModel();
                await requestContext.SendResult(objectInfos);
                
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private void ExecuteOperation(DacFxOperation operation, DacFxParams parameters, string taskName, RequestContext<DacFxResult> requestContext)
        {
            Task.Run(async () =>
            {
                try
                {
                    // show file location for export and extract operations 
                    string targetLocation = (operation is ExportOperation || operation is ExtractOperation) ? parameters.PackageFilePath : null;
                    TaskMetadata metadata = TaskMetadata.Create(parameters, taskName, operation, ConnectionServiceInstance, targetLocation);

                    // put appropriate database name since connection passed was to master
                    metadata.DatabaseName = parameters.DatabaseName;
                    SqlTask sqlTask = SqlTaskManagerInstance.CreateTask<SqlTask>(metadata);

                    await sqlTask.RunAsync();
                    await requestContext.SendResult(new DacFxResult()
                    {
                        OperationId = operation.OperationId,
                        Success = sqlTask.TaskStatus == SqlTaskStatus.Succeeded,
                        ErrorMessage = string.Empty
                    });
                }
                catch (Exception e)
                {
                    await requestContext.SendResult(new DacFxResult()
                    {
                        OperationId = operation.OperationId,
                        Success = false,
                        ErrorMessage = e.Message
                    });
                }
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
        internal void PerformOperation(DacFxOperation operation, TaskExecutionMode taskExecutionMode)
        {
            operation.Execute(taskExecutionMode);
        }
    }
}
