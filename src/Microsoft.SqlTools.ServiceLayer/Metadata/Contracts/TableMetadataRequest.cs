//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public class TableMetadataParams 
    {
        public string OwnerUri { get; set; }        

        public string Schema { get; set; }  

        public string ObjectName { get; set; }  
    }

    public class TableMetadataResult
    {
        public ColumnMetadata[] Columns { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class TableMetadataRequest
    {
        public static readonly
            RequestType<TableMetadataParams, TableMetadataResult> Type =
                RequestType<TableMetadataParams, TableMetadataResult>.Create("metadata/table");
    }
}
