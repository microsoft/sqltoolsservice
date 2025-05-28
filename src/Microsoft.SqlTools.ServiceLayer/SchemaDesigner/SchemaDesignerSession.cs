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
        private SchemaDesignerModel _lastRequestSchema;
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
            for (int i = 0; i < schemaDesigner.SimpleSchema.Tables.Count; i++)
            {
                var table = schemaDesigner.SimpleSchema.Tables[i];
                SchemaDesignerTable schemaTable = new SchemaDesignerTable()
                {
                    Id = Guid.NewGuid(),
                    Name = table.Name,
                    Schema = table.SchemaName,
                    Columns = new List<SchemaDesignerColumn>(),
                    ForeignKeys = new List<SchemaDesignerForeignKey>(),
                };

                for (int j = 0; j < table.Columns.Count; j++)
                {
                    var column = table.Columns[j];
                    schemaTable.Columns.Add(new SchemaDesignerColumn()
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
                        IsComputed = column.IsComputed,
                        ComputedFormula = column.ComputedFormula,
                        ComputedPersisted = column.ComputedPersisted,
                    });
                }

                foreach (var fk in table.ForeignKeys)
                {
                    schemaTable.ForeignKeys.Add(new SchemaDesignerForeignKey()
                    {
                        Id = Guid.NewGuid(),
                        Name = fk.Name,
                        Columns = fk.Columns,
                        ReferencedColumns = fk.ReferencedColumns,
                        ReferencedTableName = fk.ReferencedTableName,
                        ReferencedSchemaName = fk.ReferencedTableSchema,
                        OnDeleteAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnDeleteAction),
                        OnUpdateAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnUpdateAction),
                    });
                }

                schema.Tables.Add(schemaTable);
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
            var report = await SchemaDesignerUpdater.GenerateUpdateScripts(_initialSchema, updatedSchema, schemaDesigner);
            this._lastRequestSchema = updatedSchema;
            return report;
        }

        public async Task<string> GenerateScript()
        {
            return await Task.Run(() =>
            {
                return schemaDesigner.GenerateScript();
            });
        }

        public void PublishSchema()
        {
            schemaDesigner.PublishChanges();
            this._initialSchema = this._lastRequestSchema;
        }

        public void Dispose()
        {
            TableDesignerCacheManager.InvalidateItem(connectionString);
        }
    }
}