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

namespace Microsoft.Kusto.ServiceLayer.Formatter
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
                ProcessAndNormalizeWhitespaceRange(nextToken, CodeObject.TopSpecification.Position.startTokenNumber,
                    FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);

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

            // Process up to the INTO or Target index. If INTO isn't there, expect all whitespace tokens
            ProcessAndNormalizeWhitespaceRange(nextToken, intoTokenIndexOrTargetStartTokenIndex,
                FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            
            IncrementIndentLevel();

            // Process the INTO token and all whitespace up to the target start
            ProcessAndNormalizeTokenRange(intoTokenIndexOrTargetStartTokenIndex, CodeObject.Target.Position.startTokenNumber,
                FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace, areAllTokensWhitespace: false);
            
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

                ProcessAndNormalizeWhitespaceRange(nextToken, CodeObject.Source.Position.startTokenNumber,
                    FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);

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
                        DebugAssertTokenIsWhitespaceOrComment(GetTokenData(nextToken), nextToken);
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
