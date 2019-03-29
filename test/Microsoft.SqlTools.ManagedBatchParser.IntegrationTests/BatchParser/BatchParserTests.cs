//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.TSQLExecutionEngine;
using Microsoft.SqlTools.ManagedBatchParser.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined;
using Xunit;

namespace Microsoft.SqlTools.ManagedBatchParser.UnitTests.BatchParser
{
    public class BatchParserTests : BaselinedTest
    {
        private bool testFailed = false;

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
            string query = @" Invoke-Sqlcmd -Query ""select `$(calcOne)"" -Variable ""calcOne = 10 + 20"" ";

            //String query = "Select 1+2";

            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
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
            string query = @" Invoke-Sqlcmd -Query ""select `$(0alcOne)"" -Variable ""calcOne1 = 1"" ";
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
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
            string query = @" Invoke-Sqlcmd -Query ""select `$(ca@lcOne)"" -Variable ""calcOne = 1"" ";
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
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

        // Verify the execution by passing long value with AbortOnError, Pipeline execution will stop and except a exception.  
        [Fact]
        public void VerifyInvalidNumberWithAbortOnError()
        {
            string query = @"Invoke-Sqlcmd -Query ""select 1+1
                           Go 999999999999999999999999999999999999999
                           "" -AbortOnError";  

            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
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
            string query = @"Invoke-Sqlcmd -Query ""select 1+1
            Go -2
            """;
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
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

        // Verify the Batch execution is executed successfully. 
        [Fact]
        public void verifyExecute()
        {
            Batch batch = new Batch(sqlText: "select 1+1", isResultExpected: true, execTimeout: 15);
            using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
            {
                con.Open();
                if (con.State.ToString().ToLower() == "open")
                {
                    ScriptExecutionResult result = batch.Execute(con, ShowPlanType.AllShowPlan);
                }
            }
            Assert.Equal<ScriptExecutionResult>(ScriptExecutionResult.Success, ScriptExecutionResult.Success);
        }

        // Verify the exeception is handled by passing invalid keyword. 
        [Fact]
        public void verifyHandleExceptionMessage()
        {
            Batch batch = new Batch(sqlText: "sel@ect 1+1", isResultExpected: true, execTimeout: 15);
            using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
            {
                con.Open();
                if (con.State.ToString().ToLower() == "open")
                {
                    ScriptExecutionResult result = batch.Execute(con, ShowPlanType.AllShowPlan);
                }
            }
            ScriptExecutionResult finalResult = (batch.RowsAffected > 0) ? ScriptExecutionResult.Success : ScriptExecutionResult.Failure;

            Assert.Equal<ScriptExecutionResult>(finalResult, ScriptExecutionResult.Failure);
        }
        // Verify the passing query has valid text. 
        [Fact]
        public void verifyHasValidText()
        {
            Batch batch = new Batch(sqlText: null, isResultExpected: true, execTimeout: 15);
            ScriptExecutionResult finalResult = ScriptExecutionResult.All;
            using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
            {
                con.Open();
                if (con.State.ToString().ToLower() == "open")
                {
                    ScriptExecutionResult result = batch.Execute(con, ShowPlanType.AllShowPlan);
                }
            }
            finalResult = (batch.RowsAffected > 0) ? ScriptExecutionResult.Success : ScriptExecutionResult.Failure;

            Assert.Equal<ScriptExecutionResult>(finalResult, ScriptExecutionResult.Failure);
        }
        // Verify the cancel functionality is working fine. 
        [Fact]
        public void verifyCancel()
        {
            ScriptExecutionResult result = ScriptExecutionResult.All;
            Batch batch = new Batch(sqlText: "select 1+1", isResultExpected: true, execTimeout: 15);
            using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
            {
                con.Open();
                if (con.State.ToString().ToLower() == "open")
                {
                    batch.Cancel();
                    result = batch.Execute(con, ShowPlanType.AllShowPlan);
                }
            }
            Assert.Equal<ScriptExecutionResult>(result, ScriptExecutionResult.Cancel);

        }

        //[Fact]
        public void VerifyLexerSetState()
        {
            string query = ":setvar    a 10";
            var inputStream = GenerateStreamFromString(query);
            using (Lexer lexer = new Lexer(new StreamReader(inputStream), "Test.sql"))
            {
                lexer.ConsumeToken();
            }
        } 
        // Verify the custom exception functionality by raising user defined error.
        [Fact]
        public void verifyCustomBatchParserException()
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
                string query = @"select 1+2;
                    Go 2";
                using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
                {
                    ExecutionEngineConditions exeCond = new ExecutionEngineConditions();
                    BatchParserMockEventHandler batchParserMockEventHandler = new BatchParserMockEventHandler();
                    ScriptExecutionArgs scriptExecutionArgs = new ScriptExecutionArgs(query, con, 15, exeCond, batchParserMockEventHandler);
                    executionEngine.ExecuteScript(scriptExecutionArgs);
                    Assert.IsType<Exception>(new Exception());
                }
            }
        }
        // Verify whether the batchParser execute SqlCmd. 
        [Fact]
        public void VerifyIsSqlCmd()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                string query = @":SetVar a 10;
                    Go ";
                using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
                {
                    ExecutionEngineConditions exeCond = new ExecutionEngineConditions();
                    BatchParserMockEventHandler batchParserMockEventHandler = new BatchParserMockEventHandler();
                    ScriptExecutionArgs scriptExecutionArgs = new ScriptExecutionArgs(query, con, 15, exeCond, batchParserMockEventHandler);
                    executionEngine.ExecuteScript(scriptExecutionArgs);
                    executionEngine.CancelCurrentBatch();
                    Assert.IsType<Exception>(new Exception());
                }
            }
        }
        // Verify whether the executionEngine execute Batch
        [Fact]
        public void VerifyExecuteBatch()
        {
            using (ExecutionEngine executionEngine = new ExecutionEngine())
            {
                string query = "select 1+2";
                using (SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=True"))
                {
                    ExecutionEngineConditions exeCond = new ExecutionEngineConditions();
                    BatchParserMockEventHandler batchParserMockEventHandler = new BatchParserMockEventHandler();
                    ScriptExecutionArgs scriptExecutionArgs = new ScriptExecutionArgs(query, con, 15, exeCond, batchParserMockEventHandler);
                    executionEngine.ExecuteBatch(scriptExecutionArgs);
                    Assert.IsType<Exception>(new Exception());
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

        // [Fact]
        public void BatchParserTest()
        {
            CopyToOutput(FilesLocation, "TS-err-cycle1.txt");
            CopyToOutput(FilesLocation, "cycle2.txt");
            Start("err-blockComment");
            Start("err-blockComment2");
            Start("err-varDefinition");
            Start("err-varDefinition2");
            Start("err-varDefinition3");
            Start("err-varDefinition4");
            Start("err-varDefinition5");
            Start("err-varDefinition6");
            Start("err-varDefinition7");
            Start("err-varDefinition8");
            Start("err-varDefinition9");
            Start("err-variableRef");
            Start("err-variableRef2");
            Start("err-variableRef3");
            Start("err-variableRef4");
            Start("err-cycle1");
            Start("input");
            Start("input2");
            Start("pass-blockComment");
            Start("pass-lineComment");
            Start("pass-lineComment2");
            Start("pass-noBlockComments");
            Start("pass-noLineComments");
            Start("pass-varDefinition");
            Start("pass-varDefinition2");
            Start("pass-varDefinition3");
            Start("pass-varDefinition4");
            Start("pass-command-and-comment");
            Assert.False(testFailed, "At least one of test cases failed. Check output for details.");
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
                testFailed = true;
            }
        }
    }
}