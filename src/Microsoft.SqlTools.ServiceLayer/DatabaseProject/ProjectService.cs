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
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    class ProjectService
    {
        private static ConnectionService connectionService = null;
        private SqlTaskManager sqlTaskManagerInstance = null;
        private static readonly Lazy<ProjectService> instance = new Lazy<ProjectService>(() => new ProjectService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ProjectService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ProjectBuildRequest.Type, this.HandleProjectBuildRequest);
            serviceHost.SetRequestHandler(ProjectDeployRequest.Type, this.HandleProjectDeployRequest);
        }

        public async Task HandleProjectBuildRequest(ProjectBuildParams parameters, RequestContext<DacFxResult> requestContext)
        {
            try
            {
                ProjectBuildOperation operation = new ProjectBuildOperation(parameters);
                ExecuteOperation(operation, parameters, SR.ProjectBuildTaskName, requestContext);

                await requestContext.SendResult(new DacFxResult()
                {
                    OperationId = operation.OperationId,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleProjectDeployRequest(ProjectDeployParams parameters, RequestContext<DacFxResult> requestContext)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                try
                {
                    ProjectDeployOperation operation = new ProjectDeployOperation(parameters, connInfo);
                    ExecuteOperation(operation, parameters, SR.ProjectDeployTaskName, requestContext);

                    await requestContext.SendResult(new DacFxResult()
                    {
                        OperationId = operation.OperationId,
                        Success = true,
                        ErrorMessage = string.Empty
                    });
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            }
        }

        private void ExecuteOperation(DacFxOperation operation, DacFxParams parameters, string taskName, RequestContext<DacFxResult> requestContext)
        {
            Task.Run(async () =>
            {
                try
                {
                    TaskMetadata metadata = TaskMetadata.Create(parameters, taskName, operation, ConnectionServiceInstance);

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
