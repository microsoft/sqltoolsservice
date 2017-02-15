//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    internal class NoOpFormatter : ASTNodeFormatterT<SqlCodeObject>
    {
        public NoOpFormatter(FormatterVisitor visitor, SqlCodeObject codeObject)
            : base(visitor, codeObject)
        {

        }
    }
}
