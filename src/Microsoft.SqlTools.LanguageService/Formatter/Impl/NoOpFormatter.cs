//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.LanguageService.Formatter
{
    internal sealed class NoOpFormatter : ASTNodeFormatterT<SqlCodeObject>
    {
        public NoOpFormatter(FormatterVisitor visitor, SqlCodeObject codeObject)
            : base(visitor, codeObject)
        {

        }
    }
}
