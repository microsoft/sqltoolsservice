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

        // TODO : only parent connection uri or actual connection??
        public string ParentOwnerUri { get; set; }
    }

    /// <summary>
    /// Paramaters to remove a Registered Server from CMS tree
    /// </summary>
    public class RemoveRegisteredServerParams
    {
        public string RegisteredServerName { get; set; }

        // TODO : only parent connection uri or actual connection??
        public string ParentOwnerUri { get; set; }
    }
}