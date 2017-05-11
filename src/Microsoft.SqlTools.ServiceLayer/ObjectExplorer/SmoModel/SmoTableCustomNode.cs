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
            // TODO: this  code makes expanding the tables slow because of loading the IsSystemVersioned property for each table.
            // Have to uncomment this after optimizing the way properties are loaded for SMO objects 
            //Table table = smoObject as Table;
            //if (table != null && table.IsSystemVersioned)
            //{
            //    return $"{table.Schema}.{table.Name} ({SR.SystemVersioned_LabelPart})";
            //}

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
