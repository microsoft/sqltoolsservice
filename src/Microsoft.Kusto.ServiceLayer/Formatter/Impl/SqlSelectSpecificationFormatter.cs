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
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Formatter
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
                DecrementIndentLevel();
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
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
                TokenData td = TokenManager.TokenList[forTokenIndex];
                while (td.TokenId != FormatterTokens.TOKEN_FOR && forTokenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                            TokenManager.IsTokenComment(td.TokenId)
                         || TokenManager.IsTokenWhitespace(td.TokenId)
                         , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the FOR token.", Visitor.Context.GetTokenRangeAsOriginalString(forTokenIndex, forTokenIndex + 1))
                         );
                    ++forTokenIndex;
                    td = TokenManager.TokenList[forTokenIndex];
                }
                Debug.Assert(forTokenIndex < CodeObject.Position.endTokenNumber, "No FOR token.");
                #endregion // Find the "FOR" token

                
                #region Process the tokens before the "FOR" token
                for (int i = previousChild.Position.endTokenNumber; i < forTokenIndex; i++)
                {
                    Debug.Assert(
                            TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                         || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                         , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the FOR token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                         );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before the "FOR" token

                #region Process the "FOR" token
                if (previousChild.Position.endTokenNumber >= forTokenIndex
                    || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[forTokenIndex - 1].TokenId))
                {
                    td = TokenManager.TokenList[forTokenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                Visitor.Context.ProcessTokenRange(forTokenIndex, forTokenIndex + 1);
                IncrementIndentLevel();

                int nextToken = forTokenIndex + 1;
                Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View Definition ends unexpectedly after the FOR token.");
                // Ensure a whitespace after the "FOR" token
                if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
                {
                    td = TokenManager.TokenList[forTokenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process the "FOR" token

                #region Process tokens after the FOR token
                for (int i = nextToken; i < nextChild.Position.startTokenNumber; i++)
                {
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace);
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
