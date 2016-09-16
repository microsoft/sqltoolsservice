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
        }

        internal QueryExecutionService(ConnectionService connService)
        {
            ConnectionService = connService;
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
            WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback((oldSettings, newSettings, eventContext) =>
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
                    if (saveParams.IncludeHeaders) 
                    {
                        // write column names to csv
                        await csvFile.WriteLineAsync( string.Join( ",", selectedResultSet.Columns.Select( column => SaveResults.EncodeCsvField(column.ColumnName) ?? string.Empty)));
                    }

                    // write rows to csv
                    foreach (var row in selectedResultSet.Rows)
                    {
                        await csvFile.WriteLineAsync( string.Join( ",", row.Select( field => SaveResults.EncodeCsvField((field != null) ? field.ToString(): string.Empty))));
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
                    ResultSet selectedResultSet = (selectedBatch.ResultSets.ToList())[saveParams.ResultSetIndex];

                    // write each row to JSON
                    foreach (var row in selectedResultSet.Rows)
                    {
                        jsonWriter.WriteStartObject();
                        foreach (var field in row.Select((value,i) => new {value, i}))
                        {
                            jsonWriter.WritePropertyName(selectedResultSet.Columns[field.i].ColumnName);
                            if (field.value != null) 
                            {
                                jsonWriter.WriteValue(field.value);
                            } 
                            else
                            {
                                jsonWriter.WriteNull();
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
                QueryExecutionSettings settings = WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.QueryExecutionSettings;

                // If we can't add the query now, it's assumed the query is in progress
                Query newQuery = new Query(executeParams.QueryText, connectionInfo, settings, BufferFileFactory);
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
