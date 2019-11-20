//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlBatchFormatterFactory : ASTNodeFormatterFactoryT<SqlBatch>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlBatch codeObject)
        {
            return new SqlBatchFormatter(visitor, codeObject);
        }
    }

    class SqlBatchFormatter : NewLineSeparatedListFormatter
    {
        public SqlBatchFormatter(FormatterVisitor visitor, SqlCodeObject codeObject)
            :base(visitor, codeObject, false)
        {                
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            for (int i = startTokenNumber; i < firstChildStartTokenNumber; i++)
            {
                SimpleProcessToken(i, (original, context) => FormatterUtilities.NormalizeNewLinesInWhitespace(original, context, 2));
            }
        }

    }
}
