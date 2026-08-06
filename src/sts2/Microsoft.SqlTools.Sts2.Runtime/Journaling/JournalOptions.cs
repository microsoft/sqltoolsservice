//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Sts2.Runtime.Journaling
{
    /// <summary>Journal configuration; defaults are the SPEC §11.2 pinned values.</summary>
    public sealed record JournalOptions
    {
        /// <summary>Directory holding segments and the manifest (<c>&lt;log-dir&gt;/sts2/</c>).</summary>
        public required string Directory { get; init; }

        /// <summary>Segment rotation threshold (<c>sts2.journal.segmentBytes</c>).</summary>
        public long SegmentBytes { get; init; } = 64 * 1024 * 1024;

        /// <summary>Bounded flush interval for high-volume streams (<c>sts2.journal.flushIntervalMs</c>).</summary>
        public int FlushIntervalMs { get; init; } = 250;

        /// <summary>Clock for the deferred-flush bound and manifest timestamps.</summary>
        public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
    }
}
