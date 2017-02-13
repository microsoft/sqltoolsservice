//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlFromClauseFormatterFactory : ASTNodeFormatterFactoryT<SqlFromClause>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlFromClause codeObject)
        {
            return new SqlFromClauseFormatter(visitor, codeObject);
        }
    }

    internal class SqlFromClauseFormatter : ASTNodeFormatterT<SqlFromClause>
    {
        private CommaSeparatedListFormatter CommaSeparatedListFormatter { get; set; }

        internal SqlFromClauseFormatter(FormatterVisitor visitor, SqlFromClause codeObject)
            : base(visitor, codeObject)
        {
            CommaSeparatedListFormatter = new CommaSeparatedListFormatter(visitor, codeObject, FormatOptions.PlaceEachReferenceOnNewLineInQueryStatements);
        }

        internal override void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            CommaSeparatedListFormatter.ProcessChild(child);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            CommaSeparatedListFormatter.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            CommaSeparatedListFormatter.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            CommaSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
        }

    }
}
