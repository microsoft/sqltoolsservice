//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Service for executing queries
    /// </summary>
    public sealed class QueryExecutionService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<QueryExecutionService> instance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());

        /// <summary>
        /// Singleton instance of the query execution service
        /// </summary>
        public static QueryExecutionService Instance
        {
            get { return instance.Value; }
        }

        private QueryExecutionService()
        {
            ConnectionService = ConnectionService.Instance;
            WorkspaceService = WorkspaceService<SqlToolsSettings>.Instance;
        }

        internal QueryExecutionService(ConnectionService connService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            ConnectionService = connService;
            WorkspaceService = workspaceService;
        }

        #endregion

        #region Properties

        /// <summary>
        /// File factory to be used to create a buffer file for results.
        /// </summary>
        /// <remarks>
        /// Made internal here to allow for overriding in unit testing
        /// </remarks>
        internal IFileStreamFactory BufferFileStreamFactory;

        /// <summary>
        /// File factory to be used to create a buffer file for results
        /// </summary>
        private IFileStreamFactory BufferFileFactory
        {
            get { return BufferFileStreamFactory ?? (BufferFileStreamFactory = new ServiceBufferFileStreamFactory()); }
        }

        /// <summary>
        /// The collection of active queries
        /// </summary>
        internal ConcurrentDictionary<string, Query> ActiveQueries
        {
            get { return queries.Value; }
        }

        /// <summary>
        /// Instance of the connection service, used to get the connection info for a given owner URI
        /// </summary>
        private ConnectionService ConnectionService { get; set; }

        private WorkspaceService<SqlToolsSettings> WorkspaceService { get; set; }

        /// <summary>
        /// Internal storage of active queries, lazily constructed as a threadsafe dictionary
        /// </summary>
        private readonly Lazy<ConcurrentDictionary<string, Query>> queries =
            new Lazy<ConcurrentDictionary<string, Query>>(() => new ConcurrentDictionary<string, Query>());

        private SqlToolsSettings Settings { get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; } }

        #endregion

        /// <summary>
        /// Initializes the service with the service host, registers request handlers and shutdown
        /// event handler.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(QueryExecuteRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(QueryExecuteSubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
            serviceHost.SetRequestHandler(SaveResultsAsCsvRequest.Type, HandleSaveResultsAsCsvRequest);
            serviceHost.SetRequestHandler(SaveResultsAsJsonRequest.Type, HandleSaveResultsAsJsonRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                Dispose();
                return Task.FromResult(0);
            });

            // Register a handler for when the configuration changes
            WorkspaceService.RegisterConfigChangeCallback((oldSettings, newSettings, eventContext) =>
            {
                Settings.QueryExecutionSettings.Update(newSettings.QueryExecutionSettings);
                return Task.FromResult(0);
            });
        }

        #region Request Handlers

        public async Task HandleExecuteRequest(QueryExecuteParams executeParams,
            RequestContext<QueryExecuteResult> requestContext)
        {
            try
            {
                // Get a query new active query
                Query newQuery = await CreateAndActivateNewQuery(executeParams, requestContext);

                // Execute the query
                await ExecuteAndCompleteQuery(executeParams, requestContext, newQuery);
            }
            catch (Exception e)
            {
                // Dump any unexpected exceptions as errors
                await requestContext.SendError(e.Message);
            }
        }

        public async Task HandleResultSubsetRequest(QueryExecuteSubsetParams subsetParams,
            RequestContext<QueryExecuteSubsetResult> requestContext)
        {
            try
            {
                // Attempt to load the query
                Query query;
                if (!ActiveQueries.TryGetValue(subsetParams.OwnerUri, out query))
                {
                    await requestContext.SendResult(new QueryExecuteSubsetResult
                    {
                        Message = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Retrieve the requested subset and return it
                var result = new QueryExecuteSubsetResult
                {
                    Message = null,
                    ResultSubset = await query.GetSubset(subsetParams.BatchIndex,
                        subsetParams.ResultSetIndex, subsetParams.RowsStartIndex, subsetParams.RowsCount)
                };
                await requestContext.SendResult(result);
            }
            catch (InvalidOperationException ioe)
            {
                // Return the error as a result
                await requestContext.SendResult(new QueryExecuteSubsetResult
                {
                    Message = ioe.Message
                });
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                // Return the error as a result
                await requestContext.SendResult(new QueryExecuteSubsetResult
                {
                    Message = aoore.Message
                });
            }
            catch (Exception e)
            {
                // This was unexpected, so send back as error
                await requestContext.SendError(e.Message);
            }
        }

        public async Task HandleDisposeRequest(QueryDisposeParams disposeParams,
            RequestContext<QueryDisposeResult> requestContext)
        {
            try
            {
                // Attempt to remove the query for the owner uri
                Query result;
                if (!ActiveQueries.TryRemove(disposeParams.OwnerUri, out result))
                {
                    await requestContext.SendResult(new QueryDisposeResult
                    {
                        Messages = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Success
                await requestContext.SendResult(new QueryDisposeResult
                {
                    Messages = null
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        public async Task HandleCancelRequest(QueryCancelParams cancelParams,
            RequestContext<QueryCancelResult> requestContext)
        {
            try
            {
                // Attempt to find the query for the owner uri
                Query result;
                if (!ActiveQueries.TryGetValue(cancelParams.OwnerUri, out result))
                {
                    await requestContext.SendResult(new QueryCancelResult
                    {
                        Messages = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Cancel the query
                result.Cancel();

                // Attempt to dispose the query
                if (!ActiveQueries.TryRemove(cancelParams.OwnerUri, out result))
                {
                    // It really shouldn't be possible to get to this scenario, but we'll cover it anyhow
                    await requestContext.SendResult(new QueryCancelResult
                    {
                        Messages = SR.QueryServiceCancelDisposeFailed
                    });
                    return;
                }

                await requestContext.SendResult(new QueryCancelResult());
            }
            catch (InvalidOperationException e)
            {
                // If this exception occurred, we most likely were trying to cancel a completed query
                await requestContext.SendResult(new QueryCancelResult
                {
                    Messages = e.Message
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Process request to save a resultSet to a file in CSV format
        /// </summary>
        public async Task HandleSaveResultsAsCsvRequest( SaveResultsAsCsvRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // retrieve query for OwnerUri
            Query result;
            if (!ActiveQueries.TryGetValue(saveParams.OwnerUri, out result))
            {
                await requestContext.SendResult(new SaveResultRequestResult
                {
                    Messages = SR.QueryServiceRequestsNoQuery
                });
                return;
            }
            try
            {
                using (StreamWriter csvFile = new StreamWriter(File.Open(saveParams.FilePath, FileMode.Create)))
                {
                    // get the requested resultSet from query
                    Batch selectedBatch = result.Batches[saveParams.BatchIndex];
                    ResultSet selectedResultSet = (selectedBatch.ResultSets.ToList())[saveParams.ResultSetIndex];
                    int columnCount = 0;
                    int rowCount = 0;
                    int columnStartIndex = 0;
                    int rowStartIndex = 0;

                    // set column, row counts depending on whether save request is for entire result set or a subset
                    if (SaveResults.isSaveSelection(saveParams))
                    {   
                        columnCount = saveParams.ColumnEndIndex.Value - saveParams.ColumnStartIndex.Value + 1;
                        rowCount = saveParams.RowEndIndex.Value - saveParams.RowStartIndex.Value + 1;
                        columnStartIndex = saveParams.ColumnStartIndex.Value;
                        rowStartIndex =saveParams.RowStartIndex.Value;
                    }
                    else
                    {                
                        columnCount = selectedResultSet.Columns.Length;
                        rowCount = (int)selectedResultSet.RowCount;
                    }

                    // write column names if include headers option is chosen
                    if (saveParams.IncludeHeaders)
                    {
                        await csvFile.WriteLineAsync( string.Join( ",", selectedResultSet.Columns.Skip(columnStartIndex).Take(columnCount).Select( column =>
                                            SaveResults.EncodeCsvField(column.ColumnName) ?? string.Empty)));
                    }

                    // retrieve rows and write as csv
                    ResultSetSubset resultSubset = await result.GetSubset(saveParams.BatchIndex, saveParams.ResultSetIndex, rowStartIndex, rowCount);
                    foreach (var row in resultSubset.Rows)
                    {
                        await csvFile.WriteLineAsync( string.Join( ",", row.Skip(columnStartIndex).Take(columnCount).Select( field => 
                                            SaveResults.EncodeCsvField((field != null) ? field.ToString(): "NULL"))));
                    }

                }

                // Successfully wrote file, send success result
                await requestContext.SendResult(new SaveResultRequestResult { Messages = null });
            }
            catch(Exception ex)
            {
                // Delete file when exception occurs
                if (File.Exists(saveParams.FilePath))
                {
                    File.Delete(saveParams.FilePath);
                }
                await requestContext.SendError(ex.Message);
            }
        }

        /// <summary>
        /// Process request to save a resultSet to a file in JSON format
        /// </summary>
        public async Task HandleSaveResultsAsJsonRequest( SaveResultsAsJsonRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // retrieve query for OwnerUri
            Query result;
            if (!ActiveQueries.TryGetValue(saveParams.OwnerUri, out result))
            {
                await requestContext.SendResult(new SaveResultRequestResult
                {
                    Messages = "Failed to save results, ID not found."
                });
                return;
            }
            try
            {
                using (StreamWriter jsonFile = new StreamWriter(File.Open(saveParams.FilePath, FileMode.Create)))
                using (JsonWriter jsonWriter = new JsonTextWriter(jsonFile) )
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    jsonWriter.WriteStartArray();

                    // get the requested resultSet from query
                    Batch selectedBatch = result.Batches[saveParams.BatchIndex];
                    ResultSet selectedResultSet = selectedBatch.ResultSets.ToList()[saveParams.ResultSetIndex];
                    int rowCount = 0;
                    int rowStartIndex = 0;
                    int columnStartIndex = 0;
                    int columnEndIndex = 0;

                    // set column, row counts depending on whether save request is for entire result set or a subset
                    if (SaveResults.isSaveSelection(saveParams))
                    {
                       
                        rowCount = saveParams.RowEndIndex.Value - saveParams.RowStartIndex.Value + 1;
                        rowStartIndex = saveParams.RowStartIndex.Value;
                        columnStartIndex = saveParams.ColumnStartIndex.Value;
                        columnEndIndex = saveParams.ColumnEndIndex.Value + 1 ; // include the last column
                    }
                    else 
                    {
                        rowCount = (int)selectedResultSet.RowCount;
                        columnEndIndex = selectedResultSet.Columns.Length;
                    }

                    // retrieve rows and write as json
                    ResultSetSubset resultSubset = await result.GetSubset(saveParams.BatchIndex, saveParams.ResultSetIndex, rowStartIndex, rowCount);
                    foreach (var row in resultSubset.Rows)
                    {
                        jsonWriter.WriteStartObject();
                        for (int i = columnStartIndex ; i < columnEndIndex; i++)
                        {
                            //get column name
                            DbColumnWrapper col = selectedResultSet.Columns[i];
                            string val = row[i]?.ToString();
                            jsonWriter.WritePropertyName(col.ColumnName);
                            if (val == null)
                            {
                                jsonWriter.WriteNull();
                            }
                            else
                            {
                                jsonWriter.WriteValue(val);
                            }
                        }
                        jsonWriter.WriteEndObject();
                    }
                    jsonWriter.WriteEndArray();
                }

                await requestContext.SendResult(new SaveResultRequestResult { Messages = null });
            }
            catch(Exception ex)
            {
                // Delete file when exception occurs
                if (File.Exists(saveParams.FilePath))
                {
                    File.Delete(saveParams.FilePath);
                }
                await requestContext.SendError(ex.Message);
            }
        }
        #endregion

        #region Private Helpers

        private async Task<Query> CreateAndActivateNewQuery(QueryExecuteParams executeParams, RequestContext<QueryExecuteResult> requestContext)
        {
            try
            {
                // Attempt to get the connection for the editor
                ConnectionInfo connectionInfo;
                if (!ConnectionService.TryFindConnection(executeParams.OwnerUri, out connectionInfo))
                {
                    await requestContext.SendResult(new QueryExecuteResult
                    {
                        Messages = SR.QueryServiceQueryInvalidOwnerUri
                    });
                    return null;
                }

                // Attempt to clean out any old query on the owner URI
                Query oldQuery;
                if (ActiveQueries.TryGetValue(executeParams.OwnerUri, out oldQuery) && oldQuery.HasExecuted)
                {
                    ActiveQueries.TryRemove(executeParams.OwnerUri, out oldQuery);
                }

                // Retrieve the current settings for executing the query with
                QueryExecutionSettings settings = WorkspaceService.CurrentSettings.QueryExecutionSettings;

                // Get query text from the workspace.
                ScriptFile queryFile = WorkspaceService.Workspace.GetFile(executeParams.OwnerUri);

                string queryText;

                if (executeParams.QuerySelection != null) 
                {
                    string[] queryTextArray = queryFile.GetLinesInRange(
                        new BufferRange(
                            new BufferPosition(
                                executeParams.QuerySelection.StartLine + 1, 
                                executeParams.QuerySelection.StartColumn + 1
                            ), 
                            new BufferPosition(
                                executeParams.QuerySelection.EndLine + 1, 
                                executeParams.QuerySelection.EndColumn + 1
                            )
                        )
                    );
                    queryText = queryTextArray.Aggregate((a, b) => a + '\r' + '\n' + b);
                } 
                else 
                {
                    queryText = queryFile.Contents;
                }
                
                // If we can't add the query now, it's assumed the query is in progress
                Query newQuery = new Query(queryText, connectionInfo, settings, BufferFileFactory);
                if (!ActiveQueries.TryAdd(executeParams.OwnerUri, newQuery))
                {
                    await requestContext.SendResult(new QueryExecuteResult
                    {
                        Messages = SR.QueryServiceQueryInProgress
                    });
                    return null;
                }

                return newQuery;
            }
            catch (ArgumentException ane)
            {
                await requestContext.SendResult(new QueryExecuteResult { Messages = ane.Message });
                return null;
            }
            // Any other exceptions will fall through here and be collected at the end
        }

        private async Task ExecuteAndCompleteQuery(QueryExecuteParams executeParams, RequestContext<QueryExecuteResult> requestContext, Query query)
        {
            // Skip processing if the query is null
            if (query == null)
            {
                return;
            }

            // Launch the query and respond with successfully launching it
            Task executeTask = query.Execute();
            await requestContext.SendResult(new QueryExecuteResult
            {
                Messages = null
            });

            // Wait for query execution and then send back the results
            await Task.WhenAll(executeTask);
            QueryExecuteCompleteParams eventParams = new QueryExecuteCompleteParams
            {
                OwnerUri = executeParams.OwnerUri,
                BatchSummaries = query.BatchSummaries
            };
            await requestContext.SendEvent(QueryExecuteCompleteEvent.Type, eventParams);
        }

        #endregion

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var query in ActiveQueries)
                {
                    query.Value.Dispose();
                }
            }

            disposed = true;
        }

        ~QueryExecutionService()
        {
            Dispose(false);
        }

        #endregion
    }
}
