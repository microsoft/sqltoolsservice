//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class CreateCredentialRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// Credential info  
        /// </summary>
        public CredentialInfo CredentialInfo { get; set; }
        /// <summary>
        /// Connection uri
        /// </summary>
        public string ConnectionUri { get; set; }
    }

    public class CreateCredentialRequestResponse { }

    public class CreateCredentialRequest
    {
        public static readonly RequestType<CreateCredentialRequestParams, CreateCredentialRequestResponse> Type = RequestType<CreateCredentialRequestParams, CreateCredentialRequestResponse>.Create("objectManagement/createCredentialRequest");
    }
}