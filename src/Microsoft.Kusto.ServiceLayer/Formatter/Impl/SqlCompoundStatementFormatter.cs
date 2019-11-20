//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlCompoundStatementFormatterFactory : ASTNodeFormatterFactoryT<SqlCompoundStatement>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlCompoundStatement codeObject)
        {
            return new SqlCompoundStatementFormatter(visitor, codeObject);
        }
    }

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
                if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_BEGIN_CS)
                {
                    IncrementIndentLevel();
                }
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }

        internal override void  ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            DecrementIndentLevel();

            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_END_CS
                    && !TokenManager.IsTokenWhitespace(TokenManager.TokenList[i-1].TokenId))
                {
                    TokenData td = TokenManager.TokenList[i];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }
 
    }
 
}
