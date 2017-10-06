//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TSQLExecutionEngine
{
    /// <summary>
    ///This is a test class for Microsoft.Data.Tools.Schema.Common.ExecutionEngine.ExecutionEngine and is intended
    ///to contain all Microsoft.Data.Tools.Schema.Common.ExecutionEngine.ExecutionEngine Unit Tests
    ///</summary>
   
    public class ExecutionEngineTest : IDisposable
    {
        private SqlConnection connection;
        private List<int> expResultCounts = new List<int>();
        private List<string> expErrorMessage = new List<string>();

        #region Test Initialize And Cleanup

        public ExecutionEngineTest()
        {
            TestInitialize();
        }

        // Initialize the tests
        public void TestInitialize()
        {
            expResultCounts = new List<int>();
            expErrorMessage = new List<string>();
            connection = SetUpConnection("test");
        }

        // helper method to set up a Sql Connection to a database
        private SqlConnection SetUpConnection(string name)
        {
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, name);
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
            string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
            SqlConnection resultConnection = new SqlConnection(connectionString);
            resultConnection.Open();
            return resultConnection;
        }

        // Helper method to close a connection completely
        private void CloseConnection(SqlConnection conn)
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
            }
        }

        //
        //Use Dispose to close connection after each test has run
        //
        public void Dispose()
        {
            //Task.Run(() => SqlTestDb.DropDatabase(connection.Database));
            CloseConnection(connection);
            connection = null;
        }        

        #endregion

        #region Valid scripts
        /// <summary>
        ///A test for a simple SQL script
        ///</summary>
        [Fact]
        public void ExecutionEngineTest_SimpleTest()
        {
            string sqlStatement = "SELECT * FROM sysobjects";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor  executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
            Assert.Equal(1, executor.BatchFinshedEventCounter);
        }

        /// <summary>
        /// Test with a valid script using default execution condition
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_DefaultCondition_ValidScript()
        {
            string sqlStatement = "select * from sysobjects\nGo\n";

            //Use default execution condition
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, false);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        // <summary>
        // Test with multiple valid scripts in multiple batches
        // </summary>
        [Fact]
        public void ExecutionEngineTest_MultiValidScripts()
        {
            string sqlStatement = "select * from sys.databases\ngo\nselect name from sys.databases\ngo\nprint 'test'\ngo";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, false);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        /// <summary>
        /// Test with SQL comment
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_TestComment()
        {
            string sqlStatement = "/*test comments*/";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }
        #endregion

        #region Invalid Scripts
        /// <summary>
        /// Test with a invalid query using the default execution condition
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_DefaultCondition_InvalidScript()
        {
            string sqlStatement = "select ** from sysobjects";
            //Use default execution condition
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, false);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal( ScriptExecutionResult.Success | ScriptExecutionResult.Failure, executor.ExecutionResult);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
            Assert.Equal(0, executor.BatchFinshedEventCounter);
        }

        /// <summary>
        /// Test with an invalid query using a defined execution condition
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_InvalidScriptWithCondition()
        {
            string sqlStatement = "select * from authors";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(executor.ExecutionResult, ScriptExecutionResult.Success | ScriptExecutionResult.Failure);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        /// <summary>
        /// Test with multiple invalid scripts in multiple batches
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_MultipleInvalidScript()
        {
            string sqlStatement = "select ** from products \ngo\n insert into products values (1,'abc')\n go \n";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;
            
            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();
            
            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(executor.ExecutionResult, ScriptExecutionResult.Success | ScriptExecutionResult.Failure);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        /// <summary>
        /// Test with invalid scripts within a single batch
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_MultipleInvalidScript_SingleBatch()
        {
            string sqlStatement = "select ** from products \n insert into products values (1,'abc')\n go \n";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success | ScriptExecutionResult.Failure, executor.ExecutionResult);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        /// <summary>
        /// Test with mixed valid and invalid scripts
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_MixedValidandInvalidScript()
        {
            string sqlStatement = "SELECT * FROM Authors \n Go\n select * from sysobjects \n go\nif exists (select * from sysobjects where id = object_id('MyTab')) DROP TABLE MyTab2";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(executor.ExecutionResult, ScriptExecutionResult.Success | ScriptExecutionResult.Failure);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }

        [Fact]
        public void ExecutionEngineTest_DiscardConnection()
        {
            ExecutionEngine engine = new ExecutionEngine();
            Assert.True(ConnectionDiscardWrapper(engine));
            
        }
        #endregion

        #region Different execution conditions
        /// <summary>
        /// Test HaltOnError execution condition
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_HaltOnError()
        {
            string sqlStatement = "select * from authors\n go\n select * from sysbojects \n go \n";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = true;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Halted | ScriptExecutionResult.Failure, executor.ExecutionResult);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
            Assert.True(executor.ResultCountQueue.Count == 0);

        }

        /// <summary>
        /// HaltOnError with a single batch
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_HaltOnError_OneBatch()
        {
            string sqlStatement = "select * from authors\n go 30\n";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = true;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Halted | ScriptExecutionResult.Failure, executor.ExecutionResult);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
            Assert.True(executor.ResultCountQueue.Count == 0);
            Assert.Equal(0, executor.BatchFinshedEventCounter);

        }

        /// <summary>
        /// Test ParseOnly execution condition with valid scripts
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_ParseOnly_ValidScript()
        {
            string sqlStatement = "select * from sysobjects";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = true;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(executor.ResultCountQueue.Count == 0);
            Assert.Equal(0, executor.BatchFinshedEventCounter);
        }

        /// <summary>
        /// Test HaltOnError execution condition with invalid scripts
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_ParseOnly_InvalidScript()
        {
            string sqlStatement = "select ** from authors";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = true;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success | ScriptExecutionResult.Failure, executor.ExecutionResult);
            Assert.True(!executor.ParserExecutionError);
            Assert.True(executor.ResultCountQueue.Count == 0);
            Assert.True(CompareTwoStringLists(executor.ErrorMessageQueue, expErrorMessage));
        }

        /// <summary>
        /// Parse script only without transaction wrapper
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_ParseOnly_ValidScriptWithoutTransaction()
        {
            string sqlStatement = "select * from sysobjects";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = false;
            conditions.IsParseOnly = true;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(executor.ResultCountQueue.Count == 0);
            Assert.Equal(0, executor.BatchFinshedEventCounter);
        }

        /// <summary>
        /// Test with execution timeout value
        /// </summary>
        //TEST_DOESNOTWORK[TestMethod()]
        public void ExecutionEngineTest_TimeOut()
        {
            string sqlStatement = "select * from sysobjects\n go 10\n";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, -1);
            executor.Run();
            Assert.Equal(executor.ExecutionResult, ScriptExecutionResult.Success);
        }

        /// <summary>
        /// Test with invalid connection
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_InvalidConnection()
        {
            string sqlStatement = "select * from sysobjects\n go 100\n";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            connection.Close();
            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions);
            executor.Run();

            // Note: this used to also return Halted at some point in the distant past. 
            // However since that gets mapped to Failure anyhow, consider "Failure" as acceptable here
            Assert.True(executor.ExecutionResult.HasFlag(ScriptExecutionResult.Failure), "Expected failure when invalid connection is present" );
        }

        /// <summary>
        /// Test with multiple conditions true
        /// </summary>
        [Fact]
        public void TestExecutionEngineConditions()
        {
            string sqlStatement = "select * from sys.databases\ngo\nselect name from sys.databases\ngo\nprint 'test'\ngo";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsNoExec = true;
            conditions.IsStatisticsIO = true;
            conditions.IsStatisticsTime = true;
            conditions.IsEstimatedShowPlan = true;
            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, false);
            executor.Run();

            //Get the expected values
            List<string> batchScripts = executor.BatchScripts;
            ExecuteSqlBatch(batchScripts, connection);

            Assert.Equal(ScriptExecutionResult.Success, executor.ExecutionResult);
            Assert.True(CompareTwoIntLists(executor.ResultCountQueue, expResultCounts));
        }
        #endregion

        #region SQL Commands
        /// <summary>
        /// Test with SQL commands
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_SQLCmds()
        {
            string[] sqlStatements = { 
                "select $(INVALIDVAR) from sysobjects",
                ":help",
                "exit",
                "quit",
                "!! dir",
                "ed",
                "reset",
                ":list",
                ":listvar",
                ":serverlist",
                ":on error ignore",
                ":connect hypothermia -t 300 -U foo -P bar",
                ":out $(SystemDrive)\\test.txt",
                ":r $(SystemDrive)\\test.txt",
                ":error STDOUT",
                ":perftrace STDOUT",
                "exit (Select count(*) from sysobjects)"
            };
            
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();

            foreach (string stmt in sqlStatements)
            {
                TestExecutor executor = new TestExecutor(stmt, connection, conditions);
                executor.Run();
                Assert.True(executor.ResultCountQueue.Count == 0);
            }

        }
        #endregion

        #region Threading
        /// <summary>
        /// Test synchronous cancel
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_SyncCancel()
        {
            string sqlStatement = "waitfor delay '0:0:10'";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, true);
            executor.CancelTimeOut = 3000;
            executor.Run();
            
            Assert.NotNull(executor.ScriptExecuteThread);
            Assert.Equal(ScriptExecutionResult.Cancel, executor.ExecutionResult);
            Assert.True(executor.CancelEventFired);

        }

        /// <summary>
        /// Test asynchronous cancel
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_ASyncCancel()
        {
            //string sqlStatement = "--This is a test\nSELECT * FROM sysobjects as t\nGO 50\n use pubsplus \n select * from titles\n go" ;

            string sqlStatement = "waitfor delay '0:0:10'";
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, true);
            executor.SyncCancel = false;
            executor.Run();

            Assert.True(executor.CancelEventFired);
            Assert.NotNull(executor.ScriptExecuteThread);
            if (executor.ScriptExecuteThread != null)
                Assert.True(!executor.ScriptExecuteThread.IsAlive);
            Assert.Equal(ScriptExecutionResult.Cancel, executor.ExecutionResult);
        }

        /// <summary>
        /// Test sync cancel when the execution is done
        /// </summary>
        /// 
        /// Disabled test, has race condition where Sql statement will finish 
        /// before harness has an opportunity to cancel.
        //TEST_DOESNOTWORK[TestMethod()]
        public void ExecutionEngineTest_SyncCancelAfterExecutionDone()
        {
            string sqlStatement = "select 1" ;

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, true);
            executor.Run();

            Assert.True(!executor.CancelEventFired);
            Assert.NotNull(executor.ScriptExecuteThread);
            if (executor.ScriptExecuteThread != null)
                Assert.True(!executor.ScriptExecuteThread.IsAlive);
            Assert.Equal(ScriptExecutionResult.Success | ScriptExecutionResult.Cancel, executor.ExecutionResult);

        }

        /// <summary>
        /// Test async cancel when the execution is done
        /// </summary>
        [Fact]
        public void ExecutionEngineTest_ASyncCancelAfterExecutionDone()
        {
            string sqlStatement ="select 1";

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            TestExecutor executor = new TestExecutor(sqlStatement, connection, conditions, true);
            executor.SyncCancel = false;
            executor.Run();

            Assert.True(!executor.CancelEventFired);
            Assert.NotNull(executor.ScriptExecuteThread);
            if (executor.ScriptExecuteThread != null)
                Assert.True(!executor.ScriptExecuteThread.IsAlive);
            Assert.Equal(ScriptExecutionResult.Success | ScriptExecutionResult.Cancel, executor.ExecutionResult);
        }

        /// <summary>
        /// Test multiple threads of execution engine with cancel operation
        /// </summary>
        [Fact]
        public async Task ExecutionEngineTest_MultiThreading_WithCancel()
        {
            string[] sqlStatement = { "waitfor delay '0:0:10'",
                 "waitfor delay '0:0:10'",
                 "waitfor delay '0:0:10'"
            };

            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            conditions.IsTransactionWrapped = true;
            conditions.IsParseOnly = false;
            conditions.IsHaltOnError = false;

            SqlConnection connection2 = SetUpConnection("test4");
            SqlConnection connection3 = SetUpConnection("test5");

            TestExecutor executor1 = new TestExecutor(sqlStatement[0], connection, conditions, true);
            executor1.CancelTimeOut = 2000;
            TestExecutor executor2 = new TestExecutor(sqlStatement[1], connection2, conditions, true);
            executor1.CancelTimeOut = 2500;
            TestExecutor executor3 = new TestExecutor(sqlStatement[2], connection3, conditions, true);
            executor1.CancelTimeOut = 3000;

            Thread t1 = new Thread(new ThreadStart(executor1.Run));
            Thread t2 = new Thread(new ThreadStart(executor2.Run));
            Thread t3 = new Thread(new ThreadStart(executor3.Run));

            t1.Name = "Executor1";
            t1.Start();
            t2.Name = "Executor2";
            t2.Start();
            t3.Name = "Executor3";
            t3.Start();

            while ((t1.ThreadState != ThreadState.Stopped) &&
                (t2.ThreadState != ThreadState.Stopped) &&
                (t3.ThreadState != ThreadState.Stopped))
            {
                Thread.Sleep(1000);
            }

            Assert.True(!executor1.ScriptExecuteThread.IsAlive);
            Assert.True(!executor2.ScriptExecuteThread.IsAlive);
            Assert.True(!executor3.ScriptExecuteThread.IsAlive);

            Assert.True(executor1.CancelEventFired);
            Assert.True(executor2.CancelEventFired);
            Assert.True(executor3.CancelEventFired);

            CloseConnection(connection2);
            CloseConnection(connection3);
            await SqlTestDb.DropDatabase(connection2.Database);
            await SqlTestDb.DropDatabase(connection3.Database);
        }

        #endregion

        #region Get/Set Methods

        [Fact]
        public void TestShowStatements()
        {
            Assert.NotNull(ExecutionEngineConditions.ShowPlanXmlStatement(true));
            Assert.NotNull(ExecutionEngineConditions.ShowPlanAllStatement(true));
            Assert.NotNull(ExecutionEngineConditions.ShowPlanTextStatement(true));
            Assert.NotNull(ExecutionEngineConditions.StatisticsXmlStatement(true));
            Assert.NotNull(ExecutionEngineConditions.StatisticsProfileStatement(true));
            Assert.NotNull(ExecutionEngineConditions.ParseOnlyStatement(true));
            Assert.NotNull(ExecutionEngineConditions.NoExecStatement(true));
            Assert.NotNull(ExecutionEngineConditions.StatisticsIOStatement(true));
            Assert.NotNull(ExecutionEngineConditions.StatisticsTimeStatement(true));
            Assert.NotNull(ExecutionEngineConditions.ResetStatement);
        }

        [Fact]
        public void TestExecutionEngineConditionsSetMethods()
        {
            ExecutionEngineConditions conditions = new ExecutionEngineConditions();
            bool getValue = conditions.IsScriptExecutionTracked;
            conditions.IsScriptExecutionTracked = !getValue;
            Assert.Equal(conditions.IsScriptExecutionTracked, !getValue);

            getValue = conditions.IsEstimatedShowPlan;
            conditions.IsEstimatedShowPlan = !getValue;
            Assert.Equal(conditions.IsEstimatedShowPlan, !getValue);

            getValue = conditions.IsActualShowPlan;
            conditions.IsActualShowPlan = !getValue;
            Assert.Equal(conditions.IsActualShowPlan, !getValue);

            getValue = conditions.IsSuppressProviderMessageHeaders;
            conditions.IsSuppressProviderMessageHeaders = !getValue;
            Assert.Equal(conditions.IsSuppressProviderMessageHeaders, !getValue);

            getValue = conditions.IsNoExec;
            conditions.IsNoExec = !getValue;
            Assert.Equal(conditions.IsNoExec, !getValue);

            getValue = conditions.IsStatisticsIO;
            conditions.IsStatisticsIO = !getValue;
            Assert.Equal(conditions.IsStatisticsIO, !getValue);

            getValue = conditions.IsShowPlanText;
            conditions.IsShowPlanText = !getValue;
            Assert.Equal(conditions.IsShowPlanText, !getValue);

            getValue = conditions.IsStatisticsTime;
            conditions.IsStatisticsTime = !getValue;
            Assert.Equal(conditions.IsStatisticsTime, !getValue);

            getValue = conditions.IsSqlCmd;
            conditions.IsSqlCmd = !getValue;
            Assert.Equal(conditions.IsSqlCmd, !getValue);

            conditions.BatchSeparator = "GO";
            Assert.Equal(conditions.BatchSeparator, "GO");
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Connection to a database
        /// </summary>
        /// <param name="server">Server name</param>
        /// <param name="database">DB name</param>
        /// <returns></returns>
        private SqlConnection ConnectToDB(string server, string database)
        {
            return new SqlConnection(string.Format("Data Source={0};Initial Catalog={1};Integrated Security=True;", server, database));
        }

        /// <summary>
        /// Execution a script batch
        /// </summary>
        /// <param name="sqlBatch">A list of SQL queries</param>
        /// <param name="connection">SQL connection</param>
        private void ExecuteSqlBatch(List<string> sqlBatch, SqlConnection connection)
        {
            foreach (string script in sqlBatch)
            {
                ExecuteSqlCommand(script, connection);                
            }
        }

        /// <summary>
        /// Execution one sql command
        /// </summary>
        /// <param name="sqlCmdTxt">SQL query</param>
        /// <param name="connection">SQL connection</param>
        private void ExecuteSqlCommand(string sqlCmdTxt, SqlConnection connection)
        {
            SqlCommand cmd = new SqlCommand(sqlCmdTxt, connection);
            SqlTransaction transaction = connection.BeginTransaction();
            cmd.Transaction = transaction;
            
            try
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    int count = 0;
                    while (dr.Read())
                    {
                        count++;
                    }
                    if (count > 0)
                    {
                        expResultCounts.Add(count);
                    }
                }
                transaction.Commit();
            }
            catch (Exception e)
            {               
                Console.WriteLine("Executing command throws exception: " + e.Message);
                expErrorMessage.Add(e.Message);
                try
                {
                    transaction.Rollback();
                }
                catch (Exception e2)
                {
                    Console.WriteLine("Rollback throws exception");
                    Console.WriteLine("Message: " + e2.Message);
                }
            }
        }

        /// <summary>
        /// Compare two string lists
        /// </summary>
        /// <param name="l1">first list</param>
        /// <param name="l2">second list</param>
        /// <returns>True if the contents are same, otherwise false</returns>
        private bool CompareTwoStringLists(List<string> l1, List<string> l2)
        {
            bool isSame = true;
            if(l1.Count != l2.Count)
            {
                isSame = false;
                Console.WriteLine("The count of elements in two lists are not the same");
                return isSame;
            }

            for (int i = 0; i < l1.Count; i++)
            {
                if (l1[i] != l2[i])
                {
                    isSame = false;
                    Console.WriteLine("l1: {0}, l2: {1}", l1[i], l2[i]);
                    break;
                }
            }

            return isSame;
        }

        /// <summary>
        /// Compare with integer list
        /// </summary>
        /// <param name="l1">first list</param>
        /// <param name="l2">second list</param>
        /// <returns>True if the two list's contents are same, otherwise false</returns>
        private bool CompareTwoIntLists(List<int> l1, List<int> l2)
        {
            bool isSame = true;
            if (l1.Count != l2.Count)
            {
                isSame = false;
                Console.WriteLine("The count of elements in two lists are not the same");
                return isSame;
            }

            for (int i = 0; i < l1.Count; i++)
            {
                if (l1[i] != l2[i])
                {
                    isSame = false;
                    Console.WriteLine("l1: {0}, l2: {1}", l1[i], l2[i]);
                    break;
                }
            }

            return isSame;
        }

        /// <summary>
        /// Wrapper to test the Close method in ExecutionEngine
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        private bool ConnectionDiscardWrapper(ExecutionEngine engine)
        {
            if (engine == null)
            {
                return false;
            }
            engine.Close(false, true, true);
            return true;
        }
        #endregion
    }
}
