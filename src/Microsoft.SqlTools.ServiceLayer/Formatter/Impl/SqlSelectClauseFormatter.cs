//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlSelectClauseFormatterFactory : ASTNodeFormatterFactoryT<SqlSelectClause>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlSelectClause codeObject)
        {
            return new SqlSelectClauseFormatter(visitor, codeObject);
        }
    }

    internal class SqlSelectClauseFormatter : CommaSeparatedListFormatter
    {
        private NewLineSeparatedListFormatter NewLineSeparatedListFormatter { get; set; }

        internal SqlSelectClauseFormatter(FormatterVisitor visitor, SqlSelectClause codeObject)
            : base(visitor, codeObject, visitor.Context.FormatOptions.PlaceSelectStatementReferenceOnNewLine || 
                visitor.Context.FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements)
        {
            NewLineSeparatedListFormatter = new NewLineSeparatedListFormatter(visitor, codeObject, true);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            if (previousChild is SqlTopSpecification)
            {
                NewLineSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
            }
            else
            {
                base.ProcessInterChildRegion(previousChild, nextChild);
            }
        }
    }
}
