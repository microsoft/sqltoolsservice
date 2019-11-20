//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for logins
    /// </summary>
    internal partial class ServerLevelLoginsChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return LoginCustomNodeHelper.GetStatus(smoObject);
        }
        
        private readonly Lazy<List<NodeSmoProperty>> smoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty>
        {
            new NodeSmoProperty
            {
                Name = "IsDisabled",
                ValidFor = ValidForFlag.All
            }
        });

        public override IEnumerable<NodeSmoProperty> SmoProperties => smoPropertiesLazy.Value;
    }

    internal static class LoginCustomNodeHelper
    {
        internal static string GetStatus(object context)
        {
            Login login = context as Login;
            if (login != null)
            {
                if (login.IsDisabled)
                {
                    return "Disabled";
                }
            }

            return string.Empty;
        }
    }
}

