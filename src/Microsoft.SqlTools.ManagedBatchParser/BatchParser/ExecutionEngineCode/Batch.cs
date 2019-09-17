//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ManagedBatchParser;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Single batch of SQL command
    /// </summary>
    public class Batch
    {
        #region Private methods

        /// <summary>
        /// Helper method to format the provided SqlError
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private string FormatSqlErrorMessage(SqlError error)
        {
            string detailedMessage = string.Empty;

            if (error.Class > 10)
            {
                if (string.IsNullOrEmpty(error.Procedure))
                {
                    detailedMessage = string.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageNoProcedureInfo,
                            error.Number,
                            error.Class,
                            error.State,
                            error.LineNumber);
                }
                else
                {
                    detailedMessage = string.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageWithProcedureInfo,
                        error.Number,
                        error.Class,
                        error.State,
                        error.Procedure,
                        error.LineNumber);
                }
            }
            else if (error.Class > 0 && error.Number > 0)
            {
                detailedMessage = string.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageNoLineInfo,
                    error.Number,
                    error.Class,
                    error.State);
            }

            if (!string.IsNullOrEmpty(detailedMessage) && !isSuppressProviderMessageHeaders)
            {
                detailedMessage = string.Format(CultureInfo.CurrentCulture, "{0}: {1}", error.Source, detailedMessage);
            }

            return detailedMessage;
        }

        /// <summary>
        /// Handles a Sql exception
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private ScriptExecutionResult HandleSqlException(SqlException ex)
        {
            ScriptExecutionResult result;

            lock (this)
            {
                if (state == BatchState.Cancelling)
                {
                    result = ScriptExecutionResult.Cancel;
                }
                else
                {
                    result = ScriptExecutionResult.Failure;
                }
            }

            if (result != ScriptExecutionResult.Cancel)
            {
                HandleSqlMessages(ex.Errors);
            }

            return result;
        }

        /// <summary>        
        /// Called when an error message came from SqlClient
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="line"></param>
        /// <param name="textSpan"></param>
        private void RaiseBatchError(string message, SqlError error, TextSpan textSpan)
        {
            BatchErrorEventArgs args = new BatchErrorEventArgs(message, error, textSpan, null);
            RaiseBatchError(args);
        }

        /// <summary>
        /// Called when an error message came from SqlClient
        /// </summary>
        /// <param name="e"></param>
        private void RaiseBatchError(BatchErrorEventArgs e)
        {
            EventHandler<BatchErrorEventArgs> cache = BatchError;
            if (cache != null)
            {
                cache(this, e);
            }
        }

        /// <summary>
        /// Called when a message came from SqlClient
        /// </summary>
        /// <remarks>
        /// Additionally, it's being used to notify the user that the script execution
        /// has been finished.
        /// </remarks>
        /// <param name="detailedMessage"></param>
        /// <param name="message"></param>
        private void RaiseBatchMessage(string detailedMessage, string message, SqlError error)
        {
            EventHandler<BatchMessageEventArgs> cache = BatchMessage;
            if (cache != null)
            {
                BatchMessageEventArgs args = new BatchMessageEventArgs(detailedMessage, message, error);
                cache(this, args);
            }
        }

        /// <summary>
        /// Called when a new result set has to be processed
        /// </summary>
        /// <param name="resultSet"></param>
        private void RaiseBatchResultSetProcessing(IDataReader dataReader, ShowPlanType expectedShowPlan)
        {
            EventHandler<BatchResultSetEventArgs> cache = BatchResultSetProcessing;
            if (cache != null)
            {
                BatchResultSetEventArgs args = new BatchResultSetEventArgs(dataReader, expectedShowPlan);
                BatchResultSetProcessing(this, args);
            }
        }

        /// <summary>
        /// Called when the result set has been processed
        /// </summary>
        private void RaiseBatchResultSetFinished()
        {
            EventHandler<EventArgs> cache = BatchResultSetFinished;
            if (cache != null)
            {
                cache(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the batch is being cancelled with an active result set 
        /// </summary>
        private void RaiseCancelling()
        {
            EventHandler<EventArgs> cache = BatchCancelling;
            if (cache != null)
            {
                cache(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Private enums

        private enum BatchState
        {
            Initial,
            Executing,
            Executed,
            ProcessingResults,
            Cancelling,
        }
        #endregion

        #region Private fields

        // correspond to public properties
        private bool isSuppressProviderMessageHeaders;
        private bool isResultExpected = false;
        private string sqlText = string.Empty;
        private int execTimeout = 30;
        private int scriptTrackingId = 0;
        private bool isScriptExecutionTracked = false;
        private const int ChangeDatabase = 0x1645;

        //command that will be used for execution
        private IDbCommand command = null;

        //current object state
        private BatchState state = BatchState.Initial;

        //script text to be executed
        private TextSpan textSpan;

        //index of the batch in collection of batches
        private int index = 0;

        private int expectedExecutionCount = 1;

        private long totalAffectedRows = 0;

        private bool hasErrors;

        // Expected showplan if any
        private ShowPlanType expectedShowPlan;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Batch()
        {
            // nothing
        }

        /// <summary>
        /// Creates and initializes a batch object
        /// </summary>
        /// <param name="isResultExpected">Whether it is one of "set [something] on/off" type of command,
        /// that doesn't return any results from the server
        /// </param>
        /// <param name="sqlText">Text of the batch</param>
        /// <param name="execTimeout">Timeout for the batch execution. 0 means no limit </param>
        public Batch(string sqlText, bool isResultExpected, int execTimeout)
        {
            this.isResultExpected = isResultExpected;
            this.sqlText = sqlText;
            this.execTimeout = execTimeout;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Is the Batch's text valid?
        /// </summary>
        public bool HasValidText
        {
            get
            {
                return !string.IsNullOrEmpty(sqlText);
            }
        }

        /// <summary>
        /// SQL text that to be executed in the Batch
        /// </summary>
        public string Text
        {
            get
            {
                return sqlText;
            }

            set
            {
                sqlText = value;
            }
        }


        /// <summary>
        /// Determines whether batch execution returns any results
        /// </summary>
        public bool IsResultsExpected
        {
            get
            {
                return isResultExpected;
            }

            set
            {
                isResultExpected = value;
            }
        }

        /// <summary>
        /// Determines the execution timeout for the batch
        /// </summary>
        public int ExecutionTimeout
        {
            get
            {
                return execTimeout;
            }

            set
            {
                execTimeout = value;
            }
        }

        /// <summary>
        /// Determines the textspan to wich the batch belongs to
        /// </summary>
        public TextSpan TextSpan
        {
            get
            {
                return textSpan;
            }
            set
            {
                textSpan = value;
            }
        }

        /// <summary>
        /// Determines the batch index in the collection of batches being executed
        /// </summary>
        public int BatchIndex
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }

        /// <summary>
        /// The number of times this batch is expected to be executed. Will be 1 by default, but for statements
        /// with "GO 2" or other numerical values, will have a number > 1
        /// </summary>
        public int ExpectedExecutionCount
        {
            get
            {
                return expectedExecutionCount;
            }

            set
            {
                expectedExecutionCount = value;
            }
        }

        /// <summary>
        /// Returns how many rows were affected. It should be the value that can be shown
        /// in the UI. 
        /// </summary>
        /// <remarks>
        /// It can be used only after the execution of the batch is finished
        /// </remarks>
        public long RowsAffected
        {
            get
            {
                return totalAffectedRows;
            }
        }

        /// <summary>
        /// Determines if the error.Source should be used when messages are written
        /// </summary>
        public bool IsSuppressProviderMessageHeaders
        {
            get
            {
                return isSuppressProviderMessageHeaders;
            }
            set
            {
                isSuppressProviderMessageHeaders = value;
            }
        }

        /// <summary>
        /// Gets or sets the id of the script we are tracking
        /// </summary>
        public int ScriptTrackingId
        {
            get
            {
                return scriptTrackingId;
            }
            set
            {
                scriptTrackingId = value;
            }
        }

        #endregion

        #region Public events

        /// <summary>
        /// fired when there is an error message from the server
        /// </summary>
        public event EventHandler<BatchErrorEventArgs> BatchError = null;

        /// <summary>
        /// fired when there is a message from the server
        /// </summary>
        public event EventHandler<BatchMessageEventArgs> BatchMessage = null;

        /// <summary>
        /// fired when there is a new result set available. It is guarnteed
        /// to be fired from the same thread that called Execute method
        /// </summary>
        public event EventHandler<BatchResultSetEventArgs> BatchResultSetProcessing = null;

        /// <summary>
        /// fired when the batch recieved cancel request BEFORE it 
        /// initiates cancel operation. Note that it is fired from a
        /// different thread then the one used to kick off execution
        /// </summary>
        public event EventHandler<EventArgs> BatchCancelling = null;

        /// <summary>
        /// fired when we've done absolutely all actions for the current result set
        /// </summary>
        public event EventHandler<EventArgs> BatchResultSetFinished = null;
        #endregion

        #region Public methods

        /// <summary>
        /// Resets the object to its initial state
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                state = BatchState.Initial;
                command = null;
                textSpan = new TextSpan();
                totalAffectedRows = 0;
                hasErrors = false;
                expectedShowPlan = ShowPlanType.None;
                isSuppressProviderMessageHeaders = false;
                scriptTrackingId = 0;
                isScriptExecutionTracked = false;
            }
        }

        /// <summary>
        /// Executes the batch 
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="expectedShowPlan">ShowPlan type to be used</param>
        /// <returns>result of execution</returns>
        /// <remarks>
        /// It does not return until execution is finished
        /// We may have received a Cancel request by the time this function is called
        /// </remarks>
        public ScriptExecutionResult Execute(SqlConnection connection, ShowPlanType expectedShowPlan)
        {
            // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
            return Execute((IDbConnection)connection, expectedShowPlan);
        }

        /// <summary>
        /// Executes the batch 
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="expectedShowPlan">ShowPlan type to be used</param>
        /// <returns>result of execution</returns>
        /// <remarks>
        /// It does not return until execution is finished
        /// We may have received a Cancel request by the time this function is called
        /// </remarks>
        public ScriptExecutionResult Execute(IDbConnection connection, ShowPlanType expectedShowPlan)
        {

            Validate.IsNotNull(nameof(connection), connection);

            //makes sure that the batch is not in use
            lock (this)
            {
                Debug.Assert(command == null, "SQLCommand is NOT null");
                if (command != null)
                {
                    command = null;
                }
            }

            this.expectedShowPlan = expectedShowPlan;

            return DoBatchExecutionImpl(connection, sqlText);
        }

        /// <summary>
        /// Cancels the batch
        /// </summary>
        /// <remarks>
        /// When batch is actually cancelled, Execute() will return with the appropiate status 
        /// </remarks>
        public void Cancel()
        {
            lock (this)
            {
                if (state != BatchState.Cancelling)
                {
                    state = BatchState.Cancelling;

                    RaiseCancelling();

                    if (command != null)
                    {
                        try
                        {
                            command.Cancel();

                            Debug.WriteLine("Batch.Cancel: command.Cancel completed");
                        }
                        catch (SqlException)
                        {
                            // eat it
                        }
                        catch (RetryLimitExceededException)
                        {
                            // eat it
                        }
                    }
                }
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Fires an error message event
        /// </summary>
        /// <param name="ex">Exception caught</param>
        /// <remarks>
        /// Non-SQL exception
        /// </remarks>
        protected void HandleExceptionMessage(Exception ex)
        {
            BatchErrorEventArgs args = new BatchErrorEventArgs(string.Format(CultureInfo.CurrentCulture, SR.EE_BatchError_Exception, ex.Message), ex);
            RaiseBatchError(args);
        }

        /// <summary>
        /// Fires a message event
        /// </summary>
        /// <param name="errors">SqlClient errors collection</param>
        /// <remarks>
        /// Sql specific messages.
        /// </remarks>
        protected void HandleSqlMessages(SqlErrorCollection errors)
        {
            foreach (SqlError error in errors)
            {
                if (error.Number == ChangeDatabase)
                {
                    continue;
                }

                string detailedMessage = FormatSqlErrorMessage(error);

                if (error.Class > 10)
                {
                    // expose this event as error
                    Debug.Assert(detailedMessage.Length != 0);
                    RaiseBatchError(detailedMessage, error, textSpan);

                    //at least one error message has been used
                    hasErrors = true;
                }
                else
                {
                    RaiseBatchMessage(detailedMessage, error.Message, error);
                }
            }
        }

        /// <summary>
        /// method that will be passed as delegate to SqlConnection.InfoMessage
        /// </summary>
        protected void OnSqlInfoMessageCallback(object sender, SqlInfoMessageEventArgs e)
        {
            HandleSqlMessages(e.Errors);
        }

        /// <summary>
        /// Delegete for SqlCommand.RecordsAffected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This is exposed as a regular message
        /// </remarks>
        protected void OnStatementExecutionFinished(object sender, StatementCompletedEventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, SR.EE_BatchExecutionInfo_RowsAffected,
                e.RecordCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            RaiseBatchMessage(message, message, null);
        }

        /// <summary>
        /// Called on a new ResultSet on the data reader
        /// </summary>
        /// <param name="dataReader">True if result set consumed, false on a Cancel request</param>
        /// <returns></returns>
        /// <remarks>
        /// The GridStorageResultSet created is owned by the batch consumer. It's only created here.
        /// Additionally, when BatchResultSet event handler is called, it won't return until
        /// all data is prcessed or the data being processed is terminated (i.e. cancel or error)
        /// </remarks>
        protected ScriptExecutionResult ProcessResultSet(IDataReader dataReader)
        {
            if (dataReader == null)
            {
                throw new ArgumentNullException();
            }

            Debug.WriteLine("ProcessResultSet: result set has been created");

            //initialize result variable that will be set by batch consumer
            ScriptExecutionResult scriptExecutionResult = ScriptExecutionResult.Success;

            RaiseBatchResultSetProcessing(dataReader, expectedShowPlan);

            if (state != BatchState.Cancelling)
            {
                return scriptExecutionResult;
            }
            else
            {
                return ScriptExecutionResult.Cancel;
            }
        }

        // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
        protected ScriptExecutionResult DoBatchExecution(SqlConnection connection, string script)
        {
            return DoBatchExecutionImpl(connection, script);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
        private ScriptExecutionResult DoBatchExecutionImpl(IDbConnection connection, string script)
        {
            Validate.IsNotNull(nameof(connection), connection);

            lock (this)
            {
                if (state == BatchState.Cancelling)
                {
                    state = BatchState.Initial;
                    return ScriptExecutionResult.Cancel;
                }
            }

            ScriptExecutionResult result = ScriptExecutionResult.Success;

            // SqlClient event handlers setup
            SqlInfoMessageEventHandler messageHandler = new SqlInfoMessageEventHandler(OnSqlInfoMessageCallback);
            StatementCompletedEventHandler statementCompletedHandler = null;

            DbConnectionWrapper connectionWrapper = new DbConnectionWrapper(connection);
            connectionWrapper.InfoMessage += messageHandler;

            IDbCommand command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = execTimeout;

            DbCommandWrapper commandWrapper = null;
            if (isScriptExecutionTracked && DbCommandWrapper.IsSupportedCommand(command))
            {
                statementCompletedHandler = new StatementCompletedEventHandler(OnStatementExecutionFinished);
                commandWrapper = new DbCommandWrapper(command);
                commandWrapper.StatementCompleted += statementCompletedHandler;
            }

            lock (this)
            {
                state = BatchState.Executing;
                this.command = command;
                command = null;
            }

            try
            {
                result = this.ExecuteCommand();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (SqlException sqlEx)
            {
                result = HandleSqlException(sqlEx);
            }
            catch (Exception ex)
            {
                result = ScriptExecutionResult.Failure;
                HandleExceptionMessage(ex);
            }
            finally
            {
               
                if (messageHandler == null)
                {
                    Logger.Write(TraceEventType.Error, "Expected handler to be declared");
                }

                if (null != connectionWrapper)
                {
                    connectionWrapper.InfoMessage -= messageHandler;
                }

                if (commandWrapper != null)
                {

                    if (statementCompletedHandler == null)
                    {
                        Logger.Write(TraceEventType.Error, "Expect handler to be declared if we have a command wrapper");
                    }
                    commandWrapper.StatementCompleted -= statementCompletedHandler;
                }

                lock (this)
                {
                    state = BatchState.Initial;
                    if (command != null)
                    {
                        command.Dispose();
                        command = null;
                    }
                }
            }

            return result;
        }

        private ScriptExecutionResult ExecuteCommand()
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            return this.ExecuteUnTrackedCommand();
            
        }

        private ScriptExecutionResult ExecuteUnTrackedCommand()
        {
            IDataReader reader = null;

            if (!isResultExpected)
            {
                command.ExecuteNonQuery();
            }
            else
            {
                reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
            }

            return this.CheckStateAndRead(reader);
        }

        private ScriptExecutionResult CheckStateAndRead(IDataReader reader = null)
        {
            ScriptExecutionResult result = ScriptExecutionResult.Success;

            if (!isResultExpected)
            {
                lock (this)
                {
                    if (state == BatchState.Cancelling)
                    {
                        result = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        result = ScriptExecutionResult.Success;
                        state = BatchState.Executed;
                    }
                }
            }
            else
            {
                lock (this)
                {
                    if (state == BatchState.Cancelling)
                    {
                        result = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        state = BatchState.ProcessingResults;
                    }
                }

                if (result != ScriptExecutionResult.Cancel)
                {
                    ScriptExecutionResult batchExecutionResult = ScriptExecutionResult.Success;

                    if (reader != null)
                    {
                        bool hasNextResult = false;
                        do
                        {
                            // if there were no results coming from the server, then the FieldCount is 0
                            if (reader.FieldCount <= 0)
                            {
                                hasNextResult = reader.NextResult();
                                continue;
                            }

                            batchExecutionResult = ProcessResultSet(reader);

                            if (batchExecutionResult != ScriptExecutionResult.Success)
                            {
                                result = batchExecutionResult;
                                break;
                            }

                            RaiseBatchResultSetFinished();

                            hasNextResult = reader.NextResult();

                        } while (hasNextResult);
                    }

                    if (hasErrors)
                    {
                        Debug.WriteLine("DoBatchExecution: successfull processed result set, but there were errors shown to the user");
                        result = ScriptExecutionResult.Failure;
                    }

                    if (result != ScriptExecutionResult.Cancel)
                    {
                        lock (this)
                        {
                            state = BatchState.Executed;
                        }
                    }
                }
            }

            if (reader != null)
            {
                try
                {
                    // reader.Close() doesn't actually close the reader
                    // so explicitly dispose the reader
                    reader.Dispose();
                    reader = null;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (SqlException)
                {
                    // nothing
                }
            }

            return result;
        }


        #endregion

    }
}