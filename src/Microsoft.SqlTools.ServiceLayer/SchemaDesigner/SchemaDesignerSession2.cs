//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using TableViewModel = Microsoft.SqlTools.SqlCore.TableDesigner.Contracts.TableViewModel;
using TableColumnViewModel = Microsoft.SqlTools.SqlCore.TableDesigner.Contracts.TableColumnViewModel;
using ForeignKeyViewModel = Microsoft.SqlTools.SqlCore.TableDesigner.Contracts.ForeignKeyViewModel;
// using Microsoft.SqlServer.Dac.Compare;
// using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession2
    {
        private SchemaDesignerModel schema;
        private TableDesignerManager tableDesignerManager = new TableDesignerManager();
        private ConnectionInfo connectionInfo;
        private string connectionString;
        private string? accessToken;
        private string databaseName;
        private string sessionId;
        // SchemaCompareDatabaseEndpoint targetDatabase;
        // private TSqlModel clonedModel;


        public SchemaDesignerSession2(string sessionId, SchemaDesignerModel initialSchema)
        {
            ConnectionInfo newConn;
            ConnectionService.Instance.TryFindConnection(sessionId, out newConn);
            var builder = ConnectionService.CreateConnectionStringBuilder(newConn.ConnectionDetails);
            builder.ApplicationName = TableDesignerManager.TableDesignerApplicationNameSuffix;
            this.connectionString = builder.ConnectionString;
            // Set Access Token only when authentication mode is not specified.
            this.accessToken = builder.Authentication == Data.SqlClient.SqlAuthenticationMethod.NotSpecified
                ? newConn.ConnectionDetails.AzureAccountToken : null;
            this.schema = initialSchema;
            this.connectionInfo = newConn;
            this.databaseName = newConn.ConnectionDetails.DatabaseName;
            this.accessToken = newConn.ConnectionDetails.AzureAccountToken;
            this.sessionId = sessionId;
            TableDesignerCacheManager.StartDatabaseModelInitialization(connectionString, accessToken);
            LoadTableDesignersForInitialSchema();
            // if (this.accessToken.IsNullOrEmpty())
            // {
            //     targetDatabase = new SchemaCompareDatabaseEndpoint(connectionString);
            //     clonedModel = TSqlModel.LoadFromDatabaseWithAuthProvider(connectionString, new AccessTokenProvider(accessToken));

            // }
            // else
            // {
            //     targetDatabase = new SchemaCompareDatabaseEndpoint(connectionString, new AccessTokenProvider(accessToken));
            //     clonedModel = TSqlModel.LoadFromDatabase(connectionString);
            // }
        }

        /// <summary>
        /// Loads table designers for the initial schema
        /// </summary>
        public void LoadTableDesignersForInitialSchema()
        {
            // TODO: Get a proper load notification from TableDesignerCacheManager
            // Make sure at least one table is loaded.
            if (this.schema.Tables.Count == 0)
            {
                return;
            }
            SchemaDesignerTable firstTable = this.schema.Tables[0];
            TableInfo firstTableInfo = CreateTableInfo(firstTable);
            tableDesignerManager.InitializeTableDesigner(firstTableInfo);
            tableDesignerManager.DisposeTableDesigner(firstTableInfo);
        }

        private TableInfo CreateTableInfo(SchemaDesignerTable table)
        {
            // If the table is present in initial schema, then it is a new table
            // and we need to set the IsNewTable property to true
            bool isNewTable = true;
            SchemaDesignerTable? oldTable = this.schema.Tables.Find(t => t.Id.ToString() == table.Id.ToString());

            isNewTable = oldTable == null;

            TableInfo tableInfo;
            if (isNewTable)
            {
                tableInfo = new TableInfo()
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
            }
            else
            {
                tableInfo = new TableInfo()
                {
                    AccessToken = this.accessToken,
                    ConnectionString = this.connectionString,
                    Database = this.databaseName,
                    Name = oldTable.Name,
                    Schema = oldTable.Schema,
                    Server = connectionInfo.ConnectionDetails.ServerName,
                    Tooltip = $"{connectionInfo.ConnectionDetails.ServerName} - {databaseName} - {oldTable.Name}",
                    IsNewTable = isNewTable,
                    Id = table.Id.ToString(),
                };
            }

            return tableInfo;
        }

        public async Task<List<SchemaDesignerReportObject>> GetReport(SchemaDesignerModel modifiedSchema)
        {
            List<SchemaDesignerReportObject> response = SchemaDesignerUpdater.GenerateUpdateScripts(this.schema, modifiedSchema);
            // Initialize the table designer for each table in the schema
            return response;
        }

        /// <summary>
        /// Closes the session and disposes all table designers
        /// </summary>
        public void CloseSession()
        {
            if (this.schema != null)
            {
                foreach (var table in this.schema.Tables)
                {
                    if (tableDesignerManager.IsTableDesignerSessionActive(table.Id.ToString()))
                    {
                        tableDesignerManager.DisposeTableDesigner(
                            CreateTableInfo(table)
                        );
                    }
                }
            }
        }

        public class TableDesignerEditUtils
        {
            private TableDesignerManager tableDesignerManager;
            private TableInfo tableInfo;
            public TableViewModel lastViewModel;

            public static string Name = "name";
            public static string Schema = "schema";
            public static string Columns = "columns";
            public static string type = "type";
            public static string IsPrimaryKey = "isPrimaryKey";
            public static string IsIdentity = "isIdentity";
            public static string AllowNulls = "allowNulls";
            public static string ForeignKeys = "foreignKeys";
            public static string ForeignTable = "foreignTable";
            public static string Column = "column";
            public static string ForeignColumn = "foreignColumn";
            public static string OnDelete = "onDelete";
            public static string OnUpdate = "onUpdate";

            public TableDesignerEditUtils(TableDesignerManager tableDesignerManager, TableInfo tableInfo, TableViewModel viewModel)
            {
                this.tableDesignerManager = tableDesignerManager;
                this.tableInfo = tableInfo;
                this.lastViewModel = viewModel;
            }

            /// <summary>
            /// Perform a table designer edit operation and update the lastViewModel
            /// </summary>
            /// <param name="requestParams"></param>
            public void PerformTableDesignerEdit(ProcessTableDesignerEditRequestParams requestParams)
            {
                lastViewModel = tableDesignerManager.TableDesignerEdit(requestParams).ViewModel;
            }

            /// <summary>
            /// Process a new table
            /// </summary>
            /// <param name="table"></param>
            public void ProcessNewTable(SchemaDesignerTable table)
            {
                // Remove the first default column
                this.RemoveFirstDefaultColumn();

                // Update the table properties
                this.UpdateTableProperties(table);

                // Add all columns
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    this.AddNewColumn(table.Columns[i], i);
                }

                // Add all foreign keys
                for (int i = 0; i < table.ForeignKeys.Count; i++)
                {
                    this.AddForeignKey(table.ForeignKeys[i], i);
                }
            }

            /// <summary>
            /// Update the properties of the table
            /// </summary>
            /// <param name="table"></param>
            public void UpdateTableProperties(SchemaDesignerTable table)
            {
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Name },
                        Type = DesignerEditType.Update,
                        Value = table.Name
                    }
                });

                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Schema },
                        Type = DesignerEditType.Update,
                        Value = table.Schema
                    }
                });
            }

            /// <summary>
            /// Remove the first default column created by the table designer. Used when creating a new table
            /// </summary>
            public void RemoveFirstDefaultColumn()
            {
                this.RemoveColumnAt(0);
            }

            /// <summary>
            /// Remove a column at the specified index
            /// </summary>
            /// <param name="index"></param>
            public void RemoveColumnAt(int index)
            {
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString() },
                        Type = DesignerEditType.Remove,
                    }
                });
            }

            /// <summary>
            /// Add a new column to the table
            /// </summary>
            public void AddNewColumn(SchemaDesignerColumn column, int? index)
            {
                // If index is not specified, add the column at the end
                index ??= lastViewModel.Columns.Data.Count;

                // Add a new column at the specified index
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString() },
                        Type = DesignerEditType.Add,
                    }
                });

                this.UpdateColumn(column, index.Value);
            }

            public void AddNewColumn(SchemaDesignerColumn column, TableColumnViewModel? columnViewModel, int? index)
            {

                // Add a new column at the specified index
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString() },
                        Type = DesignerEditType.Add,
                    }
                });

                this.UpdateColumn(column, index.Value);

                // IF the type is not changed
                if (column.DataType == columnViewModel.Type.Value)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "advancedType" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.AdvancedType.Value
                        }
                    });

                    if (columnViewModel.Length.Enabled && columnViewModel.Length.Value != null)
                    {
                        PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                        {
                            TableInfo = this.tableInfo,
                            TableChangeInfo = new TableDesignerChangeInfo()
                            {
                                Path = new object[] { Columns, index.ToString(), "length" },
                                Type = DesignerEditType.Update,
                                Value = columnViewModel.Length.Value
                            }
                        });
                    }

                    if (columnViewModel.Scale.Enabled && columnViewModel.Scale.Value != null)
                    {
                        PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                        {
                            TableInfo = this.tableInfo,
                            TableChangeInfo = new TableDesignerChangeInfo()
                            {
                                Path = new object[] { Columns, index.ToString(), "scale" },
                                Type = DesignerEditType.Update,
                                Value = columnViewModel.Scale.Value
                            }
                        });
                    }

                    if (columnViewModel.Precision.Enabled && columnViewModel.Precision.Value != null)
                    {
                        PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                        {
                            TableInfo = this.tableInfo,
                            TableChangeInfo = new TableDesignerChangeInfo()
                            {
                                Path = new object[] { Columns, index.ToString(), "precision" },
                                Type = DesignerEditType.Update,
                                Value = columnViewModel.Precision.Value
                            }
                        });
                    }
                }

                if (columnViewModel.AllowNulls.Enabled)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "allowNulls" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.AllowNulls.Checked
                        }
                    });
                }

                if (columnViewModel.IsPrimaryKey.Enabled)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "isPrimaryKey" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IsPrimaryKey.Checked
                        }
                    });
                }

                if (columnViewModel.IsIdentity.Enabled)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "isIdentity" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IsIdentity.Checked
                        }
                    });
                }

                //identitySeed
                if (columnViewModel.IdentitySeed.Enabled && columnViewModel.IdentitySeed.Value != null)
                {
                    // If the column is identity, then set the identitySeed property
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "identitySeed" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IdentitySeed.Value
                        }
                    });
                }

                //identityIncrement
                if (columnViewModel.IdentityIncrement.Enabled && columnViewModel.IdentityIncrement.Value != null)
                {
                    // If the column is identity, then set the identityIncrement property
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "identityIncrement" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IdentityIncrement.Value
                        }
                    });
                }

                //isComputed
                if (columnViewModel.IsComputed.Enabled)
                {
                    // If the column is computed, then set the isComputed property
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "isComputed" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IsComputed.Checked
                        }
                    });
                }

                //computedFormula
                if (columnViewModel.ComputedFormula.Enabled && columnViewModel.ComputedFormula.Value != null)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "computedFormula" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.ComputedFormula.Value
                        }
                    });
                }

                //isComputedPersisted
                if (columnViewModel.IsComputedPersisted.Enabled)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "isComputedPersisted" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IsComputedPersisted.Checked
                        }
                    });
                }

                //isComputedPersistedNullable
                if (columnViewModel.IsComputedPersistedNullable.Enabled)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "isComputedPersistedNullable" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.IsComputedPersistedNullable.Checked
                        }
                    });
                }

                //defaultConstraintName
                if (columnViewModel.DefaultConstraintName.Enabled && columnViewModel.DefaultConstraintName.Value != null)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { Columns, index.ToString(), "defaultConstraintName" },
                            Type = DesignerEditType.Update,
                            Value = columnViewModel.DefaultConstraintName.Value
                        }
                    });
                }
            }

            /// <summary>
            /// Update the column at the specified index
            /// </summary>
            /// <param name="column"></param>
            /// <param name="index"></param>
            public void UpdateColumn(SchemaDesignerColumn column, int index)
            {
                // Set the name of the column
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString(), Name },
                        Type = DesignerEditType.Update,
                        Value = column.Name
                    }
                });

                // Set the type of the column
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString(), type },
                        Type = DesignerEditType.Update,
                        Value = column.DataType
                    }
                });

                // Set the primary key of the column
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString(), IsPrimaryKey },
                        Type = DesignerEditType.Update,
                        Value = column.IsPrimaryKey
                    }
                });
            }

            /// <summary>
            /// Delete a column at the specified index
            /// </summary>
            /// <param name="index"></param>
            public void DeleteColumn(int index)
            {
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { Columns, index.ToString() },
                        Type = DesignerEditType.Remove,
                    }
                });
            }

            /// <summary>
            /// Add a foreign key to the table
            /// </summary>
            public void AddForeignKey(SchemaDesignerForeignKey foreignKey, int? index)
            {
                // If index is not specified, add the foreign key at the end
                index ??= lastViewModel.ForeignKeys.Data.Count;

                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString() },
                        Type = DesignerEditType.Add,
                    }
                });

                this.UpdateForeignKeys(foreignKey, index.Value);
            }

            public void AddForeignKey(SchemaDesignerForeignKey foreignKey, ForeignKeyViewModel? foreignKeyViewModel, int? index)
            {
                // If index is not specified, add the foreign key at the end
                index ??= lastViewModel.ForeignKeys.Data.Count;

                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString() },
                        Type = DesignerEditType.Add,
                    }
                });

                this.UpdateForeignKeys(foreignKey, index.Value);

                // Set the onDelete of the foreign key
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), "onDeleteAction" },
                        Type = DesignerEditType.Update,
                        Value = foreignKeyViewModel.OnDeleteAction.Value
                    }
                });

                // Set the onUpdate of the foreign key
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), "onUpdateAction" },
                        Type = DesignerEditType.Update,
                        Value = foreignKeyViewModel.OnUpdateAction.Value
                    }
                });

                // Set the IsNotForReplication of the foreign key
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), "isNotForReplication" },
                        Type = DesignerEditType.Update,
                        Value = foreignKeyViewModel.IsNotForReplication.Checked
                    }
                });

                // Enable the foreign key
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), "enabled" },
                        Type = DesignerEditType.Update,
                        Value = foreignKeyViewModel.Enabled.Checked
                    }
                });
            }

            /// <summary>
            /// Update the foreign keys at the specified index
            /// </summary>
            public void UpdateForeignKeys(SchemaDesignerForeignKey foreignKey, int index)
            {
                // Set the foreign table of the foreign key
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), Name },
                        Type = DesignerEditType.Update,
                        Value = foreignKey.Name
                    }
                });

                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString(), ForeignTable },
                        Type = DesignerEditType.Update,
                        Value = $"{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}"
                    }
                });

                // Set the column of the foreign key
                for (int i = 0; i < foreignKey.Columns.Count; i++)
                {
                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { ForeignKeys, index.ToString(), Columns, i.ToString() },
                            Type = DesignerEditType.Add,
                        }
                    });

                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { ForeignKeys, index.ToString(), Columns, i.ToString(), Column },
                            Type = DesignerEditType.Update,
                            Value = foreignKey.Columns[i]
                        }
                    });

                    PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                    {
                        TableInfo = this.tableInfo,
                        TableChangeInfo = new TableDesignerChangeInfo()
                        {
                            Path = new object[] { ForeignKeys, index.ToString(), Columns, i.ToString(), ForeignColumn },
                            Type = DesignerEditType.Update,
                            Value = foreignKey.ReferencedColumns[i]
                        }
                    });
                }
            }

            public void DeleteForeignKey(int index)
            {
                PerformTableDesignerEdit(new ProcessTableDesignerEditRequestParams()
                {
                    TableInfo = this.tableInfo,
                    TableChangeInfo = new TableDesignerChangeInfo()
                    {
                        Path = new object[] { ForeignKeys, index.ToString() },
                        Type = DesignerEditType.Remove,
                    }
                });
            }
        }
    }
}