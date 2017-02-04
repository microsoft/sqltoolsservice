//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class EditDataService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<EditDataService> LazyInstance = new Lazy<EditDataService>(() => new EditDataService());

        public static EditDataService Instance => LazyInstance.Value;

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

        internal async Task HandleCreateRowRequest(EditCreateRowParams createParams,
            RequestContext<EditCreateRowResult> requestContext)
        {
            Session session = GetActiveSessionOrThrow(createParams.OwnerUri);
            try
            {
                // Create the row and get send the ID of the row back
                long newRowId = session.CreateRow();
                EditCreateRowResult createResult = new EditCreateRowResult
                {
                    NewRowId = newRowId
                };
                await requestContext.SendResult(createResult);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleDeleteRowRequest(EditDeleteRowParams deleteParams,
            RequestContext<EditDeleteRowResult> requestContext)
        {
            Session session = GetActiveSessionOrThrow(deleteParams.OwnerUri);
            try
            {
                // Add the delete row to the edit cache
                session.DeleteRow(deleteParams.RowId);
                await requestContext.SendResult(new EditDeleteRowResult());
            }
            catch (Exception e)
            {
                // Send back the error
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleDisposeRequest(EditDisposeParams disposeParams,
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

        internal async Task HandleInitializeRequest(EditInitializeParams initParams,
            RequestContext<EditInitializeResult> requestContext)
        {
            // Setup a callback for when the query has successfully created
            Func<Query, Task<bool>> queryCreateSuccessCallback = async query =>
            {
                await requestContext.SendResult(new EditInitializeResult());
                return true;
            };

            // Setup a callback for when the query failed to be created
            Func<string, Task> queryCreateFailureCallback = requestContext.SendError;

            // Setup a callback for when the query completes execution successfully
            Query.QueryAsyncEventHandler queryCompleteSuccessCallback = async query =>
            {
                EditSessionReadyParams readyParams = new EditSessionReadyParams
                {
                    OwnerUri = initParams.OwnerUri
                };

                try
                {
                    // Create the session and add it to the sessions list
                    Session session = new Session(query);
                    if (!ActiveSessions.TryAdd(initParams.OwnerUri, session))
                    {
                        throw new InvalidOperationException("Failed to create edit session, session already exists.");
                    }
                    readyParams.Success = true;
                }
                catch (Exception)
                {
                    // Request that the query be disposed
                    await queryExecutionService.InterServiceDisposeQuery(initParams.OwnerUri, null, null);
                    readyParams.Success = false;
                }

                // Send the edit session ready notification
                await requestContext.SendEvent(EditSessionReadyEvent.Type, readyParams);
            };

            // Setup a callback for when the query completes execution with failure
            Query.QueryAsyncEventHandler queryCompleteFailureCallback = query =>
            {
                EditSessionReadyParams readyParams = new EditSessionReadyParams
                {
                    OwnerUri = initParams.OwnerUri,
                    Success = false
                };
                return requestContext.SendEvent(EditSessionReadyEvent.Type, readyParams);
            };

            try
            {
                // Make sure we have info to process this request
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.OwnerUri), initParams.OwnerUri);
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectName), initParams.ObjectName);

                // Put together a query for the results and execute it
                ExecuteStringParams executeParams = new ExecuteStringParams
                {
                    Query = GetSelectQuery(initParams.ObjectName),
                    OwnerUri = initParams.OwnerUri
                };
                await queryExecutionService.InterServiceExecuteQuery(executeParams, requestContext,
                    queryCreateSuccessCallback, queryCreateFailureCallback,
                    queryCompleteSuccessCallback, queryCompleteFailureCallback);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleRevertRowRequest(EditRevertRowParams revertParams,
            RequestContext<EditRevertRowResult> requestContext)
        {
            Session session = GetActiveSessionOrThrow(revertParams.OwnerUri);
            try
            {
                session.RevertRow(revertParams.RowId);
                await requestContext.SendResult(new EditRevertRowResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleUpdateCellRequest(EditUpdateCellParams updateParams,
            RequestContext<EditUpdateCellResult> requestContext)
        {
            Session session = GetActiveSessionOrThrow(updateParams.OwnerUri);
            try
            {
                // @TODO: Figure out how to send back corrections
                session.UpdateCell(updateParams.RowId, updateParams.ColumnId, updateParams.NewValue);
                await requestContext.SendResult(new EditUpdateCellResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Returns the session with the given owner URI or throws if it can't be found
        /// </summary>
        /// <exception cref="Exception">If the edit session doesn't exist</exception>
        /// <param name="ownerUri">Owner URI for the edit session</param>
        /// <returns>The edit session that corresponds to the owner URI</returns>
        private Session GetActiveSessionOrThrow(string ownerUri)
        {
            // Sanity check the owner URI is provided
            Validate.IsNotNullOrWhitespaceString(nameof(ownerUri), ownerUri);

            // Attempt to get the session, throw if unable
            Session session;
            if (!ActiveSessions.TryGetValue(ownerUri, out session))
            {
                // @TODO: Move to constants file
                throw new Exception("Could not find an edit session with the given owner URI");
            }

            return session;
        }

        private string GetSelectQuery(string objectName)
        {
            // If the object is a multi-part identifier (eg, dbo.tablename) split it, and escape as necessary
            string[] identifierParts = objectName.Split('.');
            IEnumerable<string> escapedParts = identifierParts.Select(p => SqlScriptFormatter.FormatIdentifier(objectName.Trim('[', ']')));
            string escapedObject = string.Join(".", escapedParts);

            return $"SELECT * FROM {escapedObject}";
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
