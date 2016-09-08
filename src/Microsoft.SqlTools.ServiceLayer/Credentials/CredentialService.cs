//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.Credentials.Linux;
using Microsoft.SqlTools.ServiceLayer.Credentials.OSX;
using Microsoft.SqlTools.ServiceLayer.Credentials.Win32;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer.Credentials
{
    /// <summary>
    /// Service responsible for securing credentials in a platform-neutral manner. This provides
    /// a generic API for read, save and delete credentials
    /// </summary>
    public class CredentialService
    {
        internal static string DefaultSecretsFolder = ".sqlsecrets";
        internal const string DefaultSecretsFile = "sqlsecrets.json";
        

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<CredentialService> instance
            = new Lazy<CredentialService>(() => new CredentialService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static CredentialService Instance
        {
            get
            {
                return instance.Value;
            }
        }

        private ICredentialStore credStore;

        /// <summary>
        /// Default constructor is private since it's a singleton class
        /// </summary>
        private CredentialService()
            : this(null, new LinuxCredentialStore.StoreConfig() 
                { CredentialFolder = DefaultSecretsFolder, CredentialFile = DefaultSecretsFile, IsRelativeToUserHomeDir = true})
        {
        }
        
        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal CredentialService(ICredentialStore store, LinuxCredentialStore.StoreConfig config)
        {
            this.credStore = store != null ? store : GetStoreForOS(config);
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ICredentialStore GetStoreForOS(LinuxCredentialStore.StoreConfig config)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Win32CredentialStore();
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new OSXCredentialStore();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxCredentialStore(config);
            }
            throw new InvalidOperationException("Platform not currently supported");
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            // Register request and event handlers with the Service Host
            serviceHost.SetRequestHandler(ReadCredentialRequest.Type, HandleReadCredentialRequest);
            serviceHost.SetRequestHandler(SaveCredentialRequest.Type, HandleSaveCredentialRequest);
            serviceHost.SetRequestHandler(DeleteCredentialRequest.Type, HandleDeleteCredentialRequest);
        }

        public async Task HandleReadCredentialRequest(Credential credential, RequestContext<Credential> requestContext)
        {
            Func<Credential> doRead = () =>
            {
                return ReadCredential(credential);
            };
            await HandleRequest(doRead, requestContext, "HandleReadCredentialRequest");
        }


        private Credential ReadCredential(Credential credential)
        {
            Credential.ValidateForLookup(credential);

            Credential result = Credential.Copy(credential);
            string password;
            if (credStore.TryGetPassword(credential.CredentialId, out password))
            {
                result.Password = password;
            }
            return result;
        }

        public async Task HandleSaveCredentialRequest(Credential credential, RequestContext<bool> requestContext)
        {
            Func<bool> doSave = () =>
            {
                Credential.ValidateForSave(credential);
                return credStore.Save(credential);
            };
            await HandleRequest(doSave, requestContext, "HandleSaveCredentialRequest");
        }

        public async Task HandleDeleteCredentialRequest(Credential credential, RequestContext<bool> requestContext)
        {
            Func<bool> doDelete = () =>
            {
                Credential.ValidateForLookup(credential);
                return credStore.DeletePassword(credential.CredentialId);
            };
            await HandleRequest(doDelete, requestContext, "HandleDeleteCredentialRequest");
        }

        private async Task HandleRequest<T>(Func<T> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(LogLevel.Verbose, requestType);

            try
            {
                T result = handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

    }
}
