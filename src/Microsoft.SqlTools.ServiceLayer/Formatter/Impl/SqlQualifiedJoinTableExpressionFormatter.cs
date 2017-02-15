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
    internal class SqlQualifiedJoinTableExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlQualifiedJoinTableExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlQualifiedJoinTableExpression codeObject)
        {
            return new SqlQualifiedJoinTableExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlQualifiedJoinTableExpressionFormatter : ASTNodeFormatterT<SqlQualifiedJoinTableExpression>
    {
        SpaceSeparatedListFormatter SpaceSeparatedListFormatter { get; set; }

        internal SqlQualifiedJoinTableExpressionFormatter(FormatterVisitor visitor, SqlQualifiedJoinTableExpression codeObject)
            : base(visitor, codeObject)
        {
            SpaceSeparatedListFormatter = new SpaceSeparatedListFormatter(visitor, codeObject, false);
        }

        internal override void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            SpaceSeparatedListFormatter.ProcessChild(child);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            SpaceSeparatedListFormatter.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            SpaceSeparatedListFormatter.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            SpaceSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
        }

    }
}
