using System;
using System.Diagnostics;
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
        private QueryExecutionManager _queryExecutionManager;
        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());
        public static QueryExecutionService Instance => LazyInstance.Value;

        public void InitializeService(ServiceHost serviceHost, ConnectionService connectionService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            _queryExecutionManager = new QueryExecutionManager(connectionService, workspaceService);
            
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteDocumentStatementRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(SubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
            serviceHost.SetRequestHandler(SimpleExecuteRequest.Type, HandleSimpleExecuteRequest);
            
            // Register the file open update handler
            workspaceService.RegisterTextDocCloseCallback(HandleDidCloseTextDocumentNotification);
            
            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                _queryExecutionManager.Dispose();
                return Task.FromResult(0);
            });
            
            // Register a handler for when the configuration changes
            workspaceService.RegisterConfigChangeCallback(UpdateSettings);
        }
        
        private async Task HandleExecuteRequest(ExecuteRequestParamsBase executeParams, RequestContext<ExecuteRequestResult> requestContext)
        {
            try
            {
                // Setup actions to perform upon successful start and on failure to start
                async Task<bool> QueryCreateSuccessAction(Query q)
                {
                    await requestContext.SendResult(new ExecuteRequestResult());
                    Logger.Write(TraceEventType.Stop, $"Response for Query: '{executeParams.OwnerUri} sent. Query Complete!");
                    return true;
                }

                Task QueryCreateFailureAction(string message)
                {
                    Logger.Write(TraceEventType.Warning, $"Failed to create Query: '{executeParams.OwnerUri}. Message: '{message}' Complete!");
                    return requestContext.SendError(message);
                }

                // Use the internal handler to launch the query
                Parallel.Invoke(async () =>
                {
                    await _queryExecutionManager.InterServiceExecuteQuery(executeParams, requestContext, QueryCreateSuccessAction, QueryCreateFailureAction);
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
        
        private async Task HandleResultSubsetRequest(SubsetParams subsetParams, RequestContext<SubsetResult> requestContext)
        {
            try
            {
                ResultSetSubset subset = await _queryExecutionManager.GetResultSubset(subsetParams);
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
        
        private async Task HandleDisposeRequest(QueryDisposeParams disposeParams, RequestContext<QueryDisposeResult> requestContext)
        {
            // Setup action for success and failure
            Task SuccessAction() => requestContext.SendResult(new QueryDisposeResult());
            Task FailureAction(string message) => requestContext.SendError(message);

            // Use the inter-service dispose functionality
            await _queryExecutionManager.InterServiceDisposeQuery(disposeParams.OwnerUri, SuccessAction, FailureAction);
        }

        private async Task HandleCancelRequest(QueryCancelParams cancelParams, RequestContext<QueryCancelResult> requestContext)
        {
            try
            {
                await _queryExecutionManager.CancelQuery(cancelParams.OwnerUri, requestContext);
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
        
        private Task HandleSimpleExecuteRequest(SimpleExecuteParams executeParams, RequestContext<SimpleExecuteResult> requestContext)
        {
            throw new NotImplementedException();
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
                _queryExecutionManager.RemoveExecutionSetting(uri);
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex);
            }
            await Task.FromResult(true);
        }
        
        private Task UpdateSettings(SqlToolsSettings newSettings, SqlToolsSettings oldSettings, EventContext eventContext)
        {
            _queryExecutionManager.UpdateExecutionSettings(newSettings.QueryExecutionSettings);
            return Task.FromResult(0);
        }
    }
}