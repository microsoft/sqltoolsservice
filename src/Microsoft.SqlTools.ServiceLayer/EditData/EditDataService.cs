//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
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

        private readonly Lazy<ConcurrentDictionary<string, EditSession>> editSessions = new Lazy<ConcurrentDictionary<string, EditSession>>(
            () => new ConcurrentDictionary<string, EditSession>());

        private readonly Lazy<ConcurrentDictionary<string, TaskCompletionSource<bool>>> initializeWaitHandles =
            new Lazy<ConcurrentDictionary<string, TaskCompletionSource<bool>>>(
                () => new ConcurrentDictionary<string, TaskCompletionSource<bool>>());

        #endregion

        #region Properties

        /// <summary>
        /// Dictionary mapping OwnerURIs to active sessions
        /// </summary>
        internal ConcurrentDictionary<string, EditSession> ActiveSessions => editSessions.Value;

        /// <summary>
        /// Dictionary mapping OwnerURIs to wait handlers for initialize tasks. Pretty much only
        /// provided for unit test scenarios.
        /// </summary>
        internal ConcurrentDictionary<string, TaskCompletionSource<bool>> InitializeWaitHandles => initializeWaitHandles.Value;

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
            RequestContext<TResult> requestContext, Func<EditSession, TResult> sessionOperation)
        {
            try
            {
                EditSession editSession = GetActiveSessionOrThrow(sessionParams.OwnerUri);

                // Get the result from execution of the editSession operation
                TResult result = sessionOperation(editSession);
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

                // Attempt to remove the editSession
                EditSession editSession;
                if (!ActiveSessions.TryRemove(disposeParams.OwnerUri, out editSession))
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
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectType), initParams.ObjectType);

                // Try to add a new wait handler to the 
                if (!InitializeWaitHandles.TryAdd(initParams.OwnerUri, new TaskCompletionSource<bool>()))
                {
                    throw new InvalidOperationException(SR.EditDataInitializeInProgress);
                }
    
                // Setup a callback for when the query has successfully created
                Func<Query, Task<bool>> queryCreateSuccessCallback = async query =>
                {
                    await requestContext.SendResult(new EditInitializeResult());
                    return true;
                };

                // Setup a callback for when the query failed to be created
                Func<string, Task> queryCreateFailureCallback = async message =>
                {
                    await requestContext.SendError(message);
                    CompleteInitializeWaitHandler(initParams.OwnerUri, false);
                };

                // Setup a callback for when the query completes execution successfully
                Query.QueryAsyncEventHandler queryCompleteSuccessCallback =
                    q => QueryCompleteCallback(q, initParams, requestContext);

                // Setup a callback for when the query completes execution with failure
                Query.QueryAsyncEventHandler queryCompleteFailureCallback = async query =>
                {
                    EditSessionReadyParams readyParams = new EditSessionReadyParams
                    {
                        OwnerUri = initParams.OwnerUri,
                        Success = false
                    };
                    await requestContext.SendEvent(EditSessionReadyEvent.Type, readyParams);
                    CompleteInitializeWaitHandler(initParams.OwnerUri, false);
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
                CompleteInitializeWaitHandler(initParams.OwnerUri, false);
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

        internal async Task HandleCommitRequest(EditCommitParams commitParams,
            RequestContext<EditCommitResult> requestContext)
        {
            // Setup a callback for if the edits have been successfully written to the db
            Func<Task> successHandler = () => requestContext.SendResult(new EditCommitResult());

            // Setup a callback for if the edits failed to be written to db
            Func<Exception, Task> failureHandler = e => requestContext.SendError(e.Message);

            try
            {
                // Get the editSession
                EditSession editSession = GetActiveSessionOrThrow(commitParams.OwnerUri);

                // Get a connection for doing the committing
                DbConnection conn = await connectionService.GetOrOpenConnection(commitParams.OwnerUri,
                    ConnectionType.Edit);
                editSession.CommitEdits(conn, successHandler, failureHandler);
            }
            catch (Exception e)
            {
                await failureHandler(e);
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
        private EditSession GetActiveSessionOrThrow(string ownerUri)
        {
            // Sanity check the owner URI is provided
            Validate.IsNotNullOrWhitespaceString(nameof(ownerUri), ownerUri);

            // Attempt to get the editSession, throw if unable
            EditSession editSession;
            if (!ActiveSessions.TryGetValue(ownerUri, out editSession))
            {
                throw new Exception(SR.EditDataSessionNotFound);
            }

            return editSession;
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
                // Validate the query for a editSession
                ResultSet resultSet = EditSession.ValidateQueryForSession(query);

                // Get a connection we'll use for SMO metadata lookup (and committing, later on)
                DbConnection conn = await connectionService.GetOrOpenConnection(initParams.OwnerUri, ConnectionType.Edit);
                var metadata = metadataFactory.GetObjectMetadata(conn, resultSet.Columns,
                    initParams.ObjectName, initParams.ObjectType);

                // Create the editSession and add it to the sessions list
                EditSession editSession = new EditSession(resultSet, metadata);
                if (!ActiveSessions.TryAdd(initParams.OwnerUri, editSession))
                {
                    throw new InvalidOperationException("Failed to create edit editSession, editSession already exists.");
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
            CompleteInitializeWaitHandler(initParams.OwnerUri, true);
        }

        private void CompleteInitializeWaitHandler(string ownerUri, bool result)
        {
            // If there isn't a wait handler, just ignore it
            TaskCompletionSource<bool> initializeWaiter;
            if (ownerUri != null && InitializeWaitHandles.TryRemove(ownerUri, out initializeWaiter))
            {
                initializeWaiter.SetResult(result);
            }
        }

        #endregion

    }
}
