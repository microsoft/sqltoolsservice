//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Cms.Contracts
{

    public class CreateCentralManagementServerRequest
    {
        public static readonly RequestType<CreateCentralManagementServerParams, RegisteredServersResult> Type =
            RequestType<CreateCentralManagementServerParams, RegisteredServersResult>.Create("cms/createCms");
    }

    public class GetRegisteredServerRequest
    {
        public static readonly RequestType<ConnectParams, RegisteredServersResult> Type =
            RequestType<ConnectParams, RegisteredServersResult>.Create("cms/listRegServers");
    }

    public class AddRegisteredServerRequest
    {
        public static readonly RequestType<AddRegisteredServerParams, bool> Type =
            RequestType<AddRegisteredServerParams, bool>.Create("cms/addRegServers");
    }

    public class RemoveRegisteredServerRequest
    {
        public static readonly RequestType<RemoveRegisteredServerParams, bool> Type =
            RequestType<RemoveRegisteredServerParams, bool>.Create("cms/removeRegServers");
    }
}