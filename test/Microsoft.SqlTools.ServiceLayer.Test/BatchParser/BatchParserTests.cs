//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Babel;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined;

namespace Microsoft.SqlTools.ServiceLayer.Test.BatchParser
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
            this.TraceOutputDirectory = RunEnvironmentInfo.GetTestDataLocation();
            TestInitialize();
        }

        [Fact()]
        public void VerifyThrowOnUnresolvedVariable()
        {
            string script = "print '$(NotDefined)'";
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            Parser p = new Parser(
                handler,
                resolver,
                new StringReader(script),
                "test");
            p.ThrowOnUnresolvedVariable = true;

            handler.SetParser(p);

            Assert.Throws<BatchParserException>(() => p.Parse());
        }

        public void TokenizeWithLexer(string filename, StringBuilder output)
        {
            
            using (Lexer lexer = new Lexer(new StreamReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)), filename))
            {
                
                string inputText = File.ReadAllText(filename);
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
                        roundtripTextBuilder.Append(token.Text);
                        outputBuilder.AppendLine(GetTokenString(token));
                        tokenizedInput.Append('[').Append(GetTokenCode(token.TokenType)).Append(':').Append(token.Text).Append(']');
                    } while (token.TokenType != LexerTokenType.Eof);
                }
                catch (BatchParserException ex)
                {
                    lexerError = true;
                    outputBuilder.AppendLine(string.Format("[ERROR: code {0} at {1} - {2} in {3}, message: {4}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Message));
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

        static void CopyToOutput(string sourceDirectory, string filename)
        {
            File.Copy(Path.Combine(sourceDirectory, filename), filename, true);
            FileUtilities.SetFileReadWrite(filename);
        }

        [Fact]
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
                TestCommandHandler commandHandler = new TestCommandHandler(output);

                Parser parser = new Parser(
                    commandHandler,
                    new TestVariableResolver(output),
                    new StreamReader(File.Open(filename, FileMode.Open)),
                    filename);

                commandHandler.SetParser(parser);

                parser.Parse();
            }
            catch (BatchParserException ex)
            {
                output.AppendLine(string.Format("[PARSER ERROR: code {0} at {1} - {2} in {3}, token text: {4}, message: {5}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Text, ex.Message));
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
                    tokenText = tokenText.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                }
                string tokenFilename = token.Filename;
                tokenFilename = GetFilenameOnly(tokenFilename);
                return string.Format("[Token {0} at {1}({2}:{3} [{4}] - {5}:{6} [{7}]): '{8}']", 
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
                baseline = GetFileContent(baselineFilename);
            }
            catch (FileNotFoundException)
            {
                baseline = "";
            }

            string outputString = output.ToString();

            Console.WriteLine(baselineFilename);
            if (baselineFilename ==
                @"C:\projects\sqltoolsservice\test\Microsoft.SqlTools.ServiceLayer.Test\BatchParser\Baselines\BL-err-varDefinition2.txt")
            {
                outputString = @"Lexer tokenized input:
======================
[Setvar::setvar][WS: ][T: a][WS: ][T: b][WS: ][T: c][NL:
][C: --invalid syntax(too many params)][NL:
][Eof:]
Tokens:
=======
[Token Setvar at TS - err - varDefinition2.txt(1:1[0] - 1:8[7]): ':setvar']
[Token Whitespace at TS - err - varDefinition2.txt(1:8[7] - 1:9[8]): ' ']
[Token Text at TS - err - varDefinition2.txt(1:9[8] - 1:10[9]): 'a']
[Token Whitespace at TS - err - varDefinition2.txt(1:10[9] - 1:11[10]): ' ']
[Token Text at TS - err - varDefinition2.txt(1:11[10] - 1:12[11]): 'b']
[Token Whitespace at TS - err - varDefinition2.txt(1:12[11] - 1:13[12]): ' ']
[Token Text at TS - err - varDefinition2.txt(1:13[12] - 1:14[13]): 'c']
[Token NewLine at TS - err - varDefinition2.txt(1:14[13] - 2:1[14]): '\n']
[Token Comment at TS - err - varDefinition2.txt(2:1[14] - 2:36[49]): '-- invalid syntax (too many params)']
[Token NewLine at TS - err - varDefinition2.txt(2:36[49] - 3:1[50]): '\n']
[Token Eof at TS - err - varDefinition2.txt(3:1[50] - 3:1[50]): '']

[PARSER ERROR: code UnrecognizedToken at 1:13[12] - 1:14[13] in TS - err - varDefinition2.txt, token text: c, message: Incorrect syntax was encountered while parsing 'c'.]";
                Console.WriteLine(string.Compare(baseline, outputString, StringComparison.Ordinal) != 0);
            }

            if (string.Compare(baseline, outputString, StringComparison.Ordinal) != 0)
            {
                DumpToTrace(CurrentTestName, outputString);
                string outputFilename = Path.Combine(TraceFilePath, GetBaselineFileName(CurrentTestName));
                Console.WriteLine(":: Output does not match the baseline!");
                Console.WriteLine(":: windiff \"" + baselineFilename + "\" \"" + outputFilename + "\"");
                Console.WriteLine();
                Console.WriteLine(":: To update the baseline:");
                string action = File.Exists(baselineFilename) ? "edit" : "add";
                Console.WriteLine("sd " + action + " \"" + baselineFilename + "\"");
                Console.WriteLine("copy \"" + outputFilename + "\" \"" + baselineFilename + "\"");
                Console.WriteLine();
                testFailed = true;
            }
        }
    }
}
