//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Cms.Contracts
{
    public class ListRegisteredServersResult
    {
        public List<RegisteredServerResult> RegisteredServersList { get; set; }

        public List<RegisteredServerGroup> RegisteredServerGroups { get; set; }
    }

    public class RegisteredServerResult
    {
        public string Name { get; set; }

        public string ServerName { get; set; }

        public string Description { get; set; }

        public string RelativePath { get; set; }

        public ConnectionDetails ConnectionDetails { get; set; }
    }

    public class RegisteredServerGroup
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string RelativePath { get; set; }
    }
}