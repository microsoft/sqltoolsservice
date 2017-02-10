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
            if (this.CodeObject.Left is SqlQuerySpecification) this.Visitor.Context.IncrementIndentLevel();

            // if the start token is not a whitespace, we need to insert the indent string
            if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[startTokenNumber].TokenId))
            {
                string newWhiteSpace = this.Visitor.Context.GetIndentString();
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[startTokenNumber];
                this.Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", newWhiteSpace));
            }

            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            if (firstChildStartTokenNumber - 1 >= startTokenNumber)
            {
                string newWhiteSpace = this.Visitor.Context.GetIndentString();

                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[firstChildStartTokenNumber - 1].TokenId))
                {
                    newWhiteSpace = Environment.NewLine + newWhiteSpace;
                }

                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[firstChildStartTokenNumber];
                this.Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", newWhiteSpace));
            }
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
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[i];
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
            if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[previousChild.Position.endTokenNumber].TokenId))
            {
                string newWhiteSpace = Environment.NewLine + this.Visitor.Context.GetIndentString();
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[previousChild.Position.endTokenNumber];
                this.Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", newWhiteSpace));
            }

            for (int i = previousChild.Position.endTokenNumber; i < operatorTokenNumber - 1; i++)
            {
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            if (this.CodeObject.Left is SqlQuerySpecification) this.Visitor.Context.DecrementIndentLevel();

            if (operatorTokenNumber - 1 >= previousChild.Position.endTokenNumber)
            {
                this.SimpleProcessToken(operatorTokenNumber - 1, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[operatorTokenNumber - 1].TokenId))
                {
                    string newWhiteSpace = Environment.NewLine + this.Visitor.Context.GetIndentString();
                    TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[operatorTokenNumber];
                    this.Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", newWhiteSpace));
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
                    if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_ALL)
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

            this.Visitor.Context.ProcessTokenRange(operatorTokenNumber, firstTokenAfterOperator);

            #endregion // Operator

            // process tokens after the operator:
            // [firstTokenAfterOperator, nextChild.Position.startTokenNumber)
            #region AfterOperator

            if (this.CodeObject.Right is SqlQuerySpecification) this.Visitor.Context.IncrementIndentLevel();

            // if the first token is not a whitespace, we need to insert a newline in front
            if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[firstTokenAfterOperator].TokenId))
            {
                string newWhiteSpace = Environment.NewLine + this.Visitor.Context.GetIndentString();
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[firstTokenAfterOperator];
                this.Visitor.Context.Replacements.Add(new Replacement(td.StartIndex, "", newWhiteSpace));
            }

            for (int i = firstTokenAfterOperator; i < nextChild.Position.startTokenNumber; i++)
            {
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            #endregion // AfterOperator

        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            if (this.CodeObject.Right is SqlQuerySpecification) this.Visitor.Context.DecrementIndentLevel();
            base.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }
    }
}
