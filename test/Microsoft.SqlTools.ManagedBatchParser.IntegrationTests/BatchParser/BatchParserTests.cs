//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.TSQLExecutionEngine;
using Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    public class BatchParserTests : BaselinedTest
    {
        public BatchParserTests()
        {
            InitializeTest();
        }

        public void InitializeTest()
        {
            CategoryName = "BatchParser";
            this.TraceOutputDirectory = RunEnvironmentInfo.GetTraceOutputLocation();
            TestInitialize();
        }

        [Fact]
        public void VerifyThrowOnUnresolvedVariable()
        {
            string script = "print '$(NotDefined)'";
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(script),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);

                Assert.Throws<BatchParserException>(() => p.Parse());
            }
        }

        /// <summary>
        /// Variable parameter in powershell: Specifies, as a string array, a sqlcmd scripting variable
        /// for use in the sqlcmd script, and sets a value for the variable.
        /// </summary>
        [Fact]
        public void VerifyVariableResolverUsingVaribleParameter()
        {
            string query = @" Invoke-Sqlcmd -Query ""SELECT `$(calcOne)"" -Variable ""calcOne = 10 + 20"" ";

            TestCommandHandler handler = new TestCommandHandler(new StringBuilder());
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(query),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);
                Assert.Throws<BatchParserException>(() => p.Parse());
            }
        }

        // Verify the starting identifier of Both parameter and variable are same.
        [Fact]
        public void VerifyVariableResolverIsStartIdentifierChar()
        {
            // instead of using variable calcOne, I purposely used In-variable 0alcOne
            string query = @" Invoke-Sqlcmd -Query ""SELECT `$(0alcOne)"" -Variable ""calcOne1 = 1"" ";

            TestCommandHandler handler = new TestCommandHandler(new StringBuilder());
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(query),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);
                Assert.Throws<BatchParserException>(() => p.Parse());
            }
        }

        // Verify all the characters inside variable are valid Identifier.
        [Fact]
        public void VerifyVariableResolverIsIdentifierChar()
        {
            // instead of using variable calcOne, I purposely used In-variable 0alcOne
            string query = @" Invoke-Sqlcmd -Query ""SELECT `$(ca@lcOne)"" -Variable ""calcOne = 1"" ";

            TestCommandHandler handler = new TestCommandHandler(new StringBuilder());
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(query),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);
                Assert.Throws<BatchParserException>(() => p.Parse());
            }
        }

        // Verify the execution by passing long value , Except a exception.
        [Fact]
        public void VerifyInvalidNumber()
        {
            string query = @" SELECT 1+1
                           GO 999999999999999999999999999999999999999";

            TestCommandHandler handler = new TestCommandHandler(new StringBuilder());
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(query),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);
                // This test will fail because we are passing invalid number.
                // Exception will be raised from  ParseGo()
                Assert.Throws<BatchParserException>(() => p.Parse());
            }
        }

        // Verify the Batch execution is executed successfully.
        [Fact]
        public void VerifyExecute()
        {
            Batch batch = new Batch(sqlText: "SELECT 1+1", isResultExpected: true, execTimeout: 15);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
            {
                var executionResult = batch.Execute(sqlConn, ShowPlanType.AllShowPlan);
                Assert.Equal<ScriptExecutionResult>(ScriptExecutionResult.Success, executionResult);
            }

        }

        // Verify the exeception is handled by passing invalid keyword.
        [Fact]
        public void VerifyHandleExceptionMessage()
        {
            Batch batch = new Batch(sqlText: "SEL@ECT 1+1", isResultExpected: true, execTimeout: 15);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
            {
                ScriptExecutionResult result = batch.Execute(sqlConn, ShowPlanType.AllShowPlan);
            }
            ScriptExecutionResult finalResult = (batch.RowsAffected > 0) ? ScriptExecutionResult.Success : ScriptExecutionResult.Failure;

            Assert.Equal<ScriptExecutionResult>(finalResult, ScriptExecutionResult.Failure);
        }

        // Verify the passing query has valid text.
        [Fact]
        public void VerifyHasValidText()
        {
            Batch batch = new Batch(sqlText: null, isResultExpected: true, execTimeout: 15);
            ScriptExecutionResult finalResult = ScriptExecutionResult.All;
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
            {
                ScriptExecutionResult result = batch.Execute(sqlConn, ShowPlanType.AllShowPlan);
            }
            finalResult = (batch.RowsAffected > 0) ? ScriptExecutionResult.Success : ScriptExecutionResult.Failure;

            Assert.Equal<ScriptExecutionResult>(finalResult, ScriptExecutionResult.Failure);
        }

        // Verify the cancel functionality is working fine.
        [Fact]
        public void VerifyCancel()
        {
            ScriptExecutionResult result = ScriptExecutionResult.All;
            Batch batch = new Batch(sqlText: "SELECT 1+1", isResultExpected: true, execTimeout: 15);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
            {
                batch.Cancel();
                result = batch.Execute(sqlConn, ShowPlanType.AllShowPlan);
            }
            Assert.Equal<ScriptExecutionResult>(result, ScriptExecutionResult.Cancel);
        }

        // 
        /// <summary>
        /// Verify whether lexer can consume token for SqlCmd variable
        /// </summary>
        [Fact]
        public void VerifyLexerSetState()
        {
            try
            {
                string query = ":SETVAR    a 10";
                var inputStream = GenerateStreamFromString(query);
                using (Lexer lexer = new Lexer(new StreamReader(inputStream), "Test.sql"))
                {
                    lexer.ConsumeToken();
                }
            }
            catch (Exception e)
            {
                Assert.True(false, $"Unexpected error consuming token : {e.Message}");
            }
            //  we doesn't expect any exception or testCase failures

        }

        // This test case is to verify that, Powershell's Invoke-SqlCmd handles ":!!if" in an inconsistent way
        // Inconsistent way means, instead of throwing an exception as "Command Execute is not supported." it was throwing "Incorrect syntax near ':'."
        [Fact]
        public void VerifySqlCmdExecute()
        {
            string query = ":!!if exist foo.txt del foo.txt";
            var inputStream = GenerateStreamFromString(query);
            TestCommandHandler handler = new TestCommandHandler(new StringBuilder());
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            using (Parser p = new Parser(
                handler,
                resolver,
                new StringReader(query),
                "test"))
            {
                p.ThrowOnUnresolvedVariable = true;
                handler.SetParser(p);

                var exception = Assert.Throws<BatchParserException>(() => p.Parse());
                // Verify the message should be "Command Execute is not supported."
                Assert.Equal("Command Execute is not supported.", exception.Message);
            }
        }

        // This test case is to verify that, Lexer type for :!!If was set to "Text" instead of "Execute"
        [Fact]
        public void VerifyLexerTypeOfSqlCmdIFisExecute()
        {
            string query = ":!!if exist foo.txt del foo.txt";
            var inputStream = GenerateStreamFromString(query);
            LexerTokenType type = LexerTokenType.None;
            using (Lexer lexer = new Lexer(new StreamReader(inputStream), "Test.sql"))
            {
                lexer.ConsumeToken();
                type = lexer.CurrentTokenType;
            }
            // we are expecting the lexer type should to be Execute.
            Assert.Equal("Execute", type.ToString());
        }

        // Verify the custom exception functionality by raising user defined error.
        [Fact]
        public void VerifyCustomBatchParserException()
        {
            string message = "This is userDefined Error";

            Token token = new Token(LexerTokenType.Text, new PositionStruct(), new PositionStruct(), message, "test");

            BatchParserException batchParserException = new BatchParserException(ErrorCode.VariableNotDefined, token, message);
            try
            {
                throw new BatchParserException(ErrorCode.UnrecognizedToken, token, "test");
            }
            catch (Exception ex)
            {
                Assert.Equal(batchParserException.ErrorCode.ToString(), ErrorCode.VariableNotDefined.ToString());
                Assert.Equal(message, batchParserException.Text);
                Assert.Equal(LexerTokenType.Text.ToString(), batchParserException.TokenType.ToString());
                Assert.IsType<BatchParserException>(ex);
            }
        }

        // Verify whether the executionEngine execute script
        [Fact]
        public void VerifyExecuteScript()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                string query = @"SELECT 1+2
                                Go 2";
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    TestExecutor testExecutor = new TestExecutor(query, sqlConn, new ExecutionEngineConditions());
                    testExecutor.Run();

                    ScriptExecutionResult result = (testExecutor.ExecutionResult == ScriptExecutionResult.Success) ? ScriptExecutionResult.Success : ScriptExecutionResult.Failure;

                    Assert.Equal<ScriptExecutionResult>(ScriptExecutionResult.Success, result);
                }
            }
        }

        // Verify whether the batchParser execute SqlCmd.
        //[Fact]   //  This Testcase should execute and pass, But it is failing now.
        public void VerifyIsSqlCmd()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                string query = @"sqlcmd -Q ""select 1 + 2 as col"" ";
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    TestExecutor testExecutor = new TestExecutor(query, sqlConn, new ExecutionEngineConditions());
                    testExecutor.Run();
                    Assert.True(testExecutor.ResultCountQueue.Count >= 1);
                }
            }
        }

        /// <summary>
        /// Verify whether the batchParser execute SqlCmd successfully
        /// </summary>
        [Fact]
        public void VerifyRunSqlCmd()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                const string sqlCmdQuery = @"
