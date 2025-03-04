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
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerQueryExecution
    {
        /// <summary>
        /// Clones an existing connection and changes the database name
        /// </summary>
        /// <param name="existingConnectionUri"> The URI of the existing connection </param>
        /// <param name="DatabaseName"> The name of the database to connect to </param>
        /// <returns> The connection parameters for the new connection </returns>
        public static async Task<ConnectionCompleteParams> CloneConnection(string existingConnectionUri, string newConnectionUri, string DatabaseName)
        {
            // Getting existing connection
            ConnectionInfo connInfo;
            if (!ConnectionService.Instance.TryFindConnection(existingConnectionUri, out connInfo))
            {
                throw new Exception(SR.QueryServiceQueryInvalidOwnerUri);
            }
            connInfo.ConnectionDetails.DatabaseName = DatabaseName;

            // Creating new connection to execute query
            ConnectParams newConnectionParams = new ConnectParams
            {
                OwnerUri = newConnectionUri,
                Connection = connInfo.ConnectionDetails,
                Type = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default,
            };
            ConnectionCompleteParams connectionCompleteParams = await ConnectionService.Instance.Connect(newConnectionParams);
            if (!string.IsNullOrEmpty(connectionCompleteParams.Messages))
            {
                throw new Exception(connectionCompleteParams.Messages);
            }
            return connectionCompleteParams;
        }

        /// <summary>
        /// Connects to a database using the provided connection parameters
        /// </summary>
        /// <param name="connectParams"> The connection parameters to use </param>
        /// <returns></returns>
        private static async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                ConnectionCompleteParams result = await ConnectionService.Instance.Connect(connectParams);
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
        /// Disconnects from a database using the provided connection URI
        /// </summary>
        /// <param name="connectionUri"></param>
        /// <returns></returns>
        public static void Disconnect(string connectionUri)
        {
            ConnectionService.Instance.Disconnect(new DisconnectParams
            {
                OwnerUri = connectionUri
            });
        }


        /// <summary>
        /// Runs a simple query and returns the results as a list of rows
        /// </summary>
        /// <param name="connectionOwnerUri"> The connection owner URI </param>
        /// <param name="query"> The query to run </param>
        /// <returns> A list of rows </returns>
        public static async Task<List<IList<QueryExecution.Contracts.DbCellValue>>> RunSimpleQuery(string connectionOwnerUri, string query)
        {
            ConnectionInfo newConn;
            ConnectionService.Instance.TryFindConnection(connectionOwnerUri, out newConn);

            TaskCompletionSource<ResultSet> taskCompletion = new TaskCompletionSource<ResultSet>();

            ExecuteStringParams executeStringParams = new ExecuteStringParams
            {
                Query = query,
                OwnerUri = connectionOwnerUri
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

            await QueryExecutionService.Instance.InterServiceExecuteQuery(
                executeStringParams,
                newConn,
                new SchemaDesignerQueryExecutionEventSender(taskCompletion),
                null,
                queryCreateFailureAction,
                queryComplete,
                queryFail
            );

            ResultSet result = await taskCompletion.Task;
            List<IList<DbCellValue>> rows = new List<IList<DbCellValue>>();
            for (int i = 0; i < result.RowCount; i++)
            {
                rows.Add(result.GetRow(i));
            }

            return rows;
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