//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.EditorServices.Connection
{
    /// <summary>
    /// Interface for the SQL Connection factory
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Create a new SQL Connection object
        /// </summary>
        ISqlConnection CreateSqlConnection();
    }

    /// <summary>
    /// Interface for the SQL Connection wrapper
    /// </summary>
    public interface ISqlConnection
    {
        /// <summary>
        /// Open a connection to the provided connection string
        /// </summary>
        /// <param name="connectionString"></param>
        void OpenDatabaseConnection(string connectionString);

        IEnumerable<string> GetServerObjects();
    }
}
