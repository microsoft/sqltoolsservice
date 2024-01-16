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
    public class GetCredentialsRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// Target database
        /// </summary>
        public string connectionUri { get; set; }
    }


    public class GetCredentialsRequest
    {
        public static readonly RequestType<GetCredentialsRequestParams, List<string>> Type = RequestType<GetCredentialsRequestParams, List<string>>.Create("objectManagement/getCredentialsRequest");
    }
}
