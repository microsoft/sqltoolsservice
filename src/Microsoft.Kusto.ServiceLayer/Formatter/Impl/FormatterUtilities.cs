//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    internal static class FormatterUtilities
    {
        internal static string StripAllWhitespace(string original, FormatContext context)
        {
            return String.Empty;
        }

        internal static string NormalizeToOneSpace(string original, FormatContext context)
        {
            return " ";
        }

        internal static string NormalizeNewLinesInWhitespace(string original, FormatContext context)
        {
            return NormalizeNewLinesInWhitespace(original, context, 0);
        }

        internal static string NormalizeNewLinesEnsureOneNewLineMinimum(string original, FormatContext context)
        {
            return NormalizeNewLinesInWhitespace(original, context, 1);
        }

        internal static string NormalizeNewLinesInWhitespace(string original, FormatContext context, int minimumNewLines)
        {
            return NormalizeNewLinesInWhitespace(original, context, 1, () => { return original; });
        }

        internal static string NormalizeNewLinesOrCondenseToOneSpace(string original, FormatContext context)
        {
            return NormalizeNewLinesOrCondenseToNSpaces(original, context, 1);
        }

        internal static string NormalizeNewLinesOrCondenseToNSpaces(string original, FormatContext context, int nSpaces)
        {
            return NormalizeNewLinesInWhitespace(original, context, 0, () => { return new String(' ', nSpaces); });
        }

        private static string NormalizeNewLinesInWhitespace(string original, FormatContext context, int minimumNewLines, Func<String> noNewLinesProcessor)
        {
            int nNewLines = 0;
            int idx = original.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase);
            while (idx > -1)
            {
                ++nNewLines;
                idx = original.IndexOf(Environment.NewLine, idx + 1, StringComparison.OrdinalIgnoreCase);
            }

            StringBuilder sb = new StringBuilder();
            nNewLines = Math.Max(minimumNewLines, nNewLines);
            for (int i = 0; i < nNewLines; i++)
            {
                sb.Append(Environment.NewLine);
            }
            sb.Append(context.GetIndentString());

            if (nNewLines > 0)
            {
                return sb.ToString();
            }
            else
            {
                return noNewLinesProcessor();
            }
        }
        
    }
}
