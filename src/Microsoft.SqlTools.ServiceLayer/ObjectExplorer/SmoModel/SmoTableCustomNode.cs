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
            Table table = smoObject as Table;
            if (table != null && table.IsSystemVersioned)
            {
                return $"{table.Name} ({SR.SystemVersioned_LabelPart})";
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
                return $"{table.Name} ({SR.History_LabelPart})";
            }

            return string.Empty;
        }

        public override string GetNodeSubType(object context)
        {
            Table table = context as Table;

            if (table != null)
            {
                return "History";
            }
            return string.Empty;
        }
    }
}
