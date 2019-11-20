//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Metadata.Contracts
{
    /// <summary>
    /// Retreive metadata for the view described in the TableMetadataParams value.
    /// This message reuses the table metadata params and result since the exchanged
    /// data is the same.
    /// </summary>
    public class ViewMetadataRequest
    {
        public static readonly
            RequestType<TableMetadataParams, TableMetadataResult> Type =
                RequestType<TableMetadataParams, TableMetadataResult>.Create("metadata/view");
    }
}
