//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    /// <summary>
    /// Common base class for objects dealing with sys comments. These follow
    /// similar patterns so identical methods are held here
    /// </summary>
    internal abstract class SysCommentsFormatterBase<T> : ASTNodeFormatterT<T>
        where T : SqlCodeObject
    {

        internal CommaSeparatedListFormatter CommaSeparatedList { get; set; }

        public SysCommentsFormatterBase(FormatterVisitor visitor, T codeObject)
            : base(visitor, codeObject)
        {
            CommaSeparatedList = new CommaSeparatedListFormatter(Visitor, CodeObject, ShouldPlaceEachElementOnNewLine());
        }

        protected abstract bool ShouldPlaceEachElementOnNewLine();

        protected void ProcessTokenEnsuringOneNewLineMinimum(int tokenIndex)
        {
            ProcessTokenAndNormalize(tokenIndex, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
        }


        /// <summary>
        /// processes any section in a query, since the basic behavior is constant
        /// </summary>
        protected int ProcessQuerySection(int nextToken, SqlCodeObject queryObject)
        {
            if (queryObject != null)
            {
                ProcessAndNormalizeWhitespaceRange(nextToken, queryObject.Position.startTokenNumber,
                    FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                ProcessChild(queryObject);
                nextToken = queryObject.Position.endTokenNumber;
            }
            return nextToken;
        }

        protected int ProcessSectionInsideParentheses(int nextToken, NormalizeWhitespace normalizer, bool isNewlineRequired,
            Func<int, int> processSection)
        {
            int openParenIndex = FindOpenParenthesis(nextToken);
            ProcessAndNormalizeWhitespaceRange(nextToken, openParenIndex, normalizer);
            nextToken = ProcessOpenParenthesis(nextToken, openParenIndex, isNewlineRequired);

            nextToken = processSection(nextToken);

            int closedParenIndex = FindClosedParenthesis(nextToken);

            ProcessRegionBeforeClosedParenthesis(nextToken, closedParenIndex, normalizer, isNewlineRequired);

            // Process closed parenthesis
            ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
            nextToken = closedParenIndex + 1;

            return nextToken;
        }

        protected int FindOpenParenthesis(int openParenIndex)
        {
            return FindTokenWithId(openParenIndex, 40);
        }

        protected int FindClosedParenthesis(int nextToken)
        {
            return FindTokenWithId(nextToken, 41);
        }

        /// <summary>
        /// if there was no whitespace before the parenthesis to be converted into a newline, 
        /// and the references need to be on a newline, then append a newline
        /// </summary>
        protected int ProcessOpenParenthesis(int nextToken, int openParenIndex, bool isNewlineRequired)
        {
            return ProcessCompoundStatementStart(ref nextToken, openParenIndex, isNewlineRequired);
        }
        
        protected int ProcessWithStatementStart(int nextToken, int withTokenIndex)
        {
            return ProcessCompoundStatementStart(ref nextToken, withTokenIndex, true);
        }

        protected int ProcessCompoundStatementStart(ref int nextToken, int compoundStartIndex, bool isNewlineRequired)
        {
            // if a newline is required and there was no whitespace before the start to be 
            // converted into a newline, then append a newline
            if (isNewlineRequired
                && (nextToken >= compoundStartIndex
                    || !IsTokenWhitespace(PreviousTokenData(compoundStartIndex))))
            {
                // Note: nextToken index value does not match the Startindex of the TokenData. When adding
                // indentation, always get the TokenData and its StartIndex value
                TokenData td = GetTokenData(compoundStartIndex);
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            ProcessTokenRange(compoundStartIndex, compoundStartIndex + 1);
            IncrementIndentLevel();

            // Move our pointer past the start of the compount statement
            nextToken = compoundStartIndex + 1;
            TokenData nextTokenData = GetTokenData(nextToken);
            
            // Ensure a newline after the open parenthesis
            if (isNewlineRequired
                && !IsTokenWhitespace(nextTokenData))
            {
                AddIndentedNewLineReplacement(nextTokenData.StartIndex);
            }
            return nextToken;
        }

        protected void ProcessRegionBeforeClosedParenthesis(int startIndex, int closedParenIndex, NormalizeWhitespace normalizer, bool isNewlineRequired)
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

        protected int ProcessColumnList(int nextToken, SqlIdentifierCollection columnList, NormalizeWhitespace normalizer)
        {
            // find where the columns start
            IEnumerator<SqlIdentifier> columnEnum = columnList.GetEnumerator();
            if (columnEnum.MoveNext())
            {
                ProcessAndNormalizeWhitespaceRange(nextToken, columnEnum.Current.Position.startTokenNumber, normalizer);

                ProcessChild(columnEnum.Current);
                SqlIdentifier previousColumn = columnEnum.Current;
                while (columnEnum.MoveNext())
                {
                    CommaSeparatedList.ProcessInterChildRegion(previousColumn, columnEnum.Current);
                    ProcessChild(columnEnum.Current);
                    previousColumn = columnEnum.Current;
                }
                nextToken = previousColumn.Position.endTokenNumber;
            }
            return nextToken;
        }

        protected int ProcessAsToken(int nextToken, bool indentAfterAs)
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

            if (indentAfterAs)
            {
                IncrementIndentLevel();
            }
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

    }
}
