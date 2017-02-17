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
    internal class SqlInsertStatementFormatterFactory : ASTNodeFormatterFactoryT<SqlInsertStatement>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlInsertStatement codeObject)
        {
            return new SqlInsertStatementFormatter(visitor, codeObject);
        }
    }

    internal class SqlInsertStatementFormatter : NewLineSeparatedListFormatter
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SqlInsertStatementFormatter(FormatterVisitor visitor, SqlInsertStatement codeObject)
            : base(visitor, codeObject, false)
        {
        }
    }
}
