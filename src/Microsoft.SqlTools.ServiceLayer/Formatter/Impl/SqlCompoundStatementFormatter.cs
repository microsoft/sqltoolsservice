//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatter))]
    class SqlCompoundStatementFormatter : NewLineSeparatedListFormatter
    {
        internal SqlCompoundStatementFormatter(FormatterVisitor visitor, SqlCompoundStatement codeObject)
            : base(visitor, codeObject, true)
        {
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            
            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_BEGIN_CS)
                {
                    this.Visitor.Context.IncrementIndentLevel();
                }
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }

        internal override void  ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            this.Visitor.Context.DecrementIndentLevel();

            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_END_CS
                    && !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i-1].TokenId))
                {
                    TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[i];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            string.Empty,
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }
 
    }
 
}
