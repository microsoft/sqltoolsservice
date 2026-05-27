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
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.SqlCore.Utility;

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
        // When set, takes precedence over the static accessToken so DacFx can refresh tokens on demand
        // (e.g. when ConnectionInfo.AzureTokenFetcher is available for RequestMfaTokenFromClient mode).
        private Func<string>? accessTokenCallback;
        private SqlTask? publishSqlTask;

        public event EventHandler<SchemaDesignerProgressNotificationParams>? ProgressChanged;
        public event EventHandler<SchemaDesignerMessageNotificationParams>? MessageReceived;
        public string ServerName { get; }
        public string DatabaseName { get; }

        public SchemaDesignerSession(
            string sessionId,
            string connectionString,
            string? accessToken,
            EventHandler<SchemaDesignerProgressNotificationParams>? progressHandler = null,
            EventHandler<SchemaDesignerMessageNotificationParams>? messageHandler = null)
            : this(sessionId, connectionString, accessToken, accessTokenCallback: null, progressHandler, messageHandler)
        {
        }

        public SchemaDesignerSession(
            string sessionId,
            string connectionString,
            string? accessToken,
            Func<string>? accessTokenCallback,
            EventHandler<SchemaDesignerProgressNotificationParams>? progressHandler = null,
            EventHandler<SchemaDesignerMessageNotificationParams>? messageHandler = null)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            connectionStringBuilder.ApplicationName = "SchemaDesigner";
            // Access tokens / token callbacks are only meaningful when no explicit authentication
            // method is set on the connection string.
            bool useAccessToken = connectionStringBuilder.Authentication == SqlAuthenticationMethod.NotSpecified;
            accessToken = useAccessToken ? accessToken : null;
            this.accessTokenCallback = useAccessToken ? accessTokenCallback : null;

            this.connectionString = connectionStringBuilder.ConnectionString;
            this.accessToken = accessToken;
            SessionId = sessionId;
            ServerName = connectionStringBuilder.DataSource;
            DatabaseName = connectionStringBuilder.InitialCatalog;
            if (progressHandler != null)
            {
                ProgressChanged += progressHandler;
            }
            if (messageHandler != null)
            {
                MessageReceived += messageHandler;
            }
            this._lastRequestSchema = null!;
            this.schemaDesigner = null!;
            this._initialSchema = this.createInitialSchema();
        }

        private void CreateOrResetSchemaDesigner()
        {
            // Prefer the callback-backed auth provider so DacFx can refresh tokens on demand.
            schemaDesigner = accessTokenCallback != null
                ? new DacSchemaDesigner(connectionString, new AccessTokenProvider(accessTokenCallback))
                : new DacSchemaDesigner(connectionString, accessToken);
            schemaDesigner.ProgressChanged += OnDesignerProgressChanged;
            schemaDesigner.Message += OnDesignerMessage;
        }

        private SchemaDesignerModel createInitialSchema()
        {
            TableDesignerCacheManager.InvalidateItem(connectionString);
            CreateOrResetSchemaDesigner();
            try
            {
                schemaDesigner.Initialize();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
            var simpleSchema = schemaDesigner.SimpleSchema;
            SchemaDesignerModel schema = new SchemaDesignerModel();
            schema.Tables = new List<SchemaDesignerTable>();

            // First pass: create all tables and columns so table/column IDs are available for FK projection.
            for (int i = 0; i < simpleSchema.Tables.Count; i++)
            {
                var table = simpleSchema.Tables[i];
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
                schema.Tables.Add(schemaTable);
            }

            // Second pass: project foreign keys using table/column identifiers.
            for (int i = 0; i < simpleSchema.Tables.Count; i++)
            {
                var sourceTable = simpleSchema.Tables[i];
                var sourceSchemaTable = schema.Tables[i];

                foreach (var fk in sourceTable.ForeignKeys)
                {
                    var referencedTable = schema.Tables.FirstOrDefault(t =>
                        string.Equals(t.Schema, fk.ReferencedTableSchema, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(t.Name, fk.ReferencedTableName, StringComparison.OrdinalIgnoreCase));

                    sourceSchemaTable.ForeignKeys!.Add(new SchemaDesignerForeignKey()
                    {
                        Id = Guid.NewGuid(),
                        Name = fk.Name,
                        ColumnsIds = fk.Columns
                            ?.Select(columnName => sourceSchemaTable.Columns!
                                .FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase))?
                                .Id.ToString())
                            .Where(columnId => !string.IsNullOrEmpty(columnId))
                            .OfType<string>()
                            .ToList(),
                        ReferencedColumnsIds = fk.ReferencedColumns
                            ?.Select(columnName => referencedTable?.Columns!
                                .FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase))?
                                .Id.ToString())
                            .Where(columnId => !string.IsNullOrEmpty(columnId))
                            .OfType<string>()
                            .ToList(),
                        ReferencedTableId = referencedTable?.Id.ToString(),
                        OnDeleteAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnDeleteAction),
                        OnUpdateAction = SchemaDesignerUtils.ConvertSqlForeignKeyActionToOnAction(fk.OnUpdateAction),
                    });
                }
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

        public void PublishSchema(SqlTask? sqlTask = null)
        {
            publishSqlTask = sqlTask;
            try
            {
                schemaDesigner.PublishChanges();
                this._initialSchema = this._lastRequestSchema;
            }
            finally
            {
                publishSqlTask = null;
            }
        }

        public void Dispose()
        {
            TableDesignerCacheManager.InvalidateItem(connectionString);
        }

        private void OnDesignerProgressChanged(object? sender, DesignerProgressEventArgs args)
        {
            var progressParams = new SchemaDesignerProgressNotificationParams()
            {
                SessionId = SessionId,
                Operation = args.Operation.ToString(),
                Status = args.Status.ToString(),
                Message = args.Message,
            };

            ProgressChanged?.Invoke(this, progressParams);

            if (args.Operation == DesignerOperation.Publish && publishSqlTask != null)
            {
                publishSqlTask.ReportProgress(GetTaskPercent(args.Status), args.Message);
            }
        }

        private void OnDesignerMessage(object? sender, DesignerMessageEventArgs args)
        {
            var messageParams = new SchemaDesignerMessageNotificationParams()
            {
                SessionId = SessionId,
                Operation = args.Operation.ToString(),
                MessageType = args.MessageType.ToString(),
                Message = args.Message,
                Number = args.Number,
                Prefix = args.Prefix,
                Progress = args.Progress,
                SchemaName = args.SchemaName,
                TableName = args.TableName,
            };

            MessageReceived?.Invoke(this, messageParams);

            if (args.Operation == DesignerOperation.Publish && publishSqlTask != null)
            {
                if (!string.IsNullOrWhiteSpace(args.Message))
                {
                    publishSqlTask.AddMessage(
                        args.Message,
                        args.MessageType == Microsoft.SqlServer.Dac.DacMessageType.Error ? SqlTaskStatus.Failed : SqlTaskStatus.InProgress);
                }

                if (args.Progress.HasValue)
                {
                    publishSqlTask.ReportProgress(
                        Math.Min(100, Math.Max(0, (int)Math.Round(args.Progress.Value * 100))),
                        args.Message);
                }
            }
        }

        private static int GetTaskPercent(Microsoft.SqlServer.Dac.DacOperationStatus status)
        {
            return status == Microsoft.SqlServer.Dac.DacOperationStatus.Completed ? 100 : -1;
        }
    }
}
