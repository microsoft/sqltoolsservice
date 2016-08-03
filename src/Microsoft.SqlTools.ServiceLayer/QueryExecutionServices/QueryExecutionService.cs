using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices
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

        private ConcurrentDictionary<string, Query> Queries
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
            
        }

        private async Task HandleResultSubsetRequest(QueryExecuteSubsetParams subsetParams,
            RequestContext<QueryExecuteSubsetResult> requestContext)
        {
            await Task.FromResult(0);
        }

        private async Task HandleDisposeRequest(QueryDisposeParams disposeParams,
            RequestContext<QueryDisposeResult> requestContext)
        {
            string messages = null;

            Query result;
            if (!Queries.TryRemove(disposeParams., out result))
            {
                messages = "Failed to dispose query, ID not found.";
            }

            await requestContext.SendResult(new QueryDisposeResult
            {
                Messages = messages
            });
        }

        #endregion

    }
}
