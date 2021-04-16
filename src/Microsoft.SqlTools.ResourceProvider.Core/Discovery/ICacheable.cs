//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// Defines a class as cachable    
    /// </summary> 
    public interface ICacheable<T>
    {       
        /// <summary>
        /// Clears the cache for current user
        /// </summary>
        /// <returns>True if cache refreshed successfully. Otherwise returns false</returns>
        Task<bool> ClearCacheAsync();

        /// <summary>
        /// Updates the cache for current selected subscriptions
        /// </summary>
        /// <returns>The new cached data</returns>
        Task<T> RefreshCacheAsync(CancellationToken cancellationToken);
    }
}
