//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
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

            IEnumerator<SqlCodeObject> firstChildEnum = this.CodeObject.Children.GetEnumerator();
            if (firstChildEnum.MoveNext())
            {
                //
                // format the text from the start of the object to the start of it's first child
                // 
                this.ProcessPrefixRegion(this.CodeObject.Position.startTokenNumber, firstChildEnum.Current.Position.startTokenNumber);
                int nextToken = firstChildEnum.Current.Position.startTokenNumber;

                // handle top specification
                nextToken = this.ProcessTopSpecification(nextToken);

                // handle target
                nextToken = this.ProcessTarget(nextToken);

                // handle target columns
                nextToken = this.ProcessColumns(nextToken);

                // handle output clause
                nextToken = this.ProcessOutputClause(nextToken);

                // handle values / derived table / execute statement / dml_table_source
                nextToken = this.ProcessValues(nextToken);
            }
            else
            {
                throw new FormatFailedException("Insert statement has no children.");
            }
        }


        internal int ProcessTopSpecification(int nextToken)
        {
            if (this.CodeObject.TopSpecification != null)
            {
                for (int i = nextToken; i < this.CodeObject.TopSpecification.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                       this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                    || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the top specification.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                    );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
                }

                this.ProcessChild(this.CodeObject.TopSpecification);

                nextToken = this.CodeObject.TopSpecification.Position.endTokenNumber;
            }

            return nextToken;
        }


        private int ProcessTarget(int nextToken)
        {
            Debug.Assert(this.CodeObject.Target != null, "No target in insert statement.");

            // find out if there is an "INTO" token
            int intoTokenIndexOrTargetStartTokenIndex = this.CodeObject.Target.Position.startTokenNumber;
            for (int i = nextToken; i < this.CodeObject.Target.Position.startTokenNumber; i++)
            {
                if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_INTO)
                {
                    intoTokenIndexOrTargetStartTokenIndex = i;
                }
            }

            for (int i = nextToken; i < intoTokenIndexOrTargetStartTokenIndex; i++)
            {
                Debug.Assert(
                    this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                    || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the target.", this.Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                    );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }

            this.Visitor.Context.IncrementIndentLevel();

            for (int i = intoTokenIndexOrTargetStartTokenIndex ; i < this.CodeObject.Target.Position.startTokenNumber; i++)
            {
                Debug.Assert(
                    this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                    || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                    , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the target.", this.Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                    );
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
            }

            this.ProcessChild(this.CodeObject.Target);

            nextToken = this.CodeObject.Target.Position.endTokenNumber;
            this.Visitor.Context.DecrementIndentLevel();

            return nextToken;
        }

        private int ProcessColumns(int nextToken)
        {
            if (this.CodeObject.TargetColumns != null)
            {
                if (this.CodeObject.TargetColumns.Count > 0)
                {
                    this.Visitor.Context.IncrementIndentLevel();

                    // if the next token is not a whitespace, a newline is enforced.
                    if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
                    {
                        this.Visitor.Context.Replacements.Add(
                            new Replacement
                                (this.Visitor.Context.Script.TokenManager.TokenList[nextToken].StartIndex,
                                string.Empty,
                                Environment.NewLine + this.Visitor.Context.GetIndentString()));
                    }

                    NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;

                    // process tokens until we reach the closed parenthesis (with id 41)
                    for (int id = this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId; id != 41; id = this.Visitor.Context.Script.TokenManager.TokenList[++nextToken].TokenId)
                    {
                        this.SimpleProcessToken(nextToken, f);
                        if (id == 40) // open parenthesis (id == 40) changes the formatting
                        {
                            f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                        }
                    }

                    // process the cosed paren
                    this.SimpleProcessToken(nextToken, f);

                    nextToken++;

                    this.Visitor.Context.DecrementIndentLevel();
                }
            }

            return nextToken;
        }


        private int ProcessValues(int nextToken)
        {
            if (CodeObject.Source != null && HasToken(nextToken))
            {
                // if the next token is not a whitespace, a newline is enforced.
                if (!IsTokenWithIdWhitespace(nextToken))
                {
                    Visitor.Context.Replacements.Add(
                        new Replacement(
                            this.Visitor.Context.Script.TokenManager.TokenList[nextToken].StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()));
                }

                for (int i = nextToken; i < this.CodeObject.Source.Position.startTokenNumber; i++)
                {
                    Debug.Assert(
                        this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                        || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                        , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the source.", this.Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                        );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }

                this.ProcessChild(this.CodeObject.Source);
            }

            return nextToken;
        }



        private int ProcessOutputClause(int nextToken)
        {
            if (this.CodeObject.OutputIntoClause != null)
            {
                if (nextToken == this.CodeObject.OutputIntoClause.Position.startTokenNumber)
                {
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            this.Visitor.Context.Script.TokenManager.TokenList[nextToken].StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()));
                }
                else
                {
                    while (nextToken < this.CodeObject.OutputIntoClause.Position.startTokenNumber)
                    {
                        Debug.Assert(
                            this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                            || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId)
                            , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the output into clause.", this.Visitor.Context.GetTokenRangeAsOriginalString(nextToken, nextToken + 1))
                            );
                        this.SimpleProcessToken(nextToken, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                        nextToken++;
                    }

                    if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken - 1].TokenId))
                    {
                        this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            this.Visitor.Context.Script.TokenManager.TokenList[nextToken - 1].StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()));
                    }
                }
                this.ProcessChild(this.CodeObject.OutputIntoClause);
                nextToken = this.CodeObject.OutputIntoClause.Position.endTokenNumber;

            }

            return nextToken;
        }



    }
}
