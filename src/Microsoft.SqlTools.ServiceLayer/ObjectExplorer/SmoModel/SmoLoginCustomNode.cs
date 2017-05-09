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
    internal partial class ServerLevelLoginsChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object context)
        {
            return LoginCustomeNodeHelper.GetStatus(context);
        }
    }

    internal static class LoginCustomeNodeHelper
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

