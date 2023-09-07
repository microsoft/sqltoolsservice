//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class PurgeQueryStoreDataRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// SFC (SMO) URN identifying the object  
        /// </summary>
        public string ObjectUrn { get; set; }
        /// <summary>
        /// The target database name.
        /// </summary>
        public string Database { get; set; }
        /// <summary>
        /// URI of the underlying connection for this request
        /// </summary>
        public string ConnectionUri { get; set; }
    }

    public class PurgeQueryStoreDataRequestResponse { }

    public class PurgeQueryStoreDataRequest
    {
        public static readonly RequestType<PurgeQueryStoreDataRequestParams, PurgeQueryStoreDataRequestResponse> Type = RequestType<PurgeQueryStoreDataRequestParams, PurgeQueryStoreDataRequestResponse>.Create("objectManagement/purgeQueryStoreData");
    }
}
