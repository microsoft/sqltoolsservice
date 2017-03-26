//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlProcedureDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlProcedureDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlProcedureDefinition codeObject)
        {
            return new SqlProcedureDefinitionFormatter(visitor, codeObject);
        }
    }

    class SqlProcedureDefinitionFormatter : ASTNodeFormatterT<SqlProcedureDefinition>
    {
        NewLineSeparatedListFormatter NewLineSeparatedListFormatter { get; set; }
        CommaSeparatedListFormatter CommaSeparatedListFormatter { get; set; }
        bool foundTokenWith;

        internal SqlProcedureDefinitionFormatter(FormatterVisitor visitor, SqlProcedureDefinition codeObject)
            : base(visitor, codeObject)
        {
            NewLineSeparatedListFormatter = new NewLineSeparatedListFormatter(visitor, codeObject, true);
            CommaSeparatedListFormatter = new CommaSeparatedListFormatter(visitor, codeObject, true);
            foundTokenWith = false;
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            NewLineSeparatedListFormatter.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessChild(SqlCodeObject child)
        {
            CommaSeparatedListFormatter.ProcessChild(child);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            if (nextChild is SqlModuleOption)
            {
                if (!foundTokenWith)
                {
                    DecrementIndentLevel();
                }
                for (int i = previousChild.Position.endTokenNumber; i < nextChild.Position.startTokenNumber; i++)
                {
                    if (!foundTokenWith && TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_WITH)
                    {
                        IncrementIndentLevel();
                        foundTokenWith = true;
                    }
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
            }
            else if (previousChild is SqlObjectIdentifier)
            {
                NewLineSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
            }
            else
            {
                CommaSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            DecrementIndentLevel();
            NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_AS
                    && !TokenManager.IsTokenWhitespace(TokenManager.TokenList[i-1].TokenId))
                {
                    TokenData td = TokenManager.TokenList[i];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_FOR)
                {
                    f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                }
                else if (TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_REPLICATION)
                {
                    f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
                }
                SimpleProcessToken(i, f);
            }
        }
    }
}
