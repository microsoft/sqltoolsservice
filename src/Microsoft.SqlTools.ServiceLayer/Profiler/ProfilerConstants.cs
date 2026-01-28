//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Constants used by profiler services for connection and retry behavior.
    /// </summary>
    internal static class ProfilerConstants
    {
        /// <summary>
        /// Maximum number of reconnection attempts for live streaming sessions.
        /// </summary>
        public const int DefaultMaxReconnectAttempts = 3;

        /// <summary>
        /// Base delay between reconnection attempts (uses exponential backoff).
        /// </summary>
        public static readonly TimeSpan DefaultReconnectDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum number of retry attempts when stopping a profiling session.
        /// </summary>
        public const int StopSessionMaxRetryAttempts = 3;

        /// <summary>
        /// Delay between retry attempts when stopping a profiling session.
        /// </summary>
        public static readonly TimeSpan StopSessionRetryDelay = TimeSpan.FromMilliseconds(500);
    }
}
