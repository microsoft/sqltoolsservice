//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Microsoft.SqlTools.Utility;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.TSQLExecutionEngine
{
    internal class TestExecutor : IDisposable
    {
        #region Private variables

        private string sqlStatement;
        private readonly ExecutionEngineConditions conditions = new ExecutionEngineConditions();
        private readonly BatchEventHandler eventHandler = new BatchEventHandler();
        private readonly SqlConnection connection;
        private static Thread _executionThread;
        private bool _syncCancel = true;
        private bool _isFinished = false;
        private bool _cancel = false;
        private int _cancelTimeout = 500;
        private int exeTimeOut = 0;

        //For verification
        private List<int> resultCounts = new List<int>();

        private List<string> sqlMessages = new List<string>();
        private readonly List<string> errorMessage = new List<string>();
        private List<bool> batchFinished = new List<bool>();
        private static ScriptExecutionResult execResult = ScriptExecutionResult.All;
        private static List<string> batchScripts = new List<string>();
        private static Thread exeThread = null;
        private bool parserExecutionError = false;

        #endregion Private variables

        #region private methods

        /// <summary>
        /// Execute the script
        /// </summary>
        /// <param name="exec">Execution Engine</param>
        /// <param name="connection">SQL connection</param>
        /// <param name="script">script text</param>
        /// <param name="conditions">Execution condition</param>
        /// <param name="batchHandler">Batch event handler</param>
        /// <param name="timeout">time out value</param>
        private static void ExecuteScript(ExecutionEngine exec, SqlConnection connection, string script, ExecutionEngineConditions conditions, IBatchEventsHandler batchHandler, int timeout)
        {
            Validate.IsNotNull(nameof(exec), exec);
            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNullOrEmptyString(nameof(script), script);
            Validate.IsNotNull(nameof(conditions), conditions);

            Console.WriteLine("------------------------ Executing Script ----------------------");

            //exec.BeginScriptExecution(script, connection, timeout, conditions, batchConsumer);
            ScriptExecutionArgs args = new ScriptExecutionArgs(script, connection, timeout, conditions, batchHandler);
            //exec.ExecuteScript(args);

            _executionThread = new Thread(new ParameterizedThreadStart(exec.ExecuteScript));
            _executionThread.Start(args);
        }

        /// <summary>
        /// Cancel the execution
        /// </summary>
        /// <param name="exec">Execution Engine</param>
        /// <param name="isSynchronous">Cancel the execution synchronously or not</param>
        /// <param name="timeout">sycn canceo timeout</param>
        private static void Cancel(ExecutionEngine exec, bool isSynchronous, int millisecondsTimeOut)
        {
            //exec.BeginCancellingExecution(isSynchronous, timeout);

            if (_executionThread == null ||
                _executionThread.ThreadState == System.Threading.ThreadState.Unstarted ||
                _executionThread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                exec.Close(isSynchronous, /* isDiscard */ false, /* isFinishExecution */ true);
            }
            else
            {
                // activates the cancel thread
                Thread cancelThread = new Thread(new ThreadStart(exec.CancelCurrentBatch));
                cancelThread.Name = "Cancelling thread";
                cancelThread.Start();

                // in a syncrhonous call, we need to block and wait until the thread is stopped
                if (isSynchronous)
                {
                    int totalSleep = 0;
                    while (totalSleep < millisecondsTimeOut && _executionThread != null && _executionThread.IsAlive)
                    {
                        Thread.Sleep(50);
                        totalSleep += 50;
                    }

                    if (_executionThread != null && _executionThread.IsAlive)
                    {
                        exec.Close(isSynchronous, /* isDiscard */ true);
                    }
                    else
                    {
                        exec.Close(/* isCloseConnection */ true);
                    }
                }
            }
            Thread.Sleep(5000);
        }

        #endregion private methods

        #region Public properties

        public bool SyncCancel
        {
            get
            {
                return _syncCancel;
            }
            set
            {
                _syncCancel = value;
            }
        }

        public int CancelTimeOut
        {
            get
            {
                return _cancelTimeout;
            }
            set
            {
                _cancelTimeout = value;
            }
        }

        public ScriptExecutionResult ExecutionResult
        {
            get
            {
                return execResult;
            }
        }

        public List<int> ResultCountQueue
        {
            get
            {
                return resultCounts;
            }
        }

        public List<string> SQLMessageQueue
        {
            get
            {
                return sqlMessages;
            }
        }

        public List<String> ErrorMessageQueue
        {
            get
            {
                return errorMessage;
            }
        }

        public List<string> BatchScripts
        {
            get
            {
                return batchScripts;
            }
        }

        public int BatchFinshedEventCounter
        {
            get
            {
                return eventHandler.BatchfinishedEventCounter;
            }
        }

        public Thread ScriptExecuteThread
        {
            get
            {
                return exeThread;
            }
        }

        public bool CancelEventFired
        {
            get
            {
                return eventHandler.CancelFired;
            }
        }

        public bool ParserExecutionError
        {
            get
            {
                return parserExecutionError;
            }
        }

        #endregion Public properties

        #region Constructors

        public TestExecutor(string batch, SqlConnection conn, ExecutionEngineConditions exeCondition) : this(batch, conn, exeCondition, false)
        {
        }

        public TestExecutor(string batch, SqlConnection conn, ExecutionEngineConditions exeCondition, bool cancelExecution)
        {
            sqlStatement = batch;
            conditions.IsHaltOnError = exeCondition.IsHaltOnError;
            conditions.IsParseOnly = exeCondition.IsParseOnly;
            conditions.IsTransactionWrapped = exeCondition.IsTransactionWrapped;
            conditions.IsNoExec = exeCondition.IsNoExec;
            conditions.IsStatisticsIO = exeCondition.IsStatisticsIO;
            conditions.IsStatisticsTime = exeCondition.IsStatisticsTime;
            conditions.IsSqlCmd = exeCondition.IsSqlCmd;

            _cancel = cancelExecution;
            connection = conn;

            //Initialize the static variables
            execResult = ScriptExecutionResult.All;
            batchScripts = new List<string>();
            exeThread = null;
            parserExecutionError = false;
        }

        public TestExecutor(string batch, SqlConnection conn, ExecutionEngineConditions exeCondition, int timeOut)
            : this(batch, conn, exeCondition, false)
        {
            exeTimeOut = timeOut;
        }

        #endregion Constructors

        #region public methods

        /// <summary>
        /// Execute the test engine
        /// </summary>
        public void Run()
        {
            Console.WriteLine("Executing scripts {0} ...", sqlStatement);

            using (ExecutionEngine exec = new ExecutionEngine())
            {
                _isFinished = false;
                exec.BatchParserExecutionStart += new EventHandler<BatchParserExecutionStartEventArgs>(OnBatchParserExecutionStart);
                exec.BatchParserExecutionFinished += new EventHandler<BatchParserExecutionFinishedEventArgs>(OnBatchParserExecutionFinished);
                exec.BatchParserExecutionError += new EventHandler<BatchParserExecutionErrorEventArgs>(OnBatchParserExecutionError);
                exec.ScriptExecutionFinished += new EventHandler<ScriptExecutionFinishedEventArgs>(OnExecutionFinished);

                ExecuteScript(exec, connection, sqlStatement, conditions, eventHandler, exeTimeOut);

                if (!_cancel)
                {
                    //Do not cancel the execution engine
                    while (!_isFinished)
                    {
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    if (!_isFinished)
                    {
                        Console.WriteLine("Need to cancel while the batch execution is not finished!");
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Console.WriteLine("Canceling after the exe engine is disposed...");
                    }
                    Cancel(exec, _syncCancel, _cancelTimeout);
                }
            }
        }

        #endregion public methods

        #region ParserEvent

        /// <summary>
        /// Called when batch is called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnBatchParserExecutionStart(object sender, BatchParserExecutionStartEventArgs e)
        {
            Console.WriteLine("****************");
            Console.WriteLine(e.Batch.Text);
            batchScripts.Add(e.Batch.Text);
            Console.WriteLine("****************");

            Console.WriteLine("ON_BATCH_PARSER_EXECUTION_START : Start executing batch... " + e.Batch + " at line " + e.TextSpan.iStartLine);
            exeThread = Thread.CurrentThread;
        }

        /// <summary>
        /// Called when batch is done
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnBatchParserExecutionFinished(object sender, BatchParserExecutionFinishedEventArgs e)
        {
            Console.WriteLine("ON_BATCH_PARSER_EXECUTION_FINISHED : Done executing batch \n\t{0}\n\t with result... {1} ", e.Batch.Text, e.ExecutionResult);
            if (execResult == ScriptExecutionResult.All)
            {
                execResult = e.ExecutionResult;
            }
            else
            {
                execResult |= e.ExecutionResult;
            }
        }

        /// <summary>
        /// Called when batch pasing found a warning/error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchParserExecutionError(object sender, BatchParserExecutionErrorEventArgs e)
        {
            Console.WriteLine("ON_BATCH_PARSER_EXECUTION_ERROR : {0} found... at line {1}: {2}", e.MessageType.ToString(), e.Line.ToString(), e.Message);
            Console.WriteLine("\t Error Description: " + e.Description);
            parserExecutionError = true;
            errorMessage.Add(e.Description);            
        }

        /// <summary>
        /// Called when script is done
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExecutionFinished(object sender, ScriptExecutionFinishedEventArgs e)
        {
            Console.WriteLine("ON_EXECUTION_FINISHED : Script execution done with result ..." + e.ExecutionResult);
            _isFinished = true;

            if (execResult == ScriptExecutionResult.All)
            {
                execResult = e.ExecutionResult;
            }
            else
            {
                execResult |= e.ExecutionResult;
            }

            resultCounts = eventHandler.ResultCounts;
            sqlMessages = eventHandler.SqlMessages;
            errorMessage.AddRange(eventHandler.ErrorMessages);
        }

        #endregion ParserEvent

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion IDisposable Members
    }
}