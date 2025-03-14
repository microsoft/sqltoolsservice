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
    /// <summary>
    /// Provides methods for executing queries for the Schema Designer functionality.
    /// </summary>
    public static class SchemaDesignerQueryExecution
    {
        /// <summary>
        /// Clones an existing connection and changes the database name.
        /// </summary>
        /// <param name="existingConnectionUri">The URI of the existing connection.</param>
        /// <param name="newConnectionUri">The URI for the new connection.</param>
        /// <param name="databaseName">The name of the database to connect to.</param>
        /// <returns>The connection parameters for the new connection.</returns>
        /// <exception cref="Exception">Thrown when the connection fails.</exception>
        public static async Task<ConnectionCompleteParams> CloneConnectionAsync(string existingConnectionUri, string newConnectionUri, string DatabaseName)
        {
            // Getting existing connection
            if (!ConnectionService.Instance.TryFindConnection(existingConnectionUri, out ConnectionInfo connInfo))
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
        /// Connects to a database using the provided connection parameters.
        /// </summary>
        /// <param name="connectParams">The connection parameters to use.</param>
        /// <returns>The connection completion parameters.</returns>
        /// <exception cref="Exception">Thrown when the connection fails.</exception>
        private static async Task<ConnectionCompleteParams> ConnectAsync(ConnectParams connectParams)
        {
            try
            {
                ConnectionCompleteParams result = await ConnectionService.Instance.Connect(connectParams);
                string connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}" : string.Empty;

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
                throw;
            }
        }

        /// <summary>
        /// Disconnects from a database using the provided connection URI.
        /// </summary>
        /// <param name="connectionUri">The connection URI to disconnect.</param>
        public static void Disconnect(string connectionUri)
        {
            ConnectionService.Instance.Disconnect(new DisconnectParams
            {
                OwnerUri = connectionUri
            });
        }


        /// <summary>
        /// Runs a simple query and returns the results as a list of rows.
        /// </summary>
        /// <param name="connectionOwnerUri">The connection owner URI.</param>
        /// <param name="query">The query to run.</param>
        /// <returns>A list of rows as the query result.</returns>
        public static async Task<List<IList<QueryExecution.Contracts.DbCellValue>>> RunSimpleQueryAsync(string connectionOwnerUri, string query)
        {
            if (!ConnectionService.Instance.TryFindConnection(connectionOwnerUri, out ConnectionInfo connInfo))
            {
                throw new Exception(SR.QueryServiceQueryInvalidOwnerUri);
            }

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
                    if (query.Batches.Length == 0 || query.Batches[0].ResultSets.Count == 0)
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
                connInfo,
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

        /// <summary>
        /// Implementation of IEventSender for Schema Designer query execution.
        /// </summary>
        public class SchemaDesignerQueryExecutionEventSender : IEventSender
        {
            private readonly TaskCompletionSource<ResultSet> _taskCompletion;

            /// <summary>
            /// Initializes a new instance of the SchemaDesignerQueryExecutionEventSender class.
            /// </summary>
            /// <param name="taskCompletion">The task completion source.</param>
            public SchemaDesignerQueryExecutionEventSender(TaskCompletionSource<ResultSet> taskCompletion)
            {
                _taskCompletion = taskCompletion;
            }

            /// <summary>
            /// Sends an event.
            /// </summary>
            /// <typeparam name="TParams">The type of the event parameters.</typeparam>
            /// <param name="eventType">The event type.</param>
            /// <param name="eventParams">The event parameters.</param>
            /// <returns>A task representing the asynchronous operation.</returns>
            public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
            {
                return Task.FromResult(true);
            }

            /// <summary>
            /// Sends an error.
            /// </summary>
            /// <param name="errorMessage">The error message.</param>
            /// <param name="errorCode">The error code.</param>
            /// <returns>A task representing the asynchronous operation.</returns>
            public Task SendError(string errorMessage, int errorCode = 0)
            {
                _taskCompletion.SetException(new Exception(errorMessage));
                return Task.FromResult(0);
            }
        }
    }
}