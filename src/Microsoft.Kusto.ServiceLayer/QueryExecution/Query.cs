﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;
using Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Internal representation of an active query
    /// </summary>
    public class Query : IDisposable
    {
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

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        /// <param name="settings">Settings for how to execute the query, from the user</param>
        /// <param name="outputFactory">Factory for creating output files</param>
        public Query(
            string queryText, 
            ConnectionInfo connection, 
            QueryExecutionSettings settings, 
            IFileStreamFactory outputFactory,
            bool getFullColumnSchema = false,
            bool applyExecutionSettings = false)
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
            ExecutionEngineConditions conditions = null;
            List<BatchDefinition> parserResult = parser.GetBatches(queryText, conditions);

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

        /// <summary>
        /// Event that will be called when the resultSet first becomes available. This is as soon as we start reading the results.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetAvailable;

        /// <summary>
        /// Event that will be called when additional rows in the result set are available (rowCount available has increased)
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetUpdated;
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
                if (!HasExecuted && !HasCancelled && !HasErrored)
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
        /// if the query has been cancelled (before execution started)
        /// </summary>
        public bool HasCancelled { get; private set; }

        /// <summary>
        /// if the query has errored out (before batch execution started)
        /// </summary>
        public bool HasErrored { get; private set; }

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
            this.HasCancelled = true;
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
            Logger.Write(TraceEventType.Start, $"Starting GetSubset execution for batchIndex:'{batchIndex}', resultSetIndex:'{resultSetIndex}', startRow:'{startRow}', rowCount:'{rowCount}'");
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
            ReliableDataSourceConnection queryConnection = null;
            try
            {
                // check for cancellation token before actually making connection
                cancellationSource.Token.ThrowIfCancellationRequested();

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
                queryConnection = await ConnectionService.Instance.GetOrOpenConnection(editorConnection.OwnerUri,
                        ConnectionType.Query);

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
                    b.ResultSetAvailable += ResultSetAvailable;
                    b.ResultSetUpdated += ResultSetUpdated;
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
                HasErrored = true;
                if (e is OperationCanceledException)
                {
                    await BatchMessageSent(new ResultMessage(SR.QueryServiceQueryCancelled, false, null));
                }
                // Call the query failure callback
                if (QueryFailed != null)
                {
                    await QueryFailed(this, e);
                }
            }
            finally
            {                
                foreach (Batch b in Batches)
                {
                    if (b.HasError)
                    {
                        ConnectionService.EnsureConnectionIsOpen(queryConnection);
                        break;
                    }
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
        #endregion
    }
}
