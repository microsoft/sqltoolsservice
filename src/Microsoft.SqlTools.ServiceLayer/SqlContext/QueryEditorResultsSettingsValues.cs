//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    public class QueryEditorResultSettingsValues
    {
        /// <summary>
        /// Whether to remove the line break from cell values when copying results.
        /// </summary>
        public Boolean CopyRemoveNewLine { get; set; } = true;

        /// <summary>
        /// Whether to skip adding a line break between rows when copying results when the previous row already has a trailing line break.
        /// </summary>
        public bool SkipNewLineAfterTrailingLineBreak { get; set; } = false;

        /// <summary>
        /// Whether to include the column headers when copying results.
        /// </summary>
        public bool CopyIncludeHeaders { get; set; } = false;

    }
}
