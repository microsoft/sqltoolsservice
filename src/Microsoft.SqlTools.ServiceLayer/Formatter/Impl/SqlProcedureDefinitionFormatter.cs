//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

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

    class SqlProcedureDefinitionFormatter : CommaSeparatedListFormatter
    {
        NewLineSeparatedListFormatter NewLineSeparatedListFormatter { get; set; }
        bool _foundTokenWith;

        internal SqlProcedureDefinitionFormatter(FormatterVisitor visitor, SqlProcedureDefinition codeObject)
            : base(visitor, codeObject, true)
        {
            this.NewLineSeparatedListFormatter = new NewLineSeparatedListFormatter(visitor, codeObject, false);
            this._foundTokenWith = false;
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            if (nextChild is SqlModuleOption)
            {
                if (!_foundTokenWith) this.Visitor.Context.DecrementIndentLevel();
                for (int i = previousChild.Position.endTokenNumber; i < nextChild.Position.startTokenNumber; i++)
                {
                    if (!_foundTokenWith && this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_WITH)
                    {
                        this.Visitor.Context.IncrementIndentLevel();
                        _foundTokenWith = true;
                    }
                    this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
            }
            else if (previousChild is SqlObjectIdentifier)
            {
                this.NewLineSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
            }
            else
            {
                base.ProcessInterChildRegion(previousChild, nextChild);
            }
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            this.Visitor.Context.DecrementIndentLevel();
            NormalizeWhitespace f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
            for (int i = lastChildEndTokenNumber; i < endTokenNumber; i++)
            {
                if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_AS
                    && !this.Visitor.Context.Script.TokenManager.IsTokenWhitespace(this.Visitor.Context.Script.TokenManager.TokenList[i-1].TokenId))
                {
                    TokenData td = this.Visitor.Context.Script.TokenManager.TokenList[i];
                    this.Visitor.Context.Replacements.Add(
                        new Replacement(
                            td.StartIndex,
                            "",
                            Environment.NewLine + this.Visitor.Context.GetIndentString()
                            )
                        );
                }
                if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_FOR)
                {
                    f = FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace;
                }
                else if (this.Visitor.Context.Script.TokenManager.TokenList[i].TokenId == FormatterTokens.TOKEN_REPLICATION)
                {
                    f = FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum;
                }
                this.SimpleProcessToken(i, f);
            }
        }
    }
}
