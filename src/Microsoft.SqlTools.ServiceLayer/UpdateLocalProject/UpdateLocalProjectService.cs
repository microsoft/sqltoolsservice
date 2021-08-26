//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.UpdateLocalProject.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UpdateLocalProject
{
    class UpdateLocalProjectService
    {
        private static readonly Lazy<UpdateLocalProjectService> instance = new(() => new UpdateLocalProjectService());
        private static ConnectionService _connectionService = null;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static UpdateLocalProjectService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Connection service instance
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get { return _connectionService ?? ConnectionService.Instance; }
            set { _connectionService = value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(UpdateLocalProjectRequest.Type, this.HandleUpdateLocalProjectRequest);
        }

        /// <summary>
        /// Handles request to update a local project
        /// </summary>
        public async Task HandleUpdateLocalProjectRequest(UpdateLocalProjectParams parameters, RequestContext<UpdateLocalProjectResult> requestContext)
        {
            UpdateLocalProjectResult result;
            UpdateLocalProjectOperation operation;

            try
            {
                // find connection
                ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out ConnectionInfo connInfo);

                if (connInfo == null)
                {
                    throw new Exception();
                }
                else
                {
                    // execute operation and send result
                    operation = new UpdateLocalProjectOperation(parameters, connInfo);
                    result = operation.UpdateLocalProject();

                    await requestContext.SendResult(result);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }
    }
}