//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession3
    {
        private readonly string connectionString;
        private TSqlModel clonedModel;
        public SchemaDesignerModel schema;
        public DacServices dacServices;
        public string databaseName;
        public string sessionId;
        SchemaCompareDatabaseEndpoint targetDatabase;
        ConnectionInfo connectionInfo;
        TableDesignerManager tableDesignerManager = new TableDesignerManager();

        public SchemaDesignerSession3(string connectionString, string? accessToken, string databaseName, ConnectionInfo connectionInfo, string sessionId)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
            this.connectionInfo = connectionInfo;
            this.sessionId = sessionId;
            if (!accessToken.IsNullOrEmpty())
            {
                dacServices = new DacServices(connectionString, new AccessTokenProvider(accessToken));
                clonedModel = TSqlModel.LoadFromDatabaseWithAuthProvider(connectionString, new AccessTokenProvider(accessToken));
                targetDatabase = new SchemaCompareDatabaseEndpoint(connectionString, new AccessTokenProvider(accessToken));
            }
            else
            {
                dacServices = new DacServices(connectionString);
                clonedModel = TSqlModel.LoadFromDatabase(connectionString);
                targetDatabase = new SchemaCompareDatabaseEndpoint(connectionString);
            }
            var tables = new List<SchemaDesignerTable>();

            var _ = Task.Run(() =>
                {
                    try
                    {
                        var builder = ConnectionService.CreateConnectionStringBuilder(connectionInfo.ConnectionDetails);
                        builder.InitialCatalog = databaseName;
                        builder.ApplicationName = TableDesignerManager.TableDesignerApplicationNameSuffix;
                        // Set Access Token only when authentication mode is not specified.
                        var azureToken = builder.Authentication == SqlAuthenticationMethod.NotSpecified
                            ? connectionInfo.ConnectionDetails.AzureAccountToken : null;
                        TableDesignerCacheManager.StartDatabaseModelInitialization(builder.ToString(), azureToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to start database initialization for table designer: {ex.Message}");
                    }
                });

            foreach (TSqlObject table in clonedModel.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                TSqlObject schema = table.GetReferenced(Table.Schema).ToList()[0];
                List<TSqlObject> columns = table.GetReferenced(Table.Columns).ToList();
                IEnumerable<TSqlObject> foreignKeys = table.GetReferencing(ForeignKeyConstraint.Host, DacQueryScopes.UserDefined);
                tables.Add(new SchemaDesignerTable()
                {
                    Name = table.Name.Parts[1],
                    Schema = schema.Name.Parts[0],
                    Columns = columns.Select(c =>
                    {
                        string dataType = "";
                        if (c.GetReferenced(SqlServer.Dac.Model.Column.DataType).ToList().Count != 0)
                        {
                            dataType = c.GetReferenced(SqlServer.Dac.Model.Column.DataType).ToList()[0].Name.Parts[0];
                        }
                        return new SchemaDesignerColumn()
                        {
                            Name = c.Name.Parts[2],
                            DataType = dataType,
                            IsIdentity = c.GetProperty<bool>(SqlServer.Dac.Model.Column.IsIdentity),
                            IsPrimaryKey = c.GetReferencing(PrimaryKeyConstraint.Columns, DacQueryScopes.UserDefined).ToList().Count != 0
                        };
                    }).ToList(),
                    ForeignKeys = foreignKeys.Select(fk =>
                    {
                        var foreignKey = new SchemaDesignerForeignKey()
                        {
                            Name = fk.Name.Parts.Count != 0 ? fk.Name.Parts[1] : "",
                            Columns = fk.GetReferenced(ForeignKeyConstraint.Columns).ToList().Select(f => f.Name.Parts[2]).ToList(),
                            ReferencedColumns = fk.GetReferenced(ForeignKeyConstraint.ForeignColumns).ToList().Select(f => f.Name.Parts[2]).ToList(),
                            ReferencedTableName = fk.GetReferenced(ForeignKeyConstraint.ForeignTable).ToList()[0].Name.Parts[1],
                            ReferencedSchemaName = fk.GetReferenced(ForeignKeyConstraint.ForeignTable).ToList()[0].GetReferenced(Table.Schema).ToList()[0].Name.Parts[0],
                            OnDeleteAction = ConvertForeingKeyActionToOnAction(fk.GetProperty<ForeignKeyAction>(ForeignKeyConstraint.DeleteAction)),
                            OnUpdateAction = ConvertForeingKeyActionToOnAction(fk.GetProperty<ForeignKeyAction>(ForeignKeyConstraint.UpdateAction))
                        };
                        return foreignKey;
                    }).ToList()
                });
            }
            schema = new SchemaDesignerModel()
            {
                Tables = tables,
            };
        }

        public string GetCode()
        {
            string code = "";

            var tables = new List<SchemaDesignerTable>();

            foreach (TSqlObject table in clonedModel.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass))
            {
                string script = table.GetScript(); // Gets the T-SQL definition
                code += $"-- Object: {table.Name}" + "\n";
                code += script + "\n";
                code += new string('-', 80) + "\n";
            }
            return code;
        }

        /// <summary>
        /// Updates the schema model with the new model 
        public void UpdateModel(SchemaDesignerModel modifiedModel)
        {
            // Let's update the tables names first.

            List<Guid> newTableIds = new List<Guid>();

            foreach (var table in modifiedModel.Tables)
            {
                SchemaDesignerTable? oldTable = schema.Tables.FirstOrDefault(t => t.Id == table.Id);
                if (oldTable == null)
                {
                    // new table found
                    newTableIds.Add(table.Id);
                }
            }
        }



        private OnAction ConvertForeingKeyActionToOnAction(ForeignKeyAction action)
        {
            switch (action)
            {
                case ForeignKeyAction.NoAction:
                    return OnAction.NO_ACTION;
                case ForeignKeyAction.Cascade:
                    return OnAction.CASCADE;
                case ForeignKeyAction.SetDefault:
                    return OnAction.SET_DEFAULT;
                case ForeignKeyAction.SetNull:
                    return OnAction.SET_NULL;
                default:
                    return OnAction.NO_ACTION;
            }
        }

        public string GenerateChangeReport()
        {
            this.CreateNewTable(new SchemaDesignerTable()
            {
                Name = "NewTable",
                Schema = "dbo",
                Columns = new List<SchemaDesignerColumn>()
                {
                    new SchemaDesignerColumn()
                    {
                        Name = "Id",
                        DataType = "int",
                        IsIdentity = true,
                        IsPrimaryKey = true
                    },
                    new SchemaDesignerColumn()
                    {
                        Name = "Name",
                        DataType = "nvarchar(50)",
                        IsIdentity = false,
                        IsPrimaryKey = false
                    }
                },
                ForeignKeys = new List<SchemaDesignerForeignKey>()
                {
                    new SchemaDesignerForeignKey()
                    {
                        Name = "FK_NewTable_OldTable",
                        Columns = new List<string>() { "Id" },
                        ReferencedColumns = new List<string>() { "CustomerId" },
                        ReferencedTableName = "Customers",
                        ReferencedSchemaName = "Demo",
                        OnDeleteAction = OnAction.CASCADE,
                        OnUpdateAction = OnAction.NO_ACTION
                    }
                }
            });

            // modify column name to be not null and add unique
            this.clonedModel.AddOrUpdateObjects("ALTER TABLE [dbo].[OldTable] ALTER COLUMN [Name] NVARCHAR(50) NOT NULL", "dbo.OldTable.Name", new TSqlObjectOptions()
            {
                AnsiNulls = true,
                QuotedIdentifier = true
            });

            // drop column payment method from table payments
            this.clonedModel.AddOrUpdateObjects("ALTER TABLE [dbo].[Payments] DROP COLUMN [PaymentMethod]", "dbo.Payments.PaymentMethod", new TSqlObjectOptions()
            {
                AnsiNulls = true,
                QuotedIdentifier = true,
            });

            string fileName = "cloned" + this.sessionId + ".dacpac";
            if (System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }
            DacPackageExtensions.BuildPackage(fileName, clonedModel, new PackageMetadata());

            SchemaCompareDacpacEndpoint modifiedSchema = new SchemaCompareDacpacEndpoint(fileName);

            SchemaComparison result = new SchemaComparison(modifiedSchema, targetDatabase);
            SchemaComparisonResult comparisonResult = result.Compare();
            SchemaCompareScriptGenerationResult report = comparisonResult.GenerateScript(databaseName);
            string rs = report.Message + "\n" + report.Script;
            return rs;
        }

        private void CreateNewTable(SchemaDesignerTable newTable)
        {
            // Build T-SQL script for the new table
            string script = $"CREATE TABLE [{newTable.Schema}].[{newTable.Name}] (\n";

            // Add columns
            List<string> columnDefinitions = new List<string>();
            List<string> primaryKeyColumns = new List<string>();

            foreach (var column in newTable.Columns)
            {
                string columnDef = $"    [{column.Name}] {column.DataType}";

                // Handle identity
                if (column.IsIdentity)
                {
                    columnDef += " IDENTITY(1,1)";
                }

                // // Handle nullable
                // columnDef += column.IsNullable ? " NULL" : " NOT NULL";

                // Track primary key columns
                if (column.IsPrimaryKey)
                {
                    primaryKeyColumns.Add(column.Name);
                }

                columnDefinitions.Add(columnDef);
            }

            // Add primary key constraint if needed
            if (primaryKeyColumns.Count > 0)
            {
                string pkColumnList = string.Join(", ", primaryKeyColumns.Select(c => $"[{c}]"));
                columnDefinitions.Add($"    CONSTRAINT [PK_{newTable.Name}] PRIMARY KEY ({pkColumnList})");
            }

            script += string.Join(",\n", columnDefinitions);
            script += "\n)";

            // Add the table to the model
            clonedModel.AddOrUpdateObjects(script, $"{newTable.Schema}.{newTable.Name}", new TSqlObjectOptions()
            {
                AnsiNulls = true,
                QuotedIdentifier = true
            });

            // Process foreign keys separately (they need the table to exist first)
            foreach (var foreignKey in newTable.ForeignKeys)
            {
                AddForeignKey(newTable, foreignKey);
            }
        }

        /// <summary>
        /// Adds a foreign key to a table
        /// </summary>
        private void AddForeignKey(SchemaDesignerTable table, SchemaDesignerForeignKey foreignKey)
        {
            // Column lists
            string columnList = string.Join(", ", foreignKey.Columns.Select(c => $"[{c}]"));
            string refColumnList = string.Join(", ", foreignKey.ReferencedColumns.Select(c => $"[{c}]"));

            // Handle ON DELETE and ON UPDATE actions
            string onDelete = "";
            string onUpdate = "";

            if (foreignKey.OnDeleteAction != OnAction.NO_ACTION)
            {
                onDelete = $" ON DELETE {ConvertOnActionToSql(foreignKey.OnDeleteAction)}";
            }

            if (foreignKey.OnUpdateAction != OnAction.NO_ACTION)
            {
                onUpdate = $" ON UPDATE {ConvertOnActionToSql(foreignKey.OnUpdateAction)}";
            }

            // Generate FK name if empty
            string fkName = !string.IsNullOrEmpty(foreignKey.Name)
                ? foreignKey.Name
                : $"FK_{table.Name}_{foreignKey.ReferencedTableName}";

            // Create the foreign key script
            string script = $"ALTER TABLE [{table.Schema}].[{table.Name}] " +
                           $"ADD CONSTRAINT [{fkName}] " +
                           $"FOREIGN KEY ({columnList}) " +
                           $"REFERENCES [{foreignKey.ReferencedSchemaName}].[{foreignKey.ReferencedTableName}] ({refColumnList})" +
                           $"{onDelete}{onUpdate}";

            clonedModel.AddObjects(script);

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
    }
}