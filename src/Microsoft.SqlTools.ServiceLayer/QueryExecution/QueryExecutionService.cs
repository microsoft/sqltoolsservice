//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
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

        private readonly Lazy<ConcurrentDictionary<string, Query>> queries =
            new Lazy<ConcurrentDictionary<string, Query>>(() => new ConcurrentDictionary<string, Query>());

        internal ConcurrentDictionary<string, Query> ActiveQueries
        {
            get { return queries.Value; }
        }

        private ConnectionService ConnectionService { get; set; }

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
                        Message = "The requested query does not exist."
                    });
                    return;
                }

                // Retrieve the requested subset and return it
                var result = new QueryExecuteSubsetResult
                {
                    Message = null,
                    ResultSubset = query.GetSubset(
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
                        Messages = "Failed to dispose query, ID not found."
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

        #endregion

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
                        Messages = "This editor is not connected to a database."
                    });
                    return null;
                }

                // Attempt to clean out any old query on the owner URI
                Query oldQuery;
                if (ActiveQueries.TryGetValue(executeParams.OwnerUri, out oldQuery) && oldQuery.HasExecuted)
                {
                    ActiveQueries.TryRemove(executeParams.OwnerUri, out oldQuery);
                }

                // If we can't add the query now, it's assumed the query is in progress
                Query newQuery = new Query(executeParams.QueryText, connectionInfo);
                if (!ActiveQueries.TryAdd(executeParams.OwnerUri, newQuery))
                {
                    await requestContext.SendResult(new QueryExecuteResult
                    {
                        Messages = "A query is already in progress for this editor session." +
                                   "Please cancel this query or wait for its completion."
                    });
                    return null;
                }

                return newQuery;
            }
            catch (ArgumentNullException ane)
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

            try
            {
                // Wait for query execution and then send back the results
                await Task.WhenAll(executeTask);
                QueryExecuteCompleteParams eventParams = new QueryExecuteCompleteParams
                {
                    Error = false,
                    Messages = new string[] { }, // TODO: Figure out how to get messages back from the server
                    OwnerUri = executeParams.OwnerUri,
                    ResultSetSummaries = query.ResultSummary
                };
                await requestContext.SendEvent(QueryExecuteCompleteEvent.Type, eventParams);
            }
            catch (DbException dbe)
            {
                // Dump the message to a complete event
                QueryExecuteCompleteParams errorEvent = new QueryExecuteCompleteParams
                {
                    Error = true,
                    Messages = new[] {dbe.Message},
                    OwnerUri = executeParams.OwnerUri,
                    ResultSetSummaries = query.ResultSummary
                };
                await requestContext.SendEvent(QueryExecuteCompleteEvent.Type, errorEvent);
            }
        }
    }
}
