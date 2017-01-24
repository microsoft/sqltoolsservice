//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    [Flags]
    internal enum TextRuleFlags
    {
        ReportWhitespace = 1,
        RecognizeDoubleQuotedString = 2,
        RecognizeSingleQuotedString = 4,
        RecognizeLineComment = 8,
        RecognizeBlockComment = 16,
        RecognizeBrace = 32,
    }
}
