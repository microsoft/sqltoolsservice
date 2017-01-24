//------------------------------------------------------------------------------
// <copyright file="ExecutionEngine.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class ExecutionEngine : IDisposable
    {
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
        /// <remarks>
        /// The batch parser functionality is used in this case
        /// </remarks>
        public void ParseScript(String script, IBatchEventsHandler batchEventsHandler)
        {
            Validate.IsNotNullOrEmptyString(nameof(script), script);
            Validate.IsNotNull(nameof(batchEventsHandler), batchEventsHandler);


            _script = script;
            _batchEventHandlers = batchEventsHandler;
            _isLocalParse = true;

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
                    CloseConnection(_connection);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        //  resetting unmanaged resources.
        /// </summary>
        virtual public void Dispose()
        {
            //Debug.WriteLine("ExecutionEngine.Dispose");
            if (_commandParser != null)
            {
                _commandParser.Dispose();
                _commandParser = null;
            }

            ResetScript();

            _stateSyncLock = null;
            _currentBatch = null;
        }
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
                    // Parsing mode we execute only once
                    if (_conditions.IsParseOnly)
                    {
                        _numBatchExecutionTimes = 1;
                    }

                    int timesLoop = _numBatchExecutionTimes;
                    if (_numBatchExecutionTimes > 1)
                    {
                        RaiseBatchMessage(String.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_InitilizingLoop, _numBatchExecutionTimes));
                    }

                    while (timesLoop > 0 && result != ScriptExecutionResult.Cancel && result != ScriptExecutionResult.Halted)
                    {
                        result = batch.Execute(_connection, _expectedShowPlan);

                        Debug.Assert(_connection != null);
                        if (_connection == null || _connection.State != ConnectionState.Open)
                        {
                            result = ScriptExecutionResult.Halted;
                        }

                        if (result == ScriptExecutionResult.Failure)
                        {
                            if (_errorAction == OnErrorAction.Ignore)
                            {
                                if (_numBatchExecutionTimes > 1)
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
                        RaiseBatchMessage(String.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_QueryCancelledbyUser));
                    }
                    else
                    {
                        if (_numBatchExecutionTimes > 1)
                        {
                            RaiseBatchMessage(String.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionInfo_FinalizingLoop, _numBatchExecutionTimes));
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
                    Logger.Write(LogLevel.Error, "Exception Caught in ExecutionEngine.DoBatchExecution(Batch) :" + ex.ToString());
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
            lock (_stateSyncLock)
            {
                _executionState = ExecutionState.Initial;
            }

            ConfigurePrePostConditionBatches(_preConditionBatches);
            ConfigurePrePostConditionBatches(_postConditionBatches);

            _currentBatchIndex = -1;
            _conditions = null;
            _batchEventHandlers = null;
        }

        /// <summary>
        /// Configures the script for execution
        /// </summary>
        private void ConfigureBatchParser()
        {
            BatchParser batchParser;
            bool sqlCmdMode;

            if (_conditions != null && _conditions.IsSqlCmd)
            {
                BatchParserSqlCmd batchParserSqlCmd = new BatchParserSqlCmd();
                batchParserSqlCmd.ConnectionChanged = new BatchParserSqlCmd.ConnectionChangedDelegate(OnConnectionChanged);
                batchParserSqlCmd.ErrorActionChanged = new BatchParserSqlCmd.ErrorActionChangedDelegate(OnErrorActionChanged);
                batchParserSqlCmd.InternalVariables = _internalVariables;
                sqlCmdMode = true;
                batchParser = batchParserSqlCmd;
            }
            else
            {
                batchParser = new BatchParser();
                sqlCmdMode = false;
            }

            _commandParser = new Parser(batchParser, batchParser, new StringReader(_script), "[script]");
            _commandParser.SetRecognizeSqlCmdSyntax(sqlCmdMode);
            _commandParser.SetBatchDelimiter(BatchSeparator);
            _commandParser.ThrowOnUnresolvedVariable = true;
            
            batchParser.Execute = new BatchParser.ExecuteDelegate(ExecuteBatchInternal);
            batchParser.ErrorMessage = new BatchParser.ScriptErrorDelegate(RaiseScriptError);
            batchParser.Message = new BatchParser.ScriptMessageDelegate(RaiseBatchMessage);
            batchParser.HaltParser = new BatchParser.HaltParserDelegate(OnHaltParser);
            batchParser.StartingLine = _startingLine;

            if (_isLocalParse)
            {
                batchParser.DisableVariableSubstitution();
            }
        }

        /// <summary>
        /// Configures the batch before execution
        /// </summary>
        private void ConfigureBatch()
        {
            _numBatchExecutionTimes = 1;
            _currentBatch.IsResultsExpected = true;
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

                ConfigureBatchEventHandlers(_currentBatch, _batchEventHandlers, false);

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
        private void RaiseBatchParserExecutionFinished(Batch batch, ScriptExecutionResult batchResult)
        {
            Debug.Assert(batch != null);

            EventHandler<BatchParserExecutionFinishedEventArgs> cache = BatchParserExecutionFinished;
            if (cache != null)
            {
                BatchParserExecutionFinishedEventArgs args = new BatchParserExecutionFinishedEventArgs(batchResult, batch);
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
        private void RaiseBatchMessage(String message)
        {
            Validate.IsNotNullOrEmptyString(nameof(message), message);

            if (_batchEventHandlers != null)
            {
                BatchMessageEventArgs args = new BatchMessageEventArgs(message);
                _batchEventHandlers.OnBatchMessage(this, args);
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
            String batchScript, 
            int num, 
            int lineNumber)
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
                _numBatchExecutionTimes = num;
                ExecuteBatchTextSpanInternal(batchScript, localTextSpan, out continueProcessing);
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
        private void ExecuteBatchTextSpanInternal(String batchScript, TextSpan textSpan, out bool continueProcessing)
        {
            Debug.Assert(!String.IsNullOrEmpty(batchScript));
            continueProcessing = true;

            if (batchScript.Trim().Length <= 0)
            {
                _result |= ScriptExecutionResult.Success;
                return;
            }

            Debug.Assert(_currentBatch != null);

            if (_executionState == ExecutionState.Cancelling)
            {
                _result = ScriptExecutionResult.Cancel;
            }
            else
            {
                _currentBatch.Reset();
                _currentBatch.Text = batchScript;
                _currentBatch.TextSpan = textSpan;
                _currentBatch.BatchIndex = _currentBatchIndex;
                
                _currentBatchIndex++;

                if (_conditions != null)
                {
                    _currentBatch.IsSuppressProviderMessageHeaders = _conditions.IsSuppressProviderMessageHeaders;

                    // TODO this is associated with Dacfx specific situations, so uncomment if need be
                    //_currentBatch.IsScriptExecutionTracked = _conditions.IsScriptExecutionTracked;
                    if (_conditions.IsScriptExecutionTracked)
                    {
                        _currentBatch.ScriptTrackingId = _scriptTrackingId++;
                    }
                }

                //ExecutingBatch state means _currentBatch is valid to use from another thread to Cancel
                _executionState = ExecutionState.ExecutingBatch;
            }

            ScriptExecutionResult batchResult = ScriptExecutionResult.Failure;
            if (_result != ScriptExecutionResult.Cancel)
            {
                bool isExecutionDiscarded = false;
                try
                {
                    RaiseBatchParserExecutionStarted(_currentBatch, textSpan);

                    if (!_isLocalParse)
                    {
                        batchResult = DoBatchExecution(_currentBatch);
                    }
                    else
                    {
                        batchResult = ScriptExecutionResult.Success;
                    }
                }
                finally
                {
                    isExecutionDiscarded = (_executionState == ExecutionState.Discarded);
                    if (_executionState == ExecutionState.Cancelling || isExecutionDiscarded)
                    {
                        batchResult = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        _executionState = ExecutionState.Executing;
                    }
                }

                if (!isExecutionDiscarded)
                {
                    RaiseBatchParserExecutionFinished(_currentBatch, batchResult);
                }
            }
            else
            {
                batchResult = ScriptExecutionResult.Cancel;
            }

            //if we're in Cancel or Halt state, do some special actions
            if (batchResult == ScriptExecutionResult.Cancel || batchResult == ScriptExecutionResult.Halted)
            {
                _result = batchResult;
                continueProcessing = false;
                return;
            }
            else
            {
                _result |= batchResult;
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
                    _commandParser.Parse();
                }
                catch (BatchParserException ex)
                {
                    if (ex.ErrorCode != ErrorCode.Aborted)
                    {
                        _result = ScriptExecutionResult.Failure;
                        string info = ex.Text;

                        RaiseScriptError(String.Format(CultureInfo.CurrentCulture, SR.EE_ScriptError_ParsingSyntax, info), ScriptMessageType.FatalError);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write(LogLevel.Warning, "Exception Caught in ExecutionEngine.DoScriptExecution(bool): " + ex.ToString());
                    throw;
                }
            }
            else
            {
                ExecuteBatchInternal(_script, /* num */ 1, /* lineNumber */ 0);
            }

        }

        /// <summary>
        /// Executes the script (on a separated thread)
        /// </summary>
        private void DoExecute(bool isBatchParser)
        {
            //we should not be in the middle of execution here
            if (_executionState == ExecutionState.Executing || _executionState == ExecutionState.ExecutingBatch)
            {
                throw new InvalidOperationException(SR.EE_ExecutionNotYetCompleteError);
            }

            _executionState = ExecutionState.Initial;
            _result = ScriptExecutionResult.Failure;
            _currentBatchIndex = 0;
            _currentBatch.ExecutionTimeout = _executionTimeout;
            _expectedShowPlan = ShowPlanType.None;

            if (!_isLocalParse)
            {
                _errorAction = _conditions.IsHaltOnError ?
                    OnErrorAction.Exit :
                    OnErrorAction.Ignore;

                CreatePrePostConditionBatches();
            }

            ConfigureBatchEventHandlers(_currentBatch, _batchEventHandlers, true);

            // do we have a cancel request already?
            lock (_stateSyncLock)
            {
                if (_executionState == ExecutionState.Cancelling)
                {
                    RaiseScriptExecutionFinished(ScriptExecutionResult.Cancel);
                    return;
                }
                Debug.Assert(_executionState == ExecutionState.Initial);
                _executionState = ExecutionState.Executing;
            }

            if ((_result = ExecutePrePostConditionBatches(_preConditionBatches)) == ScriptExecutionResult.Success)
            {
                DoScriptExecution(isBatchParser);
            }

            if (!CheckForDiscardedConnection())
            {
                if (!_isLocalParse)
                {
                    if (_conditions.IsTransactionWrapped && !_conditions.IsParseOnly)
                    {
                        if (_result == ScriptExecutionResult.Success)
                        {
                            _postConditionBatches.Add(new Batch(ExecutionEngineConditions.CommitTransactionStatement, false, _executionTimeout));
                        }
                        else
                        {
                            _postConditionBatches.Add(new Batch(ExecutionEngineConditions.RollbackTransactionStatement, false, _executionTimeout));
                        }
                    }

                    // no need to update the _result value as it has been updated by the DoScriptExecution()
                    ExecutePrePostConditionBatches(_postConditionBatches);
                }

                //fire an event that we're done with execution of all batches
                if (_result == ScriptExecutionResult.Halted) //remap into failure
                {
                    _result = ScriptExecutionResult.Failure;
                }

                RaiseScriptExecutionFinished(_result);
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
            lock (_stateSyncLock)
            {
                state = _executionState;
                _executionState = ExecutionState.Cancelling;

                if (state == ExecutionState.ExecutingBatch)
                {
                    Debug.Assert(_currentBatch != null);
                    if (_currentBatch != null)
                    {
                        _currentBatch.Cancel();
                    }
                }
            }
        }

        private void Discard()
        {
            Debug.WriteLine("ExecutionEngine.Cancel(): Thread didn't cancel");

            ConfigureBatchEventHandlers(_currentBatch, _batchEventHandlers, false);

            lock (_stateSyncLock)
            {
                _executionState = ExecutionState.Discarded;
            }
        }

        /// <summary>
        /// Gets the Batch Separator statement
        /// </summary>
        private String BatchSeparator
        {
            get
            {
                if (_conditions != null)
                {
                    return _conditions.BatchSeparator;
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

            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                serverVersion = new Version(ReliableConnectionHelper.ReadServerVersion(_connection)).Major;
            }

            ConfigurePrePostConditionBatches(_preConditionBatches);
            ConfigurePrePostConditionBatches(_postConditionBatches);

            if (_conditions.IsNoExec)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.NoExecStatement(false));
            }

            if (_conditions.IsStatisticsIO)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsIOStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsIOStatement(false));
            }

            if (_conditions.IsStatisticsTime)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsTimeStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsTimeStatement(false));
            }

            if (_conditions.IsEstimatedShowPlan)
            {
                if (serverVersion >= 9)
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanXmlStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanXmlStatement(false));
                    _expectedShowPlan = ShowPlanType.EstimatedXmlShowPlan;
                }
                else
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanAllStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ShowPlanAllStatement(false));
                    _expectedShowPlan = ShowPlanType.EstimatedExecutionShowPlan;
                }
            }
            else if (_conditions.IsActualShowPlan)
            {
                if (serverVersion >= 9)
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsXmlStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsXmlStatement(false));
                    _expectedShowPlan = ShowPlanType.ActualXmlShowPlan;
                }
                else
                {
                    scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsProfileStatement(true));
                    scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.StatisticsProfileStatement(false));
                    _expectedShowPlan = ShowPlanType.ActualExecutionShowPlan;
                }
            }

            if (_conditions.IsTransactionWrapped)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.BeginTransactionStatement);
                // issuing a Rollback or a Commit will depend on the script execution result
            }

            if (_conditions.IsParseOnly)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ParseOnlyStatement(true));
                scriptPostBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.ParseOnlyStatement(false));
            }

            if (_conditions.IsNoExec)
            {
                scriptPreBatches.AppendFormat(CultureInfo.InvariantCulture, "{0} ", ExecutionEngineConditions.NoExecStatement(true));
            }

            if (_conditions.IsShowPlanText &&
                !_conditions.IsEstimatedShowPlan &&
                !_conditions.IsActualShowPlan)
            {
                // SET SHOWPLAN_TEXT cannot be used with other statements in the batch
                _preConditionBatches.Insert(0,
                    new Batch(
                        String.Format(CultureInfo.CurrentCulture, "{0} ", ExecutionEngineConditions.ShowPlanTextStatement(true)),
                        false,
                        _executionTimeout));

                _postConditionBatches.Insert(0,
                    new Batch(
                        String.Format(CultureInfo.CurrentCulture, "{0} ", ExecutionEngineConditions.ShowPlanTextStatement(false)),
                        false,
                        _executionTimeout));
            }

            String preBatches = scriptPreBatches.ToString().Trim();
            String postBatches = scriptPostBatches.ToString().Trim();

            if (scriptPreBatches.Length > 0)
            {
                _preConditionBatches.Add(new Batch(preBatches, false, _executionTimeout));
            }

            if (scriptPostBatches.Length > 0)
            {
                _postConditionBatches.Add(new Batch(postBatches, false, _executionTimeout));
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
                    ConfigureBatchEventHandlers(batch, _batchEventHandlers, true);
                    result = batch.Execute(_connection, ShowPlanType.None);
                }
                catch (SqlException)
                {
                    result = ScriptExecutionResult.Failure;
                }
                finally
                {
                    ConfigureBatchEventHandlers(batch, _batchEventHandlers, false);
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
            lock (_stateSyncLock)
            {
                isDiscarded = (_executionState == ExecutionState.Discarded);
            }

            if (isDiscarded)
            {
                Debug.WriteLine("ExecutionEngine.CheckForDiscardedConnection");

                ResetScript();

                CloseConnection(_connection);

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
            _result = ScriptExecutionResult.Halted;
        }

        /// <summary>
        /// Changed when parser changed the error action type
        /// </summary>
        /// <param name="ea"></param>
        private void OnErrorActionChanged(OnErrorAction ea)
        {
            _errorAction = ea;
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
                _isSqlCmdConnection = true;
                _connection = connection;

                CreatePrePostConditionBatches();
                _result = ExecutePrePostConditionBatches(_preConditionBatches);
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
                Logger.Write(LogLevel.Warning, "Exception Caught in ExecutionEngine.ConnectSqlCmdInternal(SqlConnectionStringBuilder): " + ex.ToString());
                throw;
            }

            return connection;
        }

        /// <summary>
        /// Disconnects a sqlcmd connection
        /// </summary>
        private void DisconnectSqlCmdInternal()
        {
            if (_isSqlCmdConnection)
            {
                RaiseBatchMessage(String.Format(CultureInfo.CurrentCulture, "Disconnection from server {0}", ReliableConnectionHelper.GetServerName(_connection)));
                CloseConnection(_connection);
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
                    Logger.Write(LogLevel.Warning, "Exception Caught in ExecutionEngine.CloseConnection(SqlConnection): " + ex.ToString());
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

            _executionTimeout = scriptExecutionArgs.TimeOut < 0 ? 0 : scriptExecutionArgs.TimeOut;
            _connection = scriptExecutionArgs.ReliableConnection;
            _conditions = new ExecutionEngineConditions(scriptExecutionArgs.Conditions);
            _script = scriptExecutionArgs.Script;
            _isSqlCmdConnection = false;
            _batchEventHandlers = scriptExecutionArgs.BatchEventHandlers;
            _startingLine = scriptExecutionArgs.StartingLine;
            _internalVariables = scriptExecutionArgs.Variables;

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

        #region Private fields
        private OnErrorAction _errorAction = OnErrorAction.Ignore;
        private int _numBatchExecutionTimes = 1;
        private IDbConnection _connection = null;
        private bool _isSqlCmdConnection;

        private Parser _commandParser = null;
        private int _executionTimeout;
        private int _startingLine;
        private ExecutionState _executionState = ExecutionState.Initial;
        private String _script;
        private ScriptExecutionResult _result = ScriptExecutionResult.Failure;
        private bool _isLocalParse;
        private ExecutionEngineConditions _conditions = null;
        private IList<Batch> _preConditionBatches = new List<Batch>();
        private IList<Batch> _postConditionBatches = new List<Batch>();
        private IBatchEventsHandler _batchEventHandlers = null;
        private Batch _currentBatch = new Batch();
        private ShowPlanType _expectedShowPlan;
        private int _currentBatchIndex = -1; 
        private int _scriptTrackingId = 1;
        private object _stateSyncLock = new object();
                
        /// <summary>
        /// The internal variables that can be used in SqlCommand substitution.
        /// These variables take precedence over environment variables.
        /// </summary>
        private Dictionary<String, String> _internalVariables = new Dictionary<String, String>(StringComparer.CurrentCultureIgnoreCase);
        #endregion
    }
}
