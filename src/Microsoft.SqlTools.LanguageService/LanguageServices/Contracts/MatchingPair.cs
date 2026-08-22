//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.LanguageService.Workspace.Contracts;

namespace Microsoft.SqlTools.LanguageService.LanguageServices.Contracts
{
    /// <summary>
    /// Contains the ranges of a parser-recognized matching token pair.
    /// </summary>
    public sealed class MatchingPairResult
    {
        /// <summary>
        /// Gets or sets the range of the opening token.
        /// </summary>
        public Range LeftRange { get; set; }

        /// <summary>
        /// Gets or sets the range of the closing token.
        /// </summary>
        public Range RightRange { get; set; }
    }
}
