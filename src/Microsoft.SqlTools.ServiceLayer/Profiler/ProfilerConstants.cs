//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Constants used by profiler services for Extended Events session operations.
    /// </summary>
    internal static class ProfilerConstants
    {
        /// <summary>
        /// Maximum number of retry attempts when stopping an event session.
        /// </summary>
        public const int StopSessionMaxRetryAttempts = 3;

        /// <summary>
        /// Delay between retry attempts when stopping an event session.
        /// </summary>
        public static readonly TimeSpan StopSessionRetryDelay = TimeSpan.FromMilliseconds(500);
    }
}
