//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore
{
    /// <summary>
    /// Represents a TimeInterval with strings for the start and end times instead of DateTimeOffsets for JRPC compatibility
    /// </summary>
    public class BasicTimeInterval
    {
        /// <summary>
        /// Start time of this time interval, in ISO 8601 format (<code>ToString("O")</code>).
        /// This property is ignored unless TimeIntervalOptions is set to Custom.
        /// </summary>
        public string StartDateTimeInUtc { get; set; } = null;

        /// <summary>
        /// End time of this time interval, in ISO 8601 format (<code>ToString("O")</code>).
        /// This property is ignored unless TimeIntervalOptions is set to Custom.
        /// </summary>
        public string EndDateTimeInUtc { get; set; } = null;

        /// <summary>
        /// Time interval type.  Unless set to Custom, then StartDateTimeInUtc and EndDateTimeInUtc are ignored.
        /// </summary>
        public TimeIntervalOptions TimeIntervalOptions { get; set; } = TimeIntervalOptions.Custom;

        public TimeInterval Convert()
        {
            if (TimeIntervalOptions == TimeIntervalOptions.Custom
                && !String.IsNullOrWhiteSpace(StartDateTimeInUtc)
                && !String.IsNullOrWhiteSpace(EndDateTimeInUtc))
            {
                return new TimeInterval(DateTimeOffset.Parse(StartDateTimeInUtc), DateTimeOffset.Parse(EndDateTimeInUtc));
            }
            else if (TimeIntervalOptions != TimeIntervalOptions.Custom
                && String.IsNullOrWhiteSpace(StartDateTimeInUtc)
                && String.IsNullOrWhiteSpace(EndDateTimeInUtc))
            {
                return new TimeInterval(TimeIntervalOptions);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(BasicTimeInterval)} was not populated correctly: '{TimeIntervalOptions}', '{StartDateTimeInUtc}' - '{EndDateTimeInUtc}'");
            }
        }
    }
}
