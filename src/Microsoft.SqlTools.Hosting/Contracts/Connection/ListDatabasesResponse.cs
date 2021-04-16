//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts.Admin;

namespace Microsoft.SqlTools.Hosting.Contracts.Connection
{
    /// <summary>
    /// Message format for the list databases response
    /// </summary>
    public class ListDatabasesResponse
    {
        /// <summary>
        /// Gets or sets the list of database names.
        /// </summary>
        public string[] DatabaseNames { get; set; }

        /// <summary>
        /// Gets or sets the databases details.
        /// </summary>
        public DatabaseInfo[] Databases { get; set; }
    }
}
