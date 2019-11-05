//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Diagnostics;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlBinaryQueryExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlBinaryQueryExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlBinaryQueryExpression codeObject)
        {
            return new SqlBinaryQueryExpressionFormatter(visitor, codeObject);
        }
    }

    class SqlBinaryQueryExpressionFormatter : ASTNodeFormatterT<SqlBinaryQueryExpression>
    {

        internal SqlBinaryQueryExpressionFormatter(FormatterVisitor visitor, SqlBinaryQueryExpression codeObject)
            : base(visitor, codeObject)
        {
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            if (CodeObject.Left is SqlQuerySpecification)
            {
                IncrementIndentLevel();
            }

            // if the start token is not a whitespace, we need to insert the indent string
            TokenData td = GetTokenData(startTokenNumber);
            if (!IsTokenWhitespace(td))
            {
                string newWhiteSpace = GetIndentString();
                AddReplacement(new Replacement(td.StartIndex, string.Empty, newWhiteSpace));
            }

            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            if (firstChildStartTokenNumber - 1 >= startTokenNumber)
            {
                IndentChild(firstChildStartTokenNumber);
            }
        }

        private void IndentChild(int firstChildStartTokenNumber)
        {
            string newWhiteSpace = GetIndentString();

            if (!IsTokenWhitespace(PreviousTokenData(firstChildStartTokenNumber)))
            {
                newWhiteSpace = Environment.NewLine + newWhiteSpace;
            }

            TokenData td = GetTokenData(firstChildStartTokenNumber);
            AddReplacement(td.StartIndex, string.Empty, newWhiteSpace);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            // find the potition of the operator token:
            // operatorTokenNumber
            #region FindOperator

            // look for the expression type based on the operator and determine its type and position
            int binaryOperatorTokenID = FormatterTokens.LAST_TOKEN;
            bool foundOperator = false;
            int operatorTokenNumber = nextChild.Position.startTokenNumber;
            for (int i = previousChild.Position.endTokenNumber; !foundOperator && i < nextChild.Position.startTokenNumber; i++)
            {
                TokenData td = GetTokenData(i);
                if ( td.TokenId == FormatterTokens.TOKEN_UNION ||
                     td.TokenId == FormatterTokens.TOKEN_INTERSECT ||
                     td.TokenId == FormatterTokens.TOKEN_EXCEPT )
                {
                        foundOperator = true;
                        binaryOperatorTokenID = td.TokenId;
                        operatorTokenNumber = i;
                }
            }

            // check that we actually found one
            Debug.Assert(foundOperator);
            // if we found the operator, it means we also know its position number.
            Debug.Assert(operatorTokenNumber >= previousChild.Position.endTokenNumber);
            Debug.Assert(operatorTokenNumber < nextChild.Position.startTokenNumber);
            // and we know its type
            Debug.Assert(
                binaryOperatorTokenID == FormatterTokens.TOKEN_UNION ||
                binaryOperatorTokenID == FormatterTokens.TOKEN_INTERSECT ||
                binaryOperatorTokenID == FormatterTokens.TOKEN_EXCEPT);
            #endregion

            // process the tokens before the operator:
            // [lastChild.Position.endTokenNumber, operatorTokenNumber)
            #region BeforeOperator

            // If the first token is not a whitespace and it, we need to insert a newline in front
            TokenData endTokenData = GetTokenData(previousChild.Position.endTokenNumber);
            if (!IsTokenWhitespace(endTokenData))
            {
                AddIndentedNewLineReplacement(endTokenData.StartIndex);
            }

            for (int i = previousChild.Position.endTokenNumber; i < operatorTokenNumber - 1; i++)
            {
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            if (CodeObject.Left is SqlQuerySpecification)
            {
                DecrementIndentLevel();
            }

            if (operatorTokenNumber - 1 >= previousChild.Position.endTokenNumber)
            {
                SimpleProcessToken(operatorTokenNumber - 1, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                if (!IsTokenWhitespace(PreviousTokenData(operatorTokenNumber)))
                {
                    TokenData td = GetTokenData(operatorTokenNumber);
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
            }
            
            #endregion // BeforeOperator

            // process the operator:
            // [operatorTokenNumber, firstTokenAfterOperator)
            #region Operator

            // since the operator might contain more than one token, we will keep track of its end
            int firstTokenAfterOperator = operatorTokenNumber + 1;

            // find where the operator ends
            if (binaryOperatorTokenID == FormatterTokens.TOKEN_UNION)
            {
                // the union operator may or may not be followed by the "ALL" modifier, so it might span over a number of tokens
                bool foundModifier = false;
                int modifierTokenNumber = nextChild.Position.startTokenNumber;

                for (int i = operatorTokenNumber; !foundModifier && i < nextChild.Position.startTokenNumber; i++)
                {
                    if (GetTokenData(i).TokenId == FormatterTokens.TOKEN_ALL)
                    {
                        foundModifier = true;
                        modifierTokenNumber = i;
                    }
                }

                if (foundModifier)
                {
                    // leave everythong between "UNION" and "ALL" just as it was, but format the keywords
                    firstTokenAfterOperator = modifierTokenNumber + 1;
                }
            }
            else
            {
                // only format the operator
                firstTokenAfterOperator = operatorTokenNumber + 1;
            }

            ProcessTokenRange(operatorTokenNumber, firstTokenAfterOperator);

            #endregion // Operator

            // process tokens after the operator:
            // [firstTokenAfterOperator, nextChild.Position.startTokenNumber)
            #region AfterOperator

            if (CodeObject.Right is SqlQuerySpecification)
            {
                IncrementIndentLevel();
            }

            // if the first token is not a whitespace, we need to insert a newline in front
            if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[firstTokenAfterOperator].TokenId))
            {
                TokenData td = GetTokenData(firstTokenAfterOperator);
                AddIndentedNewLineReplacement(td.StartIndex);
            }

            for (int i = firstTokenAfterOperator; i < nextChild.Position.startTokenNumber; i++)
            {
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            #endregion // AfterOperator

        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            if (CodeObject.Right is SqlQuerySpecification)
            {
                DecrementIndentLevel();
            }
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }
    }
}
