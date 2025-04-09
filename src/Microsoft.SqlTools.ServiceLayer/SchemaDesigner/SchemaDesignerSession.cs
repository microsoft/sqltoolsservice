//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using DacSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession : IDisposable
    {
        private SchemaDesignerModel _initialSchema;
        private string SessionId;
        DacSchemaDesigner schemaDesigner;
        private string connectionString;
        private string? accessToken;

        public SchemaDesignerSession(string connectionString, string? accessToken)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            connectionStringBuilder.ApplicationName = "SchemaDesigner";
            // Set Access Token only when authentication mode is not specified.
            accessToken = connectionStringBuilder.Authentication == SqlAuthenticationMethod.NotSpecified
                ? accessToken : null;
    
            this.connectionString = connectionStringBuilder.ConnectionString;
            this.accessToken = accessToken;
            SessionId = connectionString;
            this._initialSchema = this.createInitialSchema();
        }

        private void CreateOrResetSchemaDesigner()
        {
            schemaDesigner = new DacSchemaDesigner(connectionString, accessToken);
        }

        private SchemaDesignerModel createInitialSchema()
        {
            CreateOrResetSchemaDesigner();
            SchemaDesignerModel schema = new SchemaDesignerModel();
            schema.Tables = new List<SchemaDesignerTable>();
            foreach (var td in schemaDesigner.TableDesigners)
            {
                SchemaDesignerTable table = new SchemaDesignerTable()
                {
                    Id = Guid.NewGuid(),
                    Name = td.TableViewModel.Name,
                    Schema = td.TableViewModel.Schema,
                    Columns = new List<SchemaDesignerColumn>(),
                    ForeignKeys = new List<SchemaDesignerForeignKey>(),
                };

                foreach (var column in td.TableViewModel.Columns.Items)
                {
                    table.Columns.Add(new SchemaDesignerColumn()
                    {
                        Id = Guid.NewGuid(),
                        Name = column.Name,
                        DataType = column.DataType,
                        MaxLength = column.Length,
                        Precision = column.Precision,
                        Scale = column.Scale,
                        IsNullable = column.IsNullable,
                        IsPrimaryKey = column.IsPrimaryKey,
                        IsIdentity = column.IsIdentity,
                        IdentitySeed = column.IdentitySeed,
                        IdentityIncrement = column.IdentityIncrement,
                        DefaultValue = column.DefaultValue,
                    });
                }

                foreach (var fk in td.TableViewModel.ForeignKeys.Items)
                {
                    table.ForeignKeys.Add(new SchemaDesignerForeignKey()
                    {
                        Id = Guid.NewGuid(),
                        Name = fk.Name,
                        Columns = fk.Columns.ToList(),
                        ReferencedColumns = fk.ForeignColumns.ToList(),
                        ReferencedTableName = fk.ForeignTable.Split('.').Last().Replace("]", "").Replace("[", ""),
                        ReferencedSchemaName = fk.ForeignTable.Split('.').First().Replace("]", "").Replace("[", ""),
                        OnDeleteAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnDeleteAction),
                        OnUpdateAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnUpdateAction),
                    });
                }

                schema.Tables.Add(table);
            }
            return schema;
        }

        public List<string> AvailableSchemas()
        {
            // Sort schema and move db_ schemas to the end
            return schemaDesigner.AvailableSchemas
                .OrderBy(s => s.StartsWith("db_") ? 1 : 0)
                .ThenBy(s => s)
                .ToList();
        }

        public List<string> AvailableDataTypes()
        {
            if (schemaDesigner.TableDesigners.Count == 0)
            {
                schemaDesigner.CreateTable("dbo", "dummy");
            }
            var dataTypes = schemaDesigner.TableDesigners.First().DataTypes.OrderBy(x => x).ToList();
            return dataTypes;
        }

        public SchemaDesignerModel InitialSchema
        {
            get { return _initialSchema; }
        }

        public async Task<GetReportResponse> GetReport(SchemaDesignerModel updatedSchema)
        {
            this.CreateOrResetSchemaDesigner();
            return await SchemaDesignerUpdater.GenerateUpdateScripts(_initialSchema, updatedSchema, schemaDesigner);
        }

        public void Dispose()
        {
            TableDesignerCacheManager.InvalidateItem(connectionString);
        }
    }
}