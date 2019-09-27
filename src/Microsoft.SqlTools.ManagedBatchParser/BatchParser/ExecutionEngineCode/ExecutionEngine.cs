//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ManagedBatchParser;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Execution engine class which executed the parsed batches
    /// </summary>
    public class ExecutionEngine : IDisposable
    {

        #region Private fields
        private OnErrorAction errorAction = OnErrorAction.Ignore;
        private int numBatchExecutionTimes = 1;
        private IDbConnection connection = null;
        private bool isSqlCmdConnection;

        private Parser commandParser = null;
        private int executionTimeout;
        private int startingLine;
        private ExecutionState executionState = ExecutionState.Initial;
        private string script;
        private ScriptExecutionResult result = ScriptExecutionResult.Failure;
        private bool isLocalParse;
        private ExecutionEngineConditions conditions = null;
        private IList<Batch> preConditionBatches = new List<Batch>();
        private IList<Batch> postConditionBatches = new List<Batch>();
        private IBatchEventsHandler batchEventHandlers = null;
        private Batch currentBatch = new Batch();
        private ShowPlanType expectedShowPlan;
        private int currentBatchIndex = -1;
        private int scriptTrackingId = 1;
        private object stateSyncLock = new object();

        /// <summary>
        /// The internal variables that can be used in SqlCommand substitution.
        /// These variables take precedence over environment variables.
        /// </summary>
        private Dictionary<string, string> internalVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        #endregion

        #region Private members

        /// <summary>
        /// Batch to be executed
        /// </summary>
        /// <param name="batch">Batch to execute</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private ScriptExecutionResult DoBatchExecution(Batch batch)
        {
            Validate.IsNotNull(nameof(batch), batch);

            ScriptExecutionResult result = ScriptExecutionResult.Success;

            // TODO, fawinter: Do I need to keep this batch?
            if (batch.HasValidText)
            {
                try
                {
                    if (conditions.IsParseOnly)
                    {
                        numBatchExecutionTimes = 1;
                    }

                    int timesLoop = numBatchExecutionTimes;
                    if (numBatchExecutionTimes > 1)
                    {
                        RaiseBatchMessage(string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_InitializingLoop));
                    }

                    while (timesLoop > 0 && result != ScriptExecutionResult.Cancel && result != ScriptExecutionResult.Halted)
                    {
                        result = batch.Execute(connection, expectedShowPlan);

                        Debug.Assert(connection != null);
                        if (connection == null || connection.State != ConnectionState.Open)
                        {
                            result = ScriptExecutionResult.Halted;
                        }

                        if (result == ScriptExecutionResult.Failure)
                        {
                            if (errorAction == OnErrorAction.Ignore)
                            {
                                if (numBatchExecutionTimes > 1)
                                {
                                    RaiseBatchMessage(SR.EE_BatchExecutionError_Ignoring);
                                }
                            }
                            else
                            {
                                RaiseBatchMessage(SR.EE_BatchExecutionError_Halting);
                                result = ScriptExecutionResult.Halted;
                            }
                        }

                        timesLoop--;
                    }


                    if (result == ScriptExecutionResult.Cancel)
                    {
                        RaiseBatchMessage(string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_QueryCancelledbyUser));
                    }
                    else
                    {
                        if (numBatchExecutionTimes > 1)
                        {
                            RaiseBatchMessage(string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_FinalizingLoop, numBatchExecutionTimes));
                        }
                    }
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // if anything goes wrong it will shutdown VS
                    Logger.Write(TraceEventType.Error, "Exception Caught in ExecutionEngine.DoBatchExecution(Batch) :" + ex.ToString());
                    result = ScriptExecutionResult.Failure;
                }
            }
            else
            {
                // TODO, fawinter: Success will be returned on an Empty text batch
            }

            return result;
        }

        /// <summary>
        /// Resets the script's related fields 
        /// </summary>
        /// <remarks>
        /// Once the execution thread is nulled, all handles will be closed and GC will collect it 
        /// </remarks>
        private void ResetScript()
        {
            lock (stateSyncLock)
            {
                executionState = ExecutionState.Initial;
            }

            ConfigurePrePostConditionBatches(preConditionBatches);
            ConfigurePrePostConditionBatches(postConditionBatches);

            currentBatchIndex = -1;
            conditions = null;
            batchEventHandlers = null;
        }

        /// <summary>
        /// Configures the script for execution
        /// </summary>
        private void ConfigureBatchParser()
        {
            BatchParser batchParser;
            bool sqlCmdMode;

            if (conditions != null && conditions.IsSqlCmd)
            {
                BatchParserSqlCmd batchParserSqlCmd = new BatchParserSqlCmd();
                batchParserSqlCmd.ConnectionChanged = new BatchParserSqlCmd.ConnectionChangedDelegate(OnConnectionChanged);
                batchParserSqlCmd.ErrorActionChanged = new BatchParserSqlCmd.ErrorActionChangedDelegate(OnErrorActionChanged);
                batchParserSqlCmd.InternalVariables = internalVariables;
                sqlCmdMode = true;
                batchParser = batchParserSqlCmd;
            }
            else
            {
                batchParser = new BatchParser();
                sqlCmdMode = false;
            }

            commandParser = new Parser(batchParser, batchParser, new StringReader(script), "[script]");
            commandParser.SetRecognizeSqlCmdSyntax(sqlCmdMode);
            commandParser.SetBatchDelimiter(BatchSeparator);
            commandParser.ThrowOnUnresolvedVariable = true;
            
            batchParser.Execute = new BatchParser.ExecuteDelegate(ExecuteBatchInternal);
            batchParser.ErrorMessage = new BatchParser.ScriptErrorDelegate(RaiseScriptError);
            batchParser.Message = new BatchParser.ScriptMessageDelegate(RaiseBatchMessage);
            batchParser.HaltParser = new BatchParser.HaltParserDelegate(OnHaltParser);
            batchParser.StartingLine = startingLine;

            if (isLocalParse && !sqlCmdMode)
            {
                batchParser.DisableVariableSubstitution();
            }
        }

        /// <summary>
        /// Configures the batch before execution
        /// </summary>
        private void ConfigureBatch()
        {
            numBatchExecutionTimes = 1;
            currentBatch.IsResultsExpected = true;
        }

        /// <summary>
        /// Called when batch parser found an error
        /// </summary>
        /// <param name="msg"></param>
        private void RaiseBatchParserExecutionError(string errorLine, string message, ScriptMessageType messageType)
        {
            EventHandler<BatchParserExecutionErrorEventArgs> cache = BatchParserExecutionError;
            if (cache != null)
            {
                BatchParserExecutionErrorEventArgs args = new BatchParserExecutionErrorEventArgs(errorLine, message, messageType);
                cache(this, args);
            }
        }

        /// <summary>
        /// Called just after the script has been executed
        /// </summary>
        /// <param name="result">scipt execution result</param>
        private void RaiseScriptExecutionFinished(ScriptExecutionResult result)
        {
            try
            {
                DisconnectSqlCmdInternal();                

                ConfigureBatchEventHandlers(currentBatch, batchEventHandlers, false);

                ResetScript();
            }
            finally
            {
                EventHandler<ScriptExecutionFinishedEventArgs> cache = ScriptExecutionFinished;
                if (cache != null)
                {
                    ScriptExecutionFinishedEventArgs args = new ScriptExecutionFinishedEventArgs(result);
                    cache(this, args);
                }
            }
        }

        /// <summary>
        /// Called when the script parsing has errors/warnings
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        private void RaiseScriptError(string message, ScriptMessageType messageType)
        {
            switch (messageType)
            {
                case (ScriptMessageType.FatalError):
                    RaiseBatchParserExecutionError(SR.EE_ScriptError_FatalError, message, messageType);
                    break;
                case (ScriptMessageType.Error):
                    RaiseBatchParserExecutionError(SR.EE_ScriptError_Error, message, messageType);
                    break;
                default:
                    Debug.Assert(messageType == ScriptMessageType.Warning);
                    RaiseBatchParserExecutionError(SR.EE_ScriptError_Warning, message, messageType);
                    break;
            }
        }

        /// <summary>
        /// Called just after batch has been executed
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="batchResult"></param>
        private void RaiseBatchParserExecutionFinished(Batch batch, ScriptExecutionResult batchResult, SqlCmdCommand sqlCmdCommand)
        {
            Debug.Assert(batch != null);

            EventHandler<BatchParserExecutionFinishedEventArgs> cache = BatchParserExecutionFinished;
            if (cache != null)
            {
                BatchParserExecutionFinishedEventArgs args = new BatchParserExecutionFinishedEventArgs(batchResult, batch, sqlCmdCommand);
                cache(this, args);
            }
        }

        /// <summary>
        /// Called right before a batch is executed
        /// </summary>
        /// <param name="batchLineNumber"></param>
        /// <param name="batch"></param>
        private void RaiseBatchParserExecutionStarted(Batch batch, TextSpan textSpan)
        {
            Debug.Assert(batch != null);

            EventHandler<BatchParserExecutionStartEventArgs> cache = BatchParserExecutionStart;
            if (cache != null)
            {
                // TODO, fawinter: Get the batch line number as a parameter and pass it in
                BatchParserExecutionStartEventArgs args = new BatchParserExecutionStartEventArgs(textSpan, batch);
                cache(this, args);
            }
        }

        /// <summary>
        /// Called when a message needs to be notified to the consumer
        /// </summary>
        /// <param name="message"></param>
        private void RaiseBatchMessage(string message)
        {
            Validate.IsNotNullOrEmptyString(nameof(message), message);

            if (batchEventHandlers != null)
            {
                BatchMessageEventArgs args = new BatchMessageEventArgs(message);
                batchEventHandlers.OnBatchMessage(this, args);
            }
        }
        
        /// <summary>
        /// Executes a given batch given the number of times
        /// </summary>
        /// <param name="batchScript"></param>
        /// <param name="num"></param>
        /// <param name="lineNumber"></param>
        /// <returns>True if we should continue processing, false otherwise</returns>
        private bool ExecuteBatchInternal(
            string batchScript, 
            int num, 
            int lineNumber,
            SqlCmdCommand sqlCmdCommand)
        {
            if (lineNumber == -1)
            {
                //it means that there was not a single sqlcmd command, 
                //including Batch Delimiter (i.e.) "GO" at the end of the batch.
                //it should be adjusted it to be the very first line in this case 
                lineNumber = 0;
            }

            TextSpan localTextSpan = new TextSpan();
            localTextSpan.iStartLine = lineNumber;

            if (!String.IsNullOrEmpty(batchScript))
            {
                bool continueProcessing = true;
                numBatchExecutionTimes = num;
                ExecuteBatchTextSpanInternal(batchScript, localTextSpan, out continueProcessing, sqlCmdCommand);
                return continueProcessing;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Executes the batch text given the text span
        /// </summary>
        /// <param name="batchScript"></param>
        /// <param name="textSpan"></param>
        /// <param name="continueProcessing"></param>
        private void ExecuteBatchTextSpanInternal(string batchScript, TextSpan textSpan, out bool continueProcessing, SqlCmdCommand sqlCmdCommand)
        {
            Debug.Assert(!String.IsNullOrEmpty(batchScript));
            continueProcessing = true;

            if (batchScript.Trim().Length <= 0)
            {
                result |= ScriptExecutionResult.Success;
                return;
            }

            Debug.Assert(currentBatch != null);

            if (executionState == ExecutionState.Cancelling)
            {
                result = ScriptExecutionResult.Cancel;
            }
            else
            {
                currentBatch.Reset();
                currentBatch.Text = batchScript;
                currentBatch.TextSpan = textSpan;
                currentBatch.BatchIndex = currentBatchIndex;
                currentBatch.ExpectedExecutionCount = numBatchExecutionTimes;
                
                currentBatchIndex++;

                if (conditions != null)
                {
                    currentBatch.IsSuppressProviderMessageHeaders = conditions.IsSuppressProviderMessageHeaders;

                    // TODO this is associated with Dacfx specific situations, so uncomment if need be
                    //currentBatch.IsScriptExecutionTracked = conditions.IsScriptExecutionTracked;
                    if (conditions.IsScriptExecutionTracked)
                    {
                        currentBatch.ScriptTrackingId = scriptTrackingId++;
                    }
                }

                //ExecutingBatch state means currentBatch is valid to use from another thread to Cancel
                executionState = ExecutionState.ExecutingBatch;
            }

            ScriptExecutionResult batchResult = ScriptExecutionResult.Failure;
            if (result != ScriptExecutionResult.Cancel)
            {
                bool isExecutionDiscarded = false;
                try
                {
                    RaiseBatchParserExecutionStarted(currentBatch, textSpan);

                    if (!isLocalParse)
                    {
                        batchResult = DoBatchExecution(currentBatch);
                    }
                    else
                    {
                        batchResult = ScriptExecutionResult.Success;
                    }
                }
                finally
                {
                    isExecutionDiscarded = (executionState == ExecutionState.Discarded);
                    if (executionState == ExecutionState.Cancelling || isExecutionDiscarded)
                    {
                        batchResult = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        executionState = ExecutionState.Executing;
                    }
                }

                if (!isExecutionDiscarded)
                {
                    RaiseBatchParserExecutionFinished(currentBatch, batchResult, sqlCmdCommand);
                }
            }
            else
            {
                batchResult = ScriptExecutionResult.Cancel;
            }

            //if we're in Cancel or Halt state, do some special actions
            if (batchResult == ScriptExecutionResult.Cancel || batchResult == ScriptExecutionResult.Halted)
            {
                result = batchResult;
                continueProcessing = false;
                return;
            }
            else
            {
                result |= batchResult;
            }
        }

        /// <summary>
        /// Executes the script by calling ManagedBatchParser.Parse()
        /// <remarks>
        /// The parser will in turn call to the ProcessBatch() which is the 
        /// one starting the execution process
        /// </remarks>
        /// </summary>
        private void DoScriptExecution(bool isBatchParser)
        {
            ConfigureBatch();

            if (isBatchParser)
            {
                ConfigureBatchParser();

                try
                {
                    commandParser.Parse();
                }
                catch (BatchParserException ex)
                {
                    if (ex.ErrorCode != ErrorCode.Aborted)
                    {
                        result = ScriptExecutionResult.Failure;
                        string info = ex.Text;

                        RaiseScriptError(string.Format(CultureInfo.CurrentCulture, SR.EE_ScriptError_ParsingSyntax, info), ScriptMessageType.FatalError);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write(TraceEventType.Warning, "Exception Caught in ExecutionEngine.DoScriptExecution(bool): " + ex.ToString());
                    throw;
                }
            }
            else
            {
                ExecuteBatchInternal(script, /* num */ 1, /* lineNumber */ 0, /* sqlcmdCommand required for parsing only*/ null);
            }

        }

        /// <summary>
        /// Executes the script (on a separated thread)
        /// </summary>
        private void DoExecute(bool isBatchParser)
        {
            //we should not be in the middle of execution here
            if (executionState == ExecutionState.Executing || executionState == ExecutionState.ExecutingBatch)
            {
                throw new InvalidOperationException(SR.EE_ExecutionNotYetCompleteError);
            }

            executionState = ExecutionState.Initial;
            result = ScriptExecutionResult.Failure;
            currentBatchIndex = 0;
            currentBatch.ExecutionTimeout = executionTimeout;
            expectedShowPlan = ShowPlanType.None;

            if (!isLocalParse)
            {
                errorAction = conditions.IsHaltOnError ?
                    OnErrorAction.Exit :
                    OnErrorAction.Ignore;

                CreatePrePostConditionBatches();
            }

            ConfigureBatchEventHandlers(currentBatch, batchEventHandlers, true);

            // do we have a cancel request already?
            lock (stateSyncLock)
            {
                if (executionState == ExecutionState.Cancelling)
                {
                    RaiseScriptExecutionFinished(ScriptExecutionResult.Cancel);
                    return;
                }
                Debug.Assert(executionState == ExecutionState.Initial);
                executionState = ExecutionState.Executing;
            }

            if ((result = ExecutePrePostConditionBatches(preConditionBatches)) == ScriptExecutionResult.Success)
            {
                DoScriptExecution(isBatchParser);
            }

            if (!CheckForDiscardedConnection())
            {
                if (!isLocalParse)
                {
                    if (conditions.IsTransactionWrapped && !conditions.IsParseOnly)
                    {
                        if (result == ScriptExecutionResult.Success)
                        {
                            postConditionBatches.Add(new Batch(ExecutionEngineConditions.CommitTransactionStatement, false, executionTimeout));
                        }
                        else
                        {
                            postConditionBatches.Add(new Batch(ExecutionEngineConditions.RollbackTransactionStatement, false, executionTimeout));
                        }
                    }

                    // no need to update the result value as it has been updated by the DoScriptExecution()
                    ExecutePrePostConditionBatches(postConditionBatches);
                }

                //fire an event that we're done with execution of all batches
                if (result == ScriptExecutionResult.Halted) //remap into failure
                {
                    result = ScriptExecutionResult.Failure;
                }

                RaiseScriptExecutionFinished(result);
            }
        }

        /// <summary>
        /// Cancels the current batch being executed
        /// </summary>
        /// <remarks>
        /// This method is meant to be called from a separate thread
        /// in combination with the Cancel method()
        /// </remarks>
        public void CancelCurrentBatch()
        {
            ExecutionState state;
            lock (stateSyncLock)
            {
                state = executionState;
                executionState = ExecutionState.Cancelling;

                if (state == ExecutionState.ExecutingBatch)
                {
                    Debug.Assert(currentBatch != null);
                    if (currentBatch != null)
                    {
                        currentBatch.Cancel();
                    }
                }
            }
        }

        private void Discard()
        {
            Debug.WriteLine("ExecutionEngine.Cancel(): Thread didn't cancel");

            ConfigureBatchEventHandlers(currentBatch, batchEventHandlers, false);

            lock (stateSyncLock)
            {
                executionState = ExecutionState.Discarded;
            }
        }

        /// <summary>
        /// Gets the Batch Separator statement
        /// </summary>
        private string BatchSeparator
        {
            get
            {
                if (conditions != null)
                {
                    return conditions.BatchSeparator;
                }
                else
                {
                    return ExecutionEngineConditions.BatchSeparatorStatement;
                }
            }
        }


        /// <summary>
        /// Create a set of batches to be executed before and after the script is executed
        /// </summary>
        /// <remarks>
        /// This is the way some server side settings can be set. Additionally, it supports
        /// a way to wrap the script execution within a transaction block
        /// </remarks>
        private void CreatePrePostConditionBatches()
        {
            StringBuilder scriptPreBatches = new StringBuilder();
            StringBuilder scriptPostBatches = new StringBuilder();
            int serverVersion = 8;

            if (connection != null && connection.State == ConnectionState.Open)
            {
                serverVersion = new Version(ReliableConnectionHelper.ReadServerVersion(connection)).Major;
            }

            ConfigurePrePostConditionBatches(preConditionBatches);
            ConfigurePrePostConditionBatches(postConditionBatches);

            if (conditions.IsNoExec)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.NoExecStatement(false));
            }

            if (conditions.IsStatisticsIO)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsIOStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsIOStatement(false));
            }

            if (conditions.IsStatisticsTime)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsTimeStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsTimeStatement(false));
            }

            if (conditions.IsEstimatedShowPlan)
            {
                if (serverVersion >= 9)
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanXmlStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanXmlStatement(false));
                    expectedShowPlan = ShowPlanType.EstimatedXmlShowPlan;
                }
                else
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanAllStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanAllStatement(false));
                    expectedShowPlan = ShowPlanType.EstimatedExecutionShowPlan;
                }
            }
            else if (conditions.IsActualShowPlan)
            {
                if (serverVersion >= 9)
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsXmlStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsXmlStatement(false));
                    expectedShowPlan = ShowPlanType.ActualXmlShowPlan;
                }
                else
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsProfileStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsProfileStatement(false));
                    expectedShowPlan = ShowPlanType.ActualExecutionShowPlan;
                }
            }

            if (conditions.IsTransactionWrapped)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.BeginTransactionStatement);
                // issuing a Rollback or a Commit will depend on the script execution result
            }

            if (conditions.IsParseOnly)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ParseOnlyStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ParseOnlyStatement(false));
            }

            if (conditions.IsNoExec)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.NoExecStatement(true));
            }

            if (conditions.IsShowPlanText &&
                !conditions.IsEstimatedShowPlan &&
                !conditions.IsActualShowPlan)
            {
                // SET SHOWPLAN_TEXT cannot be used with other statements in the batch
                preConditionBatches.Insert(0,
                    new Batch(
                        string.Format(CultureInfo.CurrentCulture, "{0} ", ExecutionEngineConditions.ShowPlanTextStatement(true)),
                        false,
                        executionTimeout));

                postConditionBatches.Insert(0,
                    new Batch(
                        string.Format(CultureInfo.CurrentCulture, "{0} ", ExecutionEngineConditions.ShowPlanTextStatement(false)),
                        false,
                        executionTimeout));
            }

            string preBatches = scriptPreBatches.ToString().Trim();
            string postBatches = scriptPostBatches.ToString().Trim();

            if (scriptPreBatches.Length > 0)
            {
                preConditionBatches.Add(new Batch(preBatches, false, executionTimeout));
            }

            if (scriptPostBatches.Length > 0)
            {
                postConditionBatches.Add(new Batch(postBatches, false, executionTimeout));
            }
        }

        /// <summary>
        /// Executes a list of batches related to the Pre and Post scripts
        /// </summary>
        /// <param name="batches"></param>
        private ScriptExecutionResult ExecutePrePostConditionBatches(IList<Batch> batches)
        {
            Validate.IsNotNull(nameof(batches), batches);

            ScriptExecutionResult result = ScriptExecutionResult.Success;

            foreach (Batch batch in batches)
            {
                try
                {
                    ConfigureBatchEventHandlers(batch, batchEventHandlers, true);
                    result = batch.Execute(connection, ShowPlanType.None);
                }
                catch (SqlException)
                {
                    result = ScriptExecutionResult.Failure;
                }
                finally
                {
                    ConfigureBatchEventHandlers(batch, batchEventHandlers, false);
                }

                if (result != ScriptExecutionResult.Success)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Cleans up any prev
        /// </summary>
        private static void ConfigurePrePostConditionBatches(IList<Batch> batches)
        {
            Validate.IsNotNull(nameof(batches), batches);

            batches.Clear();
        }

        /// <summary>
        /// Binds/Unbinds the methods defined in IBatchEventHandlers with the batch events
        /// </summary>
        /// <param name="batch">Batch to be used in the event binding</param>
        /// <param name="handlers">object implementing the IBatcgEventHandlers interface</param>
        /// <param name="isHookup">Binds or Unbinds the evnts</param>
        private static void ConfigureBatchEventHandlers(Batch batch, IBatchEventsHandler handlers, bool isHookup)
        {
            Validate.IsNotNull(nameof(batch), batch);

            if (isHookup)
            {
                Validate.IsNotNull(nameof(handlers), handlers);

                batch.BatchError += new EventHandler<BatchErrorEventArgs>(handlers.OnBatchError);
                batch.BatchMessage += new EventHandler<BatchMessageEventArgs>(handlers.OnBatchMessage);
                batch.BatchResultSetProcessing += new EventHandler<BatchResultSetEventArgs>(handlers.OnBatchResultSetProcessing);
                batch.BatchResultSetFinished += new EventHandler<EventArgs>(handlers.OnBatchResultSetFinished);
                batch.BatchCancelling += new EventHandler<EventArgs>(handlers.OnBatchCancelling);
            }
            else
            {
                if (handlers != null)
                {
                    batch.BatchError -= new EventHandler<BatchErrorEventArgs>(handlers.OnBatchError);
                    batch.BatchMessage -= new EventHandler<BatchMessageEventArgs>(handlers.OnBatchMessage);
                    batch.BatchResultSetProcessing -= new EventHandler<BatchResultSetEventArgs>(handlers.OnBatchResultSetProcessing);
                    batch.BatchResultSetFinished -= new EventHandler<EventArgs>(handlers.OnBatchResultSetFinished);
                    batch.BatchCancelling -= new EventHandler<EventArgs>(handlers.OnBatchCancelling);
                }
            }
        }

        /// <summary>
        /// If a discarded state is found, we will close the connection
        /// </summary>
        /// <remarks>
        /// The discarded state is possible only on a synch Cancel request
        /// </remarks>
        /// <returns>
        /// True if this is discarded connection
        /// </returns>
        private bool CheckForDiscardedConnection()
        {
            bool isDiscarded = false;
            lock (stateSyncLock)
            {
                isDiscarded = (executionState == ExecutionState.Discarded);
            }

            if (isDiscarded)
            {
                Debug.WriteLine("ExecutionEngine.CheckForDiscardedConnection");

                ResetScript();

                CloseConnection(connection);

                return true;
            }

            return false;
        }

        #endregion

        #region Private SqlCmd related methods

        /// <summary>
        /// Called when parser is about to halt the execution
        /// </summary>
        private void OnHaltParser()
        {
            result = ScriptExecutionResult.Halted;
        }

        /// <summary>
        /// Changed when parser changed the error action type
        /// </summary>
        /// <param name="ea"></param>
        private void OnErrorActionChanged(OnErrorAction ea)
        {
            errorAction = ea;
        }

        /// <summary>
        /// Called when parser requests a new connection
        /// </summary>
        /// <param name="connectionStringBuilder"></param>
        private void OnConnectionChanged(SqlConnectionStringBuilder connectionStringBuilder)
        {
            // make sure that we disconnect any previous SqlCmd connection
            DisconnectSqlCmdInternal();

            // create a new SqlCmd connection
            SqlConnection connection = ConnectSqlCmdInternal(connectionStringBuilder);
            if (connection != null)
            {
                isSqlCmdConnection = true;
                this.connection = connection;

                CreatePrePostConditionBatches();
                result = ExecutePrePostConditionBatches(preConditionBatches);
            }
        }

        /// <summary>
        /// Connects when :connect is identified within the script
        /// </summary>
        /// <param name="connectionStringBuilder"></param>
        /// <returns></returns>
        private static SqlConnection ConnectSqlCmdInternal(SqlConnectionStringBuilder connectionStringBuilder)
        {
            Validate.IsNotNull(nameof(connectionStringBuilder), connectionStringBuilder);

            SqlConnection connection = null;

            try
            {
                connection = new SqlConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
            }
            catch (SqlException ex)
            {
                Logger.Write(TraceEventType.Warning, "Exception Caught in ExecutionEngine.ConnectSqlCmdInternal(SqlConnectionStringBuilder): " + ex.ToString());
                throw;
            }

            return connection;
        }

        /// <summary>
        /// Disconnects a sqlcmd connection
        /// </summary>
        private void DisconnectSqlCmdInternal()
        {
            if (isSqlCmdConnection)
            {
                RaiseBatchMessage(string.Format(CultureInfo.CurrentCulture, "Disconnection from server {0}", ReliableConnectionHelper.GetServerName(connection)));
                CloseConnection(connection);
            }
        }
        #endregion

        #region Private methods

        /// <summary>
        /// Closes a connection
        /// </summary>
        /// <param name="connection"></param>
        static private void CloseConnection(IDbConnection connection)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    connection.Close();
                }
                catch (SqlException ex)
                {
                    Logger.Write(TraceEventType.Warning, "Exception Caught in ExecutionEngine.CloseConnection(SqlConnection): " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Setups the script execution
        /// </summary>
        /// <param name="scriptExecutionArgs"></param>
        private void ExecuteInternal(ScriptExecutionArgs scriptExecutionArgs, bool isBatchParser)
        {

            Validate.IsNotNull(nameof(scriptExecutionArgs), scriptExecutionArgs);

            Validate.IsNotNullOrEmptyString(nameof(scriptExecutionArgs.Script), scriptExecutionArgs.Script);
            Validate.IsNotNull(nameof(scriptExecutionArgs.ReliableConnection), scriptExecutionArgs.ReliableConnection);
            Validate.IsNotNull(nameof(scriptExecutionArgs.Conditions), scriptExecutionArgs.Conditions);
            Validate.IsNotNull(nameof(scriptExecutionArgs.BatchEventHandlers), scriptExecutionArgs.BatchEventHandlers);

            Debug.Assert(scriptExecutionArgs.TimeOut >= 0);

            executionTimeout = scriptExecutionArgs.TimeOut < 0 ? 0 : scriptExecutionArgs.TimeOut;
            connection = scriptExecutionArgs.ReliableConnection;
            conditions = new ExecutionEngineConditions(scriptExecutionArgs.Conditions);
            script = scriptExecutionArgs.Script;
            isSqlCmdConnection = false;
            batchEventHandlers = scriptExecutionArgs.BatchEventHandlers;
            startingLine = scriptExecutionArgs.StartingLine;
            internalVariables = scriptExecutionArgs.Variables;

            DoExecute(isBatchParser);
        }

        #endregion

        #region Public Events

        /// <summary>
        /// This event gets fired when execution of one batch is completed
        /// </summary>
        public event EventHandler<BatchParserExecutionFinishedEventArgs> BatchParserExecutionFinished = null;

        /// <summary>
        /// This event gets fired when execution of a batch is about to start
        /// </summary>
        public event EventHandler<BatchParserExecutionStartEventArgs> BatchParserExecutionStart = null;

        /// <summary>
        /// This event gets fired when when there's an error/warnings from the scripting engine
        /// </summary>
        public event EventHandler<BatchParserExecutionErrorEventArgs> BatchParserExecutionError = null;

        /// <summary>
        /// This event gets fired when the script execution is completed
        /// </summary>
        public event EventHandler<ScriptExecutionFinishedEventArgs> ScriptExecutionFinished = null;

        #endregion

        #region Public members
        
        /// <summary>
        /// Executes the script
        /// </summary>
        /// <param name="scriptArgs">Script to be executed</param>
        public void ExecuteScript(object scriptArgs)
        {
            ExecuteInternal(scriptArgs as ScriptExecutionArgs, /* isBatchParser */ true);
        }

        /// <summary>
        /// Executes a given batch
        /// </summary>
        /// <param name="batchScript"></param>
        /// <returns></returns>
        public void ExecuteBatch(ScriptExecutionArgs scriptExecutionArgs)
        {
            ExecuteInternal(scriptExecutionArgs, /* isBatchParser */ false);
        }

        /// <summary>
        /// Parses the script locally
        /// </summary>
        /// <param name="script">script to parse</param>
        /// <param name="batchEventsHandler">batch handler</param>   
        /// <param name="conditions">execution engine conditions if specified</param>
        /// <remarks>
        /// The batch parser functionality is used in this case
        /// </remarks>
        public void ParseScript(string script, IBatchEventsHandler batchEventsHandler, ExecutionEngineConditions conditions = null)
        {
            Validate.IsNotNull(nameof(script), script);
            Validate.IsNotNull(nameof(batchEventsHandler), batchEventsHandler);

            if (conditions != null)
            {
                this.conditions = conditions;
            }
            this.script = script;
            batchEventHandlers = batchEventsHandler;
            isLocalParse = true;

            DoExecute(/* isBatchParser */ true);
        }

        /// <summary>
        /// Close the current connection
        /// </summary>
        /// <param name="isCloseConnection"></param>
        public void Close(bool isCloseConnection)
        {
            Close(isCloseConnection, /* isDiscard */ false);
        }

        /// <summary>
        /// Close/Discard the current connection
        /// </summary>
        /// <param name="isCloseConnection"></param>
        /// <param name="isDiscard"></param>
        public void Close(bool isCloseConnection, bool isDiscard)
        {
            Close(isCloseConnection, isDiscard, /* isFinishExecution */ false);
        }

        /// <summary>
        /// Close/Discard the current connection
        /// </summary>
        /// <param name="isCloseConnection">true if connection has to be closed</param>
        /// <param name="isDiscard">true if connection has to be discarded</param>
        /// <param name="isFinishExecution">Raises the script execution finish event</param>
        public void Close(bool isCloseConnection, bool isDiscard, bool isFinishExecution)
        {
            if (isFinishExecution)
            {
                RaiseScriptExecutionFinished(ScriptExecutionResult.Cancel);
            }

            if (isDiscard)
            {
                Discard();
            }
            else
            {
                if (isCloseConnection)
                {
                    CloseConnection(connection);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        //  resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Debug.WriteLine("ExecutionEngine.Dispose");
                if (commandParser != null)
                {
                    commandParser.Dispose();
                    commandParser = null;
                }

                ResetScript();

                stateSyncLock = null;
                currentBatch = null;
            }
        }
        #endregion
    }
}
