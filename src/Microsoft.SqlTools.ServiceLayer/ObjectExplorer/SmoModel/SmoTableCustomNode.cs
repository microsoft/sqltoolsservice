﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Custom name for table
    /// </summary>
    internal partial class TablesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            try
            {
                Table? table = smoObject as Table;
                if (table != null && IsPropertySupported("LedgerType", smoContext, table, CachedSmoProperties))
                {
                    if (table.LedgerType == LedgerTableType.AppendOnlyLedgerTable)
                    {
                        return $"{table.Schema}.{table.Name} ({SR.AppendOnlyLedger_LabelPart})";
                    }
                    else if (table.LedgerType == LedgerTableType.UpdatableLedgerTable)
                    {
                        return $"{table.Schema}.{table.Name} ({SR.UpdatableLedger_LabelPart})";
                    }
                }
                if (table != null && IsPropertySupported("IsSystemVersioned", smoContext, table, CachedSmoProperties) && table.IsSystemVersioned)
                {
                    return $"{table.Schema}.{table.Name} ({SR.SystemVersioned_LabelPart})";
                }
                else if (table != null && IsPropertySupported("IsExternal", smoContext, table, CachedSmoProperties) && table.IsExternal)
                {
                    return $"{table.Schema}.{table.Name} ({SR.External_LabelPart})";
                }
                else if (table != null && IsPropertySupported("IsFileTable", smoContext, table, CachedSmoProperties) && table.IsFileTable)
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

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            try
            {
                Table? table = smoObject as Table;
                if (table != null && IsPropertySupported("LedgerType", smoContext, table, CachedSmoProperties) &&
                    (table.LedgerType == LedgerTableType.AppendOnlyLedgerTable || table.LedgerType == LedgerTableType.UpdatableLedgerTable))
                {
                    return "Ledger";
                }
                if (table != null && IsPropertySupported("TemporalType", smoContext, table, CachedSmoProperties) && table.TemporalType != TableTemporalType.None)
                {
                    return "Temporal";
                }
                if (table != null && IsPropertySupported("IsEdge", smoContext, table, CachedSmoProperties) && table.IsEdge)
                {
                    return "GraphEdge";
                }
                if (table != null && IsPropertySupported("IsNode", smoContext, table, CachedSmoProperties) && table.IsNode)
                {
                    return "GraphNode";
                }
                // TODO carbon issue 3125 enable "External" subtype once icon is ready. Otherwise will get missing icon here.
                // else if (table != null && IsPropertySupported("IsExternal", smoContext, table, CachedSmoProperties) && table.IsExternal)
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
    /// Custom name and icon for history table
    /// </summary>
    internal partial class TableChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            Table? table = smoObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name} ({SR.History_LabelPart})";
            }

            return string.Empty;
        }

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            try
            {
                Table? table = smoObject as Table;
                if (table != null && IsPropertySupported("LedgerType", smoContext, table, CachedSmoProperties) &&
                    table.LedgerType == LedgerTableType.HistoryTable)
                {
                    return "LedgerHistory";
                }
            }
            catch { }

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
            Table? table = smoObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name}";
            }

            return string.Empty;
        }
    }


    /// <summary>
    /// Custom name and icon for dropped ledger tables
    /// </summary>
    internal partial class DroppedLedgerTablesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            try
            {
                Table? table = smoObject as Table;
                if (table != null && IsPropertySupported("LedgerType", smoContext, table, CachedSmoProperties))
                {
                    if (table.LedgerType == LedgerTableType.AppendOnlyLedgerTable)
                    {
                        return $"{table.Schema}.{table.Name} ({SR.AppendOnlyLedger_LabelPart})";
                    }
                    else if (table.LedgerType == LedgerTableType.UpdatableLedgerTable)
                    {
                        return $"{table.Schema}.{table.Name} ({SR.UpdatableLedger_LabelPart})";
                    }
                }
            }
            catch
            {
                //Ignore the exception and just not change create custom name
            }

            return string.Empty;
        }

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return "Ledger";
        }

        public override string GetNodePathName(object smoObject)
        {
            return TableCustomNodeHelper.GetPathName(smoObject);
        }
    }

}
