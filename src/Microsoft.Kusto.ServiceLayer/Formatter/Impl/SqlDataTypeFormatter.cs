//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
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
            int startTokenIndex = CodeObject.Position.startTokenNumber;
            int endTokenIndex = CodeObject.Position.endTokenNumber;

            if (startTokenIndex == endTokenIndex - 1 &&
                CodeObject.TokenManager.TokenList[startTokenIndex].TokenId == FormatterTokens.TOKEN_ID)
            {
                string sql = GetTokenRangeAsOriginalString(startTokenIndex, startTokenIndex + 1);
                if (FormatOptions.UppercaseDataTypes)
                {
                    TokenData tok = TokenManager.TokenList[startTokenIndex];
                    AddReplacement(tok.StartIndex, sql, sql.ToUpperInvariant());
                    sql = sql.ToUpperInvariant();
                }
                else if (FormatOptions.LowercaseDataTypes)
                {
                    TokenData tok = TokenManager.TokenList[startTokenIndex];
                    AddReplacement(tok.StartIndex, sql, sql.ToLowerInvariant());
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
