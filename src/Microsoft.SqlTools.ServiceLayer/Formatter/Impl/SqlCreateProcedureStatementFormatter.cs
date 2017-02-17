//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlCreateProcedureStatementFormatterFactory : ASTNodeFormatterFactoryT<SqlCreateProcedureStatement>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlCreateProcedureStatement codeObject)
        {
            return new SqlCreateProcedureStatementFormatter(visitor, codeObject);
        }
    }

    class SqlCreateProcedureStatementFormatter : NewLineSeparatedListFormatter
    {
        internal SqlCreateProcedureStatementFormatter(FormatterVisitor visitor, SqlCreateProcedureStatement codeObject)
            : base(visitor, codeObject, false)
        {
        }
    }
}
