//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Cms.Contracts
{
    /// <summary>
    /// Paramaters to create Top level Central Management Server
    /// </summary>
    public class CreateCentralManagementServerParams
    {
        public string RegisteredServerName { get; set; }

        public string RegisterdServerDescription { get; set; }

        public ConnectParams ConnectParams { get; set; }
    }

    /// <summary>
    /// Parmaters to Add Registered Server to top level CMS
    /// </summary>
    public class AddRegisteredServerParams
    {
        public string RegisteredServerName { get; set; }

        public string RegisterdServerDescription { get; set; }
        
        public ConnectionDetails RegServerConnectionDetails { get; set; }

        public string ParentOwnerUri { get; set; }

        public string[] RelativePath { get; set; }
    }

    /// <summary>
    /// Parmaters to Add Server Group to top level CMS
    /// </summary>
    public class AddServerGroupParams
    {
        public string GroupName { get; set; }

        public string GroupDescription { get; set; }

        public string ParentOwnerUri { get; set; }

        public string[] RelativePath { get; set; }
    }

    /// <summary>
    /// Parmaters to Remove Server Group from CMS
    /// </summary>
    public class RemoveServerGroupParams
    {
        public string GroupName { get; set; }
        
        public string ParentOwnerUri { get; set; }

        public string[] RelativePath { get; set; }
    }

    /// <summary>
    /// Paramaters to remove a Registered Server from CMS tree
    /// </summary>
    public class RemoveRegisteredServerParams
    {
        public string RegisteredServerName { get; set; }

        public string ParentOwnerUri { get; set; }

        public string[] RelativePath { get; set; }
    }

    /// <summary>
    /// Paramaters to list a Registered Server from CMS tree
    /// </summary>
    public class ListRegisteredServerParams
    {
        public string ParentOwnerUri { get; set; }

        public string[] RelativePath { get; set; }
    }
}