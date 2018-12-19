//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ManagedBatchParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// The Parser class on which the Batch Parser is based on
    /// </summary>
    public sealed class Parser : IDisposable
    {
        private readonly ICommandHandler commandHandler;
        private Lexer lexer;
        private List<Token> tokenBuffer;
        private readonly IVariableResolver variableResolver;

        /// <summary>
        /// Constructor for the Parser class
        /// </summary>
        public Parser(ICommandHandler commandHandler, IVariableResolver variableResolver, TextReader reader, string name)
        {
            this.commandHandler = commandHandler;
            this.variableResolver = variableResolver;
            lexer = new Lexer(reader, name);
            tokenBuffer = new List<Token>();
        }

        public bool ThrowOnUnresolvedVariable { get; set; }

        private Token LookaheadToken
        {
            get { return lexer.CurrentToken; }
        }

        private LexerTokenType LookaheadTokenType
        {
            get { return lexer.CurrentTokenType; }
        }

        public void Dispose()
        {
            if (lexer != null)
            {
                lexer.Dispose();
                lexer = null;
            }
        }

        private bool AcceptWhitespaceOrComment()
        {
            bool found = false;
            while (LookaheadTokenType == LexerTokenType.Comment ||
                LookaheadTokenType == LexerTokenType.Whitespace)
            {
                Accept();
                found = true;
            }
            return found;
        }

        private bool Accept(LexerTokenType lexerTokenType)
        {
            if (LookaheadTokenType == lexerTokenType)
            {
                Accept();
                return true;
            }
            return false;
        }

        private void AddTokenToStringBuffer()
        {
            Token token = LookaheadToken;

            if (token.TokenType != LexerTokenType.Comment)
            {
                // All variable references must be valid
                CheckVariableReferences(token);
            }
            if (token.TokenType == LexerTokenType.NewLine && token.Text.Length == 0)
            {
                // Add "virtual" token representing new line that was not in original file
                PositionStruct beginPos = token.Begin;
                PositionStruct endPos = new PositionStruct(beginPos.Line + 1, 1, beginPos.Offset, beginPos.Filename);

                tokenBuffer.Add(new Token(LexerTokenType.NewLine, beginPos, endPos, Environment.NewLine, beginPos.Filename));
            }
            else
            {
                tokenBuffer.Add(token);
            }
        }

        private void CheckVariableReferences(Token token)
        {
            // AddVariableReferences will raise error if reference is not constructed correctly 
            AddVariableReferences(token, 0, null);
        }

        private void AddVariableReferences(Token token, int offset, IList<VariableReference> variableRefs)
        {
            if (lexer.RecognizeSqlCmdSyntax == false)
            {
                // variables are recognized only in sqlcmd mode.
                return;
            }

            string text = token.Text;

            int i = 0;
            int startIndex = -1;
            int state = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (state == 0 && ch == '$')
                {
                    state = 1;
                } 
                else if (state == 1)
                {
                    if (ch == '(')
                    {
                        state = 2;
                    }
                    else if (ch != '$')
                    {
                        state = 0;
                    }
                }
                else if (state == 2)
                {
                    if (Lexer.IsStartIdentifierChar(ch))
                    {
                        startIndex = i;
                        state = 3;
                    }
                    else
                    {
                        RaiseError(ErrorCode.InvalidVariableName, GetSubToken(token, i - 2, i + 1));
                    }
                }
                else if (state == 3)
                {
                    if (ch == ')')
                    {
                        if (variableRefs != null)
                        {
                            variableRefs.Add(new VariableReference(offset + startIndex - 2, i - startIndex + 3, text.Substring(startIndex, i - startIndex)));
                        }
                        state = 0;
                    }
                    else if (Lexer.IsIdentifierChar(ch) == false)
                    {
                        RaiseError(ErrorCode.InvalidVariableName, GetSubToken(token, startIndex - 2, i + 1));
                    }
                }
                i++;
            }
            if (state == 2 || state == 3)
            {
                RaiseError(ErrorCode.InvalidVariableName, GetSubToken(token, startIndex - 2, i));
            }
        }

        private static Token GetSubToken(Token token, int startOffset, int endOffset, LexerTokenType? newTokenType = null)
        {
            LexerTokenType tokenType = newTokenType.HasValue ? newTokenType.Value : token.TokenType;
            string text = token.Text.Substring(startOffset, endOffset - startOffset);
            string filename = token.Begin.Filename;
            PositionStruct beginPos, endPos;

            int beginLine, beginColumn;
            LineInfo.CalculateLineColumnForOffset(token, startOffset, out beginLine, out beginColumn);
            beginPos = new PositionStruct(beginLine, beginColumn, token.Begin.Offset + startOffset, filename);

            int endLine, endColumn;
            LineInfo.CalculateLineColumnForOffset(token, endOffset, out endLine, out endColumn);
            endPos = new PositionStruct(endLine, endColumn, token.Begin.Offset + endOffset, filename);

            return new Token(tokenType, beginPos, endPos, text, filename);
        }

        private LexerTokenType Accept()
        {
            lexer.ConsumeToken();
            return LookaheadTokenType;
        }

        private void ExecuteBatch(int repeatCount)
        {
            BatchParserAction action;
            action = commandHandler.Go(new TextBlock(this, tokenBuffer), repeatCount);

            if (action == BatchParserAction.Abort)
            {
                RaiseError(ErrorCode.Aborted);
            }
            tokenBuffer = new List<Token>();
        }

        private bool Expect(LexerTokenType lexerTokenType)
        {
            if (LookaheadTokenType == lexerTokenType)
                return true;
            RaiseError(ErrorCode.TokenExpected);
            return false;
        }

        private bool ExpectAndAccept(LexerTokenType lexerTokenType)
        {
            if (Accept(lexerTokenType))
                return true;
            RaiseError(ErrorCode.TokenExpected);
            return false;
        }

        public void Parse()
        {
            Accept();
            ParseLines();
        }

        private void ParseGo()
        {
            int repeatCount = 1;
            AcceptWhitespaceOrComment();
            if (LookaheadTokenType == LexerTokenType.Text)
            {
                string text = ResolveVariables(LookaheadToken, 0, null);
                for (int i = 0; i < text.Length; i++)
                {
                    if (Char.IsDigit(text[i]) == false)
                    {
                        RaiseError(ErrorCode.InvalidNumber);
                    }
                }
                bool result = Int32.TryParse(text, out repeatCount);
                if (result == false)
                {
                    RaiseError(ErrorCode.InvalidNumber);
                }
                Accept(LexerTokenType.Text);
                AcceptWhitespaceOrComment();
            }
            if (LookaheadTokenType != LexerTokenType.Eof)
            {
                ExpectAndAccept(LexerTokenType.NewLine);
            }
            ExecuteBatch(repeatCount);
        }

        private void ParseInclude()
        {
            BatchParserAction action;

            Accept(LexerTokenType.Whitespace);
            Expect(LexerTokenType.Text);

            Token token = LookaheadToken;

            if (token.Text.StartsWith("--", StringComparison.Ordinal))
            {
                RaiseError(ErrorCode.TokenExpected);
            }

            lexer.SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeLineComment | TextRuleFlags.RecognizeDoubleQuotedString);
            Accept();
            AcceptWhitespaceOrComment();
            if (LookaheadTokenType != LexerTokenType.Eof)
            {
                Expect(LexerTokenType.NewLine);
            }
            TextReader textReader;
            string newFilename;

            string tokenText = token.Text;
            IEnumerable<Token> tokens;
            if (tokenText.IndexOf('"') != -1)
            {
                tokens = SplitQuotedTextToken(token);
            } 
            else 
            {
                tokens = new[] { token };
            }

            action = commandHandler.Include(new TextBlock(this, tokens), out textReader, out newFilename);

            if (action == BatchParserAction.Abort)
            {
                RaiseError(ErrorCode.Aborted);
            }
            if (textReader != null)
            {
                this.PushInput(textReader, newFilename);
            }
        }

        private IEnumerable<Token> SplitQuotedTextToken(Token token)
        {
            string tokenText = token.Text;

            if (tokenText.Length <= 1)
            {
                return new[] { token };
            }

            IList<Token> tokens = new List<Token>();

            int offset = 0;
            int quotePos = tokenText.IndexOf('"');
            while (quotePos != -1)
            {
                int closingQuotePos;
                if (quotePos != offset)
                {
                    tokens.Add(GetSubToken(token, offset, quotePos));
                    offset = quotePos;
                }
                closingQuotePos = tokenText.IndexOf('"', quotePos + 1);
                if (closingQuotePos == -1)
                {
                    // odd number of " characters
                    break;
                }
                if (closingQuotePos + 1 < tokenText.Length && tokenText[closingQuotePos + 1] == '"')
                {
                    // two consequtive " characters: report one of them
                    tokens.Add(GetSubToken(token, quotePos + 1, closingQuotePos + 1, LexerTokenType.TextVerbatim));
                }
                else
                {
                    // Add the contents of the quoted string, but remove the " characters
                    tokens.Add(GetSubToken(token, quotePos + 1, closingQuotePos, LexerTokenType.TextVerbatim));
                }
                offset = closingQuotePos + 1;
                
                quotePos = tokenText.IndexOf('"', offset);
            }
            if (offset != tokenText.Length)
            {
                tokens.Add(GetSubToken(token, offset, tokenText.Length));
            }
            return tokens;
        }

        internal void PushInput(TextReader reader, string filename)
        {
            lexer.PushInput(reader, filename);
        }

        private void ParseLines()
        {
            do
            {
                LexerTokenType tokenType = LookaheadTokenType;
                switch (tokenType)
                {
                    case LexerTokenType.OnError:
                        RemoveLastWhitespaceToken();
                        Token onErrorToken = LookaheadToken;
                        Accept();
                        ParseOnErrorCommand(onErrorToken);
                        break;
                    case LexerTokenType.Eof:
                        if (tokenBuffer.Count > 0)
                        {
                            ExecuteBatch(1);
                        }
                        return;
                    case LexerTokenType.Go:
                        RemoveLastWhitespaceToken();
                        Accept();
                        ParseGo();
                        break;
                    case LexerTokenType.Include:
                        RemoveLastWhitespaceToken();
                        Accept();
                        ParseInclude();
                        break;
                    case LexerTokenType.Comment:
                    case LexerTokenType.NewLine:
                    case LexerTokenType.Text:
                    case LexerTokenType.Whitespace:
                        AddTokenToStringBuffer();
                        Accept();
                        break;
                    case LexerTokenType.Setvar:
                        Token setvarToken = LookaheadToken;
                        RemoveLastWhitespaceToken();
                        Accept();
                        ParseSetvar(setvarToken);
                        break;
                    case LexerTokenType.Connect:
                    case LexerTokenType.Ed:
                    case LexerTokenType.ErrorCommand:
                    case LexerTokenType.Execute:
                    case LexerTokenType.Exit:
                    case LexerTokenType.Help:
                    case LexerTokenType.List:
                    case LexerTokenType.ListVar:
                    case LexerTokenType.Out:
                    case LexerTokenType.Perftrace:
                    case LexerTokenType.Quit:
                    case LexerTokenType.Reset:
                    case LexerTokenType.Serverlist:
                    case LexerTokenType.Xml:
                        RaiseError(ErrorCode.UnsupportedCommand,
                            string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionError_CommandNotSupported, tokenType));
                        break;
                    default:
                        RaiseError(ErrorCode.UnrecognizedToken);
                        break;
                }
            } while (true);
        }

        private void RemoveLastWhitespaceToken()
        {
            if (tokenBuffer.Count > 0 && tokenBuffer[tokenBuffer.Count - 1].TokenType == LexerTokenType.Whitespace)
            {
                tokenBuffer.RemoveAt(tokenBuffer.Count - 1);
            }
        }

        private void ParseOnErrorCommand(Token onErrorToken)
        {
            ExpectAndAccept(LexerTokenType.Whitespace);
            Expect(LexerTokenType.Text);

            string action;
            action = ResolveVariables(LookaheadToken, 0, null);

            OnErrorAction onErrorAction;

            if (action.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                onErrorAction = OnErrorAction.Exit;
            }
            else if (action.Equals("ignore", StringComparison.OrdinalIgnoreCase))
            {
                onErrorAction = OnErrorAction.Ignore;
            }
            else
            {
                RaiseError(ErrorCode.UnrecognizedToken);
                return;
            }

            Accept(LexerTokenType.Text);
            AcceptWhitespaceOrComment();
            if (LookaheadTokenType != LexerTokenType.Eof)
            {
                Expect(LexerTokenType.NewLine);
            }

            BatchParserAction parserAction;

            parserAction = commandHandler.OnError(onErrorToken, onErrorAction);

            if (parserAction == BatchParserAction.Abort)
            {
                RaiseError(ErrorCode.Aborted);
            }
        }

        private void ParseSetvar(Token setvarToken)
        {
            string variableName;
            string variableValue = null;
            Accept(LexerTokenType.Whitespace);
            Expect(LexerTokenType.Text);

            variableName = LookaheadToken.Text;

            Accept();
            Accept(LexerTokenType.Whitespace);

            switch (LookaheadTokenType)
            {
                case LexerTokenType.Text:
                    // No variable substitution
                    variableValue = UnquoteVariableValue(LookaheadToken.Text);
                    lexer.SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeLineComment | TextRuleFlags.RecognizeDoubleQuotedString);
                    Accept();
                    AcceptWhitespaceOrComment();

                    if (LookaheadTokenType != LexerTokenType.NewLine &&
                        LookaheadTokenType != LexerTokenType.Eof)
                    {
                        RaiseError(ErrorCode.UnrecognizedToken);
                    }

                    if (LookaheadTokenType != LexerTokenType.Eof)
                    {
                        Accept();
                    }
                    break;
                case LexerTokenType.NewLine:
                case LexerTokenType.Eof:
                    Accept();
                    break;
                default:
                    RaiseError(ErrorCode.UnrecognizedToken);
                    break;
            }

            variableResolver.SetVariable(setvarToken.Begin, variableName, variableValue);
        }

        internal void RaiseError(ErrorCode errorCode, string message = null)
        {
            RaiseError(errorCode, LookaheadToken, message);
        }

        internal static void RaiseError(ErrorCode errorCode, Token token, string message = null)
        {
            if (message == null)
            {
                message = string.Format(CultureInfo.CurrentCulture, SR.BatchParser_IncorrectSyntax, token.Text);
            }
            throw new BatchParserException(errorCode, token, message);
        }

        internal string ResolveVariables(Token inputToken, int offset, List<VariableReference> variableRefs)
        {
            List<VariableReference> variableRefsLocal = new List<VariableReference>();
            AddVariableReferences(inputToken, offset, variableRefsLocal);

            string inputString = inputToken.Text;
            if (variableRefsLocal.Count == 0)
            {
                // no variable references to substitute
                return inputString;
            }

            StringBuilder sb = new StringBuilder();
            int lastChar = 0;
            PositionStruct? variablePos = null;
            foreach (VariableReference reference in variableRefsLocal)
            {
                int line;
                int column;

                if (variablePos != null)
                {
                    LineInfo.CalculateLineColumnForOffset(inputToken.Text, reference.Start - offset, 
                        variablePos.Value.Offset, variablePos.Value.Line, variablePos.Value.Column, 
                        out line, out column);
                }
                else
                {
                    LineInfo.CalculateLineColumnForOffset(inputToken, reference.Start - offset, out line, out column);
                }

                variablePos = new PositionStruct(
                    line: line,
                    column: column,
                    offset: reference.Start - offset,
                    filename: inputToken.Filename);
                string value = variableResolver.GetVariable(variablePos.Value, reference.VariableName);
                if (value == null)
                {
                    // Undefined variable
                    if (ThrowOnUnresolvedVariable == true)
                    {
                        RaiseError(ErrorCode.VariableNotDefined, inputToken,
                            string.Format(CultureInfo.CurrentCulture, SR.BatchParser_VariableNotDefined, reference.VariableName));
                    }
                    continue;
                }

                reference.VariableValue = value;
                sb.Append(inputToken.Text, lastChar, reference.Start - offset - lastChar);
                sb.Append(value);
                lastChar = reference.Start - offset + reference.Length;
            }
            if (lastChar != inputString.Length)
            {
                sb.Append(inputString, lastChar, inputString.Length - lastChar);
            }
            if (variableRefs != null)
            {
                variableRefs.AddRange(variableRefsLocal);
            }
            return sb.ToString();
        }

        private string UnquoteVariableValue(string s)
        {
            if (string.IsNullOrEmpty(s) == false)
            {
                if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == s[0])
                {
                    // all " characters must be doubled
                    for (int i = 1; i < s.Length - 1; i++)
                    {
                        if (s[i] == '"')
                        {
                            if (i + 1 >= s.Length - 1 || s[i + 1] != '"')
                            {
                                RaiseError(ErrorCode.TokenExpected);
                            }
                            // skip next character
                            i++;
                        }
                    }
                    return s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
                }
                else if (s[0] == '"' || s[s.Length - 1] == '"')
                {
                    RaiseError(ErrorCode.TokenExpected);
                }
                else
                {
                    if (s.Length >= 2 && s[0] == '-' && s[1] == '-')
                    {
                        return null;
                    }
                    if (s.IndexOf('"') >= 0)
                    {
                        RaiseError(ErrorCode.TokenExpected);
                    }
                }
            }
            return s;
        }

        public void SetRecognizeSqlCmdSyntax(bool recognizeSqlCmdSyntax)
        {
            lexer.RecognizeSqlCmdSyntax = recognizeSqlCmdSyntax;
        }

        public static TextReader GetBufferedTextReaderForFile(string filename)
        {
            Exception handledException = null;
            try
            {
                return new StringReader(File.ReadAllText(filename));
            }
            catch (IOException exception)
            {
                handledException = exception;
            }
            catch (NotSupportedException exception)
            {
                handledException = exception;
            }
            catch (UnauthorizedAccessException exception)
            {
                handledException = exception;
            }
            catch (SecurityException exception)
            {
                handledException = exception;
            }

            if (null != handledException)
            {
                throw new Exception(handledException.Message, handledException);
            }

            Debug.Fail("This code should not be reached.");
            return null;
        }

        internal void SetBatchDelimiter(string batchSeparator)
        {
            // Not implemeneted as only GO is supported now
        }

        public static string TokenTypeToCommandString(LexerTokenType lexerTokenType)
        {
            switch (lexerTokenType)
            {
                case LexerTokenType.Connect:
                    return "Connect";
                case LexerTokenType.Ed:
                    return "Ed";
                case LexerTokenType.ErrorCommand:
                    return "Error";
                case LexerTokenType.Execute:
                    return "!!";
                case LexerTokenType.Exit:
                    return "Exit";
                case LexerTokenType.Help:
                    return "Help";
                case LexerTokenType.List:
                    return "List";
                case LexerTokenType.ListVar:
                    return "ListVar";
                case LexerTokenType.OnError:
                    return "On Error";
                case LexerTokenType.Out:
                    return "Out";
                case LexerTokenType.Perftrace:
                    return "PerfTrace";
                case LexerTokenType.Quit:
                    return "Quit";
                case LexerTokenType.Reset:
                    return "Reset";
                case LexerTokenType.Serverlist:
                    return "ServerList";
                case LexerTokenType.Xml:
                    return "Xml";
                default:
                    Debug.Fail("Unknown batch parser command");
                    return lexerTokenType.ToString();
            }
        }
    }
}
