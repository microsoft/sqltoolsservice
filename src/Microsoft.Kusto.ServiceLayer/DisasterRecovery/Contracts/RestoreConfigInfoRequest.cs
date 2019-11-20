//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.Contracts
{
    public class RestoreConfigInfoRequestParams
    {
        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class RestoreConfigInfoResponse
    {
        public RestoreConfigInfoResponse()
        {
            ConfigInfo = new Dictionary<string, object>();
        }

        /// <summary>
        /// Config Info
        /// </summary>
        public Dictionary<string, object> ConfigInfo { get; set; }

        /// <summary>
        /// Errors occurred while creating the restore config info
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class RestoreConfigInfoRequest
    {
        public static readonly
            RequestType<RestoreConfigInfoRequestParams, RestoreConfigInfoResponse> Type =
                RequestType<RestoreConfigInfoRequestParams, RestoreConfigInfoResponse>.Create("restore/restoreconfiginfo");
    }
}
