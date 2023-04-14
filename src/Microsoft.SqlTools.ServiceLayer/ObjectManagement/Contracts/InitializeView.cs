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
    public class InitializeViewRequestParams : GeneralRequestDetails
    {
        public string ContextId { get; set; }

        public JToken Object { get; set; }
    }

    public class InitializeViewResponse
    {
    }

    public class InitializeViewRequest
    {
        public static readonly RequestType<InitializeViewRequestParams, InitializeViewResponse> Type = RequestType<InitializeViewRequestParams, InitializeViewResponse>.Create("objectManagement/initializeView");
    }
}