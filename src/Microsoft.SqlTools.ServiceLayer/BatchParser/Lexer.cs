//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    internal sealed class Lexer : IDisposable
    {
        private LexerInput _currentInput;
        private bool _popInputAtNextConsume;
        private Token _currentToken;
        private ErrorCode _errorCode = ErrorCode.Success;
        private readonly Stack<LexerInput> _inputStack = new Stack<LexerInput>();
        private Func<bool> _lexerFunc;
        private TextRuleFlags _textRuleFlags;
        private PositionStruct _tokenBeginPosition;

        public Lexer(TextReader input, string name)
        {
            _currentInput = new LexerInput(input, name);
            _currentToken = null;
            RecognizeSqlCmdSyntax = true;

            SetState(RuleLine);
        }

        public Token CurrentToken
        {
            get {
                return _currentToken; 
            }
        }

        public LexerTokenType CurrentTokenType
        {
            get { return _currentToken.TokenType; }
        }


        public void ConsumeToken()
        {
            if (_currentInput == null)
            {
                return;
            }

            bool result;
            _tokenBeginPosition = new PositionStruct(_currentInput.CurrentLine, _currentInput.CurrentColumn, _currentInput.CurrentOffset, _currentInput.Filename);

            do
            {
                if (_popInputAtNextConsume)
                {
                    PopAndCloseInput();
                    _popInputAtNextConsume = false;
                }
                do
                {
                    result = _lexerFunc();
                } while (result == false);
                if (CurrentTokenType == LexerTokenType.Eof)
                {
                    _popInputAtNextConsume = true;
                    if(_inputStack.Count > 0)
                    {
                        // report as empty NewLine token
                        _currentToken = new Token(
                            LexerTokenType.NewLine, _tokenBeginPosition, _tokenBeginPosition, string.Empty, _tokenBeginPosition.Filename);
                    }
                }
            } while (result == false);
        }

        public void Dispose()
        {
            while (_inputStack.Count > 0)
            {
                PopAndCloseInput();
            }
        }

        public void PopAndCloseInput()
        {
            if (_currentInput != null)
            {
                _currentInput.Dispose();
                _currentInput = null;
            }
            if (_inputStack.Count > 0)
            {
                _currentInput = _inputStack.Pop();
                SetState(RuleLine);
            }
        }

        static string GetCircularReferenceErrorMessage(string filename)
        {
            return string.Format(CultureInfo.CurrentCulture, SR.BatchParser_CircularReference, filename);
        }

        public void PushInput(TextReader reader, string name)
        {
            Debug.Assert(_currentToken != null &&
                (_currentToken.TokenType == LexerTokenType.NewLine || _currentToken.TokenType == LexerTokenType.Eof), "New input can only be pushed after new line token or EOF");

            if (name.Equals(_currentInput.Filename, StringComparison.OrdinalIgnoreCase))
            {
                RaiseError(ErrorCode.CircularReference, GetCircularReferenceErrorMessage(name));
            }

            foreach (LexerInput input in _inputStack)
            {
                if (name.Equals(input.Filename, StringComparison.OrdinalIgnoreCase))
                {
                    RaiseError(ErrorCode.CircularReference, GetCircularReferenceErrorMessage(name));
                }
            }

            _inputStack.Push(_currentInput);
            _currentInput = new LexerInput(reader, name);
            SetState(RuleLine);
            // We don't want to close the input, in that case the current file would be taken out of the 
            // input stack and cycle detection won't work.
            _popInputAtNextConsume = false;
            ConsumeToken();
        }

        private void AcceptBlockComment()
        {
            char? ch;
            int nestingCount = 0;

            Consume();
            do
            {
                Consume();
                ch = Lookahead();

                if (ch.HasValue == false)
                {
                    RaiseError(ErrorCode.CommentNotTerminated,
                        string.Format(CultureInfo.CurrentCulture, SR.BatchParser_CommentNotTerminated));
                }

                if (ch.Value == '*')
                {
                    char? ch2 = Lookahead(1);
                    if (ch2.HasValue && ch2.Value == '/')
                    {
                        Consume();
                        if (nestingCount == 0)
                        {
                            Consume();
                            break;
                        }
                        nestingCount--;
                    }
                }
                else if (ch.Value == '/')
                {
                    char? ch2 = Lookahead(1);
                    if (ch2.HasValue && ch2.Value == '*')
                    {
                        Consume();
                        nestingCount++;
                    }
                }
            } while (true);
            SetToken(LexerTokenType.Comment);
        }

        private void AcceptLineComment()
        {
            char? ch;

            Consume();
            do
            {
                Consume();
                ch = Lookahead();

                if (ch.HasValue == false || IsNewLineChar(ch.Value))
                {
                    break;
                }
            } while (true);
            SetToken(LexerTokenType.Comment);
        }

        private void AcceptIdentifier()
        {
            char? ch;

            do
            {
                Consume();
                ch = Lookahead();
            } while (ch.HasValue && IsIdentifierChar(ch.Value));
            SetToken(LexerTokenType.Text);
        }

        private void AcceptNewLine()
        {
            char? ch = Lookahead();

            if (ch == '\r')
            {
                Consume();
                ch = Lookahead();
            }
            if (ch == '\n')
            {
                Consume();
            }

            SetToken(LexerTokenType.NewLine);
        }

        /// <summary>
        /// This method reads ahead until the closingChar is found.  When closingChar is found,
        /// the next character is checked.  If it's the same as closingChar, the character is
        /// escaped and the method resumes looking for a non-escaped closingChar.
        /// </summary>
        private void AcceptEscapableQuotedText(char closingChar)
        {
            char? ch;

            while (true)
            {
                Consume();
                ch = Lookahead();
                if (!ch.HasValue)
                {
                    // we reached the end without finding our closing character. that's an error.
                    RaiseError(ErrorCode.StringNotTerminated, SR.BatchParser_StringNotTerminated);
                    break;
                }

                if (ch == closingChar)
                {
                    // we found the closing character.  we call consume to ensure the pointer is now at the subsequent character.
                    Consume();

                    // Check whether the subsequent character is also closingChar.
                    // If it is, that means the closingChar is escaped, so we must continue searching.
                    // Otherwise, we're finished.
                    char? nextChar = Lookahead();
                    if (!nextChar.HasValue || nextChar != closingChar)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This method reads ahead until the closingChar is found.  This method does not allow for escaping
        /// of the closingChar.
        /// </summary>
        private void AcceptQuotedText(char closingChar)
        {
            char? ch;

            do
            {
                Consume();
                ch = Lookahead();
            } while (ch.HasValue && ch != closingChar);

            if (ch.HasValue == false)
            {
                RaiseError(ErrorCode.StringNotTerminated, SR.BatchParser_StringNotTerminated);
            }
            else
            {
                Consume();
            }
        }

        private void AcceptWhitespace()
        {
            char? ch;

            do
            {
                Consume();
                ch = Lookahead();
            } while (ch.HasValue && IsWhitespaceChar(ch.Value));

            SetToken(LexerTokenType.Whitespace);
        }

        private void Consume()
        {
            _currentInput.Consume();
        }

        private void ChangeStateToBatchCommand(Token token)
        {
            switch (token.TokenType)
            {
                case LexerTokenType.Setvar:
                    SetState(RuleSetvar);
                    break;
                case LexerTokenType.Go:
                    SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeLineComment);
                    break;
                case LexerTokenType.Include:
                    SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeDoubleQuotedString);
                    break;
                case LexerTokenType.OnError:
                    SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeLineComment);
                    break;
                default:
                    SetTextState(TextRuleFlags.ReportWhitespace);
                    break;
            }
        }

        private static bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        private static bool IsLetter(char ch)
        {
            return ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z';
        }

        private bool IsNewLineChar(char ch)
        {
            return ch == '\r' || ch == '\n';
        }

        private bool IsWhitespaceChar(char ch)
        {
            return ch == ' ' || ch == '\t';
        }

        private char? Lookahead()
        {
            return _currentInput.Lookahead();
        }

        private char? Lookahead(int lookahead)
        {
            return _currentInput.Lookahead(lookahead);
        }

        private bool RuleError()
        {
            // lexer repeats last error
            Parser.RaiseError(_errorCode, CurrentToken);
            Debug.Fail("Parser.RaiseError should throw an exception");
            return true;
        }

        private bool RuleLine()
        {
            char? ch = Lookahead();

            if (!ch.HasValue)
            {
                SetToken(LexerTokenType.Eof);
                return true;
            }

            switch (ch.Value)
            {
                case ' ':
                case '\t':
                    AcceptWhitespace();
                    return true;
                case '\r':
                case '\n':
                    AcceptNewLine();
                    return true;
                case ':':
                    if (RecognizeSqlCmdSyntax && TryAcceptBatchCommandAndSetToken())
                    {
                        ChangeStateToBatchCommand(_currentToken);
                        return true;
                    }
                    break;
                case 'g':
                case 'G':
                    if (TryAccept("go", true))
                    {
                        SetToken(LexerTokenType.Go);
                        ChangeStateToBatchCommand(_currentToken);
                        return true;
                    }
                    break;
            }

            SetTextState(TextRuleFlags.RecognizeSingleQuotedString | TextRuleFlags.RecognizeDoubleQuotedString | TextRuleFlags.RecognizeLineComment | TextRuleFlags.RecognizeBlockComment | TextRuleFlags.RecognizeBrace);
            return false;
        }

        private bool RuleSetvar()
        {
            char? ch = Lookahead();

            if (ch.HasValue == false)
            {
                SetToken(LexerTokenType.Eof);
                return true;
            }

            switch (ch.Value)
            {
                case '\r':
                case '\n':
                    AcceptNewLine();
                    SetState(RuleLine);
                    return true;
                case ' ':
                case '\t':
                    AcceptWhitespace();
                    return true;
                default:
                    if (IsStartIdentifierChar(ch.Value))
                    {
                        AcceptIdentifier();
                        SetTextState(TextRuleFlags.ReportWhitespace | TextRuleFlags.RecognizeDoubleQuotedString);
                        return true;
                    }
                    break;
            }

            // prepare error token
            do
            {
                Consume();
                ch = Lookahead();
            } while (ch != null && IsWhitespaceChar(ch.Value) == false && IsNewLineChar(ch.Value) == false);
            RaiseError(ErrorCode.UnrecognizedToken);

            return true;
        }

        private bool RuleText()
        {
            char? ch = Lookahead();

            if (ch.HasValue == false)
            {
                SetToken(LexerTokenType.Eof);
                return true;
            }
            
            if (ch.HasValue)
            {
                if (IsNewLineChar(ch.Value))
                {
                    AcceptNewLine();
                    SetState(RuleLine);
                    return true;
                }
                else if (_textRuleFlags.HasFlag(TextRuleFlags.ReportWhitespace) && IsWhitespaceChar(ch.Value))
                {
                    AcceptWhitespace();
                    return true;
                }
                else if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeBlockComment) || _textRuleFlags.HasFlag(TextRuleFlags.RecognizeLineComment))
                {
                    char? ch2 = Lookahead(1);
                    if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeBlockComment) && ch == '/' && ch2 == '*')
                    {
                        AcceptBlockComment();
                        return true;
                    }
                    else if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeLineComment) && ch == '-' && ch2 == '-')
                    {
                        AcceptLineComment();
                        return true;
                    }
                }
            }

            while (ch.HasValue)
            {
                bool consumed = false;
                switch (ch.Value)
                {
                    case ' ':
                    case '\t':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.ReportWhitespace))
                        {
                            SetToken(LexerTokenType.Text);
                            return true;
                        }
                        break;
                    case '\r':
                    case '\n':
                        SetToken(LexerTokenType.Text);
                        return true;
                    case '"':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeDoubleQuotedString))
                        {
                            AcceptQuotedText('"');
                            consumed = true;
                        }
                        break;
                    case '\'':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeSingleQuotedString))
                        {
                            AcceptEscapableQuotedText('\'');
                            consumed = true;
                        }
                        break;
                    case '[':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeBrace))
                        {
                            AcceptEscapableQuotedText(']');
                            consumed = true;
                        }
                        break;
                    case '-':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeLineComment))
                        {
                            char? ch2 = Lookahead(1);
                            if (ch.HasValue && ch2 == '-')
                            {
                                SetToken(LexerTokenType.Text);
                                return true;
                            }
                        }
                        break;
                    case '/':
                        if (_textRuleFlags.HasFlag(TextRuleFlags.RecognizeBlockComment))
                        {
                            char? ch2 = Lookahead(1);
                            if (ch.HasValue && ch2 == '*')
                            {
                                SetToken(LexerTokenType.Text);
                                return true;
                            }
                        }
                        break;
                    default:
                        break;
                }
                if (consumed == false)
                {
                    Consume();
                }
                ch = Lookahead();
            }
            SetToken(LexerTokenType.Text);
            return true;
        }

        private void RaiseError(ErrorCode code, string message = null)
        {
            SetState(RuleError);
            SetToken(LexerTokenType.Error);
            _errorCode = code;
            Parser.RaiseError(_errorCode, CurrentToken, message);
        }

        private void SetState(Func<bool> lexerFunc)
        {
            this._lexerFunc = lexerFunc;
        }

        internal void SetTextState(TextRuleFlags textRuleFlags)
        {
            this._textRuleFlags = textRuleFlags;
            SetState(RuleText);
        }

        private void SetToken(LexerTokenType lexerTokenType)
        {
            string text = _currentInput.FlushBufferedText();

            _currentToken = new Token(
                lexerTokenType, 
                _tokenBeginPosition, 
                new PositionStruct(_currentInput.CurrentLine, _currentInput.CurrentColumn, _currentInput.CurrentOffset, _currentInput.Filename),
                text, 
                _currentInput.Filename);
        }

        private bool TryAccept(string text, bool wordBoundary)
        {
            Debug.Assert(text.Length > 0);

            int i = 0;
            do
            {
                char? ch = Lookahead(i);
                if (ch.HasValue == false || char.ToLowerInvariant(ch.Value) != text[i])
                {
                    return false;
                }
                i++;
            } while (i < text.Length);

            if (wordBoundary)
            {
                char? ch = Lookahead(text.Length);
                if (ch != null && IsWhitespaceChar(ch.Value) == false && IsNewLineChar(ch.Value) == false 
                    && ch != '$' && ch != '/' && ch != '-' && ch != '\'' && ch != '"' && ch != '(' && ch != '[' && ch != '!')
                {
                    return false;
                }
            }

            // consume all checked characters
            for (i = 0; i < text.Length; i++)
            {
                Consume();
            }

            return true;
        }

        private bool TryAcceptBatchCommandAndSetToken()
        {
            Consume(); // colon

            if (TryAccept("reset", true))
            {
                SetToken(LexerTokenType.Reset);
                return true;
            }
            else if (TryAccept("ed", true))
            {
                SetToken(LexerTokenType.Ed);
                return true;
            }
            else if (TryAccept("!!", true))
            {
                SetToken(LexerTokenType.Execute);
                return true;
            }
            else if (TryAccept("quit", true))
            {
                SetToken(LexerTokenType.Quit);
                return true;
            }
            else if (TryAccept("exit", true))
            {
                SetToken(LexerTokenType.Exit);
                return true;
            }
            else if (TryAccept("r", true))
            {
                SetToken(LexerTokenType.Include);
                return true;
            }
            else if (TryAccept("serverlist", true))
            {
                SetToken(LexerTokenType.Serverlist);
                return true;
            }
            else if (TryAccept("setvar", true))
            {
                SetToken(LexerTokenType.Setvar);
                return true;
            }
            else if (TryAccept("list", true))
            {
                SetToken(LexerTokenType.List);
                return true;
            }
            else if (TryAccept("error", true))
            {
                SetToken(LexerTokenType.ErrorCommand);
                return true;
            }
            else if (TryAccept("out", true))
            {
                SetToken(LexerTokenType.Out);
                return true;
            }
            else if (TryAccept("perftrace", true))
            {
                SetToken(LexerTokenType.Perftrace);
                return true;
            }
            else if (TryAccept("connect", true))
            {
                SetToken(LexerTokenType.Connect);
                return true;
            }
            else if (TryAccept("on error", true))
            {
                SetToken(LexerTokenType.OnError);
                return true;
            }
            else if (TryAccept("help", true))
            {
                SetToken(LexerTokenType.Help);
                return true;
            }
            else if (TryAccept("xml", true))
            {
                SetToken(LexerTokenType.Xml);
                return true;
            }
            else if (TryAccept("listvar", true))
            {
                SetToken(LexerTokenType.ListVar);
                return true;
            }

            return false;
        }
        internal static bool IsIdentifierChar(char ch)
        {
            return IsLetter(ch) || IsDigit(ch) || ch == '_' || ch == '-';
        }

        internal static bool IsStartIdentifierChar(char ch)
        {
            return IsLetter(ch) || ch == '_';
        }

        public bool RecognizeSqlCmdSyntax { get; set; }
    }
}
