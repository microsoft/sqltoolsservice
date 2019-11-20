//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlQueryWithClauseFormatterFactory : ASTNodeFormatterFactoryT<SqlQueryWithClause>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlQueryWithClause codeObject)
        {
            return new SqlQueryWithClauseFormatter(visitor, codeObject);
        }
    }

    class SqlQueryWithClauseFormatter : CommaSeparatedListFormatter
    {
        public SqlQueryWithClauseFormatter(FormatterVisitor visitor, SqlQueryWithClause codeObject)
            : base(visitor, codeObject, true)
        {
        }
    }
}
