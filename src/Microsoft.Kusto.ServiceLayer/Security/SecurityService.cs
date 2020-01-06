//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Hosting;
using Microsoft.Kusto.ServiceLayer.Management;
using Microsoft.Kusto.ServiceLayer.Security.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Security;

namespace Microsoft.Kusto.ServiceLayer.Security
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
            this.ServiceHost.SetRequestHandler(GetCredentialsRequest.Type, HandleGetCredentialsRequest);
        }

        /// <summary>
        /// Handle request to create a credential
        /// </summary>
        internal async Task HandleCreateCredentialRequest(CreateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {               
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to update a credential
        /// </summary>
        internal async Task HandleUpdateCredentialRequest(UpdateCredentialParams parameters, RequestContext<CredentialResult> requestContext)
        {             
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new CredentialResult()
            {
                Credential = parameters.Credential,
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to delete a credential
        /// </summary>
        internal async Task HandleDeleteCredentialRequest(DeleteCredentialParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureCredential(parameters.OwnerUri,
                parameters.Credential,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        
        /// <summary>
        /// Handle request to get all credentials
        /// </summary>
        internal async Task HandleGetCredentialsRequest(GetCredentialsParams parameters, RequestContext<GetCredentialsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new GetCredentialsResult();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    
                    var credentials = dataContainer.Server.Credentials;
                    int credentialsCount = credentials.Count;
                    CredentialInfo[] credentialsInfos = new CredentialInfo[credentialsCount];
                    for (int i = 0; i < credentialsCount; ++i)
                    {
                        credentialsInfos[i] = new CredentialInfo();
                        credentialsInfos[i].Name = credentials[i].Name;
                        credentialsInfos[i].Identity = credentials[i].Identity;
                        credentialsInfos[i].Id = credentials[i].ID;
                        credentialsInfos[i].DateLastModified = credentials[i].DateLastModified;
                        credentialsInfos[i].CreateDate = credentials[i].CreateDate;
                        credentialsInfos[i].ProviderName = credentials[i].ProviderName;
                    }
                    result.Credentials = credentialsInfos;
                    result.Success = true;  
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }

                await requestContext.SendResult(result);
            });
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

#region "Helpers"

        internal async Task<Tuple<bool, string>> ConfigureCredential(
            string ownerUri,
            CredentialInfo credential,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    using (CredentialActions actions = new CredentialActions(dataContainer, credential, configAction))
                    {
                        var executionHandler = new ExecutonHandler(actions);
                        executionHandler.RunNow(runType, this);
                    }        

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

#endregion // "Helpers"
    }
}
