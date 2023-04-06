//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Main class for Security Service functionality
    /// </summary>
    public sealed class SecurityService : IDisposable
    {
        private bool disposed;

        private ConnectionService? connectionService;

        private UserServiceHandlerImpl userServiceHandler;

        private LoginServiceHandlerImpl loginServiceHandler;

        private static readonly Lazy<SecurityService> instance = new Lazy<SecurityService>(() => new SecurityService());


        /// <summary>
        /// Construct a new SecurityService instance with default parameters
        /// </summary>
        public SecurityService()
        {
            userServiceHandler = new UserServiceHandlerImpl();
            loginServiceHandler = new LoginServiceHandlerImpl();
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
                connectionService ??= ConnectionService.Instance;
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
        internal IProtocolEndpoint? ServiceHost
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
            this.ServiceHost.SetRequestHandler(CreateCredentialRequest.Type, HandleCreateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateCredentialRequest.Type, HandleUpdateCredentialRequest, true);
            this.ServiceHost.SetRequestHandler(GetCredentialsRequest.Type, HandleGetCredentialsRequest, true);

            // Login request handlers
            this.ServiceHost.SetRequestHandler(CreateLoginRequest.Type, this.loginServiceHandler.HandleCreateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateLoginRequest.Type, this.loginServiceHandler.HandleUpdateLoginRequest, true);
            this.ServiceHost.SetRequestHandler(InitializeLoginViewRequest.Type, this.loginServiceHandler.HandleInitializeLoginViewRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeLoginViewRequest.Type, this.loginServiceHandler.HandleDisposeLoginViewRequest, true);

            // User request handlers
            this.ServiceHost.SetRequestHandler(InitializeUserViewRequest.Type, this.userServiceHandler.HandleInitializeUserViewRequest, true);
            this.ServiceHost.SetRequestHandler(CreateUserRequest.Type, this.userServiceHandler.HandleCreateUserRequest, true);
            this.ServiceHost.SetRequestHandler(UpdateUserRequest.Type, this.userServiceHandler.HandleUpdateUserRequest, true);
            this.ServiceHost.SetRequestHandler(DisposeUserViewRequest.Type, this.userServiceHandler.HandleDisposeUserViewRequest, true);
        }

        #region "Credential Handlers"

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
        /// Handle request to get all credentials
        /// </summary>
        internal async Task HandleGetCredentialsRequest(GetCredentialsParams parameters, RequestContext<GetCredentialsResult> requestContext)
        {
            var result = new GetCredentialsResult();
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                var credentials = dataContainer.Server?.Credentials;
                int credentialsCount = credentials != null ? credentials.Count : 0;
                CredentialInfo[] credentialsInfos = new CredentialInfo[credentialsCount];
                if (credentials != null)
                {
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

        internal Task<Tuple<bool, string>> ConfigureCredential(
            string ownerUri,
            CredentialInfo credential,
            ConfigAction configAction,
            RunType runType)
        {
            return Task<Tuple<bool, string>>.Run(() =>
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

        #endregion // "Credential Handlers"
    }
}
