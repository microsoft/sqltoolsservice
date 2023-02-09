//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public class ListDatabasesRequestDatabaseProperties
    {
        public const string Name = "name";
        public const string SizeInMB = "sizeInMB";
        public const string State = "state";
        public const string LastBackup = "lastBackup";
    }

    /// <summary>
    /// Factory class for ListDatabasesRequest handler
    /// </summary>
    static class ListDatabaseRequestHandlerFactory
    {
        public static IListDatabaseRequestHandler getHandler(bool includeDetails, bool isSqlDB, ConnectionInfo connectionInfo = null)
        {
            if (!includeDetails || connectionInfo?.EngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                return new DatabaseNamesHandler();
            }
            else if (isSqlDB)
            {
                return new SqlDBDatabaseDetailHandler();
            }
            else
            {
                return new SqlServerDatabaseDetailHandler();
            }
        }
    }

    /// <summary>
    /// Interface of ListDatabasesRequest handler
    /// </summary>
    interface IListDatabaseRequestHandler
    {
        ListDatabasesResponse HandleRequest(ISqlConnectionFactory connectionFactory, ConnectionInfo connectionInfo);
    }

    /// <summary>
    /// Base handler
    /// </summary>
    abstract class ListDatabaseRequestHandler<T> : IListDatabaseRequestHandler
    {
        private static readonly string[] SystemDatabases = new string[] { "master", "model", "msdb", "tempdb" };

        public abstract string QueryText { get; }

        public ListDatabasesResponse HandleRequest(ISqlConnectionFactory connectionFactory, ConnectionInfo connectionInfo)
        {
            ListDatabasesResponse response = new ListDatabasesResponse();
            ConnectionDetails connectionDetails = connectionInfo.ConnectionDetails.Clone();
            // Running query against sys.databases view will only return a subset of databases the current login/user might have access to, we need to
            // query the master database to get the full database list, but for users without master db access, we have to query the
            // original database as a fallback.
            var databasesToTry = new List<string>() { CommonConstants.MasterDatabaseName };
            if (connectionDetails.DatabaseName != CommonConstants.MasterDatabaseName)
            {
                databasesToTry.Add(connectionDetails.DatabaseName);
            }
            for (int i = 0; i < databasesToTry.Count; i++)
            {
                try
                {
                    connectionDetails.DatabaseName = databasesToTry[i];
                    var results = this.GetResults(connectionFactory, connectionDetails);
                    SetResponse(response, results);
                    break;
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    // Retry when login attempt failed.
                    // https://learn.microsoft.com/sql/relational-databases/errors-events/mssqlserver-18456-database-engine-error
                    if (i != databasesToTry.Count - 1 && ex.Number == 18456)
                    {
                        Logger.Write(TraceEventType.Information, string.Format("Failed to get database list from database '{0}', will fallback to original database.", databasesToTry[i]));
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }

            }
            return response;
        }

        private T[] GetResults(ISqlConnectionFactory connectionFactory, ConnectionDetails connectionDetails)
        {
            List<T> results = new List<T>();
            using (var connection = connectionFactory.CreateSqlConnection(ConnectionService.BuildConnectionString(connectionDetails), connectionDetails.AzureAccountToken))
            {
                connection.Open();
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = this.QueryText;
                    command.CommandTimeout = 15;
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(this.CreateItem(reader));
                        }
                        // Put system databases at the top of the list
                        results = results.Where(s => SystemDatabases.Any(x => this.NameMatches(x, s))).Concat(
                            results.Where(s => SystemDatabases.All(x => !this.NameMatches(x, s)))).ToList();
                    }
                }
                connection.Close();
            }
            return results.ToArray();
        }

        protected abstract bool NameMatches(string databaseName, T item);

        protected abstract T CreateItem(DbDataReader reader);

        protected abstract void SetResponse(ListDatabasesResponse response, T[] results);
    }

    /// <summary>
    /// database names handler
    /// </summary>
    class DatabaseNamesHandler : ListDatabaseRequestHandler<string>
    {
        public override string QueryText
        {
            get
            {
                return @"SELECT name FROM sys.databases WHERE state_desc='ONLINE' ORDER BY name ASC";
            }
        }

        protected override string CreateItem(DbDataReader reader)
        {
            return reader[0].ToString();
        }

        protected override bool NameMatches(string databaseName, string item)
        {
            return databaseName == item;
        }

        protected override void SetResponse(ListDatabasesResponse response, string[] results)
        {
            response.DatabaseNames = results;
        }
    }


    abstract class BaseDatabaseDetailHandler : ListDatabaseRequestHandler<DatabaseInfo>
    {
        protected override bool NameMatches(string databaseName, DatabaseInfo item)
        {
            return databaseName == item.Options[ListDatabasesRequestDatabaseProperties.Name].ToString();
        }

        protected override void SetResponse(ListDatabasesResponse response, DatabaseInfo[] results)
        {
            response.Databases = results;
        }

        protected override DatabaseInfo CreateItem(DbDataReader reader)
        {
            DatabaseInfo databaseInfo = new DatabaseInfo();
            SetProperties(reader, databaseInfo);
            return databaseInfo;
        }

        protected virtual void SetProperties(DbDataReader reader, DatabaseInfo databaseInfo)
        {
            databaseInfo.Options[ListDatabasesRequestDatabaseProperties.Name] = reader["name"].ToString();
            databaseInfo.Options[ListDatabasesRequestDatabaseProperties.State] = reader["state"].ToString();
            databaseInfo.Options[ListDatabasesRequestDatabaseProperties.SizeInMB] = reader["size"].ToString();
        }
    }

    /// <summary>
    /// Standalone SQL Server database detail handler
    /// </summary>
    class SqlServerDatabaseDetailHandler : BaseDatabaseDetailHandler
    {
        public override string QueryText
        {
            get
            {
                // NOTES: Converting the size to BIGINT is need to handle the large database scenarios.
                // size column in sys.master_files represents the number of pages and each page is 8 KB
                // The end result is size in MB.
                return @"
WITH
    db_size
    AS
    (
        SELECT database_id, CAST(SUM(CAST(size AS BIGINT)) * 8.0 / 1024 AS BIGINT) size
        FROM sys.master_files
        GROUP BY database_id
    ),
    db_backup
    AS
    (
        SELECT database_name, MAX(backup_start_date) AS last_backup
        FROM msdb..backupset
        GROUP BY database_name
    )
SELECT name, state_desc AS state, db_size.size, db_backup.last_backup
FROM sys.databases LEFT JOIN db_size ON sys.databases.database_id = db_size.database_id
LEFT JOIN db_backup ON sys.databases.name = db_backup.database_name
WHERE state_desc='ONLINE'
ORDER BY name ASC";
            }
        }

        protected override void SetProperties(DbDataReader reader, DatabaseInfo databaseInfo)
        {
            base.SetProperties(reader, databaseInfo);
            databaseInfo.Options[ListDatabasesRequestDatabaseProperties.LastBackup] = reader["last_backup"] == DBNull.Value ? "" : Convert.ToDateTime(reader["last_backup"]).ToString("yyyy-MM-dd hh:mm:ss");
        }
    }

    /// <summary>
    /// SQL DB database detail handler
    /// </summary>
    class SqlDBDatabaseDetailHandler : BaseDatabaseDetailHandler
    {
        public override string QueryText
        {
            get
            {
                return @"
WITH
    db_size
    AS
    (
        SELECT name, storage_in_megabytes AS size
        FROM (
SELECT database_name name, max(end_time) size_time
            FROM sys.resource_stats
            GROUP BY database_name) db_size_time
            LEFT JOIN sys.resource_stats ON database_name = name AND size_time = end_time
    )
SELECT db.name, state_desc AS state, size
FROM sys.databases db LEFT JOIN db_size ON db.name = db_size.name
WHERE state_desc='ONLINE'
ORDER BY name ASC
";
            }
        }
    }
}