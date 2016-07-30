//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Factory class to create SqlClientConnections
    /// The purpose of the factory is to make it easier to mock out the database
    /// in 'offline' unit test scenarios.
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        /// <summary>
        /// Creates a new SqlClientConnection object
        /// </summary>
        public ISqlConnection CreateSqlConnection()
        {
            return new SqlClientConnection();
        }
    }

    /// <summary>
    /// Wrapper class that implements ISqlConnection and hosts a SqlConnection.
    /// This wrapper exists primarily for decoupling to support unit testing.
    /// </summary>
    public class SqlClientConnection : ISqlConnection
    {
        /// <summary>
        /// the underlying SQL connection
        /// </summary>
        private SqlConnection connection;

        /// <summary>
        /// Opens a SqlConnection using provided connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public void OpenDatabaseConnection(string connectionString)
        {
            this.connection = new SqlConnection(connectionString);
            this.connection.Open();
        }

        /// <summary>
        /// Gets a list of database server schema objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetServerObjects()
        {
            // Select the values from sys.tables to give a super basic
            // autocomplete experience.  This will be replaced by SMO.
            SqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.tables";
            command.CommandTimeout = 15;
            command.CommandType = CommandType.Text;
            var reader = command.ExecuteReader();

            List<string> results = new List<string>();
            while (reader.Read())
            {
                results.Add(reader[0].ToString());
            }

            return results;
        }
    }
}
