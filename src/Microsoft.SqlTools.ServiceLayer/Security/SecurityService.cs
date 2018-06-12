//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Security Service functionality
    /// </summary>
    public sealed class SecurityService : IDisposable
    {
        private bool disposed;

        private ConnectionService connectionService = null;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());

        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SecurityService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
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
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the Security Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Credential request handlers
            this.ServiceHost.SetRequestHandler(CreateCredentialRequest.Type, HandleCreateCredentialRequest);
            this.ServiceHost.SetRequestHandler(UpdateCredentialRequest.Type, HandleUpdateCredentialRequest);
            this.ServiceHost.SetRequestHandler(DeleteCredentialRequest.Type, HandleDeleteCredentialRequest);
        }

        /// <summary>
        /// Handle request to create a credential
        /// </summary>
        internal async Task HandleCreateCredentialRequest(CreateCredentialParams parameters, RequestContext<CreateCredentialResult> requestContext)
        {
            try
            {
                var result = new CreateCredentialResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                CDataContainer dataContainer = AdminService.CreateDataContainer(connInfo, databaseExists: true);
                Credential credential = new Credential(dataContainer);

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to update a credential
        /// </summary>
        internal async Task HandleUpdateCredentialRequest(UpdateCredentialParams parameters, RequestContext<UpdateCredentialResult> requestContext)
        {
            var result = new UpdateCredentialResult();
            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteCredentialRequest(DeleteCredentialParams parameters, RequestContext<DeleteCredentialResult> requestContext)
        {
            var result = new DeleteCredentialResult();
            await requestContext.SendResult(result);            
        }

        /// <summary>
        /// Disposes the service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {                
                disposed = true;
            }
        }
    }
}
