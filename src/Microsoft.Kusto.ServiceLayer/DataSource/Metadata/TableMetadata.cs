//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Database metadata information
    /// </summary>
    public class TableMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
        public string Folder { get; set; }
    }
}