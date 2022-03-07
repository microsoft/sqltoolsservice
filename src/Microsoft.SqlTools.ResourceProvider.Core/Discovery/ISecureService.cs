﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// Defines a class as secure which requires an account to function
    /// </summary>
    public interface ISecureService
    {
        /// <summary>
        /// Gets the account manager instance
        /// </summary>
        IAccountManager AccountManager
        {
            get;
        }
    }
}
