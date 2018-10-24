// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Internal representation of an active query
    /// </summary>
    public class Query : IDisposable
    {
        #region Constants
        
        /// <summary>
        /// "Error" code produced by SQL Server when the database context (name) for a connection changes.
        /// </summary>
        private const int DatabaseContextChangeErrorNumber = 5701;
        
        /// <summary>
        /// ON keyword
        /// </summary>
        private const string On = "ON";

        /// <summary>
        /// OFF keyword
        /// </summary>
        private const string Off = "OFF";
        
        /// <summary>
        /// showplan_xml statement
        /// </summary>
        private const string SetShowPlanXml = "SET SHOWPLAN_XML {0}";

        /// <summary>
        /// statistics xml statement
        /// </summary>
        private const string SetStatisticsXml = "SET STATISTICS XML {0}";
        
        #endregion

        #region Member Variables

        /// <summary>
        /// Cancellation token source, used for cancelling async db actions
        /// </summary>
        private readonly CancellationTokenSource cancellationSource;

        /// <summary>
        /// For IDisposable implementation, whether or not this object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The connection info associated with the file editor owner URI, used to create a new
        /// connection upon execution of the query
        /// </summary>
        private readonly ConnectionInfo editorConnection;

        /// <summary>
        /// Whether or not the execute method has been called for this query
        /// </summary>
        private bool hasExecuteBeenCalled;

        /// <summary>
        /// Name of the new database if the database name was changed in the query
        /// </summary>
        private string newDatabaseName;        

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        /// <param name="settings">Settings for how to execute the query, from the user</param>
        /// <param name="outputFactory">Factory for creating output files</param>
        public Query(string queryText, ConnectionInfo connection, QueryExecutionSettings settings, IFileStreamFactory outputFactory, bool getFullColumnSchema = false)
        {
            // Sanity check for input
            Validate.IsNotNull(nameof(queryText), queryText);
            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(settings), settings);
            Validate.IsNotNull(nameof(outputFactory), outputFactory);

            // Initialize the internal state
            QueryText = queryText;
            editorConnection = connection;
            cancellationSource = new CancellationTokenSource();

            // Process the query into batches 
            BatchParserWrapper parser = new BatchParserWrapper();
            List<BatchDefinition> parserResult = parser.GetBatches(queryText);

            var batchSelection = parserResult
                .Select((batchDefinition, index) =>
                    new Batch(batchDefinition.BatchText, 
                        new SelectionData(
                            batchDefinition.StartLine-1,
                            batchDefinition.StartColumn-1,
                            batchDefinition.EndLine-1,
                            batchDefinition.EndColumn-1),                       
                        index, outputFactory,
                        batchDefinition.BatchExecutionCount,
                        getFullColumnSchema));

            Batches = batchSelection.ToArray();

            // Create our batch lists
            BeforeBatches = new List<Batch>();
            AfterBatches = new List<Batch>();

            if (DoesSupportExecutionPlan(connection))
            {
                // Checking settings for execution plan options 
                if (settings.ExecutionPlanOptions.IncludeEstimatedExecutionPlanXml)
                {
                    // Enable set showplan xml
                    AddBatch(string.Format(SetShowPlanXml, On), BeforeBatches, outputFactory);
                    AddBatch(string.Format(SetShowPlanXml, Off), AfterBatches, outputFactory);
                }
                else if (settings.ExecutionPlanOptions.IncludeActualExecutionPlanXml)
                {
                    AddBatch(string.Format(SetStatisticsXml, On), BeforeBatches, outputFactory);
                    AddBatch(string.Format(SetStatisticsXml, Off), AfterBatches, outputFactory);
                }
            }
        }

        #region Events

        /// <summary>
        /// Delegate type for callback when a query completes or fails
        /// </summary>
        /// <param name="query">The query that completed</param>
        public delegate Task QueryAsyncEventHandler(Query query);
        
        /// <summary>
        /// Delegate type for callback when a query fails
        /// </summary>
        /// <param name="query">Query that raised the event</param>
        /// <param name="exception">Exception that caused the query to fail</param>
        public delegate Task QueryAsyncErrorEventHandler(Query query, Exception exception);
        
        /// <summary>
        /// Event to be called when a batch is completed.
        /// </summary>
        public event Batch.BatchAsyncEventHandler BatchCompleted;

        /// <summary>
        /// Event that will be called when a message has been emitted
        /// </summary>
        public event Batch.BatchAsyncMessageHandler BatchMessageSent;

        /// <summary>
        /// Event to be called when a batch starts execution.
        /// </summary>
        public event Batch.BatchAsyncEventHandler BatchStarted;

        /// <summary>
        /// Callback for when the query has completed successfully
        /// </summary>
        public event QueryAsyncEventHandler QueryCompleted;

        /// <summary>
        /// Callback for when the query has failed
        /// </summary>
        public event QueryAsyncErrorEventHandler QueryFailed;

        /// <summary>
        /// Event to be called when a resultset has completed.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetCompleted;

        #endregion

        #region Properties

        /// <summary>
        /// The batches which should run before the user batches 
        /// </summary>
        private List<Batch> BeforeBatches { get; }

        /// <summary>
        /// The batches underneath this query
        /// </summary>
        internal Batch[] Batches { get; }

        /// <summary>
        /// The batches which should run after the user batches 
        /// </summary>
        internal List<Batch> AfterBatches { get; }

        /// <summary>
        /// The summaries of the batches underneath this query
        /// </summary>
        public BatchSummary[] BatchSummaries
        {
            get
            {
                if (!HasExecuted)
                {
                    throw new InvalidOperationException("Query has not been executed.");
                }
                return Batches.Select(b => b.Summary).ToArray();
            }
        }

        /// <summary>
        /// Storage for the async task for execution. Set as internal in order to await completion
        /// in unit tests.
        /// </summary>
        internal Task ExecutionTask { get; private set; }

        /// <summary>
        /// Whether or not the query has completed executed, regardless of success or failure
        /// </summary>
        /// <remarks>
        /// Don't touch the setter unless you're doing unit tests!
        /// </remarks>
        public bool HasExecuted
        {
            get { return Batches.Length == 0 ? hasExecuteBeenCalled : Batches.All(b => b.HasExecuted); }
            internal set
            {
                hasExecuteBeenCalled = value;
                foreach (var batch in Batches)
                {
                    batch.HasExecuted = value;
                }
            }
        }

        /// <summary>
        /// The text of the query to execute
        /// </summary>
        public string QueryText { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancels the query by issuing the cancellation token
        /// </summary>
        public void Cancel()
        {
            // Make sure that the query hasn't completed execution
            if (HasExecuted)
            {
                throw new InvalidOperationException(SR.QueryServiceCancelAlreadyCompleted);
            }

            // Issue the cancellation token for the query
            cancellationSource.Cancel();
        }

        /// <summary>
        /// Launches the asynchronous process for executing the query
        /// </summary>
        public void Execute()
        {
            ExecutionTask = Task.Run(ExecuteInternal)
                .ContinueWithOnFaulted(async t =>
                {
                    if (QueryFailed != null)
                    {
                        await QueryFailed(this, t.Exception);
                    }
                });
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="batchIndex">The index for selecting the batch item</param>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int batchIndex, int resultSetIndex, long startRow, int rowCount)
        {
            // Sanity check to make sure that the batch is within bounds
            if (batchIndex < 0 || batchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(batchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            return Batches[batchIndex].GetSubset(resultSetIndex, startRow, rowCount);
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="batchIndex">The index for selecting the batch item</param>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <returns>The Execution Plan, if the result set has one</returns>
        public Task<ExecutionPlan> GetExecutionPlan(int batchIndex, int resultSetIndex)
        {
            // Sanity check to make sure that the batch is within bounds
            if (batchIndex < 0 || batchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(batchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            return Batches[batchIndex].GetExecutionPlan(resultSetIndex);
        }

        /// <summary>
        /// Saves the requested results to a file format of the user's choice
        /// </summary>
        /// <param name="saveParams">Parameters for the save as request</param>
        /// <param name="fileFactory">
        /// Factory for creating the reader/writer pair for the requested output format
        /// </param>
        /// <param name="successHandler">Delegate to call when the request completes successfully</param>
        /// <param name="failureHandler">Delegate to call if the request fails</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory, 
            ResultSet.SaveAsAsyncEventHandler successHandler, ResultSet.SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Sanity check to make sure that the batch is within bounds
            if (saveParams.BatchIndex < 0 || saveParams.BatchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(saveParams.BatchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            Batches[saveParams.BatchIndex].SaveAs(saveParams, fileFactory, successHandler, failureHandler);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Executes this query asynchronously and collects all result sets
        /// </summary>
        private async Task ExecuteInternal()
        {
            ReliableSqlConnection sqlConn = null;
            try
            {
                // Mark that we've internally executed
                hasExecuteBeenCalled = true;
    
                // Don't actually execute if there aren't any batches to execute
                if (Batches.Length == 0)
                {
                    if (BatchMessageSent != null)
                    {
                        await BatchMessageSent(new ResultMessage(SR.QueryServiceCompletedSuccessfully, false, null));
                    }
                    if (QueryCompleted != null)
                    {
                        await QueryCompleted(this);
                    }
                    return;
                }
    
                // Locate and setup the connection
                DbConnection queryConnection = await ConnectionService.Instance.GetOrOpenConnection(editorConnection.OwnerUri, ConnectionType.Query);
                sqlConn = queryConnection as ReliableSqlConnection;
                if (sqlConn != null)
                {
                    // Subscribe to database informational messages
                    sqlConn.GetUnderlyingConnection().FireInfoMessageEventOnUserErrors = true;
                    sqlConn.GetUnderlyingConnection().InfoMessage += OnInfoMessage;
                }

            
                // Execute beforeBatches synchronously, before the user defined batches 
                foreach (Batch b in BeforeBatches)
                {
                    await b.Execute(queryConnection, cancellationSource.Token);
                }

                // We need these to execute synchronously, otherwise the user will be very unhappy
                foreach (Batch b in Batches)
                {
                    // Add completion callbacks 
                    b.BatchStart += BatchStarted;
                    b.BatchCompletion += BatchCompleted;
                    b.BatchMessageSent += BatchMessageSent;
                    b.ResultSetCompletion += ResultSetCompleted;
                    await b.Execute(queryConnection, cancellationSource.Token);
                }

                // Execute afterBatches synchronously, after the user defined batches
                foreach (Batch b in AfterBatches)
                {
                    await b.Execute(queryConnection, cancellationSource.Token);
                }

                // Call the query execution callback
                if (QueryCompleted != null)
                {
                    await QueryCompleted(this);
                }
            }
            catch (Exception e)
            {
                // Call the query failure callback
                if (QueryFailed != null)
                {
                    await QueryFailed(this, e);
                }
            }
            finally
            {
                // Remove the message handler from the connection
                if (sqlConn != null)
                {
                    // Subscribe to database informational messages
                    sqlConn.GetUnderlyingConnection().InfoMessage -= OnInfoMessage;
                }
                
                // If any message notified us we had changed databases, then we must let the connection service know 
                if (newDatabaseName != null)
                {
                    ConnectionService.Instance.ChangeConnectionDatabaseContext(editorConnection.OwnerUri, newDatabaseName);
                }

                foreach (Batch b in Batches)
                {
                    if (b.HasError)
                    {
                        ConnectionService.EnsureConnectionIsOpen(sqlConn);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handler for database messages during query execution
        /// </summary>
        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs args)
        {
            SqlConnection conn = sender as SqlConnection;
            if (conn == null)
            {
                throw new InvalidOperationException(SR.QueryServiceMessageSenderNotSql);
            }

            foreach (SqlError error in args.Errors)
            {
                // Did the database context change (error code 5701)?
                if (error.Number == DatabaseContextChangeErrorNumber)
                {
                    newDatabaseName = conn.Database;
                }
            }
        }

        /// <summary>
        /// Function to add a new batch to a Batch set
        /// </summary>
        private static void AddBatch(string query, ICollection<Batch> batchSet, IFileStreamFactory outputFactory)
        {
            batchSet.Add(new Batch(query, null, batchSet.Count, outputFactory, 1));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                cancellationSource.Dispose();
                foreach (Batch b in Batches)
                {
                    b.Dispose();
                }
            }

            disposed = true;
        }

        /// <summary>
        /// Does this connection support XML Execution plans
        /// </summary>
        private bool DoesSupportExecutionPlan(ConnectionInfo connectionInfo) {
            // Determining which execution plan options may be applied (may be added to for pre-yukon support)
            return (!connectionInfo.IsSqlDW && connectionInfo.MajorVersion >= 9);
        }

        #endregion
    }
}
