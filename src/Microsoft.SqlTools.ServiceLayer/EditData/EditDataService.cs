//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
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

        private IEventSender eventSender;

        private readonly Lazy<ConcurrentDictionary<string, EditSession>> editSessions = new Lazy<ConcurrentDictionary<string, EditSession>>(
            () => new ConcurrentDictionary<string, EditSession>());

        #endregion

        #region Properties

        /// <summary>
        /// Dictionary mapping OwnerURIs to active sessions
        /// </summary>
        internal ConcurrentDictionary<string, EditSession> ActiveSessions => editSessions.Value;

        #endregion

        /// <summary>
        /// Initializes the edit data service with the service host
        /// </summary>
        /// <param name="serviceHost">The service host to register commands/events with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.eventSender = serviceHost;

            // Register handlers for requests
            serviceHost.RegisterRequestHandler(EditCreateRowRequest.Type, HandleCreateRowRequest);
            serviceHost.RegisterRequestHandler(EditDeleteRowRequest.Type, HandleDeleteRowRequest);
            serviceHost.RegisterRequestHandler(EditDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.RegisterRequestHandler(EditInitializeRequest.Type, HandleInitializeRequest);
            serviceHost.RegisterRequestHandler(EditRevertCellRequest.Type, HandleRevertCellRequest);
            serviceHost.RegisterRequestHandler(EditRevertRowRequest.Type, HandleRevertRowRequest);
            serviceHost.RegisterRequestHandler(EditSubsetRequest.Type, HandleSubsetRequest);
            serviceHost.RegisterRequestHandler(EditUpdateCellRequest.Type, HandleUpdateCellRequest);
            serviceHost.RegisterRequestHandler(EditCommitRequest.Type, HandleCommitRequest);
            serviceHost.RegisterRequestHandler(EditScriptRequest.Type, HandleEditScriptRequest);
            serviceHost.RegisterRequestHandler(EditCancelRequest.Type, HandleCancelRequest);
        }

        #region Request Handlers

        internal async Task<TResult> HandleSessionRequest<TResult>(SessionOperationParams sessionParams, Func<EditSession, TResult> sessionOperation)
        {
            try
            {
                EditSession editSession = GetActiveSessionOrThrow(sessionParams.OwnerUri);

                // Get the result from execution of the editSession operation
                TResult result = sessionOperation(editSession);
                return result;
            }
            catch (Exception e)
            {
                throw RpcErrorException.Create(e.Message);
            }
        }

        internal async Task<TResult> HandleSessionRequestAsync<TResult>(SessionOperationParams sessionParams, Func<EditSession, Task<TResult>> sessionOperationAsync)
        {
            try
            {
                EditSession editSession = GetActiveSessionOrThrow(sessionParams.OwnerUri);

                // Get the result from execution of the async editSession operation
                TResult result = await sessionOperationAsync(editSession);
                return result;
            }
            catch (Exception e)
            {
                throw RpcErrorException.Create(e.Message);
            }
        }

        internal Task<EditCreateRowResult> HandleCreateRowRequest(EditCreateRowParams createParams)
        {
            return HandleSessionRequestAsync(createParams, async session =>
            {
                EditCreateRowResult result = session.CreateRow();

                EditRow[] rows = await session.GetRows(result.NewRowId, rowCount: 1);
                result.Row = rows.Length > 0 ? rows[0] : null;

                return result;
            });
        }

        internal Task<EditDeleteRowResult> HandleDeleteRowRequest(EditDeleteRowParams deleteParams)
        {
            return HandleSessionRequest(deleteParams, session =>
            {
                // Add the delete row to the edit cache
                session.DeleteRow(deleteParams.RowId);
                return new EditDeleteRowResult();
            });
        }

        internal async Task<EditDisposeResult> HandleDisposeRequest(EditDisposeParams disposeParams)
        {
            // Sanity check the owner URI
            Validate.IsNotNullOrWhitespaceString(nameof(disposeParams.OwnerUri), disposeParams.OwnerUri);

            // Attempt to remove the editSession
            EditSession editSession;
            if (!ActiveSessions.TryRemove(disposeParams.OwnerUri, out editSession))
            {
                throw new Exception(SR.EditDataSessionNotFound);
            }

            // Everything was successful, return success
            return new EditDisposeResult();
        }

        internal async Task<EditCancelResult> HandleCancelRequest(EditCancelParams cancelParams)
        {
            try
            {
                Validate.IsNotNullOrWhitespaceString(nameof(cancelParams.OwnerUri), cancelParams.OwnerUri);

                // Cancel the underlying query in QueryExecutionService.
                // We do not require an active EditSession because the session may still
                // be initializing when the user clicks Cancel.
                Query query;
                if (queryExecutionService.ActiveQueries.TryGetValue(cancelParams.OwnerUri, out query))
                {
                    query.Cancel();
                }

                return new EditCancelResult();
            }
            catch (InvalidOperationException)
            {
                // Query already completed - treat as success
                return new EditCancelResult();
            }
            catch (Exception e)
            {
                throw RpcErrorException.Create(e.Message);
            }
        }

        internal async Task<EditInitializeResult> HandleInitializeRequest(EditInitializeParams initParams)
        {
            InitializeEditRequestContext context = new InitializeEditRequestContext(this.eventSender);
            Func<Exception, Task> executionFailureHandler = (e) => SendSessionReadyEvent(context, initParams.OwnerUri, false, e.Message);
            Func<Task> executionSuccessHandler = () => SendSessionReadyEvent(context, initParams.OwnerUri, true, null);

            EditSession.Connector connector = () => connectionService.GetOrOpenConnection(initParams.OwnerUri, ConnectionType.Edit, alwaysPersistSecurity: true);
            EditSession.QueryRunner queryRunner = q => SessionInitializeQueryRunner(initParams.OwnerUri, context, q);

            // Make sure we have info to process this request
            Validate.IsNotNullOrWhitespaceString(nameof(initParams.OwnerUri), initParams.OwnerUri);
            Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectName), initParams.ObjectName);
            Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectType), initParams.ObjectType);

            // Create a session and add it to the session list
            EditSession session = new EditSession(metadataFactory);
            if (!ActiveSessions.TryAdd(initParams.OwnerUri, session))
            {
                throw new InvalidOperationException(SR.EditDataSessionAlreadyExists);
            }

            context.ResultSetHandler = resultSetEventParams => { session.UpdateColumnInformationWithMetadata(resultSetEventParams.ResultSetSummary.ColumnInfo); };

            // Initialize the session
            session.Initialize(initParams, connector, queryRunner, executionSuccessHandler, executionFailureHandler);

            // Send the result
            return new EditInitializeResult();
        }

        internal Task<EditRevertCellResult> HandleRevertCellRequest(EditRevertCellParams revertParams)
        {
            return HandleSessionRequest(revertParams,
                session => session.RevertCell(revertParams.RowId, revertParams.ColumnId));
        }

        internal Task<EditRevertRowResult> HandleRevertRowRequest(EditRevertRowParams revertParams)
        {
            return HandleSessionRequestAsync(revertParams, async session =>
            {
                EditRow revertedRow = await session.RevertRow(revertParams.RowId);

                return new EditRevertRowResult
                {
                    Row = revertedRow
                };
            });
        }

        internal Task<EditSubsetResult> HandleSubsetRequest(EditSubsetParams subsetParams)
        {
            return HandleSessionRequestAsync(subsetParams, async session =>
            {
                EditRow[] rows = await session.GetRows(subsetParams.RowStartIndex, subsetParams.RowCount);
                
                return new EditSubsetResult
                {
                    RowCount = rows.Length,
                    Subset = rows,
                    ColumnInfo = session.GetColumnInfo()
                };
            });
        }

        internal Task<EditUpdateCellResult> HandleUpdateCellRequest(EditUpdateCellParams updateParams)
        {
            return HandleSessionRequest(updateParams,
                session => session.UpdateCell(updateParams.RowId, updateParams.ColumnId, updateParams.NewValue));
        }

        internal async Task<EditCommitResult> HandleCommitRequest(EditCommitParams commitParams)
        {
            TaskCompletionSource<EditCommitResult> taskCompletion = new TaskCompletionSource<EditCommitResult>();

            // Setup a callback for if the edits have been successfully written to the db
            Func<Task> successHandler = () =>
            {
                taskCompletion.TrySetResult(new EditCommitResult());
                return Task.CompletedTask;
            };

            // Setup a callback for if the edits failed to be written to db
            Func<Exception, Task> failureHandler = e =>
            {
                taskCompletion.TrySetException(RpcErrorException.Create(e));
                return Task.CompletedTask;
            };

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

            return await taskCompletion.Task;
        }

        internal Task<EditScriptResult> HandleEditScriptRequest(EditScriptParams scriptParams)
        {
            return HandleSessionRequest(scriptParams, session =>
            {
                string[] scripts = session.ScriptEdits();
                return new EditScriptResult { Scripts = scripts };
            });
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

        private async Task<EditSession.EditSessionQueryExecutionState> SessionInitializeQueryRunner(string ownerUri,
            IEventSender eventSender, string query)
        {
            // Open a task completion source, effectively creating a synchronous block
            TaskCompletionSource<EditSession.EditSessionQueryExecutionState> taskCompletion =
                new TaskCompletionSource<EditSession.EditSessionQueryExecutionState>();

            // Setup callback for successful query creation
            // NOTE: We do not want to set the task completion source, since we will continue executing the query after
            Func<Query, Task<bool>> queryCreateSuccessCallback = q => Task.FromResult(true);

            // Setup callback for failed query creation
            Func<string, Task> queryCreateFailureCallback = m =>
            {
                taskCompletion.SetResult(new EditSession.EditSessionQueryExecutionState(null, m));
                return Task.FromResult(0);
            };

            // Setup callback for successful query execution
            Query.QueryAsyncEventHandler queryCompleteSuccessCallback = q =>
            {
                taskCompletion.SetResult(new EditSession.EditSessionQueryExecutionState(q));
                return Task.FromResult(0);
            };

            // Setup callback for failed query execution
            Query.QueryAsyncErrorEventHandler queryCompleteFailureCallback = (q, e) =>
            {
                taskCompletion.SetResult(new EditSession.EditSessionQueryExecutionState(null));
                return Task.FromResult(0);
            };

            // Execute the query
            ExecuteStringParams executeParams = new ExecuteStringParams
            {
                Query = query,
                GetFullColumnSchema = true,
                OwnerUri = ownerUri
            };
            await queryExecutionService.InterServiceExecuteQuery(executeParams, null, eventSender,
                queryCreateSuccessCallback, queryCreateFailureCallback,
                queryCompleteSuccessCallback, queryCompleteFailureCallback);

            // Wait for the completion source to complete, this will wait until the query has
            // completed and sent all its events.
            return await taskCompletion.Task;
        }

        private static Task SendSessionReadyEvent(IEventSender eventSender, string ownerUri, bool success,
            string message)
        {
            var sessionReadyParams = new EditSessionReadyParams
            {
                OwnerUri = ownerUri,
                Message = message,
                Success = success
            };

            return eventSender.SendEvent(EditSessionReadyEvent.Type, sessionReadyParams);
        }

        #endregion

    }

    /// <summary>
    /// Context for InitializeEditRequest, to provide a way to update the result set before sending it to UI.
    /// </summary>
    internal class InitializeEditRequestContext : IEventSender
    {
        private readonly IEventSender eventSender;

        public Action<ResultSetEventParams> ResultSetHandler { get; set; }

        public InitializeEditRequestContext(IEventSender eventSender)
        {
            this.eventSender = eventSender;
        }

        public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            if (eventParams is ResultSetEventParams && this.ResultSetHandler != null)
            {
                this.ResultSetHandler(eventParams as ResultSetEventParams);
            }
            return this.eventSender?.SendEvent(eventType, eventParams) ?? Task.CompletedTask;
        }

    }
}
