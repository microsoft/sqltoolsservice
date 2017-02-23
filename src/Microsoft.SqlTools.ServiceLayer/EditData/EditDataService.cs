//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Utility;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Service that handles edit data scenarios
    /// </summary>
    public class EditDataService
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<EditDataService> LazyInstance = new Lazy<EditDataService>(() => new EditDataService());

        public static EditDataService Instance => LazyInstance.Value;

        private EditDataService()
        {
            queryExecutionService = QueryExecutionService.Instance;
            connectionService = ConnectionService.Instance;
            metadataFactory = new SmoEditMetadataFactory();
        }

        internal EditDataService(QueryExecutionService qes, ConnectionService cs, IEditMetadataFactory factory)
        {
            queryExecutionService = qes;
            connectionService = cs;
            metadataFactory = factory;
        }

        #endregion

        #region Member Variables 

        private readonly ConnectionService connectionService;

        private readonly IEditMetadataFactory metadataFactory;

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
        }

        #region Request Handlers

        internal async Task HandleSessionRequest<TResult>(SessionOperationParams sessionParams,
            RequestContext<TResult> requestContext, Func<Session, TResult> sessionOperation)
        {
            try
            {
                Session session = GetActiveSessionOrThrow(sessionParams.OwnerUri);

                // Get the result from execution of the session operation
                TResult result = sessionOperation(session);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal Task HandleCreateRowRequest(EditCreateRowParams createParams,
            RequestContext<EditCreateRowResult> requestContext)
        {
            return HandleSessionRequest(createParams, requestContext, session =>
            {
                // Create the row and get the ID of the new row
                long newRowId = session.CreateRow();
                return new EditCreateRowResult
                {
                    NewRowId = newRowId
                };
            });
        }

        internal Task HandleDeleteRowRequest(EditDeleteRowParams deleteParams,
            RequestContext<EditDeleteRowResult> requestContext)
        {
            return HandleSessionRequest(deleteParams, requestContext, session =>
            {
                // Add the delete row to the edit cache
                session.DeleteRow(deleteParams.RowId);
                return new EditDeleteRowResult();
            });
        }

        internal async Task HandleDisposeRequest(EditDisposeParams disposeParams,
            RequestContext<EditDisposeResult> requestContext)
        {
            try
            {
                // Sanity check the owner URI
                Validate.IsNotNullOrWhitespaceString(nameof(disposeParams.OwnerUri), disposeParams.OwnerUri);

                // Attempt to remove the session
                Session session;
                if (!ActiveSessions.TryRemove(disposeParams.OwnerUri, out session))
                {
                    await requestContext.SendError(SR.EditDataSessionNotFound);
                    return;
                }

                // Everything was successful, return success
                await requestContext.SendResult(new EditDisposeResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleInitializeRequest(EditInitializeParams initParams,
            RequestContext<EditInitializeResult> requestContext)
        {
            try
            {          
                // Make sure we have info to process this request
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.OwnerUri), initParams.OwnerUri);
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectName), initParams.ObjectName);

                // Setup a callback for when the query has successfully created
                Func<Query, Task<bool>> queryCreateSuccessCallback = async query =>
                {
                    await requestContext.SendResult(new EditInitializeResult());
                    return true;
                };

                // Setup a callback for when the query failed to be created
                Func<string, Task> queryCreateFailureCallback = requestContext.SendError;

                // Setup a callback for when the query completes execution successfully
                Query.QueryAsyncEventHandler queryCompleteSuccessCallback =
                    q => QueryCompleteCallback(q, initParams, requestContext);

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

                // Put together a query for the results and execute it
                ExecuteStringParams executeParams = new ExecuteStringParams
                {
                    Query = $"SELECT * FROM {SqlScriptFormatter.FormatMultipartIdentifier(initParams.ObjectName)}",
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

        internal Task HandleRevertRowRequest(EditRevertRowParams revertParams,
            RequestContext<EditRevertRowResult> requestContext)
        {
            return HandleSessionRequest(revertParams, requestContext, session =>
            {
                session.RevertRow(revertParams.RowId);
                return new EditRevertRowResult();
            });
        }

        internal Task HandleUpdateCellRequest(EditUpdateCellParams updateParams,
            RequestContext<EditUpdateCellResult> requestContext)
        {
            return HandleSessionRequest(updateParams, requestContext,
                session => session.UpdateCell(updateParams.RowId, updateParams.ColumnId, updateParams.NewValue));
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
                throw new Exception(SR.EditDataSessionNotFound);
            }

            return session;
        }

        private async Task QueryCompleteCallback(Query query, EditInitializeParams initParams,
            IEventSender requestContext)
        {
            EditSessionReadyParams readyParams = new EditSessionReadyParams
            {
                OwnerUri = initParams.OwnerUri
            };

            try
            {
                // Validate the query for a session
                ResultSet resultSet = Session.ValidateQueryForSession(query);

                // Get a connection we'll use for SMO metadata lookup (and committing, later on)
                DbConnection conn = await connectionService.GetOrOpenConnection(initParams.OwnerUri, ConnectionType.Edit);
                var metadata = metadataFactory.GetObjectMetadata(conn, resultSet.Columns,
                    initParams.ObjectName, initParams.ObjectType);

                // Create the session and add it to the sessions list
                Session session = new Session(resultSet, metadata);
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
        }

        #endregion

    }
}
