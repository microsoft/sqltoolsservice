//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Credentials
{
    /// <summary>
    /// An <see cref="ICredentialStore"/> support securely saving and retrieving passwords
    /// </summary>
    public interface ICredentialStore
    {
        /// <summary>
        /// Saves a Password linked to a given Credential
        /// </summary>
        /// <param name="credential">
        /// A <see cref="Credential"/> to be saved. 
        /// <see cref="Credential.CredentialId"/> and <see cref="Credential.Password"/> are required
        /// </param>
        /// <returns>True if successful, false otherwise</returns>
        bool Save(Credential credential);

        /// <summary>
        /// Gets a Password and sets it into a <see cref="Credential"/> object
        /// </summary>
        /// <param name="credentialId">The name of the credential to find the password for. This is required</param>
        /// <param name="password">Out value</param>
        /// <returns>true if password was found, false otherwise</returns>
        bool TryGetPassword(string credentialId, out string password);

        /// <summary>
        /// Deletes a password linked to a given credential
        /// </summary>
        /// <param name="credentialId">The name of the credential to find the password for. This is required</param>
        /// <returns>True if password existed and was deleted, false otherwise</returns>
        bool DeletePassword(string credentialId);

    }
}
