//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class UpdateObjectRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The context id.
        /// </summary>
        public string ContextId { get; set; }
        /// <summary>
        /// The object information.
        /// </summary>
        public JToken Object { get; set; }
    }

    public class UpdateObjectRequestResponse { }

    public class UpdateObjectRequest
    {
        public static readonly RequestType<UpdateObjectRequestParams, UpdateObjectRequestResponse> Type = RequestType<UpdateObjectRequestParams, UpdateObjectRequestResponse>.Create("objectManagement/update");
    }
}