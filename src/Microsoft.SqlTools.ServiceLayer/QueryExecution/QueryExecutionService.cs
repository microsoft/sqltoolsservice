//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Service for executing queries
    /// </summary>
    public sealed class QueryExecutionService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());

        /// <summary>
        /// Singleton instance of the query execution service
        /// </summary>
        public static QueryExecutionService Instance => LazyInstance.Value;

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
            get
            {
                return BufferFileStreamFactory ?? (BufferFileStreamFactory = new ServiceBufferFileStreamFactory
                {
                    MaxCharsToStore = Settings.SqlTools.QueryExecutionSettings.MaxCharsToStore,
                    MaxXmlCharsToStore = Settings.SqlTools.QueryExecutionSettings.MaxXmlCharsToStore
                });
            }
        }

        /// <summary>
        /// File factory to be used to create CSV files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory CsvFileFactory { get; set; }

        /// <summary>
        /// File factory to be used to create JSON files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory JsonFileFactory { get; set; }

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
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(SubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
            serviceHost.SetRequestHandler(SaveResultsAsCsvRequest.Type, HandleSaveResultsAsCsvRequest);
            serviceHost.SetRequestHandler(SaveResultsAsJsonRequest.Type, HandleSaveResultsAsJsonRequest);
            serviceHost.SetRequestHandler(QueryExecutionPlanRequest.Type, HandleExecutionPlanRequest);

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

        /// <summary>
        /// Handles request to execute a selection of a document in the workspace service
        /// </summary>
        public async Task HandleExecuteRequest(ExecuteRequestParamsBase executeDocumentSelectionParams,
            RequestContext<ExecuteRequestResult> requestContext)
        {
            // Get a query new active query
            Query newQuery = await CreateAndActivateNewQuery(executeDocumentSelectionParams, requestContext);

            // Execute the query -- asynchronously
            ExecuteAndCompleteQuery(executeDocumentSelectionParams.OwnerUri, requestContext, newQuery);
        }

        /// <summary>
        /// Handles a request to get a subset of the results of this query
        /// </summary>
        public async Task HandleResultSubsetRequest(SubsetParams subsetParams,
            RequestContext<SubsetResult> requestContext)
        {
            try
            {
                // Attempt to load the query
                Query query;
                if (!ActiveQueries.TryGetValue(subsetParams.OwnerUri, out query))
                {
                    await requestContext.SendResult(new SubsetResult
                    {
                        Message = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Retrieve the requested subset and return it
                var result = new SubsetResult
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
                await requestContext.SendResult(new SubsetResult
                {
                    Message = ioe.Message
                });
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                // Return the error as a result
                await requestContext.SendResult(new SubsetResult
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

         /// <summary>
        /// Handles a request to get an execution plan
        /// </summary>
        public async Task HandleExecutionPlanRequest(QueryExecutionPlanParams planParams,
            RequestContext<QueryExecutionPlanResult> requestContext)
        {
            try
            {
                // Attempt to load the query
                Query query;
                if (!ActiveQueries.TryGetValue(planParams.OwnerUri, out query))
                {
                    await requestContext.SendError(SR.QueryServiceRequestsNoQuery);
                    return;
                }

                // Retrieve the requested execution plan and return it
                var result = new QueryExecutionPlanResult
                {
                    ExecutionPlan = await query.GetExecutionPlan(planParams.BatchIndex, planParams.ResultSetIndex)
                };
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                // This was unexpected, so send back as error
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Handles a request to dispose of this query
        /// </summary>
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

                // Cleanup the query
                result.Dispose();

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

        /// <summary>
        /// Handles a request to cancel this query if it is in progress
        /// </summary>
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

                // Cancel the query and send a success message
                result.Cancel();
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
        internal async Task HandleSaveResultsAsCsvRequest(SaveResultsAsCsvRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default CSV file factory if we haven't overridden it
            IFileStreamFactory csvFactory = CsvFileFactory ?? new SaveAsCsvFileStreamFactory
            {
                SaveRequestParams = saveParams
            };
            await SaveResultsHelper(saveParams, requestContext, csvFactory);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in JSON format
        /// </summary>
        internal async Task HandleSaveResultsAsJsonRequest(SaveResultsAsJsonRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default JSON file factory if we haven't overridden it
            IFileStreamFactory jsonFactory = JsonFileFactory ?? new SaveAsJsonFileStreamFactory
            {
                SaveRequestParams = saveParams
            };
            await SaveResultsHelper(saveParams, requestContext, jsonFactory);
        }

        #endregion

        #region Private Helpers

        private async Task<Query> CreateAndActivateNewQuery(ExecuteRequestParamsBase executeParams, RequestContext<ExecuteRequestResult> requestContext)
        {
            try
            {
                // Attempt to get the connection for the editor
                ConnectionInfo connectionInfo;
                if (!ConnectionService.TryFindConnection(executeParams.OwnerUri, out connectionInfo))
                {
                    await requestContext.SendError(SR.QueryServiceQueryInvalidOwnerUri);
                    return null;
                }

                // Attempt to clean out any old query on the owner URI
                Query oldQuery;
                if (ActiveQueries.TryGetValue(executeParams.OwnerUri, out oldQuery) && oldQuery.HasExecuted)
                {
                    oldQuery.Dispose();
                    ActiveQueries.TryRemove(executeParams.OwnerUri, out oldQuery);
                }

                // Retrieve the current settings for executing the query with
                QueryExecutionSettings settings = WorkspaceService.CurrentSettings.QueryExecutionSettings;

                // Apply execution parameter settings 
                settings.ExecutionPlanOptions = executeParams.ExecutionPlanOptions;

                // If we can't add the query now, it's assumed the query is in progress
                Query newQuery = new Query(GetSqlText(executeParams), connectionInfo, settings, BufferFileFactory);
                if (!ActiveQueries.TryAdd(executeParams.OwnerUri, newQuery))
                {
                    await requestContext.SendError(SR.QueryServiceQueryInProgress);
                    newQuery.Dispose();
                    return null;
                }

                // Send the result stating that the query was successfully started
                await requestContext.SendResult(new ExecuteRequestResult());

                return newQuery;
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
                return null;
            }
        }

        private static void ExecuteAndCompleteQuery(string ownerUri, IEventSender eventSender, Query query)
        {
            // Skip processing if the query is null
            if (query == null)
            {
                return;
            }

            // Setup the query completion/failure callbacks
            Query.QueryAsyncEventHandler callback = async q =>
            {
                // Send back the results
                QueryCompleteParams eventParams = new QueryCompleteParams
                {
                    OwnerUri = ownerUri,
                    BatchSummaries = q.BatchSummaries
                };

                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            };

            Query.QueryAsyncErrorEventHandler errorCallback = async errorMessage =>
            {
                // Send back the error message
                QueryCompleteParams eventParams = new QueryCompleteParams
                {
                    OwnerUri = ownerUri,
                    //Message = errorMessage              
                };
                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            };

            query.QueryCompleted += callback;
            query.QueryFailed += callback;
            query.QueryConnectionException += errorCallback;

            // Setup the batch callbacks
            Batch.BatchAsyncEventHandler batchStartCallback = async b =>
            {
                BatchEventParams eventParams = new BatchEventParams
                {
                    BatchSummary = b.Summary,
                    OwnerUri = ownerUri
                };

                await eventSender.SendEvent(BatchStartEvent.Type, eventParams);
            };
            query.BatchStarted += batchStartCallback;

            Batch.BatchAsyncEventHandler batchCompleteCallback = async b =>
            {
                BatchEventParams eventParams = new BatchEventParams
                {
                    BatchSummary = b.Summary,
                    OwnerUri = ownerUri
                };

                await eventSender.SendEvent(BatchCompleteEvent.Type, eventParams);
            };
            query.BatchCompleted += batchCompleteCallback;

            Batch.BatchAsyncMessageHandler batchMessageCallback = async m =>
            {
                MessageParams eventParams = new MessageParams
                {
                    Message = m,
                    OwnerUri = ownerUri
                };
                await eventSender.SendEvent(MessageEvent.Type, eventParams);
            };
            query.BatchMessageSent += batchMessageCallback;

            // Setup the ResultSet completion callback
            ResultSet.ResultSetAsyncEventHandler resultCallback = async r =>
            {
                ResultSetEventParams eventParams = new ResultSetEventParams
                {
                    ResultSetSummary = r.Summary,
                    OwnerUri = ownerUri
                };
                await eventSender.SendEvent(ResultSetCompleteEvent.Type, eventParams);
            };
            query.ResultSetCompleted += resultCallback;

            // Launch this as an asynchronous task
            query.Execute();
        }

        private async Task SaveResultsHelper(SaveResultsRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext, IFileStreamFactory fileFactory)
        {
            // retrieve query for OwnerUri
            Query query;
            if (!ActiveQueries.TryGetValue(saveParams.OwnerUri, out query))
            {
                await requestContext.SendError(new SaveResultRequestError
                {
                    message = SR.QueryServiceQueryInvalidOwnerUri
                });
                return;
            }

            //Setup the callback for completion of the save task
            ResultSet.SaveAsAsyncEventHandler successHandler = async parameters =>
            {
                await requestContext.SendResult(new SaveResultRequestResult());
            };
            ResultSet.SaveAsFailureAsyncEventHandler errorHandler = async (parameters, reason) =>
            {
                string message = SR.QueryServiceSaveAsFail(Path.GetFileName(parameters.FilePath), reason);
                await requestContext.SendError(new SaveResultRequestError { message = message });
            };

            try
            {
                // Launch the task
                query.SaveAs(saveParams, fileFactory, successHandler, errorHandler);
            }
            catch (Exception e)
            {
                await errorHandler(saveParams, e.Message);
            }
        }

        // Internal for testing purposes
        internal string GetSqlText(ExecuteRequestParamsBase request)
        {
            // If it is a document selection, we'll retrieve the text from the document
            ExecuteDocumentSelectionParams docRequest = request as ExecuteDocumentSelectionParams;
            if (docRequest != null)
            {
                // Get the document from the parameters
                ScriptFile queryFile = WorkspaceService.Workspace.GetFile(docRequest.OwnerUri);

                // If a selection was not provided, use the entire document
                if (docRequest.QuerySelection == null)
                {
                    return queryFile.Contents;
                }

                // A selection was provided, so get the lines in the selected range
                string[] queryTextArray = queryFile.GetLinesInRange(
                    new BufferRange(
                        new BufferPosition(
                            docRequest.QuerySelection.StartLine + 1,
                            docRequest.QuerySelection.StartColumn + 1
                        ),
                        new BufferPosition(
                            docRequest.QuerySelection.EndLine + 1,
                            docRequest.QuerySelection.EndColumn + 1
                        )
                    )
                );
                return string.Join(Environment.NewLine, queryTextArray);
            }

            // If it is an ExecuteStringParams, return the text as is
            ExecuteStringParams stringRequest = request as ExecuteStringParams;
            if (stringRequest != null)
            {
                return stringRequest.Query;
            }

            // Note, this shouldn't be possible due to inheritance rules
            throw new InvalidCastException("Invalid request type");
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
                    if (!query.Value.HasExecuted)
                    {
                        try
                        {
                            query.Value.Cancel();
                        }
                        catch (Exception e)
                        {
                            // We don't particularly care if we fail to cancel during shutdown
                            string message = string.Format("Failed to cancel query {0} during query service disposal: {1}", query.Key, e);
                            Logger.Write(LogLevel.Warning, message);
                        }
                    }
                    query.Value.Dispose();
                }
                ActiveQueries.Clear();
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
