//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class NewS3CredentialRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// URI of the underlying connection for this request
        /// </summary>
        public string S3Url { get; set; }

        /// <summary>
        /// Secret to access S3 storage
        /// </summary>
        public string Secret { get; set; }
        /// <summary>
        /// Target database
        /// </summary>
        public string ConnectionUri { get; set; }
    }

    public class NewS3CredentialRequestResponse { }

    public class NewS3CredentialRequest
    {
        public static readonly RequestType<NewS3CredentialRequestParams, NewS3CredentialRequestResponse> Type = RequestType<NewS3CredentialRequestParams, NewS3CredentialRequestResponse>.Create("objectManagement/newS3CredentialRequest");
    }
}
