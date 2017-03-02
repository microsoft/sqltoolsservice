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
    internal class SqlColumnDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlColumnDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlColumnDefinition codeObject)
        {
            return new SqlColumnDefinitionFormatter(visitor, codeObject);
        }
    }

    internal class SqlColumnDefinitionFormatter : ASTNodeFormatterT<SqlColumnDefinition>
    {
        private PaddedSpaceSeparatedListFormatter SpaceSeparatedListFormatter { get; set; }

        internal SqlColumnDefinitionFormatter(FormatterVisitor visitor, SqlColumnDefinition codeObject)
            : base(visitor, codeObject)
        {
            SpaceSeparatedListFormatter = new PaddedSpaceSeparatedListFormatter(visitor, codeObject, Visitor.Context.CurrentColumnSpacingFormatDefinitions, true);
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
