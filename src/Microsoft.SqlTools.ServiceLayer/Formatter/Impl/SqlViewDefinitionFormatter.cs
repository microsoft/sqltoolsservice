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
            CommaSeparatedList = new CommaSeparatedListFormatter(this.Visitor, this.CodeObject, true);
        }

        public override void Format()
        {
            LexLocation loc = this.CodeObject.Position;

            SqlCodeObject firstChild = this.CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of its first child
                //
                LexLocation firstChildStart = firstChild.Position;
                this.ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                this.ProcessChild(firstChild);

                // keep track of the next token to process
                int nextToken = firstChildStart.endTokenNumber;

                // process the columns if available
                nextToken = this.ProcessColumns(nextToken);

                // process options if available
                nextToken = this.ProcessOptions(nextToken);

                // process the region containing the AS token
                nextToken = this.ProcessAsToken(nextToken);

                // process the query with clause if present
                nextToken = this.ProcessQueryWithClause(nextToken);

                // process the query expression
                nextToken = this.ProcessQueryExpression(nextToken);

                this.Visitor.Context.DecrementIndentLevel();

                #region format text from end of last child to end of object.
                //
                SqlCodeObject lastChild = this.CodeObject.Children.LastOrDefault();
                Debug.Assert(lastChild != null, "last child is null.  Need to write code to deal with this case");
                this.ProcessSuffixRegion(lastChild.Position.endTokenNumber, loc.endTokenNumber);
                #endregion
            }
            else
            {
                // no children
                this.Visitor.Context.ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
            }
        }

        private int ProcessColumns(int nextToken)
        {
            if (this.CodeObject.ColumnList != null && this.CodeObject.ColumnList.Count > 0)
            {
                #region Find the open parenthesis
                int openParenIndex = nextToken;

                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[openParenIndex];
                while (td.TokenId != 40 && openParenIndex < this.CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(td.TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the open parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(openParenIndex, openParenIndex + 1))
                     );
                    ++openParenIndex;
                    td = this.Visitor.Context.Script.TokenManager.TokenList[openParenIndex];
                }
                Debug.Assert(openParenIndex < this.CodeObject.Position.endTokenNumber, "No open parenthesis in the columns definition.");
                #endregion // Find the open parenthesis

                #region Process tokens before the open parenthesis
                for (int i = nextToken; i < openParenIndex; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the open parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process tokens before the open parenthesis

                #region Process open parenthesis
                // if there was no whitespace before the parenthesis to be converted into a newline, then append a newline
                if (nextToken >= openParenIndex
                    || !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[openParenIndex - 1].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[openParenIndex];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                this.Visitor.Context.ProcessTokenRange(openParenIndex, openParenIndex + 1);
                this.Visitor.Context.IncrementIndentLevel();

                nextToken = openParenIndex + 1;
                Debug.Assert(nextToken < this.CodeObject.Position.endTokenNumber, "Unexpected end of View Definition after open parenthesis in the columns definition.");

                // Ensure a newline after the open parenthesis
                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[nextToken];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                #endregion // Process open parenthesis

                #region Process tokens before the columns
                // find where the columns start
                IEnumerator<SqlIdentifier> columnEnum = this.CodeObject.ColumnList.GetEnumerator();
                Debug.Assert(columnEnum.MoveNext(), "The list of columns is empty.");
                for (int i = nextToken; i < columnEnum.Current.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" after the open parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion

                #region Process columns
                this.ProcessChild(columnEnum.Current);
                SqlIdentifier previousColumn = columnEnum.Current;
                while (columnEnum.MoveNext())
                {
                    this.CommaSeparatedList.ProcessInterChildRegion(previousColumn, columnEnum.Current);
                    this.ProcessChild(columnEnum.Current);
                    previousColumn = columnEnum.Current;
                }
                nextToken = previousColumn.Position.endTokenNumber;
                #endregion // Process columns

                #region Find closed parenthesis
                int closedParenIndex = nextToken;
                td = this.Visitor.Context.Script.TokenManager.TokenList[closedParenIndex];
                while (td.TokenId != 41 && closedParenIndex < this.CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(td.TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(closedParenIndex, closedParenIndex + 1))
                     );
                    ++closedParenIndex;
                    td = this.Visitor.Context.Script.TokenManager.TokenList[closedParenIndex];
                }
                Debug.Assert(closedParenIndex < this.CodeObject.Position.endTokenNumber, "No closing parenthesis after the columns definition.");
                #endregion // Find closed parenthesis

                #region Process region between columns and the closed parenthesis
                for (int i = nextToken; i < closedParenIndex - 1; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                this.Visitor.Context.DecrementIndentLevel();
                if (nextToken < closedParenIndex)
                {
                    this.SimpleProcessToken(closedParenIndex - 1, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }

                // Ensure a newline before the closing parenthesis

                td = this.Visitor.Context.Script.TokenManager.TokenList[closedParenIndex - 1];
                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[closedParenIndex];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                #endregion // Process region between columns and the closed parenthesis

                #region Process closed parenthesis
                this.Visitor.Context.ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
                nextToken = closedParenIndex + 1;
                #endregion // Process closed parenthesis
            }
            return nextToken;
        }

        private int ProcessOptions(int nextToken)
        {
            if (this.CodeObject.Options != null && this.CodeObject.Options.Count > 0)
            {
                #region Find the "WITH" token
                int withTokenIndex = nextToken;
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[withTokenIndex];
                while (td.TokenId != FormatterTokens.TOKEN_WITH && withTokenIndex < this.CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(td.TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", this.Visitor.Context.GetTokenRangeAsOriginalString(withTokenIndex, withTokenIndex + 1))
                     );
                    ++withTokenIndex;
                    td = this.Visitor.Context.Script.TokenManager.TokenList[withTokenIndex];
                }
                Debug.Assert(withTokenIndex < this.CodeObject.Position.endTokenNumber , "No WITH token in the options definition.");
                #endregion // Find the "WITH" token

                #region Process the tokens before "WITH"
                for (int i = nextToken; i < withTokenIndex; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before "WITH"

                #region Process "WITH"
                if (nextToken >= withTokenIndex
                    || !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[withTokenIndex - 1].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[withTokenIndex];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                this.Visitor.Context.ProcessTokenRange(withTokenIndex, withTokenIndex + 1);
                this.Visitor.Context.IncrementIndentLevel();

                nextToken = withTokenIndex + 1;
                Debug.Assert(nextToken < this.CodeObject.Position.endTokenNumber, "View definition ends unexpectedly after the WITH token.");

                // Ensure a whitespace after the "WITH" token
                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[nextToken];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                #endregion // Process "WITH"

                #region Process the tokens before the options
                // find where the options start
                IEnumerator<SqlModuleOption> optionEnum = this.CodeObject.Options.GetEnumerator();
                Debug.Assert(optionEnum.MoveNext(), "Empty list of options.");
                for (int i = nextToken; i < optionEnum.Current.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" after the WITH token.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before the options

                #region Process the options
                this.ProcessChild(optionEnum.Current);
                SqlModuleOption previousOption = optionEnum.Current;
                while (optionEnum.MoveNext())
                {
                    this.CommaSeparatedList.ProcessInterChildRegion(previousOption, optionEnum.Current);
                    this.ProcessChild(optionEnum.Current);
                    previousOption = optionEnum.Current;
                }
                nextToken = previousOption.Position.endTokenNumber;
                this.Visitor.Context.DecrementIndentLevel();
                #endregion // Process the options

            }
            return nextToken;
        }

        private int ProcessAsToken(int nextToken)
        {
            #region Find the "AS" token
            int asTokenIndex = nextToken;
            TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[asTokenIndex];
            while (td.TokenId != FormatterTokens.TOKEN_AS && asTokenIndex < this.CodeObject.Position.endTokenNumber)
            {
                Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(td.TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", this.Visitor.Context.GetTokenRangeAsOriginalString(asTokenIndex, asTokenIndex + 1))
                     );
                ++asTokenIndex;
                td = this.Visitor.Context.Script.TokenManager.TokenList[asTokenIndex];
            }
            Debug.Assert(asTokenIndex < this.CodeObject.Position.endTokenNumber, "No AS token.");
            #endregion // Find the "AS" token

            #region Process the tokens before the "AS" token
            for (int i = nextToken; i < asTokenIndex; i++)
            {
                Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process the tokens before the "AS" token

            #region Process the "AS" token
            if (nextToken >= asTokenIndex
                || !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[asTokenIndex - 1].TokenId))
            {
                td = this.Visitor.Context.Script.TokenManager.TokenList[asTokenIndex];
                this.Visitor.Context.Replacements.Add(
                    new Replacement(
                        td.StartIndex,
                        "",
                        Environment.NewLine + this.Visitor.Context.GetIndentString()
                        )
                    );
            }
            this.Visitor.Context.ProcessTokenRange(asTokenIndex, asTokenIndex + 1);
            this.Visitor.Context.IncrementIndentLevel();

            nextToken = asTokenIndex + 1;
            Debug.Assert(nextToken < this.CodeObject.Position.endTokenNumber, "View Definition ends unexpectedly after the AS token.");
            // Ensure a newline after the "AS" token
            if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
            {
                td = this.Visitor.Context.Script.TokenManager.TokenList[nextToken];
                this.Visitor.Context.Replacements.Add(
                    new Replacement(
                        td.StartIndex,
                        "",
                        Environment.NewLine + this.Visitor.Context.GetIndentString()
                        )
                    );
            }
            #endregion // Process the "AS" token
            return nextToken;
        }

        private int ProcessQueryWithClause(int nextToken)
        {
            if (this.CodeObject.QueryWithClause != null)
            {
                #region Process tokens before the query with clause
                for (int i = nextToken; i < this.CodeObject.QueryWithClause.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the query with clause.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process tokens before the query with clause

                #region Process query with clause
                this.ProcessChild(this.CodeObject.QueryWithClause);
                #endregion // Process query with clause
                nextToken = this.CodeObject.QueryWithClause.Position.endTokenNumber;
            }
            return nextToken;
        }

        private int ProcessQueryExpression(int nextToken)
        {
            #region Process tokens before the query expression
            for (int i = nextToken; i < this.CodeObject.QueryExpression.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the query expression.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process tokens before the query specification

            #region Process query expression
            this.ProcessChild(this.CodeObject.QueryExpression);
            nextToken = this.CodeObject.QueryExpression.Position.endTokenNumber;
            #endregion // Process query expression

            return nextToken;
        }        
    }
}