:setvar __var1 1
:setvar __var2 2
:setvar __IsSqlCmdEnabled " + "\"True\"" + @"
GO
IF N'$(__IsSqlCmdEnabled)' NOT LIKE N'True'
    BEGIN
        PRINT N'SQLCMD mode must be enabled to successfully execute this script.';
                SET NOEXEC ON;
                END
GO
select $(__var1) + $(__var2) as col
GO";

                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                var condition = new ExecutionEngineConditions() { IsSqlCmd = true };
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                using (TestExecutor testExecutor = new TestExecutor(sqlCmdQuery, sqlConn, condition))
                {
                    testExecutor.Run();

                    Assert.True(testExecutor.ResultCountQueue.Count >= 1, $"Unexpected number of ResultCount items - expected 0 but got {testExecutor.ResultCountQueue.Count}");
                    Assert.True(testExecutor.ErrorMessageQueue.Count == 0, $"Unexpected error messages from test executor : {string.Join(Environment.NewLine, testExecutor.ErrorMessageQueue)}");
                }
            }
        }

        /// <summary>
        /// Verify whether the batchParser parsed connect command successfully
        /// </summary>
        [Fact]
        public void VerifyConnectSqlCmd()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                string serverName = liveConnection.ConnectionInfo.ConnectionDetails.ServerName;
                string userName = liveConnection.ConnectionInfo.ConnectionDetails.UserName;
                string password = liveConnection.ConnectionInfo.ConnectionDetails.Password;
                string sqlCmdQuery = $@"
