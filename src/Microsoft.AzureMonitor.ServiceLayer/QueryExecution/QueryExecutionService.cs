using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.AzureMonitor.ServiceLayer.Localization;
using Microsoft.AzureMonitor.ServiceLayer.Workspace;
using Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.DataContracts.SqlContext;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution
{
    public class QueryExecutionService: IDisposable
    {
        private ConnectionService _connectionService;
        private WorkspaceService<SqlToolsSettings> _workspaceService;
        private readonly ConcurrentDictionary<string, Query> _activeQueries;
        private readonly Lazy<ConcurrentDictionary<string, QueryExecutionSettings>> _queryExecutionSettings =
            new Lazy<ConcurrentDictionary<string, QueryExecutionSettings>>(() => new ConcurrentDictionary<string, QueryExecutionSettings>());    
        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());
        private bool _disposed;
        
        public static QueryExecutionService Instance => LazyInstance.Value;

        public QueryExecutionService()
        {
            _activeQueries = new ConcurrentDictionary<string, Query>();
        }
        
        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            _connectionService = connectionService;
            _workspaceService = workspaceService;
            
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteDocumentStatementRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(SubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
            
            // Register the file open update handler
            workspaceService.RegisterTextDocCloseCallback(HandleDidCloseTextDocumentNotification);
        }

        private async Task HandleExecuteRequest(ExecuteRequestParamsBase executeParams, RequestContext<ExecuteRequestResult> requestContext)
        {
            try
            {
                var datasource = _connectionService.GetDataSource(executeParams.OwnerUri);
                var query = GetQueryString(executeParams);
                var result = await datasource.QueryAsync(query, new CancellationToken());
                await requestContext.SendResult(new ExecuteRequestResult());
                
                var queryCompleteParams = new QueryCompleteParams
                {
                    OwnerUri = executeParams.OwnerUri,
                    BatchSummaries = new []
                    {
                        new BatchSummary
                        {
                            ResultSetSummaries = new []
                            {
                                new ResultSetSummary
                                {
                                    ColumnInfo = result.Columns
                                }
                            }
                        }
                    }
                };

                await requestContext.SendEvent(QueryCompleteEvent.Type, queryCompleteParams);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private string GetQueryString(ExecuteRequestParamsBase executeParams)
        {
            if (executeParams is ExecuteDocumentSelectionParams selectionParams)
            {
                return _workspaceService.GetSqlTextFromSelectionData(executeParams.OwnerUri, selectionParams.QuerySelection);    
            }

            return string.Empty;
        }
        
        private async Task HandleResultSubsetRequest(SubsetParams subsetParams, RequestContext<SubsetResult> requestContext)
        {
            try
            {
                ResultSetSubset subset = await InterServiceResultSubset(subsetParams);
                var result = new SubsetResult
                {
                    ResultSubset = subset
                };
                await requestContext.SendResult(result);
                Logger.Write(TraceEventType.Stop, $"Done Handler for Subset request with for Query:'{subsetParams.OwnerUri}', Batch:'{subsetParams.BatchIndex}', ResultSetIndex:'{subsetParams.ResultSetIndex}', RowsStartIndex'{subsetParams.RowsStartIndex}', Requested RowsCount:'{subsetParams.RowsCount}'\r\n\t\t with subset response of:[ RowCount:'{subset.RowCount}', Rows array of length:'{subset.Rows.Length}']");
            }
            catch (Exception e)
            {
                // This was unexpected, so send back as error
                await requestContext.SendError(e.Message);
            }
        }
        
        /// <summary>
        /// Retrieves the requested subset of rows from the requested result set. Intended to be
        /// called by another service.
        /// </summary>
        /// <param name="subsetParams">Parameters for the subset to retrieve</param>
        /// <returns>The requested subset</returns>
        /// <exception cref="ArgumentOutOfRangeException">The requested query does not exist</exception>
        private async Task<ResultSetSubset> InterServiceResultSubset(SubsetParams subsetParams)
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

        private async Task HandleCancelRequest(QueryCancelParams cancelParams, RequestContext<QueryCancelResult> requestContext)
        {
            try
            {
                // Attempt to find the query for the owner uri
                if (!_activeQueries.TryGetValue(cancelParams.OwnerUri, out Query query))
                {
                    await requestContext.SendResult(new QueryCancelResult
                    {
                        Messages = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Cancel the query and send a success message
                query.Cancel();
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
        /// Handle the file open notification
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="scriptFile"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        private async Task HandleDidCloseTextDocumentNotification(string uri, ScriptFile scriptFile, EventContext eventContext)
        {
            try
            {
                // remove any query execution settings when an editor is closed
                if (_queryExecutionSettings.Value.ContainsKey(uri))
                {
                    _queryExecutionSettings.Value.TryRemove(uri, out _);
                }                
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
            }
            await Task.FromResult(true);
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