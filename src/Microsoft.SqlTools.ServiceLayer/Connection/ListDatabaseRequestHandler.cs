//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

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
        public static IListDatabaseRequestHandler getHandler(bool includeDetails, bool isSqlDB)
        {
            if (!includeDetails)
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
            ConnectionDetails connectionDetails = connectionInfo.ConnectionDetails.Clone();

            using (var connection = connectionFactory.CreateSqlConnection(ConnectionService.BuildConnectionString(connectionDetails), connectionDetails.AzureAccountToken))
            {
                connection.Open();
                ListDatabasesResponse response = new ListDatabasesResponse();
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = this.QueryText;
                    command.CommandTimeout = 15;
                    command.CommandType = CommandType.Text;
                    using (var reader = command.ExecuteReader())
                    {
                        List<T> results = new List<T>();
                        while (reader.Read())
                        {
                            results.Add(this.CreateItem(reader));
                        }
                        // Put system databases at the top of the list
                        results = results.Where(s => SystemDatabases.Any(x => this.NameMatches(x, s))).Concat(
                            results.Where(s => SystemDatabases.All(x => !this.NameMatches(x, s)))).ToList();
                        SetResponse(response, results.ToArray());
                    }
                }
                connection.Close();
                return response;
            }
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
                return @"
WITH
    db_size
    AS
    (
        SELECT database_id, CAST(SUM(size) * 8.0 / 1024 AS INTEGER) size
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