//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlCommonTableExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlCommonTableExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlCommonTableExpression codeObject)
        {
            return new SqlCommonTableExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlCommonTableExpressionFormatter : ASTNodeFormatterT<SqlCommonTableExpression>
    {

        internal CommaSeparatedListFormatter CommaSeparatedList { get; set; }

        public SqlCommonTableExpressionFormatter(FormatterVisitor visitor, SqlCommonTableExpression codeObject)
            : base(visitor, codeObject)
        {
            CommaSeparatedList = new CommaSeparatedListFormatter(this.Visitor, this.CodeObject, this.Visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            this.Visitor.Context.IncrementIndentLevel();
            base.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            this.Visitor.Context.DecrementIndentLevel();
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        public override void Format()
        {
            int nextToken = this.CodeObject.Position.startTokenNumber;

            nextToken = ProcessExpressionName(nextToken);

            nextToken = ProcessColumns(nextToken);

            nextToken = ProcessAsToken(nextToken);
            
            nextToken = ProcessQueryExpression(nextToken);

        }

        private int ProcessQueryExpression(int nextToken)
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
            // if there was no whitespace before the parenthesis to be converted into a newline, and the references need to be on a newline, then append a newline
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

            #region Process tokens before the query expression
            for (int i = nextToken; i < this.CodeObject.QueryExpression.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                    this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                 || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                 , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" after the open parenthesis.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                 );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process tokens before the query expression

            #region Process query expression
            this.ProcessChild(this.CodeObject.QueryExpression);
            nextToken = this.CodeObject.QueryExpression.Position.endTokenNumber;
            #endregion // Process query expression

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

            #region Process region between query expression and the closed parenthesis
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

            // Enforce a whitespace before the closing parenthesis
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
            #endregion // Process region between query expression and the closed parenthesis

            #region Process closed parenthesis
            this.Visitor.Context.ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
            nextToken = closedParenIndex + 1;
            #endregion // Process closed parenthesis

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
            Debug.Assert(asTokenIndex < this.CodeObject.Position.endTokenNumber, "No AS token in the options definition.");
            #endregion // Find the "AS" token

            #region Process the tokens before "AS"
            for (int i = nextToken; i < asTokenIndex; i++)
            {
                Debug.Assert(
                    this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                 || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                 , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                 );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process the tokens before "AS"

            #region Process "AS"
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

            nextToken = asTokenIndex + 1;
            Debug.Assert(nextToken < this.CodeObject.Position.endTokenNumber, "View definition ends unexpectedly after the AS token.");

            // Ensure a whitespace after the "AS" token
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
            #endregion // Process "AS"

            return nextToken;
        }

        private int ProcessColumns(int nextToken)
        {
            if (this.CodeObject.ColumnList != null && this.CodeObject.ColumnList.Count > 0)
            {
                NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                if (this.Visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements)
                {
                    f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
                }

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
                    this.SimpleProcessToken(i, f);
                }
                #endregion // Process tokens before the open parenthesis

                #region Process open parenthesis
                // if a newline is required and there was no whitespace before the parenthesis to be converted into a newline, then append a newline
                if (this.Visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements
                    && (  nextToken >= openParenIndex
                       || !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[openParenIndex - 1].TokenId)))
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

                // If needed, ensure a newline after the open parenthesis
                if (    this.Visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements
                    && !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
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
                    this.SimpleProcessToken(i, f);
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
                    this.SimpleProcessToken(i, f);
                }
                this.Visitor.Context.DecrementIndentLevel();
                if (nextToken < closedParenIndex)
                {
                    this.SimpleProcessToken(closedParenIndex - 1, f);
                }

                // If needed, ensure a newline before the closing parenthesis

                td = this.Visitor.Context.Script.TokenManager.TokenList[closedParenIndex - 1];
                if (    this.Visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements
                    && !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId))
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

        private int ProcessExpressionName(int nextToken)
        {
            #region Process the tokens preceeding the expression name
            SqlIdentifier name = this.CodeObject.Name;
            for (int i = nextToken; i < name.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the expression name.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );

                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
            }
            #endregion // Process the tokens preceeding the expression name

            #region Process the expression name
            this.Visitor.Context.ProcessTokenRange(name.Position.startTokenNumber, name.Position.endTokenNumber);
            #endregion // Process the expression name
            
            nextToken = name.Position.endTokenNumber;

            return nextToken;
        }


    }
}
