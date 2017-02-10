//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
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
                this.SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
        }

    }
}
