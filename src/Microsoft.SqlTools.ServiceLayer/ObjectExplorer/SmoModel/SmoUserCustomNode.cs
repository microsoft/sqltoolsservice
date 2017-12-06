//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
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

