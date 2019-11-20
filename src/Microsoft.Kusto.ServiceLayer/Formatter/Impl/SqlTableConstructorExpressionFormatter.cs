//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlTableConstructorExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlTableConstructorExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlTableConstructorExpression codeObject)
        {
            return new SqlTableConstructorExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlTableConstructorExpressionFormatter : CommaSeparatedListFormatter
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SqlTableConstructorExpressionFormatter(FormatterVisitor visitor, SqlTableConstructorExpression codeObject)
            : base(visitor, codeObject, true)
        {
        }
    }
}
