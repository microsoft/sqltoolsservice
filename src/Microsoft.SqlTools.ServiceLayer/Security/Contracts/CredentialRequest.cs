//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    /// <summary>
    /// Get Credential parameters
    /// </summary>
    public class GetCredentialsParams: GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    public class GetCredentialsResult: ResultStatus
    {
        public CredentialInfo[] Credentials { get; set; }
    }

    /// <summary>
    /// SQL Agent Credentials request type
    /// </summary>
    public class GetCredentialsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<GetCredentialsParams, GetCredentialsResult> Type =
            RequestType<GetCredentialsParams, GetCredentialsResult>.Create("security/credentials");
    }

    /// <summary>
    /// Create Credential parameters
    /// </summary>
    public class CreateCredentialParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public CredentialInfo Credential { get; set; }
    }

    /// <summary>
    /// Create Credential result
    /// </summary>
    public class CredentialResult : ResultStatus
    {
        public CredentialInfo Credential { get; set; }
        
    }

    /// <summary>
    /// Create Credential request type
    /// </summary>
    public class CreateCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateCredentialParams, CredentialResult> Type =
            RequestType<CreateCredentialParams, CredentialResult>.Create("security/createcredential");
    }

    /// <summary>
    /// Update Credential params
    /// </summary>
    public class UpdateCredentialParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public CredentialInfo Credential { get; set; }
    }

    /// <summary>
    /// Update Credential request type
    /// </summary>
    public class UpdateCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateCredentialParams, CredentialResult> Type =
            RequestType<UpdateCredentialParams, CredentialResult>.Create("security/updatecredential");
    }

    /// <summary>
    /// Delete Credential params
    /// </summary>
    public class DeleteCredentialParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public CredentialInfo Credential { get; set; }
    }

    /// <summary>
    /// Delete Credential request type
    /// </summary>
    public class DeleteCredentialRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteCredentialParams, ResultStatus> Type =
            RequestType<DeleteCredentialParams, ResultStatus>.Create("security/deletecredential");
    }
}
