//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class GetCredentialNamesRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// Connection uri to database
        /// </summary>
        public string ConnectionUri { get; set; }
    }


    public class GetCredentialNamesRequest
    {
        public static readonly RequestType<GetCredentialNamesRequestParams, List<string>> Type = RequestType<GetCredentialNamesRequestParams, List<string>>.Create("objectManagement/getCredentialNamesRequest");
    }
}
