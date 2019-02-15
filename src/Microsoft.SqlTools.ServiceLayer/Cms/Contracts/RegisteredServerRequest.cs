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
        public static readonly RequestType<CreateCentralManagementServerParams, ListRegisteredServersResult> Type =
            RequestType<CreateCentralManagementServerParams, ListRegisteredServersResult>.Create("cms/createCms");
    }

    public class ListRegisteredServerRequest
    {
        public static readonly RequestType<ListRegisteredServerParams, ListRegisteredServersResult> Type =
            RequestType<ListRegisteredServerParams, ListRegisteredServersResult>.Create("cms/listRegServers");
    }

    public class AddRegisteredServerRequest
    {
        public static readonly RequestType<AddRegisteredServerParams, bool> Type =
            RequestType<AddRegisteredServerParams, bool>.Create("cms/addRegServers");
    }

    public class AddServerGroupRequest
    {
        public static readonly RequestType<AddServerGroupParams, bool> Type =
            RequestType<AddServerGroupParams, bool>.Create("cms/addCmsServerGroup");
    }

    public class RemoveServerGroupRequest
    {
        public static readonly RequestType<RemoveServerGroupParams, bool> Type =
            RequestType<RemoveServerGroupParams, bool>.Create("cms/removeCmsServerGroup");
    }

    public class RemoveRegisteredServerRequest
    {
        public static readonly RequestType<RemoveRegisteredServerParams, bool> Type =
            RequestType<RemoveRegisteredServerParams, bool>.Create("cms/removeRegServers");
    }
}