//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for triggers
    /// </summary>
    internal partial class TriggersChildFactory : SmoChildFactoryBase
    {
        public static readonly Lazy<List<NodeSmoProperty>> SmoPropertiesLazy = new Lazy<List<NodeSmoProperty>>(() => new List<NodeSmoProperty>
        {
            new NodeSmoProperty
            {
                Name = "IsEnabled",
                ValidFor = ValidForFlag.All
            }
        });

        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties => SmoPropertiesLazy.Value;
    }

    internal partial class ServerLevelServerTriggersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
            get
            {
                return TriggersChildFactory.SmoPropertiesLazy.Value;
            }
        }
    }

    internal partial class DatabaseTriggersChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
            get
            {
                return TriggersChildFactory.SmoPropertiesLazy.Value;
            }
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
