//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Service for executing queries
    /// </summary>
    public sealed class QueryExecutionService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<QueryExecutionService> LazyInstance = new Lazy<QueryExecutionService>(() => new QueryExecutionService());

        /// <summary>
        /// Singleton instance of the query execution service
        /// </summary>
        public static QueryExecutionService Instance => LazyInstance.Value;

        private QueryExecutionService()
        {
            ConnectionService = ConnectionService.Instance;
            WorkspaceService = WorkspaceService<SqlToolsSettings>.Instance;
            Settings = new SqlToolsSettings();
        }

        internal QueryExecutionService(ConnectionService connService, WorkspaceService<SqlToolsSettings> workspaceService)
        {
            ConnectionService = connService;
            WorkspaceService = workspaceService;
            Settings = new SqlToolsSettings();
        }

        #endregion

        #region Properties

        /// <summary>
        /// File factory to be used to create a buffer file for results.
        /// </summary>
        /// <remarks>
        /// Made internal here to allow for overriding in unit testing
        /// </remarks>
        internal IFileStreamFactory BufferFileStreamFactory;

        /// <summary>
        /// File factory to be used to create a buffer file for results
        /// </summary>
        private IFileStreamFactory BufferFileFactory
        {
            get
            {
                if (BufferFileStreamFactory == null)
                {
                    BufferFileStreamFactory = new ServiceBufferFileStreamFactory
                    {
                        QueryExecutionSettings = Settings.QueryExecutionSettings
                    };
                }
                return BufferFileStreamFactory;
            }
        }

        /// <summary>
        /// File factory to be used to create CSV files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory CsvFileFactory { get; set; }

        /// <summary>
        /// File factory to be used to create Excel files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory ExcelFileFactory { get; set; }

        /// <summary>
        /// File factory to be used to create JSON files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory JsonFileFactory { get; set; }

        /// <summary>
        /// File factory to be used to create XML files from result sets. Set to internal in order
        /// to allow overriding in unit testing
        /// </summary>
        internal IFileStreamFactory XmlFileFactory { get; set; }

        /// <summary>
        /// The collection of active queries
        /// </summary>
        internal ConcurrentDictionary<string, Query> ActiveQueries => queries.Value;

        /// <summary>
        /// The collection of query execution options
        /// </summary>
        internal ConcurrentDictionary<string, QueryExecutionSettings> ActiveQueryExecutionSettings => queryExecutionSettings.Value;

        /// <summary>
        /// Internal task for testability
        /// </summary>
        internal Task WorkTask { get; private set; }

        /// <summary>
        /// Instance of the connection service, used to get the connection info for a given owner URI
        /// </summary>
        private ConnectionService ConnectionService { get; }

        private WorkspaceService<SqlToolsSettings> WorkspaceService { get; }

        /// <summary>
        /// Internal storage of active queries, lazily constructed as a threadsafe dictionary
        /// </summary>
        private readonly Lazy<ConcurrentDictionary<string, Query>> queries =
            new Lazy<ConcurrentDictionary<string, Query>>(() => new ConcurrentDictionary<string, Query>());

        /// <summary>
        /// Internal storage of active query settings
        /// </summary>
        private readonly Lazy<ConcurrentDictionary<string, QueryExecutionSettings>> queryExecutionSettings =
            new Lazy<ConcurrentDictionary<string, QueryExecutionSettings>>(() => new ConcurrentDictionary<string, QueryExecutionSettings>());            

        /// <summary>
        /// Settings that will be used to execute queries. Internal for unit testing
        /// </summary>
        internal SqlToolsSettings Settings { get; set; }

        /// <summary>
        /// Holds a map from the simple execute unique GUID and the underlying task that is being ran
        /// </summary>
        private readonly Lazy<ConcurrentDictionary<string, Task>> simpleExecuteRequests =
            new Lazy<ConcurrentDictionary<string, Task>>(() => new ConcurrentDictionary<string, Task>());

        /// <summary>
        /// Holds a map from the simple execute unique GUID and the underlying task that is being ran
        /// </summary>
        internal ConcurrentDictionary<string, Task> ActiveSimpleExecuteRequests => simpleExecuteRequests.Value;

        #endregion

        /// <summary>
        /// Initializes the service with the service host, registers request handlers and shutdown
        /// event handler.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(ExecuteDocumentSelectionRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteDocumentStatementRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ExecuteStringRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(SubsetRequest.Type, HandleResultSubsetRequest);
            serviceHost.SetRequestHandler(QueryDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(QueryCancelRequest.Type, HandleCancelRequest);
            serviceHost.SetRequestHandler(SaveResultsAsCsvRequest.Type, HandleSaveResultsAsCsvRequest);
            serviceHost.SetRequestHandler(SaveResultsAsTextRequest.Type, HandleSaveResultsAsTextRequest);
            serviceHost.SetRequestHandler(SaveResultsAsExcelRequest.Type, HandleSaveResultsAsExcelRequest);
            serviceHost.SetRequestHandler(SaveResultsAsJsonRequest.Type, HandleSaveResultsAsJsonRequest);
            serviceHost.SetRequestHandler(SaveResultsAsXmlRequest.Type, HandleSaveResultsAsXmlRequest);
            serviceHost.SetRequestHandler(QueryExecutionPlanRequest.Type, HandleExecutionPlanRequest);
            serviceHost.SetRequestHandler(SimpleExecuteRequest.Type, HandleSimpleExecuteRequest);
            serviceHost.SetRequestHandler(QueryExecutionOptionsRequest.Type, HandleQueryExecutionOptionsRequest);

            // Register the file open update handler
            WorkspaceService<SqlToolsSettings>.Instance.RegisterTextDocCloseCallback(HandleDidCloseTextDocumentNotification);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                Dispose();
                return Task.FromResult(0);
            });

            // Register a handler for when the configuration changes
            WorkspaceService.RegisterConfigChangeCallback(UpdateSettings);
        }

        #region Request Handlers

        /// <summary>
        /// Handles request to execute a selection of a document in the workspace service
        /// </summary>
        internal async Task HandleExecuteRequest(ExecuteRequestParamsBase executeParams,
            RequestContext<ExecuteRequestResult> requestContext)
        {
            try
            {
                // Setup actions to perform upon successful start and on failure to start
                Func<Query, Task<bool>> queryCreateSuccessAction = async q =>
                {
                    await requestContext.SendResult(new ExecuteRequestResult());
                    Logger.Write(TraceEventType.Stop, $"Response for Query: '{executeParams.OwnerUri} sent. Query Complete!");
                    return true;
                };
                Func<string, Task> queryCreateFailureAction = message =>
                {
                    Logger.Write(TraceEventType.Warning, $"Failed to create Query: '{executeParams.OwnerUri}. Message: '{message}' Complete!");
                    return requestContext.SendError(message);
                };

                // Use the internal handler to launch the query
                WorkTask = Task.Run(async () =>
                {
                    await InterServiceExecuteQuery(
                        executeParams, 
                        null, 
                        requestContext, 
                        queryCreateSuccessAction, 
                        queryCreateFailureAction, 
                        null, 
                        null,
                        isQueryEditor(executeParams.OwnerUri));
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handles a request to execute a string and return the result
        /// </summary>
        internal async Task HandleSimpleExecuteRequest(SimpleExecuteParams executeParams,
            RequestContext<SimpleExecuteResult> requestContext)
        {
            try
            {
                string randomUri = Guid.NewGuid().ToString();
                ExecuteStringParams executeStringParams = new ExecuteStringParams
                {
                    Query = executeParams.QueryString,
                    // generate guid as the owner uri to make sure every query is unique
                    OwnerUri = randomUri
                };

                // get connection
                ConnectionInfo connInfo;
                if (!ConnectionService.TryFindConnection(executeParams.OwnerUri, out connInfo))
                {
                    await requestContext.SendError(SR.QueryServiceQueryInvalidOwnerUri);
                    return;
                }

                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = randomUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                Task workTask = Task.Run(async () => {
                    await ConnectionService.Connect(connectParams);

                    ConnectionInfo newConn;
                    ConnectionService.TryFindConnection(randomUri, out newConn);

                    Func<string, Task> queryCreateFailureAction = message => requestContext.SendError(message);

                    ResultOnlyContext<SimpleExecuteResult> newContext = new ResultOnlyContext<SimpleExecuteResult>(requestContext);

                    // handle sending event back when the query completes
                    Query.QueryAsyncEventHandler queryComplete = async query =>
                    {
                        try
                        {
                            // check to make sure any results were recieved
                            if (query.Batches.Length == 0
                                || query.Batches[0].ResultSets.Count == 0)
                            {
                                await requestContext.SendError(SR.QueryServiceResultSetHasNoResults);
                                return;
                            }

                            long rowCount = query.Batches[0].ResultSets[0].RowCount;
                            // check to make sure there is a safe amount of rows to load into memory
                            if (rowCount > Int32.MaxValue)
                            {
                                await requestContext.SendError(SR.QueryServiceResultSetTooLarge);
                                return;
                            }

                            SimpleExecuteResult result = new SimpleExecuteResult
                            {
                                RowCount = rowCount,
                                ColumnInfo = query.Batches[0].ResultSets[0].Columns,
                                Rows = new DbCellValue[0][]
                            };

                            if (rowCount > 0)
                            {
                                SubsetParams subsetRequestParams = new SubsetParams
                                {
                                    OwnerUri = randomUri,
                                    BatchIndex = 0,
                                    ResultSetIndex = 0,
                                    RowsStartIndex = 0,
                                    RowsCount = Convert.ToInt32(rowCount)
                                };
                                // get the data to send back
                                ResultSetSubset subset = await InterServiceResultSubset(subsetRequestParams);
                                result.Rows = subset.Rows;
                            }
                            await requestContext.SendResult(result);
                        }
                        finally
                        {
                            Query removedQuery;
                            Task removedTask;
                            // remove the active query since we are done with it
                            ActiveQueries.TryRemove(randomUri, out removedQuery);
                            ActiveSimpleExecuteRequests.TryRemove(randomUri, out removedTask);
                            ConnectionService.Disconnect(new DisconnectParams(){
                                OwnerUri = randomUri,
                                Type = null
                            });
                        }
                    };

                    // handle sending error back when query fails
                    Query.QueryAsyncErrorEventHandler queryFail = async (q, e) =>
                    {
                        await requestContext.SendError(e);
                    };

                    await InterServiceExecuteQuery(executeStringParams, newConn, newContext, null, queryCreateFailureAction, queryComplete, queryFail);
                });

                ActiveSimpleExecuteRequests.TryAdd(randomUri, workTask);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handles a request to get a subset of the results of this query
        /// </summary>
        internal async Task HandleResultSubsetRequest(SubsetParams subsetParams,
            RequestContext<SubsetResult> requestContext)
        {
            try
            {
                ResultSetSubset subset = await InterServiceResultSubset(subsetParams);
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

        
        /// <summary>
        /// Handles a request to set query execution options
        /// </summary>
        internal async Task HandleQueryExecutionOptionsRequest(QueryExecutionOptionsParams queryExecutionOptionsParams,
            RequestContext<bool> requestContext)
        {
            try
            {
                string uri = queryExecutionOptionsParams.OwnerUri; 
                if (ActiveQueryExecutionSettings.ContainsKey(uri))
                {
                    QueryExecutionSettings settings;
                    ActiveQueryExecutionSettings.TryRemove(uri, out settings);
                }

                ActiveQueryExecutionSettings.TryAdd(uri, queryExecutionOptionsParams.Options);

                await requestContext.SendResult(true);
            }
            catch (Exception e)
            {
                // This was unexpected, so send back as error
                await requestContext.SendError(e.Message);
            }        
        }

        /// <summary>
        /// Handles a request to get an execution plan
        /// </summary>
        internal async Task HandleExecutionPlanRequest(QueryExecutionPlanParams planParams,
            RequestContext<QueryExecutionPlanResult> requestContext)
        {
            try
            {
                // Attempt to load the query
                Query query;
                if (!ActiveQueries.TryGetValue(planParams.OwnerUri, out query))
                {
                    await requestContext.SendError(SR.QueryServiceRequestsNoQuery);
                    return;
                }

                // Retrieve the requested execution plan and return it
                var result = new QueryExecutionPlanResult
                {
                    ExecutionPlan = await query.GetExecutionPlan(planParams.BatchIndex, planParams.ResultSetIndex)
                };
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                // This was unexpected, so send back as error
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Handles a request to dispose of this query
        /// </summary>
        internal async Task HandleDisposeRequest(QueryDisposeParams disposeParams,
            RequestContext<QueryDisposeResult> requestContext)
        {
            // Setup action for success and failure
            Func<Task> successAction = () => requestContext.SendResult(new QueryDisposeResult());
            Func<string, Task> failureAction = message => requestContext.SendError(message);

            // Use the inter-service dispose functionality
            await InterServiceDisposeQuery(disposeParams.OwnerUri, successAction, failureAction);
        }

        /// <summary>
        /// Handles a request to cancel this query if it is in progress
        /// </summary>
        internal async Task HandleCancelRequest(QueryCancelParams cancelParams,
            RequestContext<QueryCancelResult> requestContext)
        {
            try
            {
                // Attempt to find the query for the owner uri
                Query result;
                if (!ActiveQueries.TryGetValue(cancelParams.OwnerUri, out result))
                {
                    await requestContext.SendResult(new QueryCancelResult
                    {
                        Messages = SR.QueryServiceRequestsNoQuery
                    });
                    return;
                }

                // Cancel the query and send a success message
                result.Cancel();
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

        /// <summary>
        /// Process request to save a resultSet to a file in CSV format
        /// </summary>
        internal async Task HandleSaveResultsAsCsvRequest(SaveResultsAsCsvRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default CSV file factory if we haven't overridden it
            IFileStreamFactory csvFactory = CsvFileFactory ?? new SaveAsCsvFileStreamFactory
            {
                SaveRequestParams = saveParams,
                QueryExecutionSettings = Settings.QueryExecutionSettings
            };
            await SaveResultsHelper(saveParams, requestContext, csvFactory);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in CSV format
        /// </summary>
        internal async Task HandleSaveResultsAsTextRequest(SaveResultsAsTextRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default text file factory if we haven't overridden it
            IFileStreamFactory csvFactory = CsvFileFactory ?? new SaveAsTextFileStreamFactory
            {
                SaveRequestParams = saveParams,
                QueryExecutionSettings = Settings.QueryExecutionSettings
            };
            await SaveResultsHelper(saveParams, requestContext, csvFactory);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in Excel format
        /// </summary>
        internal async Task HandleSaveResultsAsExcelRequest(SaveResultsAsExcelRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default Excel file factory if we haven't overridden it
            IFileStreamFactory excelFactory = ExcelFileFactory ?? new SaveAsExcelFileStreamFactory
            {
                SaveRequestParams = saveParams,
                QueryExecutionSettings = Settings.QueryExecutionSettings
            };
            await SaveResultsHelper(saveParams, requestContext, excelFactory);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in JSON format
        /// </summary>
        internal async Task HandleSaveResultsAsJsonRequest(SaveResultsAsJsonRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default JSON file factory if we haven't overridden it
            IFileStreamFactory jsonFactory = JsonFileFactory ?? new SaveAsJsonFileStreamFactory
            {
                SaveRequestParams = saveParams,
                QueryExecutionSettings = Settings.QueryExecutionSettings
            };
            await SaveResultsHelper(saveParams, requestContext, jsonFactory);
        }

        /// <summary>
        /// Process request to save a resultSet to a file in XML format
        /// </summary>
        internal async Task HandleSaveResultsAsXmlRequest(SaveResultsAsXmlRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext)
        {
            // Use the default XML file factory if we haven't overridden it
            IFileStreamFactory xmlFactory = XmlFileFactory ?? new SaveAsXmlFileStreamFactory
            {
                SaveRequestParams = saveParams,
                QueryExecutionSettings = Settings.QueryExecutionSettings
            };
            await SaveResultsHelper(saveParams, requestContext, xmlFactory);
        }

        #endregion

        #region Inter-Service API Handlers

        /// <summary>
        /// Query execution meant to be called from another service. Utilizes callbacks to allow
        /// custom actions to be taken upon creation of query and failure to create query.
        /// </summary>
        /// <param name="executeParams">Parameters for execution</param>
        /// <param name="connInfo">Connection Info to use; will try and get the connection from owneruri if not provided</param>
        /// <param name="queryEventSender">Event sender that will send progressive events during execution of the query</param>
        /// <param name="queryCreateSuccessFunc">
        /// Callback for when query has been created successfully. If result is <c>true</c>, query
        /// will be executed asynchronously. If result is <c>false</c>, query will be disposed. May
        /// be <c>null</c>
        /// </param>
        /// <param name="queryCreateFailFunc">
        /// Callback for when query failed to be created successfully. Error message is provided.
        /// May be <c>null</c>.
        /// </param>
        /// <param name="querySuccessFunc">
        /// Callback to call when query has completed execution successfully. May be <c>null</c>.
        /// </param>
        /// <param name="queryFailureFunc">
        /// Callback to call when query has completed execution with errors. May be <c>null</c>.
        /// </param>
        public async Task InterServiceExecuteQuery(ExecuteRequestParamsBase executeParams,
            ConnectionInfo connInfo,
            IEventSender queryEventSender,
            Func<Query, Task<bool>> queryCreateSuccessFunc,
            Func<string, Task> queryCreateFailFunc,
            Query.QueryAsyncEventHandler querySuccessFunc,
            Query.QueryAsyncErrorEventHandler queryFailureFunc,
            bool applyExecutionSettings = false)
        {
            Validate.IsNotNull(nameof(executeParams), executeParams);
            Validate.IsNotNull(nameof(queryEventSender), queryEventSender);

            Query newQuery;
            try
            {
                // Get a new active query
                newQuery = CreateQuery(executeParams, connInfo, applyExecutionSettings);
                if (queryCreateSuccessFunc != null && !await queryCreateSuccessFunc(newQuery))
                {
                    // The callback doesn't want us to continue, for some reason
                    // It's ok if we leave the query behind in the active query list, the next call
                    // to execute will replace it.
                    newQuery.Dispose();
                    return;
                }
            }
            catch (Exception e)
            {
                // Call the failure callback if it was provided
                if (queryCreateFailFunc != null)
                {
                    await queryCreateFailFunc(e.Message);
                }
                return;
            }

            // Execute the query asynchronously
            ExecuteAndCompleteQuery(executeParams.OwnerUri, newQuery, queryEventSender, querySuccessFunc, queryFailureFunc);
        }

        /// <summary>
        /// Query disposal meant to be called from another service. Utilizes callbacks to allow
        /// custom actions to be performed on success or failure.
        /// </summary>
        /// <param name="ownerUri">The identifier of the query to be disposed</param>
        /// <param name="successAction">Action to perform on success</param>
        /// <param name="failureAction">Action to perform on failure</param>
        public async Task InterServiceDisposeQuery(string ownerUri, Func<Task> successAction,
            Func<string, Task> failureAction)
        {
            Validate.IsNotNull(nameof(successAction), successAction);
            Validate.IsNotNull(nameof(failureAction), failureAction);

            try
            {
                // Attempt to remove the query for the owner uri
                Query result;
                if (!ActiveQueries.TryRemove(ownerUri, out result))
                {
                    await failureAction(SR.QueryServiceRequestsNoQuery);
                    return;
                }

                // Cleanup the query
                result.Dispose();

                // Success
                await successAction();
            }
            catch (Exception e)
            {
                await failureAction(e.Message);
            }
        }

        /// <summary>
        /// Retrieves the requested subset of rows from the requested result set. Intended to be
        /// called by another service.
        /// </summary>
        /// <param name="subsetParams">Parameters for the subset to retrieve</param>
        /// <returns>The requested subset</returns>
        /// <exception cref="ArgumentOutOfRangeException">The requested query does not exist</exception>
        public async Task<ResultSetSubset> InterServiceResultSubset(SubsetParams subsetParams)
        {
            Validate.IsNotNullOrEmptyString(nameof(subsetParams.OwnerUri), subsetParams.OwnerUri);

            // Attempt to load the query
            Query query;
            if (!ActiveQueries.TryGetValue(subsetParams.OwnerUri, out query))
            {
                throw new ArgumentOutOfRangeException(SR.QueryServiceRequestsNoQuery);
            }

            // Retrieve the requested subset and return it
            return await query.GetSubset(subsetParams.BatchIndex, subsetParams.ResultSetIndex,
                subsetParams.RowsStartIndex, subsetParams.RowsCount);
        }

        /// <summary>
        /// Handle the file open notification
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        public async Task HandleDidCloseTextDocumentNotification(
            string uri,
            ScriptFile scriptFile,
            EventContext eventContext)
        {
            try
            {
                // remove any query execution settings when an editor is closed
                if (this.ActiveQueryExecutionSettings.ContainsKey(uri))
                {
                    QueryExecutionSettings settings;
                    this.ActiveQueryExecutionSettings.TryRemove(uri, out settings);
                }                
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
            }
            await Task.FromResult(true);
        }        

        #endregion

        #region Private Helpers

        private Query CreateQuery(
            ExecuteRequestParamsBase executeParams, 
            ConnectionInfo connInfo, 
            bool applyExecutionSettings)
        {
            // Attempt to get the connection for the editor
            ConnectionInfo connectionInfo;
            if (connInfo != null) 
            {
                connectionInfo = connInfo;
            } 
            else if (!ConnectionService.TryFindConnection(executeParams.OwnerUri, out connectionInfo))
            {
                throw new ArgumentOutOfRangeException(nameof(executeParams.OwnerUri), SR.QueryServiceQueryInvalidOwnerUri);
            }

            // Attempt to clean out any old query on the owner URI
            Query oldQuery;
            // DevNote:
            //    if any oldQuery exists on the executeParams.OwnerUri but it has not yet executed,
            //    then shouldn't we cancel and clean out that query since we are about to create a new query object on the current OwnerUri.
            //
            if (ActiveQueries.TryGetValue(executeParams.OwnerUri, out oldQuery) && (oldQuery.HasExecuted || oldQuery.HasCancelled || oldQuery.HasErrored))
            {
                oldQuery.Dispose();
                ActiveQueries.TryRemove(executeParams.OwnerUri, out oldQuery);
            }
            
            // check if there are active query execution settings for the editor, otherwise, use the global settings
            QueryExecutionSettings settings;            
            if (this.ActiveQueryExecutionSettings.TryGetValue(executeParams.OwnerUri, out settings))
            {                
                // special-case handling for query plan options to maintain compat with query execution API parameters
                // the logic is that if either the query execute API parameters or the active query setttings 
                // request a plan then enable the query option
                ExecutionPlanOptions executionPlanOptions = executeParams.ExecutionPlanOptions;
                if (settings.IncludeActualExecutionPlanXml)
                {
                    executionPlanOptions.IncludeActualExecutionPlanXml = settings.IncludeActualExecutionPlanXml;
                }
                if (settings.IncludeEstimatedExecutionPlanXml)
                {
                    executionPlanOptions.IncludeEstimatedExecutionPlanXml = settings.IncludeEstimatedExecutionPlanXml;
                }
                settings.ExecutionPlanOptions = executionPlanOptions;
            }
            else
            {     
                settings = Settings.QueryExecutionSettings;
                settings.ExecutionPlanOptions = executeParams.ExecutionPlanOptions;
            }

            // If we can't add the query now, it's assumed the query is in progress
            Query newQuery = new Query(
                GetSqlText(executeParams), 
                connectionInfo, 
                settings, 
                BufferFileFactory, 
                executeParams.GetFullColumnSchema,
                applyExecutionSettings);

            if (!ActiveQueries.TryAdd(executeParams.OwnerUri, newQuery))
            {
                newQuery.Dispose();
                throw new InvalidOperationException(SR.QueryServiceQueryInProgress);
            }

            Logger.Write(TraceEventType.Information, $"Query object for URI:'{executeParams.OwnerUri}' created");
            return newQuery;
        }

        private static void ExecuteAndCompleteQuery(string ownerUri, Query query,
            IEventSender eventSender,
            Query.QueryAsyncEventHandler querySuccessCallback,
            Query.QueryAsyncErrorEventHandler queryFailureCallback)
        {
            // Setup the callback to send the complete event
            Query.QueryAsyncEventHandler completeCallback = async q =>
            {
                // Send back the results
                QueryCompleteParams eventParams = new QueryCompleteParams
                {
                    OwnerUri = ownerUri,
                    BatchSummaries = q.BatchSummaries
                };

                Logger.Write(TraceEventType.Information, $"Query:'{ownerUri}' completed");
                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            };

            // Setup the callback to send the failure event
            Query.QueryAsyncErrorEventHandler failureCallback = async (q, e) =>
            {
                // Send back the results
                QueryCompleteParams eventParams = new QueryCompleteParams
                {
                    OwnerUri = ownerUri,
                    BatchSummaries = q.BatchSummaries
                };

                Logger.Write(TraceEventType.Error, $"Query:'{ownerUri}' failed");
                await eventSender.SendEvent(QueryCompleteEvent.Type, eventParams);
            };
            query.QueryCompleted += completeCallback;
            query.QueryFailed += failureCallback;

            // Add the callbacks that were provided by the caller
            // If they're null, that's no problem
            query.QueryCompleted += querySuccessCallback;
            query.QueryFailed += queryFailureCallback;

            // Setup the batch callbacks
            Batch.BatchAsyncEventHandler batchStartCallback = async b =>
            {
                BatchEventParams eventParams = new BatchEventParams
                {
                    BatchSummary = b.Summary,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Batch:'{b.Summary}' on Query:'{ownerUri}' started");
                await eventSender.SendEvent(BatchStartEvent.Type, eventParams);
            };
            query.BatchStarted += batchStartCallback;

            Batch.BatchAsyncEventHandler batchCompleteCallback = async b =>
            {
                BatchEventParams eventParams = new BatchEventParams
                {
                    BatchSummary = b.Summary,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Batch:'{b.Summary}' on Query:'{ownerUri}' completed");
                await eventSender.SendEvent(BatchCompleteEvent.Type, eventParams);
            };
            query.BatchCompleted += batchCompleteCallback;

            Batch.BatchAsyncMessageHandler batchMessageCallback = async m =>
            {
                MessageParams eventParams = new MessageParams
                {
                    Message = m,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Message generated on Query:'{ownerUri}' :'{m}'");
                await eventSender.SendEvent(MessageEvent.Type, eventParams);
            };
            query.BatchMessageSent += batchMessageCallback;

            // Setup the ResultSet available callback
            ResultSet.ResultSetAsyncEventHandler resultAvailableCallback = async r =>
            {
                ResultSetAvailableEventParams eventParams = new ResultSetAvailableEventParams
                {
                    ResultSetSummary = r.Summary,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is available");
                await eventSender.SendEvent(ResultSetAvailableEvent.Type, eventParams);
            };
            query.ResultSetAvailable += resultAvailableCallback;

            // Setup the ResultSet updated callback
            ResultSet.ResultSetAsyncEventHandler resultUpdatedCallback = async r =>
            {
                ResultSetUpdatedEventParams eventParams = new ResultSetUpdatedEventParams
                {
                    ResultSetSummary = r.Summary,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is updated with additional rows");
                await eventSender.SendEvent(ResultSetUpdatedEvent.Type, eventParams);
            };
            query.ResultSetUpdated += resultUpdatedCallback;

            // Setup the ResultSet completion callback
            ResultSet.ResultSetAsyncEventHandler resultCompleteCallback = async r =>
            {
                ResultSetCompleteEventParams eventParams = new ResultSetCompleteEventParams
                {
                    ResultSetSummary = r.Summary,
                    OwnerUri = ownerUri
                };

                Logger.Write(TraceEventType.Information, $"Result:'{r.Summary} on Query:'{ownerUri}' is complete");
                await eventSender.SendEvent(ResultSetCompleteEvent.Type, eventParams);
            };
            query.ResultSetCompleted += resultCompleteCallback;

            // Launch this as an asynchronous task
            query.Execute();
        }

        private async Task SaveResultsHelper(SaveResultsRequestParams saveParams,
            RequestContext<SaveResultRequestResult> requestContext, IFileStreamFactory fileFactory)
        {
            // retrieve query for OwnerUri
            Query query;
            if (!ActiveQueries.TryGetValue(saveParams.OwnerUri, out query))
            {
                await requestContext.SendError(SR.QueryServiceQueryInvalidOwnerUri);
                return;
            }

            //Setup the callback for completion of the save task
            ResultSet.SaveAsAsyncEventHandler successHandler = async parameters =>
            {
                await requestContext.SendResult(new SaveResultRequestResult());
            };
            ResultSet.SaveAsFailureAsyncEventHandler errorHandler = async (parameters, reason) =>
            {
                string message = SR.QueryServiceSaveAsFail(Path.GetFileName(parameters.FilePath), reason);
                await requestContext.SendError(message);
            };

            try
            {
                // Launch the task
                query.SaveAs(saveParams, fileFactory, successHandler, errorHandler);
            }
            catch (Exception e)
            {
                await errorHandler(saveParams, e.Message);
            }
        }

        // Internal for testing purposes
        internal string GetSqlText(ExecuteRequestParamsBase request)
        {
            // If it is a document selection, we'll retrieve the text from the document
            ExecuteDocumentSelectionParams docRequest = request as ExecuteDocumentSelectionParams;
            if (docRequest != null)
            {
                return GetSqlTextFromSelectionData(docRequest.OwnerUri, docRequest.QuerySelection);
            }

            // If it is a document statement, we'll retrieve the text from the document
            ExecuteDocumentStatementParams stmtRequest = request as ExecuteDocumentStatementParams;
            if (stmtRequest != null)
            {
                return GetSqlStatementAtPosition(stmtRequest.OwnerUri, stmtRequest.Line, stmtRequest.Column);
            }

            // If it is an ExecuteStringParams, return the text as is
            ExecuteStringParams stringRequest = request as ExecuteStringParams;
            if (stringRequest != null)
            {
                return stringRequest.Query;
            }

            // Note, this shouldn't be possible due to inheritance rules
            throw new InvalidCastException("Invalid request type");
        }

        /// <summary>
        /// Return portion of document corresponding to the selection range
        /// </summary>
        internal string GetSqlTextFromSelectionData(string ownerUri, SelectionData selection)
        {
            // Get the document from the parameters
            ScriptFile queryFile = WorkspaceService.Workspace.GetFile(ownerUri);
            if (queryFile == null)
            {
                return string.Empty;
            }
            // If a selection was not provided, use the entire document
            if (selection == null)
            {
                return queryFile.Contents;
            }

            // A selection was provided, so get the lines in the selected range
            string[] queryTextArray = queryFile.GetLinesInRange(
                new BufferRange(
                    new BufferPosition(
                        selection.StartLine + 1,
                        selection.StartColumn + 1
                    ),
                    new BufferPosition(
                        selection.EndLine + 1,
                        selection.EndColumn + 1
                    )
                )
            );
            return string.Join(Environment.NewLine, queryTextArray);
        }

        /// <summary>
        /// Return portion of document corresponding to the statement at the line and column
        /// </summary>
        internal string GetSqlStatementAtPosition(string ownerUri, int line, int column)
        {
            // Get the document from the parameters
            ScriptFile queryFile = WorkspaceService.Workspace.GetFile(ownerUri);
            if (queryFile == null)
            {
                return string.Empty;
            }

            return LanguageServices.LanguageService.Instance.ParseStatementAtPosition(
                queryFile.Contents, line, column);
        }

        /// Internal for testing purposes
        internal Task UpdateSettings(SqlToolsSettings newSettings, SqlToolsSettings oldSettings, EventContext eventContext)
        {
            Settings.QueryExecutionSettings.Update(newSettings.QueryExecutionSettings);
            return Task.FromResult(0);
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
                foreach (var query in ActiveQueries)
                {
                    if (!query.Value.HasExecuted)
                    {
                        try
                        {
                            query.Value.Cancel();
                        }
                        catch (Exception e)
                        {
                            // We don't particularly care if we fail to cancel during shutdown
                            string message = string.Format("Failed to cancel query {0} during query service disposal: {1}", query.Key, e);
                            Logger.Write(TraceEventType.Warning, message);
                        }
                    }
                    query.Value.Dispose();
                }
                ActiveQueries.Clear();
            }

            disposed = true;
        }

        /// <summary>
        /// Verify if the URI maps to a query editor document
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private bool isQueryEditor(string uri)
        {
            return (!string.IsNullOrWhiteSpace(uri)
                && (uri.StartsWith("untitled:")
                || uri.StartsWith("file:")));
        }

        ~QueryExecutionService()
        {
            Dispose(false);
        }

        #endregion
    }
}
