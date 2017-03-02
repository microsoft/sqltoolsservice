//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Credentials.Win32
{
    /// <summary>
    /// Win32 implementation of the credential store
    /// </summary>
    internal class Win32CredentialStore : ICredentialStore
    {
        private const string AnyUsername = "*";

        public bool DeletePassword(string credentialId)
        {
            using (Win32Credential cred = new Win32Credential() { Target = credentialId, Username = AnyUsername })
            {
                return cred.Delete();
            }
        }

        public bool TryGetPassword(string credentialId, out string password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            password = null;

            using (CredentialSet set = new CredentialSet(credentialId).Load())
            {
                // Note: Credentials are disposed on disposal of the set
                Win32Credential foundCred = null;
                if (set.Count > 0)
                {
                    foundCred = set[0];
                }                

                if (foundCred != null)
                {
                    password = foundCred.Password;
                    return true;
                }
                return false;
            }
        }

        public bool Save(Credential credential)
        {
            Credential.ValidateForSave(credential);

            using (Win32Credential cred = 
                new Win32Credential(AnyUsername, credential.Password, credential.CredentialId, CredentialType.Generic)
                { PersistanceType = PersistanceType.LocalComputer })
            {
                return cred.Save();
            }
                
        }
    }
    
}
