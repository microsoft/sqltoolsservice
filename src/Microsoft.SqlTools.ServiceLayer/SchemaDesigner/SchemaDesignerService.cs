//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public sealed class SchemaDesignerService : IDisposable
    {
        private static readonly Lazy<SchemaDesignerService> instance = new Lazy<SchemaDesignerService>(() => new SchemaDesignerService());
        private bool disposed = false;
        private IProtocolEndpoint? serviceHost;
        private ConnectionService? connectionService;
        private QueryExecutionService? queryService;

        public static SchemaDesignerService Instance => instance.Value;

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Verbose("Initializing Schema Designer Service");
            this.serviceHost = serviceHost;
            connectionService = ConnectionService.Instance;
            queryService = QueryExecutionService.Instance;
            serviceHost.SetRequestHandler(GetSchemaModelRequest.Type, HandleGetSchemaModelRequest);
            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal async Task HandleGetSchemaModelRequest(GetSchemaModelRequestParams requestParams, RequestContext<SchemaModel> requestContext)
        {
            try
            {
                var schema = new SchemaModel();
                var connectionCompleteParams = await CreateNewConnection(requestParams.OwnerUri, Guid.NewGuid().ToString(), requestParams.DatabaseName);

                var columnQuery = @"
                SELECT 
                    SCHEMA_NAME(t.schema_id) AS SchemaName,
                    t.name AS TableName,
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    c.is_identity AS IsIdentity,
                    CASE 
                        WHEN pk.column_id IS NOT NULL THEN 1 
                        ELSE 0 
                    END AS IsPrimaryKey,
                    CASE 
                        WHEN fk.column_id IS NOT NULL THEN 1 
                        ELSE 0 
                    END AS IsForeignKey
                FROM sys.tables t
                JOIN sys.columns c 
                    ON t.object_id = c.object_id
                JOIN sys.types ty
                    ON c.user_type_id = ty.user_type_id
                LEFT JOIN (
                    -- Get primary key columns
                    SELECT 
                        kc.parent_object_id, 
                        ic.column_id
                    FROM sys.key_constraints kc
                    JOIN sys.index_columns ic 
                        ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                    WHERE kc.type = 'PK'
                ) pk 
                    ON t.object_id = pk.parent_object_id AND c.column_id = pk.column_id
                LEFT JOIN (
                    -- Get foreign key columns
                    SELECT 
                        fk.parent_object_id, 
                        fkc.parent_column_id AS column_id
                    FROM sys.foreign_keys fk
                    JOIN sys.foreign_key_columns fkc 
                        ON fk.object_id = fkc.constraint_object_id
                ) fk 
                    ON t.object_id = fk.parent_object_id AND c.column_id = fk.column_id
                WHERE t.type = 'U'
                ";

                var columnResult = await RunSimpleQuery(connectionCompleteParams, requestParams.DatabaseName, columnQuery) ?? throw new Exception("Failed to get schema information");

                var entityDict = new Dictionary<string, Entity>();
                for (int i = 0; i < columnResult.RowCount; i++)
                {
                    var row = columnResult.GetRow(i);
                    var schemaName = row[0].DisplayValue;
                    var tableName = row[1].DisplayValue;
                    var columnName = row[2].DisplayValue;
                    var dataType = row[3].DisplayValue;
                    var isIdentity = row[4].DisplayValue;
                    var isPrimaryKey = row[5].DisplayValue;
                    var key = $"{schemaName}.{tableName}";
                    if (!entityDict.ContainsKey(key))
                    {
                        entityDict[key] = new Entity
                        {
                            Schema = schemaName,
                            Name = tableName,
                            Columns = new List<Column>()
                        };
                    }
                    entityDict[key].Columns.Add(new Column
                    {
                        Name = columnName,
                        DataType = dataType,
                        IsIdentity = isIdentity == "1",
                        IsPrimaryKey = isPrimaryKey == "1"
                    });
                }

                schema.Entities = new List<Entity>(entityDict.Values);

                var relationshipQuery = @"
                SELECT 
                    fk.name AS ForeignKeyName, 
                    tp.name AS ParentTable, 
                    cp.name AS ParentColumn, 
                    tr.name AS ReferencedTable, 
                    cr.name AS ReferencedColumn, 
                    fk.delete_referential_action_desc AS OnDeleteAction, 
                    fk.update_referential_action_desc AS OnUpdateAction
                FROM sys.foreign_keys fk
                INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
                INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
                INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id";

                var relationshipResult = await RunSimpleQuery(connectionCompleteParams, requestParams.DatabaseName, relationshipQuery) ?? throw new Exception("Failed to get schema information");

                schema.Relationships = new List<Relationship>();

                for (int i = 0; i < relationshipResult.RowCount; i++)
                {
                    var row = relationshipResult.GetRow(i);
                    schema.Relationships.Add(new Relationship
                    {
                        ForeignKeyName = row[0].DisplayValue,
                        Entity = row[1].DisplayValue,
                        Column = row[2].DisplayValue,
                        ReferencedEntity = row[3].DisplayValue,
                        ReferencedColumn = row[4].DisplayValue,
                        OnDeleteAction = MapOnAction(row[5].DisplayValue),
                        OnUpdateAction = MapOnAction(row[6].DisplayValue),
                    });
                }
                await requestContext.SendResult(schema);
                if (connectionCompleteParams != null)
                {
                    this.connectionService.Disconnect(new DisconnectParams()
                    {
                        OwnerUri = connectionCompleteParams.OwnerUri
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        private async Task<ConnectionCompleteParams> CreateNewConnection(string existingConnectionUri, string newConnectionUri, string DatabaseName)
        {
            string randomUri = Guid.NewGuid().ToString();
            // Getting existing connection
            ConnectionInfo connInfo;
            if (!this.connectionService.TryFindConnection(existingConnectionUri, out connInfo))
            {
                throw new Exception(SR.QueryServiceQueryInvalidOwnerUri);
            }
            connInfo.ConnectionDetails.DatabaseName = DatabaseName;

            // Creating new connection to execute query
            ConnectParams newConnectionParams = new ConnectParams
            {
                OwnerUri = randomUri,
                Connection = connInfo.ConnectionDetails,
                Type = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default,
            };
            ConnectionCompleteParams connectionCompleteParams = await this.connectionService.Connect(newConnectionParams);
            if (!string.IsNullOrEmpty(connectionCompleteParams.Messages))
            {
                throw new Exception(connectionCompleteParams.Messages);
            }
            return connectionCompleteParams;
        }


        private async Task<ResultSet> RunSimpleQuery(ConnectionCompleteParams connectionCompleteParams, string DatabaseName, string query)
        {
            ConnectionInfo newConn;
            this.connectionService.TryFindConnection(connectionCompleteParams.OwnerUri, out newConn);

            TaskCompletionSource<ResultSet> taskCompletion = new TaskCompletionSource<ResultSet>();

            ExecuteStringParams executeStringParams = new ExecuteStringParams
            {
                Query = query,
                OwnerUri = connectionCompleteParams.OwnerUri
            };

            // Setting up query execution handlers

            // handle sending error back when query fails to create
            Func<string, Task> queryCreateFailureAction = message =>
            {
                taskCompletion.SetException(new Exception(message));
                return Task.FromResult(0);
            };

            // handle sending event back when the query completes
            Query.QueryAsyncEventHandler queryComplete = async query =>
            {
                try
                {
                    // check to make sure any results were recieved
                    if (query.Batches.Length == 0
                        || query.Batches[0].ResultSets.Count == 0)
                    {
                        taskCompletion.SetException(new Exception(SR.QueryServiceResultSetHasNoResults));
                        return;
                    }

                    long rowCount = query.Batches[0].ResultSets[0].RowCount;
                    // check to make sure there is a safe amount of rows to load into memory
                    if (rowCount > Int32.MaxValue)
                    {
                        taskCompletion.SetException(new Exception(SR.QueryServiceResultSetTooLarge));
                        return;
                    }
                    taskCompletion.SetResult(query.Batches[0].ResultSets[0]);
                }
                catch (Exception e)
                {
                    taskCompletion.SetException(e);
                }
            };

            // handle sending error back when query fails
            Query.QueryAsyncErrorEventHandler queryFail = async (q, e) =>
            {
                taskCompletion.SetException(e);
                return;
            };

            await queryService.InterServiceExecuteQuery(
                executeStringParams,
                newConn,
                new SchemaDesignerQueryExecutionEventSender(taskCompletion),
                null,
                queryCreateFailureAction,
                queryComplete,
                queryFail
            );

            return await taskCompletion.Task;
        }

        internal async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                ConnectionCompleteParams result = await this.connectionService.Connect(connectParams);
                connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}" : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    throw new Exception(connectionErrorMessage);
                }

            }
            catch (Exception ex)
            {
                int? errorNum = ex is SqlException sqlEx ? sqlEx.Number : null;
                return null;
            }
        }

        internal static OnAction MapOnAction(string action)
        {
            return action switch
            {
                "CASCADE" => OnAction.CASACADE,
                "NO_ACTION" => OnAction.NO_ACTION,
                "SET_NULL" => OnAction.SET_NULL,
                "SET_DEFAULT" => OnAction.SET_DEFAULT,
                _ => OnAction.NO_ACTION
            };
        }


        public class SchemaDesignerQueryExecutionEventSender : IEventSender
        {
            private readonly TaskCompletionSource<ResultSet> TaskCompletion;

            public SchemaDesignerQueryExecutionEventSender(TaskCompletionSource<ResultSet> taskCompletion)
            {
                TaskCompletion = taskCompletion;
            }

            public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
            {
                return Task.FromResult(true);
            }

            public Task SendError(string errorMessage, int errorCode = 0)
            {
                TaskCompletion.SetException(new Exception(errorMessage));
                return Task.FromResult(0);
            }
        }

    }
}