//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Stable, non-localized error codes returned on Object Explorer responses so clients can
    /// reliably detect specific failure reasons without matching on (localized) error messages.
    /// </summary>
    public static class ObjectExplorerErrorCodes
    {
        /// <summary>
        /// An Object Explorer expand/refresh operation did not complete before its timeout and
        /// was canceled. Reported on <see cref="ExpandResponse.ErrorCode"/>.
        /// </summary>
        public const string ExpandTimeout = "EXPAND_TIMEOUT";

        /// <summary>
        /// An Object Explorer create-session operation did not complete before its timeout and
        /// was canceled. Reported on <see cref="SessionCreatedParameters.ErrorCode"/>.
        /// </summary>
        public const string CreateSessionTimeout = "CREATE_SESSION_TIMEOUT";
    }
}
