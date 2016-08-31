//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Win32
{
    /// <summary>
    /// Win32 implementation of the credential store
    /// </summary>
    internal class Win32CredentialStore : ICredentialStore
    {
        public bool DeletePassword(string credentialId, string username)
        {
            using (Win32Credential cred = new Win32Credential() { Target = credentialId, Username = username })
            {
                return cred.Delete();
            }
        }

        public bool TryGetPassword(string credentialId, string username, out string password)
        {
            Validate.IsNotNullOrEmptyString("credentialId", credentialId);
            password = null;

            using (CredentialSet set = new CredentialSet(credentialId).Load())
            {
                // Note: Credentials are disposed on disposal of the set
                Win32Credential foundCred = null;
                if (string.IsNullOrEmpty(username))
                {
                    // Expecting just 1 credential
                    if (set.Count > 0)
                    {
                        foundCred = set[0];
                    }
                }
                else
                {
                    foreach (Win32Credential cred in set)
                    {
                        if (string.Equals(cred.Username, username, StringComparison.Ordinal))
                        {
                            foundCred = cred;
                            break;
                        }
                    }
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
                new Win32Credential(credential.Username, credential.Password, credential.CredentialId, CredentialType.Generic)
                { PersistanceType = PersistanceType.LocalComputer })
            {
                return cred.Save();
            }
                
        }
    }
    
}
