//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for triggers
    /// </summary>
    internal partial class TriggersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object context)
        {
            return TriggersCustomeNodeHelper.GetStatus(context);
        }
    }

    internal partial class ServerLevelServerTriggersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object context)
        {
            return TriggersCustomeNodeHelper.GetStatus(context);
        }
    }

    internal partial class DatabaseTriggersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object context)
        {
            return TriggersCustomeNodeHelper.GetStatus(context);
        }
    }

    internal static class TriggersCustomeNodeHelper
    {
        internal static string GetStatus(object context)
        {
            Trigger trigger = context as Trigger;
            if (trigger != null)
            {
                if (!trigger.IsEnabled)
                {
                    return "Disabled";
                }
            }

            ServerDdlTrigger serverDdlTrigger = context as ServerDdlTrigger;
            if (serverDdlTrigger != null)
            {
                if (!serverDdlTrigger.IsEnabled)
                {
                    return "Disabled";
                }
            }

            DatabaseDdlTrigger databaseDdlTrigger = context as DatabaseDdlTrigger;
            if (databaseDdlTrigger != null)
            {
                if (!databaseDdlTrigger.IsEnabled)
                {
                    return "Disabled";
                }
            }

            return string.Empty;
        }
    }
}
