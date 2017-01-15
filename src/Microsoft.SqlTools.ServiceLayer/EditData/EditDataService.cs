//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class EditDataService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<EditDataService> instance = new Lazy<EditDataService>(() => new EditDataService());

        public static EditDataService Instance => instance.Value;

        private EditDataService()
        {
            queryExecutionService = QueryExecutionService.Instance;
        }

        internal EditDataService(QueryExecutionService qes)
        {
            queryExecutionService = qes;
        }

        #endregion

        #region Member Variables 

        private readonly QueryExecutionService queryExecutionService;

        private readonly Lazy<ConcurrentDictionary<string, Session>> editSessions = new Lazy<ConcurrentDictionary<string, Session>>(
            () => new ConcurrentDictionary<string, Session>());

        #endregion

        #region Properties

        /// <summary>
        /// Dictionary mapping OwnerURIs to active sessions
        /// </summary>
        internal ConcurrentDictionary<string, Session> ActiveSessions => editSessions.Value;

        #endregion

        /// <summary>
        /// Initializes the edit data service with the service host
        /// </summary>
        /// <param name="serviceHost">The service host to register commands/events with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(EditCreateRowRequest.Type, HandleCreateRowRequest);
            serviceHost.SetRequestHandler(EditDeleteRowRequest.Type, HandleDeleteRowRequest);
            serviceHost.SetRequestHandler(EditDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(EditInitializeRequest.Type, HandleInitializeRequest);
            serviceHost.SetRequestHandler(EditRevertRowRequest.Type, HandleRevertRowRequest);
            serviceHost.SetRequestHandler(EditUpdateCellRequest.Type, HandleUpdateCellRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                Dispose();
                return Task.FromResult(0);
            });
        }

        #region Request Handlers

        public async Task HandleCreateRowRequest(EditCreateRowParams createParams,
            RequestContext<EditCreateRowResult> requestContext)
        {
            
        }

        public async Task HandleDeleteRowRequest(EditDeleteRowParams deleteParams,
            RequestContext<EditDeleteRowResult> requestContext)
        {

        }

        public async Task HandleDisposeRequest(EditDisposeParams disposeParams,
            RequestContext<EditDisposeResult> requestContext)
        {
            // Sanity check the owner URI
            Validate.IsNotNullOrWhitespaceString(nameof(disposeParams.OwnerUri), disposeParams.OwnerUri);

            // Attempt to remove the session
            Session session;
            if (!ActiveSessions.TryRemove(disposeParams.OwnerUri, out session))
            {
                // @TODO: Move to constants file
                await requestContext.SendError("Failed to dispose session, session does not exist.");
                return;
            }

            // Everything was successful, return success
            await requestContext.SendResult(new EditDisposeResult());
        }

        public async Task HandleInitializeRequest(EditInitializeParams initParams,
            RequestContext<EditInitializeResult> requestContext)
        {
            // Verify that the query exists
            Query query;
            if (!queryExecutionService.ActiveQueries.TryGetValue(initParams.OwnerUri, out query))
            {
                await requestContext.SendError("Failed to create edit session, query does not exist.");
                return;
            }

            try
            {
                // Create the session and add it to the sessions list
                Session session = new Session(query);
                if (!ActiveSessions.TryAdd(initParams.OwnerUri, session))
                {
                    // @TODO: Move to constants file
                    await requestContext.SendError("Failed to create edit session, session already exists.");
                    return;
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }

            // Everything was successful, return success
            await requestContext.SendResult(new EditInitializeResult());
        }

        public async Task HandleRevertRowRequest(EditRevertRowParams revertParams,
            RequestContext<EditRevertRowResult> requestContext)
        {
            
        }

        public async Task HandleUpdateCellRequest(EditUpdateCellParams updateParams,
            RequestContext<EditUpdateCellResult> requestContext)
        {
            
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
                // TODO: Dispose objects that need disposing
            }

            disposed = true;
        }

        ~EditDataService()
        {
            Dispose(false);
        }

        #endregion

    }
}
