//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
