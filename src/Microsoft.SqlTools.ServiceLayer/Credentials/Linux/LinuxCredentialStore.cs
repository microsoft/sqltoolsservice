//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Linux
{
    /// <summary>
    /// Linux implementation of the credential store.
    /// 
    /// <remarks>
    /// This entire implementation may need to be revised to support encryption of
    /// passwords and protection of them when loaded into memory.
    /// </remarks>
    /// </summary>
    internal class LinuxCredentialStore : ICredentialStore
    {
        private string credentialFolder;
        private string credentialFileName;
        private FileTokenStorage storage;

        public LinuxCredentialStore(string credentialFolder, string credentialFileName)
        {
            Validate.IsNotNullOrEmptyString("credentialFolder", credentialFolder);
            Validate.IsNotNullOrEmptyString("credentialFileName", credentialFileName);
            this.credentialFolder = credentialFolder;
            this.credentialFileName = credentialFileName;
            string combinedPath = Path.Combine(credentialFolder, credentialFileName);
            storage = new FileTokenStorage(combinedPath);
        }
        
        public bool DeletePassword(string credentialId)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            IEnumerable<Credential> creds;
            if (LoadCredentialsAndFilterById(credentialId, out creds))
            {
                storage.SaveEntries(creds);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets filtered credentials with a specific ID filtered out
        /// </summary>
        /// <returns>True if the credential to filter was removed, false if it was not found</returns>
        private bool LoadCredentialsAndFilterById(string idToFilter, out IEnumerable<Credential> creds)
        {
            bool didRemove = false;
            creds = storage.LoadEntries().Where(cred =>
            {
                if (IsCredentialMatch(idToFilter, cred))
                {
                    didRemove = true;
                    return false; // filter this out
                }
                return true;
            });

            return didRemove;
        }

        private static bool IsCredentialMatch(string credentialId, Credential cred)
        {
            return string.Equals(credentialId, cred.CredentialId, StringComparison.Ordinal);
        }

        public bool TryGetPassword(string credentialId, out string password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            Credential cred = storage.LoadEntries().FirstOrDefault(c => IsCredentialMatch(credentialId, c));
            if (cred != null)
            {
                password = cred.Password;
                return true;
            }

            // Else this was not found in the list
            password = null;
            return false;
        }

        public bool Save(Credential credential)
        {
            Credential.ValidateForSave(credential);

            // Load the credentials, removing the existing Cred for this 
            IEnumerable<Credential> creds;
            LoadCredentialsAndFilterById(credential.CredentialId, out creds);
            storage.SaveEntries(creds.Append(credential));
            
            return true;
        }
    }
}