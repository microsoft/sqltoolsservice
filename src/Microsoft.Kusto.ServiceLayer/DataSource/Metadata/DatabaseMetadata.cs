//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : DataSourceObjectMetadata
    {
        public string ClusterName { get; set; }
        
        public string SizeInMB { get; set; }
    }
}