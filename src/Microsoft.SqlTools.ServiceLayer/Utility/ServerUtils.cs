//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class ServerUtils
    {
        public static bool IsManagedInstance(this Server server)
        {
            return server.Information?.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;
        }

        public static bool IsArcEnabledManagedInstance(this Server server)
        {
            return server.Information?.DatabaseEngineEdition == DatabaseEngineEdition.SqlAzureArcManagedInstance;
        }

        public static bool IsAnyManagedInstance(this Server server)
        {
            return (IsManagedInstance(server) || IsArcEnabledManagedInstance(server));
        }
    }
}