//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlSelectSpecificationFormatterFactory : ASTNodeFormatterFactoryT<SqlSelectSpecification>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlSelectSpecification codeObject)
        {
            return new SqlSelectSpecificationFormatter(visitor, codeObject);
        }
    }

    internal class SqlSelectSpecificationFormatter : NewLineSeparatedListFormatter
    {

        internal SqlSelectSpecificationFormatter(FormatterVisitor visitor, SqlSelectSpecification codeObject)
            : base(visitor, codeObject, false)
        {
     
        }
        
        internal override void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            base.ProcessChild(child);
            if (child is SqlForBrowseClause)
            {
                this.Visitor.Context.DecrementIndentLevel();
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            /* Due to the current behavior of the T-SQL Parser, the FOR clause needs to be treated separately */
            if (nextChild is SqlForBrowseClause || nextChild is SqlForXmlClause)
            {
                #region Find the "FOR" token
                int forTokenIndex = previousChild.Position.endTokenNumber;
                TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[forTokenIndex];
                while (td.TokenId != FormatterTokens.TOKEN_FOR && forTokenIndex < this.CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                            this.Visitor.Context.Script.TokenManager.IsTokenComment(td.TokenId)
                         || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(td.TokenId)
                         , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the FOR token.", this.Visitor.Context.GetTokenRangeAsOriginalString(forTokenIndex, forTokenIndex + 1))
                         );
                    ++forTokenIndex;
                    td = this.Visitor.Context.Script.TokenManager.TokenList[forTokenIndex];
                }
                Debug.Assert(forTokenIndex < this.CodeObject.Position.endTokenNumber, "No FOR token.");
                #endregion // Find the "FOR" token

                
                #region Process the tokens before the "FOR" token
                for (int i = previousChild.Position.endTokenNumber; i < forTokenIndex; i++)
                {
                    Debug.Assert(
                            this.Visitor.Context.Script.TokenManager.IsTokenComment(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                         || this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId)
                         , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the FOR token.", this.Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                         );
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before the "FOR" token

                #region Process the "FOR" token
                if (previousChild.Position.endTokenNumber >= forTokenIndex
                    || !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[forTokenIndex - 1].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[forTokenIndex];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                this.Visitor.Context.ProcessTokenRange(forTokenIndex, forTokenIndex + 1);
                this.Visitor.Context.IncrementIndentLevel();

                int nextToken = forTokenIndex + 1;
                Debug.Assert(nextToken < this.CodeObject.Position.endTokenNumber, "View Definition ends unexpectedly after the FOR token.");
                // Ensure a whitespace after the "FOR" token
                if (!this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[nextToken].TokenId))
                {
                    td = this.Visitor.Context.Script.TokenManager.TokenList[forTokenIndex];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            " "
                            )
                        );
                }
                #endregion // Process the "FOR" token

                #region Process tokens after the FOR token
                for (int i = nextToken; i < nextChild.Position.startTokenNumber; i++)
                {
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
                }
                #endregion // Process tokens after the FOR token
            }
            else
            {
                base.ProcessInterChildRegion(previousChild, nextChild);
            }
        }
        
    }
}
