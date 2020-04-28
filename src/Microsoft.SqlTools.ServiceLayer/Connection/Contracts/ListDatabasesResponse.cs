//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Database detail
    /// </summary>
    public abstract class DatabaseDetailBase
    {
        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the database state.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the database size.
        /// </summary>
        public string SizeInMB { get; set; }
    }

    public class SqlDBDatabaseDetail : DatabaseDetailBase
    {
    }

    public class SqlServerDatabaseDetail : DatabaseDetailBase
    {
        /// <summary>
        /// Gets or sets the database last backup date.
        /// </summary>
        public string LastBackup { get; set; }
    }

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
        public DatabaseDetailBase[] Databases { get; set; }
    }
}
