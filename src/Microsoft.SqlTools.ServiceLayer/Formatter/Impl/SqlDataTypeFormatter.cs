//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlDataTypeFormatterFactory : ASTNodeFormatterFactoryT<SqlDataType>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlDataType codeObject)
        {
            return new SqlDataTypeFormatter(visitor, codeObject);
        }
    }

    internal class SqlDataTypeFormatter : ASTNodeFormatterT<SqlDataType>
    {
        internal SqlDataTypeFormatter(FormatterVisitor visitor, SqlDataType codeObject)
            : base(visitor, codeObject)
        {
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public override void Format()
        {
            int startTokenIndex = this.CodeObject.Position.startTokenNumber;
            int endTokenIndex = this.CodeObject.Position.endTokenNumber;

            if (startTokenIndex == endTokenIndex - 1 &&
                this.CodeObject.TokenManager.TokenList[startTokenIndex].TokenId == FormatterTokens.TOKEN_ID)
            {
                string sql = this.Visitor.Context.GetTokenRangeAsOriginalString(startTokenIndex, startTokenIndex + 1);
                if (this.Visitor.Context.FormatOptions.UppercaseDataTypes)
                {
                    TokenData tok = this.Visitor.Context.Script.TokenManager.TokenList[startTokenIndex];
                    this.Visitor.Context.Replacements.Add(new Replacement(tok.StartIndex, sql, sql.ToUpperInvariant()));
                    sql = sql.ToUpperInvariant();
                }
                else if (this.Visitor.Context.FormatOptions.LowercaseDataTypes)
                {
                    TokenData tok = this.Visitor.Context.Script.TokenManager.TokenList[startTokenIndex];
                    this.Visitor.Context.Replacements.Add(new Replacement(tok.StartIndex, sql, sql.ToLowerInvariant()));
                    sql = sql.ToLowerInvariant();
                }

            }
            else
            {
                base.Format();
            }
        }

    }
}
