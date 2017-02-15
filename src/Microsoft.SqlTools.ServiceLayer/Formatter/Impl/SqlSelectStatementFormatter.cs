//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlSelectStatementFormatterFactory : ASTNodeFormatterFactoryT<SqlSelectStatement>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlSelectStatement codeObject)
        {
            return new SqlSelectStatementFormatter(visitor, codeObject);
        }
    }

    class SqlSelectStatementFormatter : NewLineSeparatedListFormatter
    {
        
        internal SqlSelectStatementFormatter(FormatterVisitor visitor, SqlSelectStatement codeObject)
            : base(visitor, codeObject, false)
        {
            
        }

    }
}
