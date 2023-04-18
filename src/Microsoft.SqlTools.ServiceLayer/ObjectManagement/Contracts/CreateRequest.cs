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
    public class CreateObjectRequestParams : GeneralRequestDetails
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

    public class CreateObjectRequestResponse { }

    public class CreateObjectRequest
    {
        public static readonly RequestType<CreateObjectRequestParams, CreateObjectRequestResponse> Type = RequestType<CreateObjectRequestParams, CreateObjectRequestResponse>.Create("objectManagement/create");
    }
}