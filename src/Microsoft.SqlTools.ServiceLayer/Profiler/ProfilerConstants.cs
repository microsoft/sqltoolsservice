//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Constants and utilities used by profiler services for Extended Events session operations.
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

        /// <summary>
        /// Converts an XEvent field/action value to a locale-invariant string.
        /// DateTimeOffset and DateTime values use the ISO 8601 round-trip format ("o")
        /// to ensure JavaScript clients can parse them with new Date().
        /// All other types use Convert.ToString with InvariantCulture.
        /// </summary>
        public static string ToInvariantString(object value)
        {
            return value switch
            {
                null => string.Empty,
                DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }
    }
}
