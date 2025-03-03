//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.SchemaDesigner;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;

namespace Microsoft.SqlToolsServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession2
    {
        private SchemaModel schema;
        private TableDesignerManager tableDesignerManager = new TableDesignerManager();
        private ConnectionInfo connectionInfo;
        private string connectionString;
        private string? accessToken;
        private string databaseName;
        private string sessionId;

        public SchemaDesignerSession2(ConnectionInfo connectionInfo, SchemaModel initialSchema)
        {
            var builder = ConnectionService.CreateConnectionStringBuilder(connectionInfo.ConnectionDetails);
            builder.ApplicationName = TableDesignerManager.TableDesignerApplicationNameSuffix;
            this.connectionString = builder.ConnectionString;
            // Set Access Token only when authentication mode is not specified.
            this.accessToken = builder.Authentication == Data.SqlClient.SqlAuthenticationMethod.NotSpecified
                ? connectionInfo.ConnectionDetails.AzureAccountToken : null;
            TableDesignerCacheManager.StartDatabaseModelInitialization(connectionString, accessToken);
            this.schema = initialSchema;
            this.LoadTableDesignersForInitialSchema();
        }

        public void LoadTableDesignersForInitialSchema()
        {
            foreach (var table in this.schema.Tables)
            {
                TableInfo tableInfo = CreateTableInfo(table);
                tableDesignerManager.InitializeTableDesigner(tableInfo);
            }
        }

        private TableInfo CreateTableInfo(ITable table)
        {
            // If the table is present in initial schema, then it is a new table
            // and we need to set the IsNewTable property to true
            bool isNewTable = this.schema.Tables.Find(t => t.Id == table.Id) != null;

            TableInfo tableInfo = new TableInfo()
            {
                AccessToken = this.accessToken,
                ConnectionString = this.connectionString,
                Database = this.databaseName,
                Name = table.Name,
                Schema = table.Schema,
                Server = connectionInfo.ConnectionDetails.ServerName,
                Tooltip = $"{connectionInfo.ConnectionDetails.ServerName} - {databaseName} - {table.Name}",
                IsNewTable = isNewTable,
                Id = table.Id.ToString(),
            };
            return tableInfo;
        }

        private Di

        public string GetCreateAsScriptForTable(ITable table)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table.Name}] (");

            List<string> columnDefinitions = new List<string>();
            List<string> primaryKeys = new List<string>();
            List<string> foreignKeys = new List<string>();

            foreach (var column in table.Columns)
            {
                StringBuilder columnDef = new StringBuilder();
                columnDef.Append($"[{column.Name}] {column.DataType}");

                if (!column.IsNullable)
                    columnDef.Append(" NOT NULL");

                // TODO: Add default value
                // if (!string.IsNullOrEmpty(column.DefaultValue))
                //     columnDef.Append($" DEFAULT {column.DefaultValue}");

                if (column.IsPrimaryKey)
                    primaryKeys.Add(column.Name);

                columnDefinitions.Add(columnDef.ToString());
            }

            // Handle Primary Key constraint
            if (primaryKeys.Count > 0)
            {
                columnDefinitions.Add($"CONSTRAINT [PK_{table.Name}] PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }


            // Handle Foreign Keys
            int fkIndex = 1;
            foreach (var fk in table.ForeignKeys)
            {
                List<string> localColumns = new List<string>();
                List<string> referencedColumns = new List<string>();

                for (int i = 0; i < fk.Columns.Count; i++)
                {
                    var localColumn = fk.Columns[i];
                    var ReferencedColumn = fk.ReferencedColumns[i];
                    localColumns.Add($"[{localColumn}]");
                    referencedColumns.Add($"[{referencedColumns}]");
                }

                string onDelete = fk.OnDeleteAction != null ? $" ON DELETE {ConvertOnActionToSql(fk.OnDeleteAction)}" : "";
                string onUpdate = fk.OnUpdateAction != null ? $" ON UPDATE {ConvertOnActionToSql(fk.OnUpdateAction)}" : "";

                foreignKeys.Add(
                    $"CONSTRAINT [FK_{table.Name}_{fkIndex}] FOREIGN KEY ({string.Join(", ", localColumns)}) " +
                    $"REFERENCES [{fk.ReferencedTableName}] ({string.Join(", ", referencedColumns)}){onDelete}{onUpdate}"
                );
                fkIndex++;
            }

            columnDefinitions.AddRange(foreignKeys);
            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");");

            return sb.ToString();
        }

        /// <summary>
        /// Converts OnAction enum to T-SQL action text
        /// </summary>
        private string ConvertOnActionToSql(OnAction action)
        {
            switch (action)
            {
                case OnAction.CASCADE:
                    return "CASCADE";
                case OnAction.SET_NULL:
                    return "SET NULL";
                case OnAction.SET_DEFAULT:
                    return "SET DEFAULT";
                case OnAction.NO_ACTION:
                default:
                    return "NO ACTION";
            }
        }

        public void CloseSession()
        {
            if (this.schema != null)
            {
                foreach (var table in this.schema.Tables)
                {
                    tableDesignerManager.DisposeTableDesigner(
                        CreateTableInfo(table)
                    );
                }
            }
        }
    }
}