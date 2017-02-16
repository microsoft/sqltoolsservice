//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    internal class SpaceSeparatedListFormatter : WhiteSpaceSeparatedListFormatter
    {
        internal SpaceSeparatedListFormatter(FormatterVisitor visitor, SqlCodeObject codeObject, bool incrementIndentLevelOnPrefixRegion)
            : base(visitor, codeObject, incrementIndentLevelOnPrefixRegion)
        {
        }

        internal override string FormatWhitespace(string original, FormatContext context)
        {
            return FormatterUtilities.NormalizeNewLinesOrCondenseToOneSpace(original, context);
        }

    }
    
    internal class NewLineSeparatedListFormatter : WhiteSpaceSeparatedListFormatter
    {
        public NewLineSeparatedListFormatter(FormatterVisitor visitor, SqlCodeObject codeObject, bool incrementIndentLevelOnPrefixRegion)
            : base(visitor, codeObject, incrementIndentLevelOnPrefixRegion)
        {
        }

        internal override string FormatWhitespace(string original, FormatContext context)
        {
            return FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum(original, context);
        }
    }
}
