using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.AzureMonitor.ServiceLayer.Workspace;
using Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.DataContracts.SqlContext;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution
{
    public class QueryExecutionService
    {
        private ConnectionService _connectionService;
        private WorkspaceService<SqlToolsSettings> _workspaceService;
        private ConcurrentDictionary<string, string> _activeQueries;
        private readonly Lazy<ConcurrentDictionary<string, QueryExecutionSettings>> _queryExecutionSettings =
            new Lazy<ConcurrentDictionary<string, QueryExecutionSettings>>(() => new ConcurrentDictionary<string, QueryExecutionSettings>());    
        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());
        
        public static QueryExecutionService Instance => LazyInstance.Value;

        public QueryExecutionService()
        {
            _activeQueries = new ConcurrentDictionary<string, string>();
        }
        
        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            _connectionService = connectionService;
            _workspaceService = workspaceService;
            
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteDocumentStatementRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
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

        private async Task HandleCancelRequest(QueryCancelParams cancelParams, RequestContext<QueryCancelResult> requestContext)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
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
    }
}