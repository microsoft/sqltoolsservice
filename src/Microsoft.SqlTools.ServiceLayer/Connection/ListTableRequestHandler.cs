//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public class ListTablesRequestTableProperties
    {
        public const string Name = "name";
        public const string SizeInMB = "sizeInMB";
        public const string State = "state";
        public const string LastBackup = "lastBackup";
    }

    /// <summary>
    /// Factory class for ListTablesRequest handler
    /// </summary>
    static class ListTableRequestHandlerFactory
    {
        public static IListTableRequestHandler getHandler(bool includeDetails, bool isSqlDB, ConnectionInfo connectionInfo = null)
        {
            if (!includeDetails || connectionInfo?.EngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                return new TableNamesHandler();
            }
            else if (isSqlDB)
            {
                return new SqlDBTableDetailHandler();
            }
            else
            {
                return new SqlServerTableDetailHandler();
            }
        }
    }

    /// <summary>
    /// Interface of ListTablesRequest handler
    /// </summary>
    interface IListTableRequestHandler
    {
        ListTablesResponse HandleRequest(ISqlConnectionFactory connectionFactory, ConnectionInfo connectionInfo, String database);
    }

    /// <summary>
    /// Base handler
    /// </summary>
    abstract class ListTableRequestHandler<T> : IListTableRequestHandler
    {
        private static readonly string[] SystemTables = new string[] { "master", "model", "msdb", "tempdb" };

        public abstract string QueryText { get; }

        public ListTablesResponse HandleRequest(ISqlConnectionFactory connectionFactory, ConnectionInfo connectionInfo, String database)
        {
            ConnectionDetails connectionDetails = connectionInfo.ConnectionDetails.Clone();

            // Connect to master
            connectionDetails.DatabaseName = "master";
            using (var connection = connectionFactory.CreateSqlConnection(ConnectionService.BuildConnectionString(connectionDetails), connectionDetails.AzureAccountToken))
            {
                connection.Open();
                ListTablesResponse response = new ListTablesResponse();
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
                        // Put system Tables at the top of the list
                        results = results.Where(s => SystemTables.Any(x => this.NameMatches(x, s))).Concat(
                            results.Where(s => SystemTables.All(x => !this.NameMatches(x, s)))).ToList();
                        SetResponse(response, results.ToArray());
                    }
                }
                connection.Close();
                return response;
            }
        }

        protected abstract bool NameMatches(string TableName, T item);
        protected abstract T CreateItem(DbDataReader reader);
        protected abstract void SetResponse(ListTablesResponse response, T[] results);
    }

    /// <summary>
    /// Table names handler
    /// </summary>
    class TableNamesHandler : ListTableRequestHandler<string>
    {
        public override string QueryText
        {
            get
            {
                return @"SELECT name FROM sys.Tables WHERE state_desc='ONLINE' ORDER BY name ASC";
            }
        }

        protected override string CreateItem(DbDataReader reader)
        {
            return reader[0].ToString();
        }

        protected override bool NameMatches(string TableName, string item)
        {
            return TableName == item;
        }

        protected override void SetResponse(ListTablesResponse response, string[] results)
        {
            response.TableNames = results;
        }
    }


    abstract class BaseTableDetailHandler : ListTableRequestHandler<TableInfo>
    {
        protected override bool NameMatches(string TableName, TableInfo item)
        {
            return TableName == item.Options[ListTablesRequestTableProperties.Name].ToString();
        }

        protected override void SetResponse(ListTablesResponse response, TableInfo[] results)
        {
            response.Tables = results;
        }

        protected override TableInfo CreateItem(DbDataReader reader)
        {
            TableInfo TableInfo = new TableInfo();
            SetProperties(reader, TableInfo);
            return TableInfo;
        }

        protected virtual void SetProperties(DbDataReader reader, TableInfo TableInfo)
        {
            TableInfo.Options[ListTablesRequestTableProperties.Name] = reader["name"].ToString();
            TableInfo.Options[ListTablesRequestTableProperties.State] = reader["state"].ToString();
            TableInfo.Options[ListTablesRequestTableProperties.SizeInMB] = reader["size"].ToString();
        }
    }

    /// <summary>
    /// Standalone SQL Server Table detail handler
    /// </summary>
    class SqlServerTableDetailHandler : BaseTableDetailHandler
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
        SELECT Table_id, CAST(SUM(size) * 8.0 / 1024 AS INTEGER) size
        FROM sys.master_files
        GROUP BY Table_id
    ),
    db_backup
    AS
    (
        SELECT Table_name, MAX(backup_start_date) AS last_backup
        FROM msdb..backupset
        GROUP BY Table_name
    )
SELECT name, state_desc AS state, db_size.size, db_backup.last_backup
FROM sys.Tables LEFT JOIN db_size ON sys.Tables.Table_id = db_size.Table_id
LEFT JOIN db_backup ON sys.Tables.name = db_backup.Table_name
WHERE state_desc='ONLINE'
ORDER BY name ASC";
            }
        }

        protected override void SetProperties(DbDataReader reader, TableInfo TableInfo)
        {
            base.SetProperties(reader, TableInfo);
            TableInfo.Options[ListTablesRequestTableProperties.LastBackup] = reader["last_backup"] == DBNull.Value ? "" : Convert.ToDateTime(reader["last_backup"]).ToString("yyyy-MM-dd hh:mm:ss");
        }
    }

    /// <summary>
    /// SQL DB Table detail handler
    /// </summary>
    class SqlDBTableDetailHandler : BaseTableDetailHandler
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
SELECT Table_name name, max(end_time) size_time
            FROM sys.resource_stats
            GROUP BY Table_name) db_size_time
            LEFT JOIN sys.resource_stats ON Table_name = name AND size_time = end_time
    )
SELECT db.name, state_desc AS state, size
FROM sys.Tables db LEFT JOIN db_size ON db.name = db_size.name
WHERE state_desc='ONLINE'
ORDER BY name ASC
";
            }
        }
    }
}