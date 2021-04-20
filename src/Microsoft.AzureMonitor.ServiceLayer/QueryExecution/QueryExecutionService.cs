using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution
{
    public class QueryExecutionService
    {
        private ConnectionService _connectionService;
        private ConcurrentDictionary<string, string> _activeQueries;
        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());
        public static QueryExecutionService Instance => LazyInstance.Value;

        public QueryExecutionService()
        {
            _activeQueries = new ConcurrentDictionary<string, string>();
        }
        
        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService)
        {
            _connectionService = connectionService;
            
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteDocumentStatementRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
        }

        private async Task HandleExecuteRequest(ExecuteRequestParamsBase executeParams, RequestContext<ExecuteRequestResult> requestContext)
        {
            try
            {
                var datasource = _connectionService.GetDataSource(executeParams.OwnerUri);

                var result = await datasource.QueryAsync("union * | summarize count() by _ResourceId, Type", new CancellationToken());
                await requestContext.SendResult(new ExecuteRequestResult());

                var queryCompleteParams = new QueryCompleteParams
                {
                    OwnerUri = executeParams.OwnerUri,
                    BatchSummaries = new BatchSummary[0]
                };

                await requestContext.SendEvent(QueryCompleteEvent.Type, queryCompleteParams);

            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
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
    }
}