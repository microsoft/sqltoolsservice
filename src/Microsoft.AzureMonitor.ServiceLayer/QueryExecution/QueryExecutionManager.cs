using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.AzureMonitor.ServiceLayer.Localization;
using Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.AzureMonitor.ServiceLayer.Workspace;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.DataContracts.SqlContext;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution
{
    public class QueryExecutionManager : IDisposable
    {
        private bool _disposed;
        private readonly ConnectionService _connectionService;
        private readonly WorkspaceService<SqlToolsSettings> _workspaceService;
        private readonly ConcurrentDictionary<string, Query> _activeQueries;
        private readonly SqlToolsSettings _sqlToolsSettings;
        
        /// <summary>
        /// File factory to be used to create a buffer file for results.
        /// </summary>
        private IFileStreamFactory _bufferFileFactory;
        private readonly Lazy<ConcurrentDictionary<string, QueryExecutionSettings>> _queryExecutionSettings =
            new Lazy<ConcurrentDictionary<string, QueryExecutionSettings>>(() => new ConcurrentDictionary<string, QueryExecutionSettings>());
        
        public QueryExecutionManager(ConnectionService connectionService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            _connectionService = connectionService;
            _workspaceService = workspaceService;
            _activeQueries = new ConcurrentDictionary<string, Query>();
            _sqlToolsSettings = new SqlToolsSettings();
        }

        public async Task CancelQuery(string ownerUri, RequestContext<QueryCancelResult> requestContext)
        {
            // Attempt to find the query for the owner uri
            if (!_activeQueries.TryGetValue(ownerUri, out Query query))
            {
                await requestContext.SendResult(new QueryCancelResult
                {
                    Messages = SR.QueryServiceRequestsNoQuery
                });
                return;
            }

            // Cancel the query and send a success message
            query.Cancel();
        }

        /// <summary>
        /// Query execution meant to be called from another service. Utilizes callbacks to allow
        /// custom actions to be taken upon creation of query and failure to create query.
        /// </summary>
        /// <param name="executeParams">Parameters for execution</param>
        /// <param name="queryEventSender">Event sender that will send progressive events during execution of the query</param>
        /// <param name="queryCreateSuccessFunc">
        /// Callback for when query has been created successfully. If result is <c>true</c>, query
        /// will be executed asynchronously. If result is <c>false</c>, query will be disposed. May
        /// be <c>null</c>
        /// </param>
        /// <param name="queryCreateFailFunc">
        /// Callback for when query failed to be created successfully. Error message is provided.
        /// May be <c>null</c>.
        /// </param>
        /// <param name="querySuccessFunc">
        /// Callback to call when query has completed execution successfully. May be <c>null</c>.
        /// </param>
        /// <param name="queryFailureFunc">
        /// Callback to call when query has completed execution with errors. May be <c>null</c>.
        /// </param>
        public async Task InterServiceExecuteQuery(ExecuteRequestParamsBase executeParams,
            IEventSender queryEventSender,
            Func<Query, Task<bool>> queryCreateSuccessFunc,
            Func<string, Task> queryCreateFailFunc,
            Query.QueryAsyncEventHandler querySuccessFunc = null,
            Query.QueryAsyncErrorEventHandler queryFailureFunc = null)
        {
            Validate.IsNotNull(nameof(executeParams), executeParams);
            Validate.IsNotNull(nameof(queryEventSender), queryEventSender);

            Query newQuery;
            try
            {
                // Get a new active query
                newQuery = CreateQuery(executeParams);
                if (queryCreateSuccessFunc != null && !await queryCreateSuccessFunc(newQuery))
                {
                    // The callback doesn't want us to continue, for some reason
                    // It's ok if we leave the query behind in the active query list, the next call
                    // to execute will replace it.
                    newQuery.Dispose();
                    return;
                }
            }
            catch (Exception e)
            {
                // Call the failure callback if it was provided
                if (queryCreateFailFunc != null)
                {
                    await queryCreateFailFunc(e.Message);
                }
                return;
            }

            // Execute the query asynchronously
            ExecuteAndCompleteQuery(executeParams.OwnerUri, newQuery, queryEventSender, querySuccessFunc, queryFailureFunc);
        }
        
        private Query CreateQuery(ExecuteRequestParamsBase executeParams)
        {
            // Attempt to get the connection for the editor
            var datasource = _connectionService.GetDataSource(executeParams.OwnerUri); 
            if (datasource == null)
            {
                throw new ArgumentOutOfRangeException(nameof(executeParams.OwnerUri), SR.QueryServiceQueryInvalidOwnerUri);
            }

            // Attempt to clean out any old query on the owner URI
            // DevNote:
            //    if any oldQuery exists on the executeParams.OwnerUri but it has not yet executed,
            //    then shouldn't we cancel and clean out that query since we are about to create a new query object on the current OwnerUri.
            //
            if (_activeQueries.TryGetValue(executeParams.OwnerUri, out Query oldQuery) && (oldQuery.HasExecuted || oldQuery.HasCancelled || oldQuery.HasErrored))
            {
                oldQuery.Dispose();
                _activeQueries.TryRemove(executeParams.OwnerUri, out oldQuery);
            }
            
            // check if there are active query execution settings for the editor, otherwise, use the global settings
            if (_queryExecutionSettings.Value.TryGetValue(executeParams.OwnerUri, out QueryExecutionSettings settings))
            {                
                // special-case handling for query plan options to maintain compat with query execution API parameters
                // the logic is that if either the query execute API parameters or the active query settings 
                // request a plan then enable the query option
                ExecutionPlanOptions executionPlanOptions = executeParams.ExecutionPlanOptions;
                if (settings.IncludeActualExecutionPlanXml)
                {
                    executionPlanOptions.IncludeActualExecutionPlanXml = settings.IncludeActualExecutionPlanXml;
                }
                if (settings.IncludeEstimatedExecutionPlanXml)
                {
                    executionPlanOptions.IncludeEstimatedExecutionPlanXml = settings.IncludeEstimatedExecutionPlanXml;
                }
                settings.ExecutionPlanOptions = executionPlanOptions;
            }
            else
            {     
                settings = _sqlToolsSettings.QueryExecutionSettings;
                settings.ExecutionPlanOptions = executeParams.ExecutionPlanOptions;
            }

            // If we can't add the query now, it's assumed the query is in progress
            var newQuery = new Query(GetQueryString(executeParams), datasource, settings, GetBufferFileFactory(), executeParams.GetFullColumnSchema);
            if (!_activeQueries.TryAdd(executeParams.OwnerUri, newQuery))
            {
                newQuery.Dispose();
                throw new InvalidOperationException(SR.QueryServiceQueryInProgress);
            }

            Logger.Write(TraceEventType.Information, $"Query object for URI:'{executeParams.OwnerUri}' created");
            return newQuery;
        }
        
        private static void ExecuteAndCompleteQuery(string ownerUri, Query query,
            IEventSender eventSender,
            Query.QueryAsyncEventHandler querySuccessCallback,
            Query.QueryAsyncErrorEventHandler queryFailureCallback)
        {
            // Setup the callback to send the complete event
            async Task CompleteCallback(Query q)
            {
                // Send back the results
                QueryCompleteParams eventParams = new QueryCompleteParams {OwnerUri = ownerUri, BatchSummaries = q.BatchSummaries};

                Logger.Write(TraceEventType.Information, $"Query:'{ownerUri}' completed");
                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            }

            // Setup the callback to send the failure event
            async Task FailureCallback(Query q, Exception e)
            {
                // Send back the results
                QueryCompleteParams eventParams = new QueryCompleteParams {OwnerUri = ownerUri, BatchSummaries = q.BatchSummaries};

                Logger.Write(TraceEventType.Error, $"Query:'{ownerUri}' failed");
                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            }

            query.QueryCompleted += CompleteCallback;
            query.QueryFailed += FailureCallback;

            // Add the callbacks that were provided by the caller
            // If they're null, that's no problem
            query.QueryCompleted += querySuccessCallback;
            query.QueryFailed += queryFailureCallback;

            // Setup the batch callbacks
            async Task BatchStartCallback(Batch b)
            {
                BatchEventParams eventParams = new BatchEventParams {BatchSummary = b.Summary, OwnerUri = ownerUri};

                Logger.Write(TraceEventType.Information, $"Batch:'{b.Summary}' on Query:'{ownerUri}' started");
                await eventSender.SendEvent(BatchStartEvent.Type, eventParams);
            }

            query.BatchStarted += BatchStartCallback;

            async Task BatchCompleteCallback(Batch b)
            {
                BatchEventParams eventParams = new BatchEventParams {BatchSummary = b.Summary, OwnerUri = ownerUri};

                Logger.Write(TraceEventType.Information, $"Batch:'{b.Summary}' on Query:'{ownerUri}' completed");
                await eventSender.SendEvent(BatchCompleteEvent.Type, eventParams);
            }

            query.BatchCompleted += BatchCompleteCallback;

            async Task BatchMessageCallback(ResultMessage m)
            {
                MessageParams eventParams = new MessageParams {Message = m, OwnerUri = ownerUri};

                Logger.Write(TraceEventType.Information, $"Message generated on Query:'{ownerUri}' :'{m}'");
                await eventSender.SendEvent(MessageEvent.Type, eventParams);
            }

            query.BatchMessageSent += BatchMessageCallback;

            // Setup the ResultSet available callback
            async Task ResultAvailableCallback(ResultSet r)
            {
                ResultSetAvailableEventParams eventParams = new ResultSetAvailableEventParams
                {
                    ResultSetSummary = r.Summary, 
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is available");
                await eventSender.SendEvent(ResultSetAvailableEvent.Type, eventParams);
            }

            query.ResultSetAvailable += ResultAvailableCallback;

            // Setup the ResultSet updated callback
            async Task ResultUpdatedCallback(ResultSet r)
            {
                ResultSetUpdatedEventParams eventParams = new ResultSetUpdatedEventParams
                {
                    ResultSetSummary = r.Summary, 
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is updated with additional rows");
                await eventSender.SendEvent(ResultSetUpdatedEvent.Type, eventParams);
            }

            query.ResultSetUpdated += ResultUpdatedCallback;

            // Setup the ResultSet completion callback
            async Task ResultCompleteCallback(ResultSet r)
            {
                ResultSetCompleteEventParams eventParams = new ResultSetCompleteEventParams
                {
                    ResultSetSummary = r.Summary, 
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is complete");
                await eventSender.SendEvent(ResultSetCompleteEvent.Type, eventParams);
            }

            query.ResultSetCompleted += ResultCompleteCallback;

            // Launch this as an asynchronous task
            query.Execute();
        }
        
        private string GetQueryString(ExecuteRequestParamsBase executeParams)
        {
            if (executeParams is ExecuteDocumentSelectionParams selectionParams)
            {
                return _workspaceService.GetSqlTextFromSelectionData(executeParams.OwnerUri, selectionParams.QuerySelection);    
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Retrieves the requested subset of rows from the requested result set. Intended to be
        /// called by another service.
        /// </summary>
        /// <param name="subsetParams">Parameters for the subset to retrieve</param>
        /// <returns>The requested subset</returns>
        /// <exception cref="ArgumentOutOfRangeException">The requested query does not exist</exception>
        public async Task<ResultSetSubset> GetResultSubset(SubsetParams subsetParams)
        {
            Validate.IsNotNullOrEmptyString(nameof(subsetParams.OwnerUri), subsetParams.OwnerUri);

            // Attempt to load the query
            if (!_activeQueries.TryGetValue(subsetParams.OwnerUri, out Query query))
            {
                throw new ArgumentOutOfRangeException(SR.QueryServiceRequestsNoQuery);
            }

            // Retrieve the requested subset and return it
            return await query.GetSubset(subsetParams.BatchIndex, subsetParams.ResultSetIndex,
                subsetParams.RowsStartIndex, subsetParams.RowsCount);
        }
        
        /// <summary>
        /// Query disposal meant to be called from another service. Utilizes callbacks to allow
        /// custom actions to be performed on success or failure.
        /// </summary>
        /// <param name="ownerUri">The identifier of the query to be disposed</param>
        /// <param name="successAction">Action to perform on success</param>
        /// <param name="failureAction">Action to perform on failure</param>
        public async Task InterServiceDisposeQuery(string ownerUri, Func<Task> successAction,
            Func<string, Task> failureAction)
        {
            Validate.IsNotNull(nameof(successAction), successAction);
            Validate.IsNotNull(nameof(failureAction), failureAction);

            try
            {
                // Attempt to remove the query for the owner uri
                if (!_activeQueries.TryRemove(ownerUri, out Query result))
                {
                    await failureAction(SR.QueryServiceRequestsNoQuery);
                    return;
                }

                // Cleanup the query
                result.Dispose();

                // Success
                await successAction();
            }
            catch (Exception e)
            {
                await failureAction(e.Message);
            }
        }

        private IFileStreamFactory GetBufferFileFactory()
        {
            return _bufferFileFactory ??= new ServiceBufferFileStreamFactory
            {
                ExecutionSettings = _sqlToolsSettings.QueryExecutionSettings
            };
        }

        public void RemoveExecutionSetting(string uri)
        {
            if (_queryExecutionSettings.Value.ContainsKey(uri))
            {
                _queryExecutionSettings.Value.TryRemove(uri, out _);
            }
        }

        public void UpdateExecutionSettings(QueryExecutionSettings newSettings)
        {
            _sqlToolsSettings.QueryExecutionSettings.Update(newSettings);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var query in _activeQueries)
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
                            string message = $"Failed to cancel query {query.Key} during query service disposal: {e}";
                            Logger.Write(TraceEventType.Warning, message);
                        }
                    }
                    query.Value.Dispose();
                }
                _activeQueries.Clear();
            }

            _disposed = true;
        }
    }
}