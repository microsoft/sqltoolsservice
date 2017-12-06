//
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
                Table table = smoObject as Table;
                if (table != null && IsPropertySupported("IsSystemVersioned", smoContext, table, CachedSmoProperties) && table.IsSystemVersioned)
                {
                    return $"{table.Schema}.{table.Name} ({SR.SystemVersioned_LabelPart})";
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
                Table table = smoObject as Table;
                if (table != null && IsPropertySupported("TemporalType", smoContext, table, CachedSmoProperties) && table.TemporalType != TableTemporalType.None)
                {
                    return "Temporal";
                }
               // return string.Empty;

            }
            catch
            {
                //Ignore the exception and just not change create custom name
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Custom name for history table
    /// </summary>
    internal partial class TableChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            Table table = smoObject as Table;
            if (table != null)
            {
                return $"{table.Schema}.{table.Name} ({SR.History_LabelPart})";
            }

            return string.Empty;
        }
    }
}
