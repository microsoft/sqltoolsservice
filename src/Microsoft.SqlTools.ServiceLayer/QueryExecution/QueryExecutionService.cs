using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public sealed class QueryExecutionService
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<QueryExecutionService> instance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());

        public static QueryExecutionService Instance
        {
            get { return instance.Value; }
        }

        private QueryExecutionService() { }

        #endregion

        #region Properties

        private readonly Lazy<ConcurrentDictionary<string, Query>> queries =
            new Lazy<ConcurrentDictionary<string, Query>>(() => new ConcurrentDictionary<string, Query>());

        private ConcurrentDictionary<string, Query> ActiveQueries
        {
            get { return queries.Value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(QueryExecuteRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(QueryExecuteSubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryDisposeRequest.Type, HandleDisposeRequest);

            // Register handlers for events
        }

        #endregion

        #region Request Handlers

        private async Task HandleExecuteRequest(QueryExecuteParams executeParams,
            RequestContext<QueryExecuteResult> requestContext)
        {
            // Attempt to get the connection for the editor
            ConnectionInfo connectionInfo;
            if(!ConnectionService.Instance.TryFindConnection(executeParams.OwnerUri, out connectionInfo))
            {
                await requestContext.SendError("This editor is not connected to a database.");
                return;
            }

            // If there is already an in-flight query, error out
            Query newQuery = new Query(executeParams.QueryText, connectionInfo);
            if (!ActiveQueries.TryAdd(executeParams.OwnerUri, newQuery))
            {
                await requestContext.SendError("A query is already in progress for this editor session." +
                                               "Please cancel this query or wait for its completion.");
                return;
            }

            // Launch the query and respond with successfully launching it
            Task executeTask = newQuery.Execute();
            await requestContext.SendResult(new QueryExecuteResult
            {
                Messages = null
            });

            // Wait for query execution and then send back the results
            await Task.WhenAll(executeTask);
            QueryExecuteCompleteParams eventParams = new QueryExecuteCompleteParams
            {
                Error = false,
                Messages = new string[]{},  // TODO: Figure out how to get messages back from the server
                OwnerUri = executeParams.OwnerUri,
                ResultSetSummaries = newQuery.ResultSummary
            };
            await requestContext.SendEvent(QueryExecuteCompleteEvent.Type, eventParams);
        }

        private async Task HandleResultSubsetRequest(QueryExecuteSubsetParams subsetParams,
            RequestContext<QueryExecuteSubsetResult> requestContext)
        {
            // Attempt to load the query
            Query query;
            if (!ActiveQueries.TryGetValue(subsetParams.OwnerUri, out query))
            {
                var errorResult = new QueryExecuteSubsetResult
                {
                    Message = "The requested query does not exist."
                };
                await requestContext.SendResult(errorResult);
                return;
            }

            try
            {
                // Retrieve the requested subset and return it
                var result = new QueryExecuteSubsetResult
                {
                    Message = null,
                    ResultSubset = query.GetSubset(
                        subsetParams.ResultSetIndex, subsetParams.RowsStartIndex, subsetParams.RowsCount)
                };
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendResult(new QueryExecuteSubsetResult
                {
                    Message = e.Message
                });
            }
        }

        private async Task HandleDisposeRequest(QueryDisposeParams disposeParams,
            RequestContext<QueryDisposeResult> requestContext)
        {
            // Attempt to remove the query for the owner uri
            Query result;
            if (!ActiveQueries.TryRemove(disposeParams.OwnerUri, out result))
            {
                await requestContext.SendError("Failed to dispose query, ID not found.");
                return;
            }

            // Success
            await requestContext.SendResult(new QueryDisposeResult
            {
                Messages = null
            });
        }

        #endregion

    }
}
