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
    internal class SqlInsertSpecificationFormatterFactory : ASTNodeFormatterFactoryT<SqlInsertSpecification>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlInsertSpecification codeObject)
        {
            return new SqlInsertSpecificationFormatter(visitor, codeObject);
        }
    }

    class SqlInsertSpecificationFormatter : ASTNodeFormatterT<SqlInsertSpecification>
    {
        internal SqlInsertSpecificationFormatter(FormatterVisitor visitor, SqlInsertSpecification codeObject)
            : base(visitor, codeObject)
        {
        }

        public override void Format()
        {

            IEnumerator<SqlCodeObject> firstChildEnum = CodeObject.Children.GetEnumerator();
            if (firstChildEnum.MoveNext())
            {
                //
                // format the text from the start of the object to the start of it's first child
                // 
                ProcessPrefixRegion(CodeObject.Position.startTokenNumber, firstChildEnum.Current.Position.startTokenNumber);
                int nextToken = firstChildEnum.Current.Position.startTokenNumber;

                // handle top specification
                nextToken = ProcessTopSpecification(nextToken);

                // handle target
                nextToken = ProcessTarget(nextToken);

                // handle target columns
                nextToken = ProcessColumns(nextToken);

                // handle output clause
                nextToken = ProcessOutputClause(nextToken);

                // handle values / derived table / execute statement / dml_table_source
                nextToken = ProcessValues(nextToken);
            }
            else
            {
                throw new FormatFailedException("Insert statement has no children.");
            }
        }


        internal int ProcessTopSpecification(int nextToken)
        {
            if (CodeObject.TopSpecification != null)
            {
                for (int i = nextToken; i < CodeObject.TopSpecification.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                       TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                    || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the top specification.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                    );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
                }

                ProcessChild(CodeObject.TopSpecification);

                nextToken = CodeObject.TopSpecification.Position.endTokenNumber;
            }

            return nextToken;
        }


        private int ProcessTarget(int nextToken)
        {
            Debug.Assert(CodeObject.Target != null, "No target in insert statement.");

            // find out if there is an "INTO" token
            int intoTokenIndexOrTargetStartTokenIndex = CodeObject.Target.Position.startTokenNumber;
            for (int i = nextToken; i < CodeObject.Target.Position.startTokenNumber; i++)
            {
                if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_INTO)
                {
                    intoTokenIndexOrTargetStartTokenIndex = i;
                }
            }

            for (int i = nextToken; i < intoTokenIndexOrTargetStartTokenIndex; i++)
            {
                Debug.Assert(
                    TokenManager.IsTokenComment(TokenManager.TokenList[nextToken].TokenId)
                    || TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the target.", Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                    );
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            IncrementIndentLevel();

            for (int i = intoTokenIndexOrTargetStartTokenIndex ; i < CodeObject.Target.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                    TokenManager.IsTokenComment(TokenManager.TokenList[nextToken].TokenId)
                    || TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the target.", Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                    );
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
            }

            ProcessChild(CodeObject.Target);

            nextToken = CodeObject.Target.Position.endTokenNumber;
            DecrementIndentLevel();

            return nextToken;
        }

        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.TargetColumns != null)
            {
                if (CodeObject.TargetColumns.Count > 0)
                {
                    IncrementIndentLevel();

                    // if the next token is not a whitespace, a newline is enforced.
                    TokenData nextTokenData = GetTokenData(nextToken);
                    if (!IsTokenWhitespace(nextTokenData))
                    {
                        AddIndentedNewLineReplacement(nextTokenData.StartIndex);
                    }

                    NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;

                    // process tokens until we reach the closed parenthesis (with id 41)
                    for (int id = TokenManager.TokenList[nextToken].TokenId; id != 41; id = TokenManager.TokenList[++nextToken].TokenId)
                    {
                        SimpleProcessToken(nextToken, f);
                        if (id == 40) // open parenthesis (id == 40) changes the formatting
                        {
                            f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                        }
                    }

                    // process the cosed paren
                    SimpleProcessToken(nextToken, f);

                    nextToken++;

                    DecrementIndentLevel();
                }
            }

            return nextToken;
        }


        private int ProcessValues(int nextToken)
        {
            if (CodeObject.Source != null && HasToken(nextToken))
            {
                TokenData nextTokenData = GetTokenData(nextToken);
                // if the next token is not a whitespace, a newline is enforced.
                if (!IsTokenWhitespace(nextTokenData))
                {
                    AddIndentedNewLineReplacement(nextTokenData.StartIndex);
                }

                for (int i = nextToken; i < CodeObject.Source.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[nextToken].TokenId)
                        || TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId)
                        , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the source.", Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                        );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }

                ProcessChild(CodeObject.Source);
            }

            return nextToken;
        }



        private int ProcessOutputClause(int nextToken)
        {
            if (CodeObject.OutputIntoClause != null)
            {
                if (nextToken == CodeObject.OutputIntoClause.Position.startTokenNumber)
                {
                    AddIndentedNewLineReplacement(GetTokenData(nextToken).StartIndex);
                }
                else
                {
                    while (nextToken < CodeObject.OutputIntoClause.Position.startTokenNumber)
                    {
                        Debug.Assert(
                            TokenManager.IsTokenComment(TokenManager.TokenList[nextToken].TokenId)
                            || TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId)
                            , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the output into clause.", Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                            );
                        SimpleProcessToken(nextToken, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                        nextToken++;
                    }

                    TokenData previousTokenData = PreviousTokenData(nextToken);
                    if (!IsTokenWhitespace(previousTokenData))
                    {
                        AddIndentedNewLineReplacement(previousTokenData.StartIndex);
                    }
                }
                ProcessChild(CodeObject.OutputIntoClause);
                nextToken = CodeObject.OutputIntoClause.Position.endTokenNumber;

            }

            return nextToken;
        }



    }
}
