﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlBinaryBooleanExpressionFormatterFactory : ASTNodeFormatterFactoryT<SqlBinaryBooleanExpression>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlBinaryBooleanExpression codeObject)
        {
            return new SqlBinaryBooleanExpressionFormatter(visitor, codeObject);
        }
    }

    internal class SqlBinaryBooleanExpressionFormatter : ASTNodeFormatterT<SqlBinaryBooleanExpression>
    {
        SpaceSeparatedListFormatter SpaceSeparatedListFormatter { get; set; }

        internal SqlBinaryBooleanExpressionFormatter(FormatterVisitor visitor, SqlBinaryBooleanExpression codeObject)
            : base(visitor, codeObject)
        {
            SpaceSeparatedListFormatter = new SpaceSeparatedListFormatter(visitor, codeObject, true);
        }

        internal override void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            SpaceSeparatedListFormatter.ProcessChild(child);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            // Binary boolean expressions 
            bool allowIncrement = !(CodeObject.Parent is SqlBinaryBooleanExpression);
            SpaceSeparatedListFormatter.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber, allowIncrement);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            bool allowDecrement = !(CodeObject.Parent is SqlBinaryBooleanExpression);
            SpaceSeparatedListFormatter.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber, allowDecrement);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            SpaceSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
        }

    }
}
