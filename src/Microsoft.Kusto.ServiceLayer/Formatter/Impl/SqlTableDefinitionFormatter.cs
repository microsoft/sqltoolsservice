//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlTableDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlTableDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlTableDefinition codeObject)
        {
            return new SqlTableDefinitionFormatter(visitor, codeObject);
        }
    }
    
    internal class SqlTableDefinitionFormatter : ASTNodeFormatterT<SqlTableDefinition>
    {
        private CommaSeparatedListFormatter CommaSeparatedListFormatter { get; set; }

        public SqlTableDefinitionFormatter(FormatterVisitor visitor, SqlTableDefinition codeObject)
            : base(visitor, codeObject)
        {
            CommaSeparatedListFormatter = new CommaSeparatedListFormatter(visitor, codeObject, true);

            // figure out the size of paddings required to align column definitions in a "columnar" form
            if (FormatOptions.AlignColumnDefinitionsInColumns)
            {
                int range1MaxLength = 0;
                int range2MaxLength = 0;

                foreach (SqlCodeObject child in CodeObject.Children)
                {
                    if (child is SqlColumnDefinition && !(child is SqlComputedColumnDefinition))
                    {
                        SqlIdentifier identifierChild = child.Children.ElementAtOrDefault(0) as SqlIdentifier;

                        if (identifierChild == null)
                        {
                            throw new FormatFailedException("unexpected token at index start Token Index");
                        }

                        string s1 = child.TokenManager.GetText(identifierChild.Position.startTokenNumber, identifierChild.Position.endTokenNumber);
                        range1MaxLength = Math.Max(range1MaxLength, s1.Length);

                        SqlDataTypeSpecification dataTypeChildchild = child.Children.ElementAtOrDefault(1) as SqlDataTypeSpecification;

                        // token "timestamp" should be ignorred
                        if (dataTypeChildchild != null)
                        {
                            string s2 = child.TokenManager.GetText(dataTypeChildchild.Position.startTokenNumber, dataTypeChildchild.Position.endTokenNumber);
                            range2MaxLength = Math.Max(range2MaxLength, s2.Length);
                        }
                    }
                }

                PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition d1 = new PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition(typeof(SqlIdentifier), typeof(SqlDataTypeSpecification), range1MaxLength + 1);
                PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition d2 = new PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition(typeof(SqlDataTypeSpecification), null, range2MaxLength + 1);
                List<PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition> columnSpacingFormatDefinitions = new List<PaddedSpaceSeparatedListFormatter.ColumnSpacingFormatDefinition>(2);
                columnSpacingFormatDefinitions.Add(d1);
                columnSpacingFormatDefinitions.Add(d2);
                Visitor.Context.CurrentColumnSpacingFormatDefinitions = columnSpacingFormatDefinitions;
            }
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
            Visitor.Context.CurrentColumnSpacingFormatDefinitions = null;
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
