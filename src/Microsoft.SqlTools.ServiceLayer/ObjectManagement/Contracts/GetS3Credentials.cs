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
    public class GetS3CredentialsRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// Target database
        /// </summary>
        public string connectionUri { get; set; }
    }


    public class GetS3CredentialsRequest
    {
        public static readonly RequestType<GetS3CredentialsRequestParams, List<string>> Type = RequestType<GetS3CredentialsRequestParams, List<string>>.Create("objectManagement/getS3CredentialsRequest");
    }
}
