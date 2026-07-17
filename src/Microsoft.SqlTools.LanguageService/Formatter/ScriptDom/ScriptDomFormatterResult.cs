//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.LanguageService.Formatter.ScriptDom
{
    internal sealed class ScriptDomFormatterResult
    {
        public ScriptDomFormatterResult(ScriptDomFormatterOutcome outcome, string? formattedText = null, int parseErrorCount = 0)
        {
            Outcome = outcome;
            FormattedText = formattedText;
            ParseErrorCount = parseErrorCount;
        }

        public ScriptDomFormatterOutcome Outcome { get; }

        public string? FormattedText { get; }

        public int ParseErrorCount { get; }
    }
}