:Connect {serverName} -U {userName} -P {password}
GO
select * from sys.databases where name = 'master'
GO";

                string sqlCmdQueryIncorrect = $@"
:Connect {serverName} -u {userName} -p {password}
GO
select * from sys.databases where name = 'master'
GO";
                var condition = new ExecutionEngineConditions() { IsSqlCmd = true };
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                using (TestExecutor testExecutor = new TestExecutor(sqlCmdQuery, sqlConn, condition))
                {
                    testExecutor.Run();
                    Assert.True(testExecutor.ParserExecutionError == false, "Parse Execution error should be false");
                    Assert.True(testExecutor.ResultCountQueue.Count == 1, $"Unexpected number of ResultCount items - expected 1 but got {testExecutor.ResultCountQueue.Count}");
                    Assert.True(testExecutor.ErrorMessageQueue.Count == 0, $"Unexpected error messages from test executor : {string.Join(Environment.NewLine, testExecutor.ErrorMessageQueue)}");
                }

                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                using (TestExecutor testExecutor = new TestExecutor(sqlCmdQueryIncorrect, sqlConn, condition))
                {
                    testExecutor.Run();

                    Assert.True(testExecutor.ParserExecutionError == true, "Parse Execution error should be true");
                }
            }
        }
        
        /// <summary>
        /// Verify whether the batchParser parsed on error successfully
        /// </summary>
        [Fact]
        public void VerifyOnErrorSqlCmd()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                string serverName = liveConnection.ConnectionInfo.ConnectionDetails.ServerName;
                string sqlCmdQuery = $@"
