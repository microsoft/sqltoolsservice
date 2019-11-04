//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Kusto;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.KustoModel
{
    /// <summary>
    /// Custom name for table
    /// </summary>
    internal partial class TablesChildFactory : KustoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, KustoQueryContext smoContext)
        {
            try
            {
                Table table = smoObject as Table;
                if (table != null && IsPropertySupported("IsSystemVersioned", smoContext, table, CachedKustoProperties) && table.IsSystemVersioned)
                {
                    return $"{table.Schema}.{table.Name} ({SR.SystemVersioned_LabelPart})";
                }
                else if (table != null && IsPropertySupported("IsExternal", smoContext, table, CachedKustoProperties) && table.IsExternal)
                {
                    return $"{table.Schema}.{table.Name} ({SR.External_LabelPart})"; 
                }
                else if (table != null && IsPropertySupported("IsFileTable", smoContext, table, CachedKustoProperties) && table.IsFileTable)
                {
                    return $"{table.Schema}.{table.Name} ({SR.FileTable_LabelPart})"; 
                }
            }
            catch
            {
                //Ignore the exception and just not change create custom name
            }

            return string.Empty;
        }

        public override string GetNodeSubType(object smoObject, KustoQueryContext smoContext)
        {
            try
            {
                Table table = smoObject as Table;
                if (table != null && IsPropertySupported("TemporalType", smoContext, table, CachedKustoProperties) && table.TemporalType != TableTemporalType.None)
                {
                    return "Temporal";
                }
                // TODO carbon issue 3125 enable "External" subtype once icon is ready. Otherwise will get missing icon here.
                // else if (table != null && IsPropertySupported("IsExternal", smoContext, table, CachedKustoProperties) && table.IsExternal)
                // {
                //     return "External";
                // }
               // return string.Empty;

            }
            catch
            {
                //Ignore the exception and just not change create custom name
            }

            return string.Empty;
        }

        public override string GetNodePathName(object smoObject)
        {
            return TableCustomNodeHelper.GetPathName(smoObject);
        }
    }

    /// <summary>
    /// Custom name for history table
    /// </summary>
    internal partial class TableChildFactory : KustoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, KustoQueryContext smoContext)
        {
            Table table = smoObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name} ({SR.History_LabelPart})";
            }

            return string.Empty;
        }

        public override string GetNodePathName(object smoObject)
        {
            return TableCustomNodeHelper.GetPathName(smoObject);
        }
    }

    internal static class TableCustomNodeHelper
    {
        internal static string GetPathName(object smoObject)
        {
            Table table = smoObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name}";
            }

            return string.Empty;
        }
    }
}
