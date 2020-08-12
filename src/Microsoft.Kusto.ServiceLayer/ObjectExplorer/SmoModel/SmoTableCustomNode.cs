//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;


// TODOKusto: Remove this file. These classes might not be needed.
namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Custom name for table
    /// </summary>
    internal partial class TablesChildFactory : DataSourceChildFactoryBase
    {
        // TODOKusto: If we are always passing DataSourceMetadataObject, stop passing object. Make it type safe.
        public override string GetNodePathName(object objectMetadata)
        {
            return base.GetNodePathName(objectMetadata);
        }
    }

    /// <summary>
    /// Custom name for history table
    /// </summary>
    internal partial class TableChildFactory : DataSourceChildFactoryBase
    {
        public override string GetNodePathName(object objectMetadata)
        {
            return base.GetNodePathName(objectMetadata);
        }
    }
}
