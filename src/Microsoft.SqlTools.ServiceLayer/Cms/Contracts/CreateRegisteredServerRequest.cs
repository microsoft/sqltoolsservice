//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Cms.Contracts
{
    public class GetRegisteredServerRequest
    {
        public static readonly RequestType<ConnectParams, ListCmsServersResult> Type =
            RequestType<ConnectParams, ListCmsServersResult>.Create("cms/get");
    }
}