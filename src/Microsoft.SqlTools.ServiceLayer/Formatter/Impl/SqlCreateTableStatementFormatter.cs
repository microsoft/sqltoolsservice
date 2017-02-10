//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlCreateTableStatementFormatterFactory : ASTNodeFormatterFactoryT<SqlCreateTableStatement>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlCreateTableStatement codeObject)
        {
            return new SqlCreateTableStatementFormatter(visitor, codeObject);
        }
    }

    internal class SqlCreateTableStatementFormatter : ASTNodeFormatterT<SqlCreateTableStatement>
    {
        internal SqlCreateTableStatementFormatter(FormatterVisitor visitor, SqlCreateTableStatement codeObject)
            : base(visitor, codeObject)
        { }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            int nTokens = firstChildStartTokenNumber - startTokenNumber;
            Debug.Assert(nTokens >= 4, "unexpected token count for SqlCreateTableStatement prefix region");

            int createTokenIndex = -1;
            int tableTokenIndex = -1;
            bool foundComment = false;
            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                TokenData td = Visitor.Context.Script.TokenManager.TokenList[i];

                if (td.TokenId == FormatterTokens.LEX_END_OF_LINE_COMMENT)
                {
                    foundComment = true;
                } else if (td.TokenId == FormatterTokens.TOKEN_TABLE)
                {
                    tableTokenIndex = i;
                } else if (td.TokenId == FormatterTokens.TOKEN_CREATE)
                {
                    createTokenIndex = i;
                }
            }

            // logic below doesn't support single-line comments inside of a create table statement
            if (!foundComment && createTokenIndex < tableTokenIndex)
            {
                for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.NormalizeToOneSpace);
                }
            }
            else
            {
                Visitor.Context.ProcessTokenRange(startTokenNumber, firstChildStartTokenNumber);
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            // Due to a current limitation of the parser, the suffix region stops right after the list of column definitions.
            // According to the TSQL grammar schema, the statement could continue (see: http://msdn.microsoft.com/en-us/library/ms174979.aspx).
            // We will preserve the text in the sufix region in its current formatting, with the exception that we ensure the closed parenthesis
            // which closes the list of column definitions is on a new line and that all the tokens preceding it (which should only be comments)
            // are also each on a separate line and indented

            Visitor.Context.IncrementIndentLevel();

            int closeParenToken = -1;

            for (int i = lastChildEndTokenNumber; i < endTokenNumber && closeParenToken < 0; i++)
            {
                if (Visitor.Context.Script.TokenManager.TokenList[i].TokenId == 41) closeParenToken = i;
            }

            if (closeParenToken > 0)
            {
                for (int i = lastChildEndTokenNumber; i < closeParenToken - 1; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesInWhitespace);
                }

                Visitor.Context.DecrementIndentLevel();

                TokenData td2 = Visitor.Context.Script.TokenManager.TokenList[closeParenToken - 1];

                if (Visitor.Context.Script.TokenManager.IsTokenWhitespace(td2.TokenId))
                {
                    SimpleProcessToken(closeParenToken - 1, FormatterUtilities.NormalizeNewLinesInWhitespace);
                }
                else
                {
                    TokenData td = Visitor.Context.Script.TokenManager.TokenList[closeParenToken];
                    Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", Environment.NewLine + Visitor.Context.GetIndentString()));
                }

                // Add the closed parenthesis and the additional unparsed elements of the statement
                // which should keep their old formatting
                Visitor.Context.ProcessTokenRange(closeParenToken, endTokenNumber);
            }
            else
            {
                Visitor.Context.ProcessTokenRange(lastChildEndTokenNumber, endTokenNumber);
            }
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            if (previousChild is SqlObjectIdentifier && nextChild is SqlTableDefinition)
            {
                //
                // We want to make sure that the open-paren is on a new line and followed by a new-line & correctly indented.
                //

                // find the open paren token
                int openParenToken = -1;
                for (int i = previousChild.Position.endTokenNumber; i < nextChild.Position.startTokenNumber; i++)
                {
                    TokenData currentToken = Visitor.Context.Script.TokenManager.TokenList[i];
                    if (currentToken.TokenId == 40)
                    {
                        openParenToken = i;
                        break;
                    }
                }



                // normalize whitespace between last token & open paren.  Each whitespace token should be condensed down to a single space character
                for (int i = previousChild.Position.endTokenNumber; i < openParenToken - 1; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.NormalizeToOneSpace);
                }

                // If there is a whitespace before the open parenthisis, normalize it to a new line
                TokenData td = Visitor.Context.Script.TokenManager.TokenList[openParenToken - 1];

                if (Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId))
                {
                    if (previousChild.Position.endTokenNumber < openParenToken)
                    {
                        SimpleProcessToken(openParenToken - 1, FormatterUtilities.NormalizeNewLinesInWhitespace);
                    }
                }
                else
                {
                    if (previousChild.Position.endTokenNumber < openParenToken)
                    {
                        SimpleProcessToken(openParenToken - 1, FormatterUtilities.NormalizeToOneSpace);
                    }
                    TokenData tok = Visitor.Context.Script.TokenManager.TokenList[openParenToken];
                    Visitor.Context.Replacements.Add(new Replacement(tok.StartIndex, "", Environment.NewLine + Visitor.Context.GetIndentString()));
                }

                // append open-paren token
                Visitor.Context.ProcessTokenRange(openParenToken, openParenToken + 1);

                // process tokens between open paren & first child start
                Visitor.Context.IncrementIndentLevel();
                for (int i = openParenToken + 1; i < nextChild.Position.startTokenNumber; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesInWhitespace);
                }

                // ensure we have at least one new line
                if (openParenToken + 1 >= nextChild.Position.startTokenNumber || !Visitor.Context.Script.TokenManager.IsTokenWhitespace(Visitor.Context.Script.TokenManager.TokenList[nextChild.Position.startTokenNumber - 1].TokenId))
                {
                    TokenData tok = Visitor.Context.Script.TokenManager.TokenList[nextChild.Position.startTokenNumber];
                    Visitor.Context.Replacements.Add(new Replacement(tok.StartIndex, "", Environment.NewLine + Visitor.Context.GetIndentString()));
                }
                Visitor.Context.DecrementIndentLevel();

            }
            else
            {
                Visitor.Context.ProcessTokenRange(previousChild.Position.endTokenNumber, nextChild.Position.startTokenNumber);
            }

        }

    }
}
