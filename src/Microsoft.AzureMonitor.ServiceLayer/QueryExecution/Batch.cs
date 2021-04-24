// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.DataSource;
using Microsoft.AzureMonitor.ServiceLayer.Localization;
using Microsoft.AzureMonitor.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.QueryExecution
{
    /// <summary>
    /// This class represents a batch within a query
    /// </summary>
    public class Batch : IDisposable
    {
        #region Member Variables

        /// <summary>
        /// For IDisposable implementation, whether or not this has been disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Local time when the execution and retrieval of files is finished
        /// </summary>
        private DateTime _executionEndTime;

        /// <summary>
        /// Local time when the execution starts, specifically when the object is created
        /// </summary>
        private DateTime _executionStartTime;

        /// <summary>
        /// Whether or not any messages have been sent
        /// </summary>
        private bool _messagesSent;

        /// <summary>
        /// Factory for creating readers/writers for the output of the batch
        /// </summary>
        private readonly IFileStreamFactory _outputFileFactory;

        /// <summary>
        /// Internal representation of the result sets so we can modify internally
        /// </summary>
        private readonly List<ResultSet> _resultSets;

        /// <summary>
        /// Special action which this batch performed 
        /// </summary>
        private readonly SpecialAction _specialAction;

        /// <summary>
        /// Flag indicating whether a separate KeyInfo query should be run
        /// to get the full ColumnSchema metadata.
        /// </summary>
        private readonly bool _getFullColumnSchema;

        #endregion

        internal Batch(string batchText, SelectionData selection, int ordinalId,
            IFileStreamFactory outputFileFactory, int executionCount = 1, bool getFullColumnSchema = false)
        {
            // Sanity check for input
            Validate.IsNotNullOrEmptyString(nameof(batchText), batchText);
            Validate.IsNotNull(nameof(outputFileFactory), outputFileFactory);
            Validate.IsGreaterThan(nameof(ordinalId), ordinalId, 0);

            // Initialize the internal state
            BatchText = batchText;
            Selection = selection;
            _executionStartTime = DateTime.Now;
            HasExecuted = false;
            Id = ordinalId;
            _resultSets = new List<ResultSet>();
            this._outputFileFactory = outputFileFactory;
            _specialAction = new SpecialAction();
            BatchExecutionCount = executionCount > 0 ? executionCount : 1;
            this._getFullColumnSchema = getFullColumnSchema;
        }

        #region Events

        /// <summary>
        /// Asynchronous handler for when batches are completed
        /// </summary>
        /// <param name="batch">The batch that completed</param>
        public delegate Task BatchAsyncEventHandler(Batch batch);

        /// <summary>
        /// Asynchronous handler for when a message is emitted by the sql connection
        /// </summary>
        /// <param name="message">The message that was emitted</param>
        public delegate Task BatchAsyncMessageHandler(ResultMessage message);

        /// <summary>
        /// Event that will be called when the batch has completed execution
        /// </summary>
        public event BatchAsyncEventHandler BatchCompletion;

        /// <summary>
        /// Event that will be called when a message has been emitted
        /// </summary>
        public event BatchAsyncMessageHandler BatchMessageSent;

        /// <summary>
        /// Event to call when the batch has started execution
        /// </summary>
        public event BatchAsyncEventHandler BatchStart;

        /// <summary>
        /// Event that will be called when the resultset has completed execution. It will not be
        /// called from the Batch but from the ResultSet instance.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetCompletion;

        /// <summary>
        /// Event that will be called when the resultSet first becomes available. This is as soon as we start reading the results. It will not be
        /// called from the Batch but from the ResultSet instance.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetAvailable;

        /// <summary>
        /// Event that will be called when additional rows in the result set are available (rowCount available has increased). It will not be
        /// called from the Batch but from the ResultSet instance.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// The text of batch that will be executed
        /// </summary>
        public string BatchText { get; set; }

        public int BatchExecutionCount { get; private set; }

        /// <summary>
        /// Localized timestamp for when the execution completed.
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string ExecutionEndTimeStamp => _executionEndTime.ToString("o");

        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsedTime
        {
            get
            {
                TimeSpan elapsedTime = _executionEndTime - _executionStartTime;
                return elapsedTime.ToString();
            }
        }

        /// <summary>
        /// Localized timestamp for when the execution began.
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string ExecutionStartTimeStamp => _executionStartTime.ToString("o");

        /// <summary>
        /// Whether or not this batch encountered an error that halted execution
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Whether or not this batch has been executed, regardless of success or failure 
        /// </summary>
        public bool HasExecuted { get; set; }

        /// <summary>
        /// Ordinal of the batch in the query
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The result sets of the batch execution
        /// </summary>
        public IList<ResultSet> ResultSets => _resultSets;

        /// <summary>
        /// Property for generating a set result set summaries from the result sets
        /// </summary>
        public ResultSetSummary[] ResultSummaries
        {
            get
            {
                lock (_resultSets)
                {
                    return _resultSets.Select(set => set.Summary).ToArray();
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="BatchSummary"/> based on the batch instance
        /// </summary>
        public BatchSummary Summary
        {
            get
            {
                // Batch summary with information available at start
                BatchSummary summary = new BatchSummary
                {
                    Id = Id,
                    Selection = Selection,
                    ExecutionStart = ExecutionStartTimeStamp,
                    HasError = HasError
                };

                // Add on extra details if we finished executing it
                if (HasExecuted)
                {
                    summary.ResultSetSummaries = ResultSummaries;
                    summary.ExecutionEnd = ExecutionEndTimeStamp;
                    summary.ExecutionElapsed = ExecutionElapsedTime;
                    summary.SpecialAction = ProcessResultSetSpecialActions();
                }

                return summary;
            }
        }

        /// <summary>
        /// The range from the file that is this batch
        /// </summary>
        internal SelectionData Selection { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes this batch and captures any server messages that are returned.
        /// </summary>
        /// <param name="datasource">The connection to use to execute the batch</param>
        /// <param name="cancellationToken">Token for cancelling the execution</param>
        public async Task Execute(MonitorDataSource datasource, CancellationToken cancellationToken)
        {
            // Sanity check to make sure we haven't already run this batch
            if (HasExecuted)
            {
                throw new InvalidOperationException("Batch has already executed.");
            }

            // Notify that we've started execution
            if (BatchStart != null)
            {
                await BatchStart(this);
            }

            try
            {
                await DoExecute(datasource, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Cancellation isn't considered an error condition
                await SendMessage(SR.QueryServiceQueryCancelled, false);
                throw;
            }
            catch (Exception e)
            {
                HasError = true;
                await SendMessage(SR.QueryServiceQueryFailed(e.Message), true);
                throw;
            }
            finally
            {
                // Mark that we have executed
                HasExecuted = true;
                _executionEndTime = DateTime.Now;

                // Fire an event to signify that the batch has completed
                if (BatchCompletion != null)
                {
                    await BatchCompletion(this);
                }
            }
        }

        private async Task DoExecute(MonitorDataSource datasource, CancellationToken cancellationToken)
        {
            await SendMessageIfExecutingMultipleTimes(SR.EE_ExecutionInfo_InitializingLoop, false);

            _executionStartTime = DateTime.Now;

            int timesLoop = BatchExecutionCount;
            while (timesLoop > 0)
            {
                try
                {
                    await ExecuteOnce(datasource, cancellationToken);
                }

                catch (DbException ex)
                {
                    HasError = true;
                    // If it's a multi-batch, we notify the user that we're ignoring a single failure.
                    await SendMessageIfExecutingMultipleTimes(SR.EE_BatchExecutionError_Ignoring, false);
                }

                timesLoop--;
            }

            await SendMessageIfExecutingMultipleTimes(
                string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_FinalizingLoop, this.BatchExecutionCount), false);
        }

        private async Task SendMessageIfExecutingMultipleTimes(string message, bool isError)
        {
            if (IsExecutingMultipleTimes())
            {
                await SendMessage(message, isError);
            }
        }

        private bool IsExecutingMultipleTimes()
        {
            return this.BatchExecutionCount > 1;
        }

        private async Task ExecuteOnce(MonitorDataSource datasource, CancellationToken cancellationToken)
        {
            // Make sure we haven't cancelled yet
            cancellationToken.ThrowIfCancellationRequested();
            
            // Execute the command to get back a reader
            using (IDataReader reader = await datasource.QueryAsync(BatchText, cancellationToken))
            {
                do
                {
                    // Verify that the cancellation token hasn't been canceled
                    cancellationToken.ThrowIfCancellationRequested();

                    // This result set has results (i.e. SELECT/etc queries)
                    var resultSet = new ResultSet(_resultSets.Count, Id, _outputFileFactory);
                    resultSet.ResultAvailable += ResultSetAvailable;
                    resultSet.ResultUpdated += ResultSetUpdated;
                    resultSet.ResultCompletion += ResultSetCompletion;

                    // Add the result set to the results of the query
                    lock (_resultSets)
                    {
                        _resultSets.Add(resultSet);
                    }

                    // Read until we hit the end of the result set
                    await resultSet.ReadResultToEnd(reader, cancellationToken);

                } while (reader.NextResult());

                // If there were no messages, for whatever reason (NO COUNT set, messages 
                // were emitted, records returned), output a "successful" message
                if (!_messagesSent)
                {
                    await SendMessage(SR.QueryServiceCompletedSuccessfully, false);
                }
            }
        }


        /// <summary>
        /// Generates a subset of the rows from a result set of the batch
        /// </summary>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int resultSetIndex, long startRow, int rowCount)
        {
            ResultSet targetResultSet;
            lock (_resultSets)
            {
                // Sanity check to make sure we have valid numbers
                if (resultSetIndex < 0 || resultSetIndex >= _resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(resultSetIndex),
                        SR.QueryServiceSubsetResultSetOutOfRange);
                }

                targetResultSet = _resultSets[resultSetIndex];
            }

            // Retrieve the result set
            return targetResultSet.GetSubset(startRow, rowCount);
        }

        /// <summary>
        /// Saves a result to a file format selected by the user
        /// </summary>
        /// <param name="saveParams">Parameters for the save as request</param>
        /// <param name="fileFactory">
        /// Factory for creating the reader/writer pair for outputing to the selected format
        /// </param>
        /// <param name="successHandler">Delegate to call when request successfully completes</param>
        /// <param name="failureHandler">Delegate to call if the request fails</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory,
            ResultSet.SaveAsAsyncEventHandler successHandler, ResultSet.SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Get the result set to save
            ResultSet resultSet;
            lock (_resultSets)
            {
                // Sanity check to make sure we have a valid result set
                if (saveParams.ResultSetIndex < 0 || saveParams.ResultSetIndex >= _resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(saveParams.BatchIndex), SR.QueryServiceSubsetResultSetOutOfRange);
                }


                resultSet = _resultSets[saveParams.ResultSetIndex];
            }

            resultSet.SaveAs(saveParams, fileFactory, successHandler, failureHandler);
        }

        #endregion

        #region Private Helpers

        private async Task SendMessage(string message, bool isError)
        {
            // If the message event is null, this is a no-op
            if (BatchMessageSent == null)
            {
                return;
            }

            // State that we've sent any message, and send it
            _messagesSent = true;
            await BatchMessageSent(new ResultMessage(message, isError, Id));
        }

        /// <summary>
        /// Handler for when the StatementCompleted event is fired for this batch's command. This
        /// will be executed ONLY when there is a rowcount to report. If this event is not fired
        /// either NOCOUNT has been set or the command doesn't affect records.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">Arguments for the event</param>
        internal void StatementCompletedHandler(object sender, StatementCompletedEventArgs args)
        {
            // Add a message for the number of rows the query returned
            string message = args.RecordCount == 1
                ? SR.QueryServiceAffectedOneRow
                : SR.QueryServiceAffectedRows(args.RecordCount);
            SendMessage(message, false).Wait();
        }

        /// <summary>
        /// Handle a single SqlError's error message by processing and displaying it. The arguments come from the error being handled
        /// </summary>
        internal async Task HandleSqlErrorMessage(int errorNumber, byte errorClass, byte state, int lineNumber, string procedure,
            string message)
        {
            // Did the database context change (error code 5701)?
            if (errorNumber == 5701)
            {
                return;
            }

            string detailedMessage;
            if (string.IsNullOrEmpty(procedure))
            {
                detailedMessage = string.Format("Msg {0}, Level {1}, State {2}, Line {3}{4}{5}",
                    errorNumber, errorClass, state, lineNumber + (Selection != null ? Selection.StartLine : 0),
                    Environment.NewLine, message);
            }
            else
            {
                detailedMessage = string.Format("Msg {0}, Level {1}, State {2}, Procedure {3}, Line {4}{5}{6}",
                    errorNumber, errorClass, state, procedure, lineNumber,
                    Environment.NewLine, message);
            }

            bool isError;
            if (errorClass > 10)
            {
                isError = true;
            }
            else if (errorClass > 0 && errorNumber > 0)
            {
                isError = false;
            }
            else
            {
                isError = false;
                detailedMessage = null;
            }

            if (detailedMessage != null)
            {
                await SendMessage(detailedMessage, isError);
            }
            else
            {
                await SendMessage(message, isError);
            }

            if (isError)
            {
                this.HasError = true;
            }
        }

        /// <summary>
        /// Aggregates all result sets in the batch into a single special action 
        /// </summary>
        private SpecialAction ProcessResultSetSpecialActions()
        {
            foreach (ResultSet resultSet in _resultSets)
            {
                _specialAction.CombineSpecialAction(resultSet.Summary.SpecialAction);
            }

            return _specialAction;
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_resultSets)
                {
                    foreach (ResultSet r in _resultSets)
                    {
                        r.Dispose();
                    }
                }
            }

            _disposed = true;
        }

        #endregion
    }
}