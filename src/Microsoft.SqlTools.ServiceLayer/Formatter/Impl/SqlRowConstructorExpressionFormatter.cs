//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlRowConstructorExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlRowConstructorExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlRowConstructorExpression codeObject)
        {
            return new SqlRowConstructorExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlRowConstructorExpressionFormatter : CommaSeparatedListFormatter
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SqlRowConstructorExpressionFormatter(FormatterVisitor visitor, SqlRowConstructorExpression codeObject)
            : base(visitor, codeObject, false)
        {
        }
    }
}
