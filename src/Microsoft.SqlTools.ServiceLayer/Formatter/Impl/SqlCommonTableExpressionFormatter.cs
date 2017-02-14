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
            CommaSeparatedList = new CommaSeparatedListFormatter(Visitor, CodeObject, FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            IncrementIndentLevel();
            base.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            DecrementIndentLevel();
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        public override void Format()
        {
            int nextToken = ProcessExpressionName(CodeObject.Position.startTokenNumber);

            nextToken = ProcessColumns(nextToken);

            nextToken = ProcessAsToken(nextToken);
            
            nextToken = ProcessQueryExpression(nextToken);

        }

        private int ProcessQueryExpression(int nextToken)
        {
            int openParenIndex = FindOpenParenthesis(nextToken);
            ProcessAndNormalizeTokenRange(nextToken, openParenIndex, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            nextToken = ProcessOpenParenthesis(nextToken, openParenIndex, isNewlineRequired: true);

            ProcessTokenRangeEnsuringOneNewLineMinumum(nextToken, CodeObject.QueryExpression.Position.startTokenNumber);

            ProcessChild(CodeObject.QueryExpression);
            nextToken = CodeObject.QueryExpression.Position.endTokenNumber;

            int closedParenIndex = FindClosedParenthesis(nextToken);
            
            ProcessRegionAfterQueryExpression(nextToken, closedParenIndex);
            
            // Process closed parenthesis
            ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
            nextToken = closedParenIndex + 1;

            return nextToken;
        }

        private int FindOpenParenthesis(int openParenIndex)
        {
            return FindTokenWithId(openParenIndex, 40);
        }

        /// <summary>
        /// if there was no whitespace before the parenthesis to be converted into a newline, 
        /// and the references need to be on a newline, then append a newline
        /// </summary>
        private int ProcessOpenParenthesis(int nextToken, int openParenIndex, bool isNewlineRequired)
        {
            // if a newline is required and there was no whitespace before the parenthesis to be 
            // converted into a newline, then append a newline
            
            if (isNewlineRequired
                && (nextToken >= openParenIndex
                    || !IsTokenWhitespace(PreviousTokenData(openParenIndex))))
            {
                // Note: nextToken index value does not match the Startindex of the TokenData. When adding
                // indentation, always get the TokenData and its StartIndex value
                TokenData td = GetTokenData(openParenIndex);
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            ProcessTokenRange(openParenIndex, openParenIndex + 1);
            IncrementIndentLevel();

            // Move our pointer past the parenthesis
            nextToken = openParenIndex + 1;
            TokenData nextTokenData = GetTokenData(nextToken);
            Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "Unexpected end of View Definition after open parenthesis in the columns definition.");

            // Ensure a newline after the open parenthesis
            if (isNewlineRequired
                && !IsTokenWhitespace(nextTokenData))
            {
                AddIndentedNewLineReplacement(nextTokenData.StartIndex);
            }
            return nextToken;
        }
        
        private int FindClosedParenthesis(int nextToken)
        {
            return FindTokenWithId(nextToken, 41);
        }

        private void ProcessRegionAfterQueryExpression(int startIndex, int closedParenIndex)
        {
            ProcessRegionBeforeClosedParenthesis(startIndex, closedParenIndex,
                FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum, 
                isNewlineRequired: true);
        }

        private void ProcessRegionBeforeClosedParenthesis(int startIndex, int closedParenIndex, NormalizeWhitespace normalizer, bool isNewlineRequired)
        {
            for (int i = startIndex; i < closedParenIndex - 1; i++)
            {
                ProcessTokenAndNormalize(i, normalizer);
            }
            DecrementIndentLevel();
            if (startIndex < closedParenIndex)
            {
                SimpleProcessToken(closedParenIndex - 1, normalizer);
            }

            // Enforce a whitespace before the closing parenthesis
            TokenData td = PreviousTokenData(closedParenIndex);
            if (isNewlineRequired && !IsTokenWhitespace(td))
            {
                td = GetTokenData(closedParenIndex);
                AddIndentedNewLineReplacement(td.StartIndex);
            }
        }

        private int FindTokenWithId(int tokenIndex, int id)
        {
            TokenData td = GetTokenData(tokenIndex);
            while (td.TokenId != id && tokenIndex < CodeObject.Position.endTokenNumber)
            {
                DebugAssertTokenIsWhitespaceOrComment(td, tokenIndex);
                ++tokenIndex;
                td = GetTokenData(tokenIndex);
            }
            Debug.Assert(tokenIndex < CodeObject.Position.endTokenNumber, "No token with ID" + id + " found in the columns definition.");
            return tokenIndex;
        }

        private void ProcessTokenEnsuringOneNewLineMinimum(int tokenIndex)
        {
            ProcessTokenAndNormalize(tokenIndex, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
        }


        private int ProcessAsToken(int nextToken)
        {
            int asTokenIndex = FindTokenWithId(nextToken, FormatterTokens.TOKEN_AS);

            // Preprocess
            ProcessTokenRangeEnsuringOneNewLineMinumum(nextToken, asTokenIndex);

            // Process As
            if (nextToken >= asTokenIndex
                || !IsTokenWhitespace(PreviousTokenData(asTokenIndex)))
            {
                TokenData td = GetTokenData(asTokenIndex);
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            ProcessTokenRange(asTokenIndex, asTokenIndex + 1);

            // Post Process
            nextToken = EnsureWhitespaceAfterAs(asTokenIndex);

            return nextToken;
        }

        private int EnsureWhitespaceAfterAs(int asTokenIndex)
        {
            int nextToken = asTokenIndex + 1;
            Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View definition ends unexpectedly after the AS token.");

            TokenData nextTokenData = GetTokenData(nextToken);
            // Ensure a whitespace after the "AS" token
            if (!IsTokenWhitespace(nextTokenData))
            {
                AddIndentedNewLineReplacement(nextTokenData.StartIndex);
            }

            return nextToken;
        }
        
        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.ColumnList != null && CodeObject.ColumnList.Count > 0)
            {
                NormalizeWhitespace normalizer = GetColumnWhitespaceNormalizer();

                int openParenIndex = FindOpenParenthesis(nextToken);

                // Process up to the open parenthesis
                ProcessAndNormalizeTokenRange(nextToken, openParenIndex, normalizer);

                nextToken = ProcessOpenParenthesis(nextToken, openParenIndex, isNewlineRequired: FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements);

                // find where the columns start, and process everything before it
                IEnumerator<SqlIdentifier> columnEnum = FindColumnStart();
                if (columnEnum.Current != null)
                {
                    ProcessAndNormalizeTokenRange(nextToken, columnEnum.Current.Position.startTokenNumber, normalizer);
                    nextToken = ProcessColumnList(columnEnum);
                }

                int closedParenIndex = FindClosedParenthesis(nextToken);

                // Process region between columns and the closed parenthesis
                ProcessRegionBeforeClosedParenthesis(nextToken, closedParenIndex, normalizer,
                    isNewlineRequired: FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements);
                
                // Process closed parenthesis
                ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
                nextToken = closedParenIndex + 1;
            }

            return nextToken;
        }
        
        private int ProcessColumnList(IEnumerator<SqlIdentifier> columnEnum)
        {
            int nextToken;
            ProcessChild(columnEnum.Current);
            SqlIdentifier previousColumn = columnEnum.Current;
            while (columnEnum.MoveNext())
            {
                CommaSeparatedList.ProcessInterChildRegion(previousColumn, columnEnum.Current);
                ProcessChild(columnEnum.Current);
                previousColumn = columnEnum.Current;
            }
            nextToken = previousColumn.Position.endTokenNumber;
            return nextToken;
        }

        /// <summary>
        /// Moves to the first column index
        /// </summary>
        private IEnumerator<SqlIdentifier> FindColumnStart()
        {
            IEnumerator<SqlIdentifier> columnEnum = CodeObject.ColumnList.GetEnumerator();
            bool hasColumns = columnEnum.MoveNext();
            Debug.Assert(hasColumns, "The list of columns is empty.");
            return columnEnum;
        }

        private NormalizeWhitespace GetColumnWhitespaceNormalizer()
        {
            if (FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements)
            {
                return FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            }
            return FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
        }

        private int ProcessExpressionName(int nextToken)
        {
            SqlIdentifier name = CodeObject.Name;
            for (int i = nextToken; i < name.Position.startTokenNumber; i++)
            {
                ProcessTokenEnsuringOneNewLineMinimum(i);
            }
            
            ProcessTokenRange(name.Position.startTokenNumber, name.Position.endTokenNumber);
            
            nextToken = name.Position.endTokenNumber;

            return nextToken;
        }
    }
}
