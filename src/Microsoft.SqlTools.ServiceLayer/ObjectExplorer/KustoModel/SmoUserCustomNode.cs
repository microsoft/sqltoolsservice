//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Kusto;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.KustoModel
{
    /// <summary>
    /// Status for logins
    /// </summary>
    internal partial class UsersChildFactory : KustoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, KustoQueryContext smoContext)
        {
            return UserCustomeNodeHelper.GetStatus(smoObject);
        }

        private readonly Lazy<List<NodeKustoProperty>> smoPropertiesLazy = new Lazy<List<NodeKustoProperty>>(() => new List<NodeKustoProperty> 
        {
            new NodeKustoProperty
            {
                Name = "HasDBAccess",
                ValidFor = ValidForFlag.All
            }
        });

        public override IEnumerable<NodeKustoProperty> KustoProperties => smoPropertiesLazy.Value;
    }

    internal static class UserCustomeNodeHelper
    {
        internal static string GetStatus(object context)
        {
            User user = context as User;
            if (user != null)
            {
                if (!user.HasDBAccess)
                {
                    return "Disabled";
                }
            }

            return string.Empty;
        }
    }
}

