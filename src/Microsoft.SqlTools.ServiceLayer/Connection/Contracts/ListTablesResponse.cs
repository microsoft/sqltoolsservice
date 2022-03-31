//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Message format for the list tables response
    /// </summary>
    public class ListTablesResponse
    {
        /// <summary>
        /// Gets or sets the list of table names.
        /// </summary>
        public string[] TableNames { get; set; }

        /// <summary>
        /// Gets or sets the table details.
        /// </summary>
        public TableInfo[] Tables { get; set; }
    }
}