:on error ignore
GO
select * from sys.databases_wrong where name = 'master'
GO
select* from sys.databases where name = 'master'
GO
:on error exit
GO
select* from sys.databases_wrong where name = 'master'
GO
select* from sys.databases where name = 'master'
GO";
                var condition = new ExecutionEngineConditions() { IsSqlCmd = true };
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                using (TestExecutor testExecutor = new TestExecutor(sqlCmdQuery, sqlConn, condition))
                {
                    testExecutor.Run();

                    Assert.True(testExecutor.ResultCountQueue.Count == 1, $"Unexpected number of ResultCount items - expected only 1 since the later should not be executed but got {testExecutor.ResultCountQueue.Count}");
                    Assert.True(testExecutor.ErrorMessageQueue.Count == 2, $"Unexpected number error messages from test executor expected 2, actual : {string.Join(Environment.NewLine, testExecutor.ErrorMessageQueue)}");
                }
            }
        }

        /// <summary>
        /// Verify whether the batchParser parses Include command successfully
        /// </summary>
        [Fact]
        public void VerifyIncludeSqlCmd()
        {
            string file = "VerifyIncludeSqlCmd_test.sql";
            try
            {
                using (ExecutionEngine executionEngine = new ExecutionEngine())
                {
                    var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                    string serverName = liveConnection.ConnectionInfo.ConnectionDetails.ServerName;
                    string sqlCmdFile = $@"
select * from sys.databases where name = 'master'
GO";
                    File.WriteAllText("VerifyIncludeSqlCmd_test.sql", sqlCmdFile);
                     
                    string sqlCmdQuery = $@"
:r {file}
GO
select * from sys.databases where name = 'master'
GO";

                    var condition = new ExecutionEngineConditions() { IsSqlCmd = true };
                    using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                    using (TestExecutor testExecutor = new TestExecutor(sqlCmdQuery, sqlConn, condition))
                    {
                        testExecutor.Run();

                        Assert.True(testExecutor.ResultCountQueue.Count == 2, $"Unexpected number of ResultCount items - expected 1 but got {testExecutor.ResultCountQueue.Count}");
                        Assert.True(testExecutor.ErrorMessageQueue.Count == 0, $"Unexpected error messages from test executor : {string.Join(Environment.NewLine, testExecutor.ErrorMessageQueue)}");
                    }
                    File.Delete(file);
                }
            }
            catch
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        // Verify whether the executionEngine execute Batch
        [Fact]
        public void VerifyExecuteBatch()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                string query = "SELECT 1+2";
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    var executionPromise = new TaskCompletionSource<bool>();
                    executionEngine.BatchParserExecutionFinished += (object sender, BatchParserExecutionFinishedEventArgs e) =>
                    {
                        Assert.Equal(ScriptExecutionResult.Success, e.ExecutionResult);
                        executionPromise.SetResult(true);
                    };
                    executionEngine.ExecuteBatch(new ScriptExecutionArgs(query, sqlConn, 15, new ExecutionEngineConditions(), new BatchParserMockEventHandler()));
                    Task.WaitAny(executionPromise.Task, Task.Delay(5000));
                    Assert.True(executionPromise.Task.IsCompleted, "Execution did not finish in time");
                }
            }
        }

        [Fact]
        public void CanceltheBatch()
        {
            Batch batch = new Batch();
            batch.Cancel();
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void TokenizeWithLexer(string filename, StringBuilder output)
        {
            // Create a new file by changing CRLFs to LFs and generate a new steam
            // or the tokens generated by the lexer will always have off by one errors
            string input = File.ReadAllText(filename).Replace("\r\n", "\n");
            var inputStream = GenerateStreamFromString(input);
            using (Lexer lexer = new Lexer(new StreamReader(inputStream), filename))
            {
                string inputText = File.ReadAllText(filename);
                inputText = inputText.Replace("\r\n", "\n");
                StringBuilder roundtripTextBuilder = new StringBuilder();
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder tokenizedInput = new StringBuilder();
                bool lexerError = false;

                Token token = null;
                try
                {
                    do
                    {
                        lexer.ConsumeToken();
                        token = lexer.CurrentToken;
                        roundtripTextBuilder.Append(token.Text.Replace("\r\n", "\n"));
                        outputBuilder.AppendLine(GetTokenString(token));
                        tokenizedInput.Append('[').Append(GetTokenCode(token.TokenType)).Append(':').Append(token.Text.Replace("\r\n", "\n")).Append(']');
                    } while (token.TokenType != LexerTokenType.Eof);
                }
                catch (BatchParserException ex)
                {
                    lexerError = true;
                    outputBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, "[ERROR: code {0} at {1} - {2} in {3}, message: {4}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Message));
                }
                output.AppendLine("Lexer tokenized input:");
                output.AppendLine("======================");
                output.AppendLine(tokenizedInput.ToString());
                output.AppendLine("Tokens:");
                output.AppendLine("=======");
                output.AppendLine(outputBuilder.ToString());

                if (lexerError == false)
                {
                    // Verify that all text from tokens can be recombined into original string
                    Assert.Equal<string>(inputText, roundtripTextBuilder.ToString());
                }
            }
        }

        private string GetTokenCode(LexerTokenType lexerTokenType)
        {
            switch (lexerTokenType)
            {
                case LexerTokenType.Text:
                    return "T";

                case LexerTokenType.Whitespace:
                    return "WS";

                case LexerTokenType.NewLine:
                    return "NL";

                case LexerTokenType.Comment:
                    return "C";

                default:
                    return lexerTokenType.ToString();
            }
        }

        private static void CopyToOutput(string sourceDirectory, string filename)
        {
            File.Copy(Path.Combine(sourceDirectory, filename), filename, true);
            FileUtilities.SetFileReadWrite(filename);
        }

        public void TestParser(string filename, StringBuilder output)
        {
            try
            {
                // Create a new file by changing CRLFs to LFs and generate a new steam
                // or the tokens generated by the lexer will always have off by one errors
                TestCommandHandler commandHandler = new TestCommandHandler(output);
                string input = File.ReadAllText(filename).Replace("\r\n", "\n");
                var inputStream = GenerateStreamFromString(input);
                StreamReader streamReader = new StreamReader(inputStream);

                using (Parser parser = new Parser(
                    commandHandler,
                    new TestVariableResolver(output),
                    streamReader,
                    filename))
                {
                    commandHandler.SetParser(parser);
                    parser.Parse();
                }
            }
            catch (BatchParserException ex)
            {
                output.AppendLine(string.Format(CultureInfo.CurrentCulture, "[PARSER ERROR: code {0} at {1} - {2} in {3}, token text: {4}, message: {5}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Text, ex.Message));
            }
        }

        private string GetPositionString(PositionStruct pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1} [{2}]", pos.Line, pos.Column, pos.Offset);
        }

        private string GetTokenString(Token token)
        {
            if (token == null)
            {
                return "(null)";
            }
            else
            {
                string tokenText = token.Text;
                if (tokenText != null)
                {
                    tokenText = tokenText.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                }
                string tokenFilename = token.Filename;
                tokenFilename = GetFilenameOnly(tokenFilename);
                return string.Format(CultureInfo.CurrentCulture, "[Token {0} at {1}({2}:{3} [{4}] - {5}:{6} [{7}]): '{8}']",
                    token.TokenType,
                    tokenFilename,
                    token.Begin.Line, token.Begin.Column, token.Begin.Offset,
                    token.End.Line, token.End.Column, token.End.Offset,
                    tokenText);
            }
        }

        internal static string GetFilenameOnly(string fullPath)
        {
            return fullPath != null ? Path.GetFileName(fullPath) : null;
        }

        public override void Run()
        {
            string inputFilename = GetTestscriptFilePath(CurrentTestName);
            StringBuilder output = new StringBuilder();

            TokenizeWithLexer(inputFilename, output);
            TestParser(inputFilename, output);

            string baselineFilename = GetBaselineFilePath(CurrentTestName);
            string baseline;

            try
            {
                baseline = GetFileContent(baselineFilename).Replace("\r\n", "\n");
            }
            catch (FileNotFoundException)
            {
                baseline = string.Empty;
            }

            string outputString = output.ToString().Replace("\r\n", "\n");

            //Console.WriteLine(baselineFilename);

            if (string.Compare(baseline, outputString, StringComparison.Ordinal) != 0)
            {
                DumpToTrace(CurrentTestName, outputString);
                string outputFilename = Path.Combine(TraceFilePath, GetBaselineFileName(CurrentTestName));
                Console.WriteLine(":: Output does not match the baseline!");
                Console.WriteLine("code --diff \"" + baselineFilename + "\" \"" + outputFilename + "\"");
                Console.WriteLine();
                Console.WriteLine(":: To update the baseline:");
                Console.WriteLine("copy \"" + outputFilename + "\" \"" + baselineFilename + "\"");
                Console.WriteLine();
            }
        }
    }
}