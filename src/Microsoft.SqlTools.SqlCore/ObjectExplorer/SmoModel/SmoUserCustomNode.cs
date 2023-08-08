﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for logins
    /// </summary>
    internal partial class UsersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return UserCustomeNodeHelper.GetStatus(smoObject);
        }

        private readonly Lazy<List<NodeSmoProperty>> smoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty> 
        {
            new NodeSmoProperty
            {
                Name = "HasDBAccess",
                ValidFor = ValidForFlag.All
            }
        });

        public override IEnumerable<NodeSmoProperty> SmoProperties => smoPropertiesLazy.Value;
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

