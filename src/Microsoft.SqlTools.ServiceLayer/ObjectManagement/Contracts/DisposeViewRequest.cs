//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class DisposeObjectViewRequestParams : GeneralRequestDetails
    {
        public string ContextId { get; set; }
    }

    public class DisposeViewRequestResponse { }

    public class DisposeViewRequest
    {
        public static readonly RequestType<DisposeObjectViewRequestParams, DisposeViewRequestResponse> Type = RequestType<DisposeObjectViewRequestParams, DisposeViewRequestResponse>.Create("objectManagement/disposeView");
    }
}