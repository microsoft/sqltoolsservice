// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.Utility;
using System.Globalization;
using System.Collections.ObjectModel;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
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
        private bool disposed;

        /// <summary>
        /// Local time when the execution and retrieval of files is finished
        /// </summary>
        private DateTime executionEndTime;

        /// <summary>
        /// Local time when the execution starts, specifically when the object is created
        /// </summary>
        private DateTime executionStartTime;

        /// <summary>
        /// Whether or not any messages have been sent
        /// </summary>
        private bool messagesSent;

        /// <summary>
        /// Factory for creating readers/writers for the output of the batch
        /// </summary>
        private readonly IFileStreamFactory outputFileFactory;

        /// <summary>
        /// Internal representation of the result sets so we can modify internally
        /// </summary>
        private readonly List<ResultSet> resultSets;

        /// <summary>
        /// Special action which this batch performed 
        /// </summary>
        private readonly SpecialAction specialAction;

        #endregion

        internal Batch(string batchText, SelectionData selection, int ordinalId,
            IFileStreamFactory outputFileFactory, int executionCount = 1)
        {
            // Sanity check for input
            Validate.IsNotNullOrEmptyString(nameof(batchText), batchText);
            Validate.IsNotNull(nameof(outputFileFactory), outputFileFactory);
            Validate.IsGreaterThan(nameof(ordinalId), ordinalId, 0);

            // Initialize the internal state
            BatchText = batchText;
            Selection = selection;
            executionStartTime = DateTime.Now;
            HasExecuted = false;
            Id = ordinalId;
            resultSets = new List<ResultSet>();
            this.outputFileFactory = outputFileFactory;
            specialAction = new SpecialAction();
            BatchExecutionCount = executionCount > 0 ? executionCount : 1;
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
        /// called from the Batch but from the ResultSet instance
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetCompletion;

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
        public string ExecutionEndTimeStamp => executionEndTime.ToString("o");

        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsedTime
        {
            get
            {
                TimeSpan elapsedTime = executionEndTime - executionStartTime;
                return elapsedTime.ToString();
            }
        }

        /// <summary>
        /// Localized timestamp for when the execution began.
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string ExecutionStartTimeStamp => executionStartTime.ToString("o");

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
        public IList<ResultSet> ResultSets => resultSets;

        /// <summary>
        /// Property for generating a set result set summaries from the result sets
        /// </summary>
        public ResultSetSummary[] ResultSummaries
        {
            get
            {
                lock (resultSets)
                {
                    return resultSets.Select(set => set.Summary).ToArray();
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
        /// <param name="conn">The connection to use to execute the batch</param>
        /// <param name="cancellationToken">Token for cancelling the execution</param>
        public async Task Execute(DbConnection conn, CancellationToken cancellationToken)
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
                await DoExecute(conn, cancellationToken);
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
                // Remove the message event handler from the connection
                ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
                if (sqlConn != null)
                {
                    sqlConn.GetUnderlyingConnection().InfoMessage -= ServerMessageHandler;
                }

                // Mark that we have executed
                HasExecuted = true;
                executionEndTime = DateTime.Now;

                // Fire an event to signify that the batch has completed
                if (BatchCompletion != null)
                {
                    await BatchCompletion(this);
                }
            }

        }

        private async Task DoExecute(DbConnection conn, CancellationToken cancellationToken)
        {
            bool canContinue = true;
            int timesLoop = this.BatchExecutionCount;

            await SendMessageIfExecutingMultipleTimes(SR.EE_ExecutionInfo_InitializingLoop, false);

            while (canContinue && timesLoop > 0)
            {
                try
                {
                    await ExecuteOnce(conn, cancellationToken);
                }
                catch (DbException dbe)
                {
                    HasError = true;
                    canContinue = await UnwrapDbException(dbe);
                    if (canContinue)
                    {
                        // If it's a multi-batch, we notify the user that we're ignoring a single failure.
                        await SendMessageIfExecutingMultipleTimes(SR.EE_BatchExecutionError_Ignoring, false);
                    }
                }
                timesLoop--;
            }

            await SendMessageIfExecutingMultipleTimes(string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_FinalizingLoop, this.BatchExecutionCount), false);
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

        private async Task ExecuteOnce(DbConnection conn, CancellationToken cancellationToken)
        {
            // Make sure we haven't cancelled yet
            cancellationToken.ThrowIfCancellationRequested();

            // Create a command that we'll use for executing the query
            using (DbCommand dbCommand = CreateCommand(conn))
            {
                // Make sure that we cancel the command if the cancellation token is cancelled
                cancellationToken.Register(() => dbCommand?.Cancel());

                // Setup the command for executing the batch
                dbCommand.CommandText = BatchText;
                dbCommand.CommandType = CommandType.Text;
                dbCommand.CommandTimeout = 0;
                executionStartTime = DateTime.Now;

                // Fetch schema info separately, since CommandBehavior.KeyInfo will include primary
                // key columns in the result set, even if they weren't part of the select statement.
                // Extra key columns get added to the end, so just correlate via Column Ordinal.
                List<DbColumn[]> columnSchemas = new List<DbColumn[]>();
                using (DbDataReader reader = await dbCommand.ExecuteReaderAsync(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly, cancellationToken))
                {
                    if (reader != null && reader.CanGetColumnSchema())
                    {
                        do
                        {
                            columnSchemas.Add(reader.GetColumnSchema().ToArray());
                        } while (await reader.NextResultAsync(cancellationToken));
                    }
                }

                // Execute the command to get back a reader
                using (DbDataReader reader = await dbCommand.ExecuteReaderAsync(cancellationToken))
                {
                    do
                    {
                        // Verify that the cancellation token hasn't been canceled
                        cancellationToken.ThrowIfCancellationRequested();

                        // Skip this result set if there aren't any rows (i.e. UPDATE/DELETE/etc queries)
                        if (!reader.HasRows && reader.FieldCount == 0)
                        {
                            continue;
                        }

                        // This resultset has results (i.e. SELECT/etc queries)
                        ResultSet resultSet = new ResultSet(resultSets.Count, Id, outputFileFactory);
                        resultSet.ResultCompletion += ResultSetCompletion;

                        // Add the result set to the results of the query
                        lock (resultSets)
                        {
                            resultSets.Add(resultSet);
                        }

                        // Read until we hit the end of the result set
                        await resultSet.ReadResultToEnd(reader, cancellationToken);

                    } while (await reader.NextResultAsync(cancellationToken));

                    // If there were no messages, for whatever reason (NO COUNT set, messages 
                    // were emitted, records returned), output a "successful" message
                    if (!messagesSent)
                    {
                        await SendMessage(SR.QueryServiceCompletedSuccessfully, false);
                    }
                }

                if (columnSchemas != null)
                {
                    ExtendResultMetadata(columnSchemas, resultSets);
                }
            }
        }

        private void ExtendResultMetadata(List<DbColumn[]> columnSchemas, List<ResultSet> results)
        {
            if (columnSchemas.Count != results.Count) return;

            for(int i = 0; i < results.Count; i++)
            {
                ResultSet result = results[i];
                DbColumn[] columnSchema = columnSchemas[i];
                if(result.Columns.Length > columnSchema.Length)
                {
                    throw new InvalidOperationException("Did not receive enough metadata columns.");
                }

                for(int j = 0; j < result.Columns.Length; j++)
                {
                    DbColumnWrapper resultCol = result.Columns[j];
                    DbColumn schemaCol = columnSchema[j];

                    if(!string.Equals(resultCol.ColumnName, schemaCol.ColumnName)
                        || !string.Equals(resultCol.DataTypeName, schemaCol.DataTypeName))
                    {
                        throw new InvalidOperationException("Inconsistent column metadata.");
                    }

                    result.Columns[j] = new DbColumnWrapper(schemaCol);
                }
            }
        }

        private DbCommand CreateCommand(DbConnection conn)
        {
            // Register the message listener to *this instance* of the batch
            // Note: This is being done to associate messages with batches
            ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
            DbCommand dbCommand;
            if (sqlConn != null)
            {
                // Register the message listener to *this instance* of the batch
                // Note: This is being done to associate messages with batches
                sqlConn.GetUnderlyingConnection().InfoMessage += ServerMessageHandler;
                dbCommand = sqlConn.GetUnderlyingConnection().CreateCommand();

                // Add a handler for when the command completes
                SqlCommand sqlCommand = (SqlCommand)dbCommand;
                sqlCommand.StatementCompleted += StatementCompletedHandler;
            }
            else
            {
                dbCommand = conn.CreateCommand();
            }

            // Make sure we aren't using a ReliableCommad since we do not want automatic retry
            Debug.Assert(!(dbCommand is ReliableSqlConnection.ReliableSqlCommand),
                "ReliableSqlCommand command should not be used to execute queries");

            return dbCommand;
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
            lock (resultSets)
            {
                // Sanity check to make sure we have valid numbers
                if (resultSetIndex < 0 || resultSetIndex >= resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(resultSetIndex),
                        SR.QueryServiceSubsetResultSetOutOfRange);
                }

                targetResultSet = resultSets[resultSetIndex];
            }

            // Retrieve the result set
            return targetResultSet.GetSubset(startRow, rowCount);
        }

        /// <summary>
        /// Generates an execution plan
        /// </summary>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <returns>An execution plan object</returns>
        public Task<ExecutionPlan> GetExecutionPlan(int resultSetIndex)
        {
            ResultSet targetResultSet;
            lock (resultSets)
            {
                // Sanity check to make sure we have valid numbers
                if (resultSetIndex < 0 || resultSetIndex >= resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(resultSetIndex),
                        SR.QueryServiceSubsetResultSetOutOfRange);
                }

                targetResultSet = resultSets[resultSetIndex];
            }

            // Retrieve the result set
            return targetResultSet.GetExecutionPlan();
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
            lock (resultSets)
            {
                // Sanity check to make sure we have a valid result set
                if (saveParams.ResultSetIndex < 0 || saveParams.ResultSetIndex >= resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(saveParams.BatchIndex), SR.QueryServiceSubsetResultSetOutOfRange);
                }


                resultSet = resultSets[saveParams.ResultSetIndex];
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
            messagesSent = true;
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
        /// Delegate handler for storing messages that are returned from the server
        /// </summary>
        /// <param name="sender">Object that fired the event</param>
        /// <param name="args">Arguments from the event</param>
        private async void ServerMessageHandler(object sender, SqlInfoMessageEventArgs args)
        {
            foreach (SqlError error in args.Errors)
            {
                await HandleSqlErrorMessage(error.Number, error.Class, error.State, error.LineNumber, error.Procedure, error.Message);
            }
        }

        /// <summary>
        /// Handle a single SqlError's error message by processing and displaying it. The arguments come from the error being handled
        /// </summary>
        internal async Task HandleSqlErrorMessage(int errorNumber, byte errorClass, byte state, int lineNumber, string procedure, string message)
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
                    errorNumber, errorClass, state, lineNumber + Selection.StartLine,
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
        /// Attempts to convert an <see cref="Exception"/> to a <see cref="SqlException"/> that
        /// contains much more info about Sql Server errors. The exception is then unwrapped and
        /// messages are formatted and sent to the extension. If the exception cannot be 
        /// converted to SqlException, the message is written to the messages list.
        /// </summary>
        /// <param name="dbe">The exception to unwrap</param>
        /// <returns>true is exception can be ignored when in a loop, false otherwise</returns>
        private async Task<bool> UnwrapDbException(Exception dbe)
        {
            bool canIgnore = true;
            SqlException se = dbe as SqlException;
            if (se != null)
            {
                var errors = se.Errors.Cast<SqlError>().ToList();

                // Detect user cancellation errors
                if (errors.Any(error => error.Class == 11 && error.Number == 0))
                {
                    // User cancellation error, add the single message
                    await SendMessage(SR.QueryServiceQueryCancelled, false);
                    canIgnore = false;
                }
                else
                {
                    // Not a user cancellation error, add all 
                    foreach (var error in errors)
                    {
                        int lineNumber = error.LineNumber + Selection.StartLine;
                        string message = string.Format("Msg {0}, Level {1}, State {2}, Line {3}{4}{5}",
                            error.Number, error.Class, error.State, lineNumber,
                            Environment.NewLine, error.Message);
                        await SendMessage(message, true);
                    }
                }
            }
            else
            {
                await SendMessage(dbe.Message, true);
            }
            return canIgnore;
        }

        /// <summary>
        /// Aggregates all result sets in the batch into a single special action 
        /// </summary>
        private SpecialAction ProcessResultSetSpecialActions()
        {
            foreach (ResultSet resultSet in resultSets) 
            {
                specialAction.CombineSpecialAction(resultSet.Summary.SpecialAction);
            }

            return specialAction;
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
                lock (resultSets)
                {
                    foreach (ResultSet r in resultSets)
                    {
                        r.Dispose();
                    }
                }
            }

            disposed = true;
        }

        #endregion
    }
}
