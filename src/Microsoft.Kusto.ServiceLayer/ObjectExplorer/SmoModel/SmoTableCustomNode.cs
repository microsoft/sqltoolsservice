//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.OEModel
{
    /// <summary>
    /// Custom name for table
    /// </summary>
    internal partial class TablesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object oeObject, OEQueryContext oeContext)
        {
            try
            {
                Table table = oeObject as Table;
                if (table != null && IsPropertySupported("IsSystemVersioned", oeContext, table, CachedSmoProperties) && table.IsSystemVersioned)
                {
                    return $"{table.Schema}.{table.Name} ({SR.SystemVersioned_LabelPart})";
                }
                else if (table != null && IsPropertySupported("IsExternal", oeContext, table, CachedSmoProperties) && table.IsExternal)
                {
                    return $"{table.Schema}.{table.Name} ({SR.External_LabelPart})"; 
                }
                else if (table != null && IsPropertySupported("IsFileTable", oeContext, table, CachedSmoProperties) && table.IsFileTable)
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

        public override string GetNodeSubType(object oeObject, OEQueryContext oeContext)
        {
            try
            {
                Table table = oeObject as Table;
                if (table != null && IsPropertySupported("TemporalType", oeContext, table, CachedSmoProperties) && table.TemporalType != TableTemporalType.None)
                {
                    return "Temporal";
                }
                // TODO carbon issue 3125 enable "External" subtype once icon is ready. Otherwise will get missing icon here.
                // else if (table != null && IsPropertySupported("IsExternal", oeContext, table, CachedSmoProperties) && table.IsExternal)
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

        public override string GetNodePathName(object oeObject)
        {
            return TableCustomNodeHelper.GetPathName(oeObject);
        }
    }

    /// <summary>
    /// Custom name for history table
    /// </summary>
    internal partial class TableChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object oeObject, OEQueryContext oeContext)
        {
            Table table = oeObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name} ({SR.History_LabelPart})";
            }

            return string.Empty;
        }

        public override string GetNodePathName(object oeObject)
        {
            return TableCustomNodeHelper.GetPathName(oeObject);
        }
    }

    internal static class TableCustomNodeHelper
    {
        internal static string GetPathName(object oeObject)
        {
            Table table = oeObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name}";
            }

            return string.Empty;
        }
    }
}
