//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlViewDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlViewDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlViewDefinition codeObject)
        {
            return new SqlViewDefinitionFormatter(visitor, codeObject);
        }
    }

    class SqlViewDefinitionFormatter : ASTNodeFormatterT<SqlViewDefinition>
    {
        internal CommaSeparatedListFormatter CommaSeparatedList { get; set; }

        internal SqlViewDefinitionFormatter(FormatterVisitor visitor, SqlViewDefinition sqlCodeObject)
            : base(visitor, sqlCodeObject)
        {
            CommaSeparatedList = new CommaSeparatedListFormatter(Visitor, CodeObject, true);
        }

        public override void Format()
        {
            LexLocation loc = CodeObject.Position;

            SqlCodeObject firstChild = CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of its first child
                //
                LexLocation firstChildStart = firstChild.Position;
                ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                ProcessChild(firstChild);

                // keep track of the next token to process
                int nextToken = firstChildStart.endTokenNumber;

                // process the columns if available
                nextToken = ProcessColumns(nextToken);

                // process options if available
                nextToken = ProcessOptions(nextToken);

                // process the region containing the AS token
                nextToken = ProcessAsToken(nextToken);

                // process the query with clause if present
                nextToken = ProcessQueryWithClause(nextToken);

                // process the query expression
                nextToken = ProcessQueryExpression(nextToken);

                DecrementIndentLevel();

                // format text from end of last child to end of object.
                SqlCodeObject lastChild = CodeObject.Children.LastOrDefault();
                Debug.Assert(lastChild != null, "last child is null.  Need to write code to deal with this case");
                ProcessSuffixRegion(lastChild.Position.endTokenNumber, loc.endTokenNumber);
            }
            else
            {
                // no children
                Visitor.Context.ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
            }
        }

        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.ColumnList != null && CodeObject.ColumnList.Count > 0)
            {
                #region Find the open parenthesis
                int openParenIndex = nextToken;

                TokenData td = TokenManager.TokenList[openParenIndex];
                while (td.TokenId != 40 && openParenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the open parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(openParenIndex, openParenIndex + 1))
                     );
                    ++openParenIndex;
                    td = TokenManager.TokenList[openParenIndex];
                }
                Debug.Assert(openParenIndex < CodeObject.Position.endTokenNumber, "No open parenthesis in the columns definition.");
                #endregion // Find the open parenthesis

                #region Process tokens before the open parenthesis
                for (int i = nextToken; i < openParenIndex; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the open parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process tokens before the open parenthesis

                #region Process open parenthesis
                // if there was no whitespace before the parenthesis to be converted into a newline, then append a newline
                if (nextToken >= openParenIndex
                    || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[openParenIndex - 1].TokenId))
                {
                    td = TokenManager.TokenList[openParenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                Visitor.Context.ProcessTokenRange(openParenIndex, openParenIndex + 1);
                IncrementIndentLevel();

                nextToken = openParenIndex + 1;
                Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "Unexpected end of View Definition after open parenthesis in the columns definition.");

                // Ensure a newline after the open parenthesis
                if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
                {
                    td = TokenManager.TokenList[nextToken];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process open parenthesis

                #region Process tokens before the columns
                // find where the columns start
                IEnumerator<SqlIdentifier> columnEnum = CodeObject.ColumnList.GetEnumerator();
                Debug.Assert(columnEnum.MoveNext(), "The list of columns is empty.");
                for (int i = nextToken; i < columnEnum.Current.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" after the open parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion

                #region Process columns
                ProcessChild(columnEnum.Current);
                SqlIdentifier previousColumn = columnEnum.Current;
                while (columnEnum.MoveNext())
                {
                    CommaSeparatedList.ProcessInterChildRegion(previousColumn, columnEnum.Current);
                    ProcessChild(columnEnum.Current);
                    previousColumn = columnEnum.Current;
                }
                nextToken = previousColumn.Position.endTokenNumber;
                #endregion // Process columns

                #region Find closed parenthesis
                int closedParenIndex = nextToken;
                td = TokenManager.TokenList[closedParenIndex];
                while (td.TokenId != 41 && closedParenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(closedParenIndex, closedParenIndex + 1))
                     );
                    ++closedParenIndex;
                    td = TokenManager.TokenList[closedParenIndex];
                }
                Debug.Assert(closedParenIndex < CodeObject.Position.endTokenNumber, "No closing parenthesis after the columns definition.");
                #endregion // Find closed parenthesis

                #region Process region between columns and the closed parenthesis
                for (int i = nextToken; i < closedParenIndex - 1; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                DecrementIndentLevel();
                if (nextToken < closedParenIndex)
                {
                    SimpleProcessToken(closedParenIndex - 1, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }

                // Ensure a newline before the closing parenthesis

                td = TokenManager.TokenList[closedParenIndex - 1];
                if (!TokenManager.IsTokenWhitespace(td.TokenId))
                {
                    td = TokenManager.TokenList[closedParenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process region between columns and the closed parenthesis

                #region Process closed parenthesis
                Visitor.Context.ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
                nextToken = closedParenIndex + 1;
                #endregion // Process closed parenthesis
            }
            return nextToken;
        }

        private int ProcessOptions(int nextToken)
        {
            if (CodeObject.Options != null && CodeObject.Options.Count > 0)
            {
                #region Find the "WITH" token
                int withTokenIndex = nextToken;
                TokenData td = TokenManager.TokenList[withTokenIndex];
                while (td.TokenId != FormatterTokens.TOKEN_WITH && withTokenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", Visitor.Context.GetTokenRangeAsOriginalString(withTokenIndex, withTokenIndex + 1))
                     );
                    ++withTokenIndex;
                    td = TokenManager.TokenList[withTokenIndex];
                }
                Debug.Assert(withTokenIndex < CodeObject.Position.endTokenNumber , "No WITH token in the options definition.");
                #endregion // Find the "WITH" token

                #region Process the tokens before "WITH"
                for (int i = nextToken; i < withTokenIndex; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before "WITH"

                #region Process "WITH"
                if (nextToken >= withTokenIndex
                    || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[withTokenIndex - 1].TokenId))
                {
                    td = TokenManager.TokenList[withTokenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                Visitor.Context.ProcessTokenRange(withTokenIndex, withTokenIndex + 1);
                IncrementIndentLevel();

                nextToken = withTokenIndex + 1;
                Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View definition ends unexpectedly after the WITH token.");

                // Ensure a whitespace after the "WITH" token
                if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
                {
                    td = TokenManager.TokenList[nextToken];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process "WITH"

                #region Process the tokens before the options
                // find where the options start
                IEnumerator<SqlModuleOption> optionEnum = CodeObject.Options.GetEnumerator();
                Debug.Assert(optionEnum.MoveNext(), "Empty list of options.");
                for (int i = nextToken; i < optionEnum.Current.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" after the WITH token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before the options

                #region Process the options
                ProcessChild(optionEnum.Current);
                SqlModuleOption previousOption = optionEnum.Current;
                while (optionEnum.MoveNext())
                {
                    CommaSeparatedList.ProcessInterChildRegion(previousOption, optionEnum.Current);
                    ProcessChild(optionEnum.Current);
                    previousOption = optionEnum.Current;
                }
                nextToken = previousOption.Position.endTokenNumber;
                DecrementIndentLevel();
                #endregion // Process the options

            }
            return nextToken;
        }

        private int ProcessAsToken(int nextToken)
        {
            #region Find the "AS" token
            int asTokenIndex = nextToken;
            TokenData td = TokenManager.TokenList[asTokenIndex];
            while (td.TokenId != FormatterTokens.TOKEN_AS && asTokenIndex < CodeObject.Position.endTokenNumber)
            {
                Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", Visitor.Context.GetTokenRangeAsOriginalString(asTokenIndex, asTokenIndex + 1))
                     );
                ++asTokenIndex;
                td = TokenManager.TokenList[asTokenIndex];
            }
            Debug.Assert(asTokenIndex < CodeObject.Position.endTokenNumber, "No AS token.");
            #endregion // Find the "AS" token

            #region Process the tokens before the "AS" token
            for (int i = nextToken; i < asTokenIndex; i++)
            {
                Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process the tokens before the "AS" token

            #region Process the "AS" token
            if (nextToken >= asTokenIndex
                || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[asTokenIndex - 1].TokenId))
            {
                td = TokenManager.TokenList[asTokenIndex];
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            Visitor.Context.ProcessTokenRange(asTokenIndex, asTokenIndex + 1);
            IncrementIndentLevel();

            nextToken = asTokenIndex + 1;
            Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View Definition ends unexpectedly after the AS token.");
            // Ensure a newline after the "AS" token
            if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
            {
                td = TokenManager.TokenList[nextToken];
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            #endregion // Process the "AS" token
            return nextToken;
        }

        private int ProcessQueryWithClause(int nextToken)
        {
            if (CodeObject.QueryWithClause != null)
            {
                #region Process tokens before the query with clause
                for (int i = nextToken; i < CodeObject.QueryWithClause.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the query with clause.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process tokens before the query with clause

                #region Process query with clause
                ProcessChild(CodeObject.QueryWithClause);
                #endregion // Process query with clause
                nextToken = CodeObject.QueryWithClause.Position.endTokenNumber;
            }
            return nextToken;
        }

        private int ProcessQueryExpression(int nextToken)
        {
            #region Process tokens before the query expression
            for (int i = nextToken; i < CodeObject.QueryExpression.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the query expression.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process tokens before the query specification

            #region Process query expression
            ProcessChild(CodeObject.QueryExpression);
            nextToken = CodeObject.QueryExpression.Position.endTokenNumber;
            #endregion // Process query expression

            return nextToken;
        }        
    }
}
