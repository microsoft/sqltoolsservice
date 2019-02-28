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
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;

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
        private readonly Lazy<ConcurrentDictionary<string, ITaskOperation>> operations =
            new Lazy<ConcurrentDictionary<string, ITaskOperation>>(() => new ConcurrentDictionary<string, ITaskOperation>());

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
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, ITaskOperation> ActiveOperations => operations.Value;

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
                    ErrorMessage = operation.ErrorMessage,
                    AreEqual = operation.ComparisonResult.IsEqual,
                    Differences = operation.Differences
                });
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
