//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.Credentials.Linux;
using Microsoft.SqlTools.Credentials.OSX;
using Microsoft.SqlTools.Credentials.Win32;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Credentials
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
            : this(null, new StoreConfig()
            { CredentialFolder = DefaultSecretsFolder, CredentialFile = DefaultSecretsFile, IsRelativeToUserHomeDir = true })
        {
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal CredentialService(ICredentialStore store, StoreConfig config)
        {
            credStore = store != null ? store : GetStoreForOS(config);
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ICredentialStore GetStoreForOS(StoreConfig config)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Win32CredentialStore();
            }
#if !WINDOWS_ONLY_BUILD
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new OSXCredentialStore();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxCredentialStore(config);
            }
#endif
            throw new InvalidOperationException("Platform not currently supported");
        }

        public void InitializeService(IRpcServiceHost serviceHost)
        {
            // Register request and event handlers with the Service Host
            serviceHost.RegisterRequestHandler(ReadCredentialRequest.Type, HandleReadCredentialRequest);
            serviceHost.RegisterRequestHandler(SaveCredentialRequest.Type, HandleSaveCredentialRequest);
            serviceHost.RegisterRequestHandler(DeleteCredentialRequest.Type, HandleDeleteCredentialRequest);
        }

        public async Task<Credential> HandleReadCredentialRequest(Credential credential)
        {
            Func<Task<Credential>> doRead = () =>
            {
                return ReadCredentialAsync(credential);
            };
            return await HandleRequest(doRead, "HandleReadCredentialRequest");
        }


        public Task<Credential> ReadCredentialAsync(Credential credential)
        {
            return Task.Run(() => ReadCredential(credential));
        }

        public Credential ReadCredential(Credential credential)
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

        public async Task<bool> HandleSaveCredentialRequest(Credential credential)
        {
            Func<Task<bool>> doSave = () =>
            {
                return SaveCredentialAsync(credential);
            };
            return await HandleRequest(doSave, "HandleSaveCredentialRequest");
        }

        public Task<bool> SaveCredentialAsync(Credential credential)
        {
            return Task.Run(() => SaveCredential(credential));
        }

        public bool SaveCredential(Credential credential)
        {
            Credential.ValidateForSave(credential);
            return credStore.Save(credential);
        }

        public async Task<bool> HandleDeleteCredentialRequest(Credential credential)
        {
            Func<Task<bool>> doDelete = () =>
            {
                return DeletePasswordAsync(credential);
            };
            return await HandleRequest(doDelete, "HandleDeleteCredentialRequest");
        }

        private Task<bool> DeletePasswordAsync(Credential credential)
        {
            return Task.Run(() =>
            {
                Credential.ValidateForLookup(credential);
                return credStore.DeletePassword(credential.CredentialId);
            });
        }

        private async Task<T> HandleRequest<T>(Func<Task<T>> handler, string requestType)
        {
            Logger.Verbose(requestType);
            T result = await handler();
            return result;
        }

    }
}
