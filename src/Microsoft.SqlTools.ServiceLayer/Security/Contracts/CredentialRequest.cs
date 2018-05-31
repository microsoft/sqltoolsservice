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
    public class CreateCredentialResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
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
            RequestType<CreateCredentialParams, CreateCredentialResult> Type =
            RequestType<CreateCredentialParams, CreateCredentialResult>.Create("security/createcredential");
    }
}
