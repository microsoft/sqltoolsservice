//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.Azure.Core.Authentication;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// A session used by <see cref="IAzureResourceManager" />. Includes all the clients that the resource management needs to get ther resources
    /// </summary>
    public interface IAzureResourceManagementSession : IDisposable
    {       
        /// <summary>
        /// Closes the session
        /// </summary>
        /// <returns></returns>
        bool CloseSession();

        /// <summary>
        /// Teh subscription for the current session
        /// </summary>
        IAzureUserAccountSubscriptionContext SubscriptionContext
        {
            get;
            set;
        }
    }
}
