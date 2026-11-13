//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    /// <summary>
    /// Detects whether a script contains SQLCMD syntax, so that SQLCMD mode can be enabled automatically.
    /// </summary>
    public static class SqlCmdSyntaxDetector
    {
        /// <summary>
        /// Returns true if the script contains a SQLCMD command recognized by the batch parser (such as
        /// <c>:setvar</c>, <c>:r</c>, <c>:on error</c>, <c>:connect</c> or <c>:!!</c>) or a <c>$(variable)</c>
        /// reference outside of string literals and comments.
        /// </summary>
        /// <param name="script">The script text to inspect</param>
        public static bool ContainsSqlCmdSyntax(string script)
        {
            if (string.IsNullOrEmpty(script) || (script.IndexOf(':') < 0 && script.IndexOf("$(", System.StringComparison.Ordinal) < 0))
            {
                return false;
            }

            using var lexer = new Lexer(new StringReader(script), string.Empty);
            lexer.RecognizeSqlCmdSyntax = true;

            try
            {
                while (true)
                {
                    lexer.ConsumeToken();
                    Token token = lexer.CurrentToken;
                    switch (token.TokenType)
                    {
                        case LexerTokenType.Eof:
                            return false;
                        case LexerTokenType.Text:
                            if (ContainsVariableReference(token.Text))
                            {
                                return true;
                            }
                            break;
                        default:
                            if (IsSqlCmdCommand(token.TokenType))
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            catch (BatchParserException)
            {
                // Malformed scripts (e.g. unterminated strings or comments) are left for the regular
                // parser to report; they don't indicate SQLCMD usage on their own.
                return false;
            }
        }

        private static bool IsSqlCmdCommand(LexerTokenType tokenType)
        {
            switch (tokenType)
            {
                case LexerTokenType.Reset:
                case LexerTokenType.Ed:
                case LexerTokenType.Execute:
                case LexerTokenType.Quit:
                case LexerTokenType.Exit:
                case LexerTokenType.Include:
                case LexerTokenType.Serverlist:
                case LexerTokenType.Setvar:
                case LexerTokenType.List:
                case LexerTokenType.ErrorCommand:
                case LexerTokenType.Out:
                case LexerTokenType.Perftrace:
                case LexerTokenType.Connect:
                case LexerTokenType.OnError:
                case LexerTokenType.Help:
                case LexerTokenType.Xml:
                case LexerTokenType.ListVar:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Looks for a well-formed <c>$(name)</c> reference in a lexer text token. Single-quoted string
        /// literals are skipped, while bracketed and double-quoted identifiers are scanned since they
        /// commonly contain variable references (e.g. <c>USE [$(DatabaseName)]</c>).
        /// </summary>
        private static bool ContainsVariableReference(string text)
        {
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch == '\'' || ch == '[' || ch == '"')
                {
                    char closingChar = ch == '[' ? ']' : ch;
                    bool scanContents = ch != '\'';
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == closingChar)
                        {
                            // A doubled closing character is an escape, except for double-quoted identifiers
                            if (closingChar != '"' && i + 1 < text.Length && text[i + 1] == closingChar)
                            {
                                i += 2;
                                continue;
                            }
                            break;
                        }
                        if (scanContents && IsVariableReferenceAt(text, i))
                        {
                            return true;
                        }
                        i++;
                    }
                }
                else if (IsVariableReferenceAt(text, i))
                {
                    return true;
                }
                i++;
            }
            return false;
        }

        private static bool IsVariableReferenceAt(string text, int index)
        {
            if (index + 3 >= text.Length || text[index] != '$' || text[index + 1] != '(' || !Lexer.IsStartIdentifierChar(text[index + 2]))
            {
                return false;
            }

            for (int i = index + 3; i < text.Length; i++)
            {
                if (text[i] == ')')
                {
                    return true;
                }
                if (!Lexer.IsIdentifierChar(text[i]))
                {
                    return false;
                }
            }
            return false;
        }
    }
}
