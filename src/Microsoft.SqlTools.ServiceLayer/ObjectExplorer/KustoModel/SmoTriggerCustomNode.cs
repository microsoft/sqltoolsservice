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
    /// Status for triggers
    /// </summary>
    internal partial class TriggersChildFactory : KustoChildFactoryBase
    {
        public static readonly Lazy<List<NodeKustoProperty>> KustoPropertiesLazy = new Lazy<List<NodeKustoProperty>>(() => new List<NodeKustoProperty>
        {
            new NodeKustoProperty
            {
                Name = "IsEnabled",
                ValidFor = ValidForFlag.All
            }
        });

        public override string GetNodeStatus(object smoObject, KustoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeKustoProperty> KustoProperties => KustoPropertiesLazy.Value;
    }

    internal partial class ServerLevelServerTriggersChildFactory : KustoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, KustoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeKustoProperty> KustoProperties
        {
            get
            {
                return TriggersChildFactory.KustoPropertiesLazy.Value;
            }
        }
    }

    internal partial class DatabaseTriggersChildFactory : KustoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, KustoQueryContext smoContext)
        {
            return TriggersCustomeNodeHelper.GetStatus(smoObject);
        }

        public override IEnumerable<NodeKustoProperty> KustoProperties
        {
            get
            {
                return TriggersChildFactory.KustoPropertiesLazy.Value;
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
