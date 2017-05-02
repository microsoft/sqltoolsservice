//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Credentials.Contracts
{
    /// <summary>
    /// A Credential containing information needed to log into a resource. This is primarily 
    /// defined as a unique <see cref="CredentialId"/> with an associated <see cref="Password"/>
    /// that's linked to it. 
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// A unique ID to identify the credential being saved. 
        /// </summary>
        public string CredentialId { get; set; }
        
        /// <summary>
        /// The Password stored for this credential. 
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Credential()
        {
        }

        /// <summary>
        /// Constructor used when only <paramref name="credentialId"/> is known
        /// </summary>
        /// <param name="credentialId"><see cref="CredentialId"/></param>
        public Credential(string credentialId)
            : this(credentialId, null)
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="credentialId"><see cref="CredentialId"/></param>
        /// <param name="password"><see cref="Password"/></param>
        public Credential(string credentialId, string password)
        {
            CredentialId = credentialId;
            Password = password;
        }

        internal static Credential Copy(Credential credential)
        {
            return new Credential
            {
                CredentialId = credential.CredentialId,
                Password = credential.Password
            };
        }

        /// <summary>
        /// Validates the credential has all the properties needed to look up the password
        /// </summary>
        public static void ValidateForLookup(Credential credential)
        {
            Validate.IsNotNull("credential", credential);
            Validate.IsNotNullOrEmptyString("credential.CredentialId", credential.CredentialId);
        }


        /// <summary>
        /// Validates the credential has all the properties needed to save a password
        /// </summary>
        public static void ValidateForSave(Credential credential)
        {
            ValidateForLookup(credential);
            Validate.IsNotNull("credential.Password", credential.Password);
        }
    }

    /// <summary>
    /// Read Credential request mapping entry. Expects a Credential with CredentialId, 
    /// and responds with the <see cref="Credential.Password"/> filled in if found
    /// </summary>
    public class ReadCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<Credential, Credential> Type =
            RequestType<Credential, Credential>.Create("credential/read");
    }

    /// <summary>
    /// Save Credential request mapping entry
    /// </summary>
    public class SaveCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<Credential, bool> Type =
            RequestType<Credential, bool>.Create("credential/save");
    }

    /// <summary>
    /// Delete Credential request mapping entry
    /// </summary>
    public class DeleteCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<Credential, bool> Type =
            RequestType<Credential, bool>.Create("credential/delete");
    }
}
