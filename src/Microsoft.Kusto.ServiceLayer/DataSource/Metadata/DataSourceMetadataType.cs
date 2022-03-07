//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Metadata type enumeration
    /// </summary>
    public enum DataSourceMetadataType
    {
        Cluster = 0,
        Database = 1,
        Table = 2,
        Column = 3,
        Function = 4,
        Folder = 5
    }
}