//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public sealed class SchemaDesignerService : IDisposable
    {
        private static readonly Lazy<SchemaDesignerService> instance = new Lazy<SchemaDesignerService>(() => new SchemaDesignerService());
        private bool disposed = false;
        public static SchemaDesignerService Instance => instance.Value;

        private IProtocolEndpoint serviceHost;
        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(needsMetadata: false);
        private ConnectionService connectionService;
        private QueryExecutionService queryService;

        public SchemaDesignerService()
        {
        }

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
        }


        internal async Task HandleGetSchemaModelRequest(GetSchemaModelRequestParams requestParams, RequestContext<SchemaModel> requestContext)
        {
            try
            {
                var schema = new SchemaModel();
                var columnQuery = @"
                SELECT 
                    t.TABLE_SCHEMA, t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE,
                    COLUMNPROPERTY(OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
                    CASE WHEN kcu.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
                FROM INFORMATION_SCHEMA.TABLES t
                JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
                    ON t.TABLE_NAME = kcu.TABLE_NAME AND t.TABLE_SCHEMA = kcu.TABLE_SCHEMA AND c.COLUMN_NAME = kcu.COLUMN_NAME
                WHERE t.TABLE_TYPE = 'BASE TABLE'";

                var columnResult = await RunSimpleQuery(requestParams.OwnerUri, requestParams.DatabaseName, columnQuery) ?? throw new Exception("Failed to get schema information");

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

                var relationshipResult = await RunSimpleQuery(requestParams.OwnerUri, requestParams.DatabaseName, relationshipQuery);

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
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        private async Task<ResultSet> RunSimpleQuery(string connectionUri, string DatabaseName, string query, RequestContext<Object> requestContext = null)
        {

            TaskCompletionSource<ResultSet> taskCompletion =
                new TaskCompletionSource<ResultSet>();

            string randomUri = Guid.NewGuid().ToString();
            ExecuteStringParams executeStringParams = new ExecuteStringParams
            {
                Query = query,
                // generate guid as the owner uri to make sure every query is unique
                OwnerUri = randomUri
            };

            // get connection
            ConnectionInfo connInfo;
            if (!this.connectionService.TryFindConnection(connectionUri, out connInfo))
            {
                taskCompletion.SetException(new Exception(SR.QueryServiceQueryInvalidOwnerUri));
            }

            ConnectParams connectParams = new ConnectParams
            {
                OwnerUri = randomUri,
                Connection = connInfo.ConnectionDetails,
                Type = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default,
            };

            await this.connectionService.Connect(connectParams);

            ConnectionCompleteParams connectionCompleteParams = await this.connectionService.Connect(connectParams);
            if (!string.IsNullOrEmpty(connectionCompleteParams.Messages))
            {
                throw new Exception(connectionCompleteParams.Messages);
            }

            // Get Connection
           
            ConnectionInfo newConn;
            this.connectionService.TryFindConnection(randomUri, out newConn);
            newConn.ConnectionDetails.DatabaseName = DatabaseName;
            ConnectionCompleteParams connectionResult = await connectionService.Connect(new ConnectParams() {
                OwnerUri = randomUri,
                Connection = newConn.ConnectionDetails,
                Type = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default,
            });

            ConnectionInfo connectionInfo = this.connectionService.OwnerToConnectionMap[connectionResult.OwnerUri];
            connectionInfo.ConnectionDetails.DatabaseName = DatabaseName;
            ServerConnection serverConn = ConnectionService.OpenServerConnection(connectionInfo);

            Func<string, Task> queryCreateFailureAction = message =>
            {
                taskCompletion.SetException(new Exception(message));
                return Task.FromResult(0);
            };

            ResultOnlyContext<Object> newContext = new ResultOnlyContext<Object>(requestContext);

            // handle sending event back when the query completes
            Query.QueryAsyncEventHandler queryComplete = async query =>
            {
                try
                {
                    // check to make sure any results were recieved
                    if (query.Batches.Length == 0
                        || query.Batches[0].ResultSets.Count == 0)
                    {
                        await requestContext.SendError(SR.QueryServiceResultSetHasNoResults);
                        return;
                    }

                    long rowCount = query.Batches[0].ResultSets[0].RowCount;
                    // check to make sure there is a safe amount of rows to load into memory
                    if (rowCount > Int32.MaxValue)
                    {
                        await requestContext.SendError(SR.QueryServiceResultSetTooLarge);
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

            await queryService.InterServiceExecuteQuery(executeStringParams, connectionInfo, newContext, null, queryCreateFailureAction, queryComplete, queryFail);
            return await taskCompletion.Task;
        }

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
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

        /// <summary>
        /// Generates a URI for object explorer using a similar pattern to Mongo DB (which has URI-based database definition)
        /// as this should ensure uniqueness
        /// </summary>
        /// <param name="details"></param>
        /// <returns>string representing a URI</returns>
        /// <remarks>Internal for testing purposes only</remarks>
        internal static string GenerateUri(ConnectionDetails details)
        {
            return ConnectedBindingQueue.GetConnectionContextKey(details);
        }


        private static OnAction MapOnAction(string action)
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


        public class SchemaDesignerEventSender : IEventSender
        {
            public Action<ResultSetEventParams> ResultSetHandler { get; set; }

            public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
            {
                if (eventParams is ResultSetEventParams && this.ResultSetHandler != null)
                {
                    this.ResultSetHandler(eventParams as ResultSetEventParams);
                }
                return Task.FromResult(0);
            }
        }

    }
}