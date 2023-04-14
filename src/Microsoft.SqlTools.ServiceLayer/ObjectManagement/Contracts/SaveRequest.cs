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
    public class SaveObjectRequestParams : GeneralRequestDetails
    {
        public string ContextId { get; set; }

        public JToken Object { get; set; }
    }

    public class SaveObjectResponse
    {
    }

    public class SaveObjectRequest
    {
        public static readonly RequestType<SaveObjectRequestParams, SaveObjectResponse> Type = RequestType<SaveObjectRequestParams, SaveObjectResponse>.Create("objectManagement/create");
    }
}