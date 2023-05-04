//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public enum QueryResultType{
        DatabaseTableInfo
    }

    /// <summary>
    /// Parameters to run query on a database
    /// </summary>
    public class RunQueryParams 
    {
        /// <summary>
        /// Owner URI
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// List of databases to run queries on
        /// </summary>
        public string[] Databases { get; set; }

        /// <summary>
        /// Is connection to azure sql db
        /// </summary>
        public bool isAzureSqlDb { get; set; }

        /// <summary>
        /// Query result type
        /// </summary>
        public QueryResultType queryResultType { get; set; }
    }

    /// <summary>
    /// Run given query on databases in the connection defined by the connection string
    /// </summary>
    public class RunQueryEvent
    {
        public static readonly
            EventType<RunQueryParams> Type =
                EventType<RunQueryParams>.Create("migration/runquery");
    }

    /// <summary>
    /// Send Result for query on a database
    /// </summary>
    public class RunQueryResultEvent
    {
        public static readonly
            EventType<IQueryResult> Type =
                EventType<IQueryResult>.Create("migration/runqueryresult");
    }

        /// <summary>
    /// Send Result for query on a database
    /// </summary>
    public class RunQueryDatabaseTableInfoResultEvent
    {
        public static readonly
            EventType<DatabaseTableInfoDatabaseResult> Type =
                EventType<DatabaseTableInfoDatabaseResult>.Create("migration/runqueryresult");
    }

    /// <summary>
    /// Send Result for query on a database
    /// </summary>
    public class RunQueryErrorEvent
    {
        public static readonly
            EventType<RunQueryError> Type =
                EventType<RunQueryError>.Create("migration/runqueryerror");
    }

    /// <summary>
    /// Send Result for query on a database
    /// </summary>
    public class RunQueryError
    {
        public string databaseName { get; set; }

        public string error { get; set; }

        public bool isAzureSqlDb { get; set; }
    }

    public interface IQueryResult
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string databaseName { get; set; }

        /// <summary>
        /// Error if available
        /// </summary>
        public bool isAzureSqlDb { get; set; }

        /// <summary>
        /// Generic query result reader async
        /// </summary>
        Task ReadQueryResultAsync(SqlDataReader reader);
    }

    /// <summary>
    /// Result of query for table info of a database
    /// </summary>
    public class DatabaseTableInfoDatabaseResult : IQueryResult
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string databaseName { get; set; }

        /// <summary>
        /// Error if available
        /// </summary>
        public bool isAzureSqlDb { get; set; }

        /// <summary>
        /// List of table info for tables in a database
        /// </summary>
        public List<TableInfo> databaseTableInfo { get; set; }

        /// <summary>
        /// Read table info result from query
        /// </summary>
        public async Task ReadQueryResultAsync(SqlDataReader reader)
        {
            this.databaseTableInfo = new List<TableInfo>();
            while (await reader.ReadAsync())
            {
                this.databaseTableInfo.Add(new TableInfo{
                    databaseName = reader.GetString(0),
                    tableName = reader.GetString(1),
                    rowCount = reader.GetInt64(2)
                });
            }
        }
    }

    /// <summary>
    /// Info for a single table in a database
    /// </summary>
    public class TableInfo
    {    
        /// <summary>
        /// Database name
        /// </summary>
        public string databaseName { get; set; }

        /// <summary>
        /// Table name
        /// </summary>
        public string tableName { get; set; }

        /// <summary>
        /// Table row count
        /// </summary>
        public long rowCount { get; set; }
    }
}
