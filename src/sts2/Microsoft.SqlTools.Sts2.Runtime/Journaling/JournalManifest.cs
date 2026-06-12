//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.Sts2.Runtime.Journaling
{
    /// <summary>Static facts about the run that produced a journal (SPEC §8.3).</summary>
    public sealed record JournalRunInfo
    {
        /// <summary>Service assembly version.</summary>
        public required string ServiceVersion { get; init; }

        /// <summary>Git commit the binary was built from, when known.</summary>
        public string? GitCommit { get; init; }

        /// <summary>Command-line flags with secrets removed.</summary>
        public IReadOnlyList<string> CommandLine { get; init; } = [];
    }

    /// <summary>One closed or active journal segment.</summary>
    public sealed record JournalSegment
    {
        /// <summary>Segment file name relative to the journal directory.</summary>
        public required string FileName { get; init; }

        /// <summary>Exact byte count of the segment file.</summary>
        public required long Bytes { get; init; }

        /// <summary><c>sha256:&lt;hex&gt;</c> of the segment bytes.</summary>
        public required string Sha256 { get; init; }

        /// <summary>The previous segment's hash, chaining segments; null for the first.</summary>
        public string? PreviousSegmentSha256 { get; init; }
    }

    /// <summary>The journal manifest (<c>journal-&lt;runId&gt;.manifest.json</c>).</summary>
    public sealed record JournalManifest
    {
        /// <summary>Manifest schema identifier.</summary>
        public string Schema { get; init; } = "sts2.journal.manifest/1";

        /// <summary>The run that produced this journal.</summary>
        public required string RunId { get; init; }

        /// <summary>Service assembly version.</summary>
        public required string ServiceVersion { get; init; }

        /// <summary>Git commit when known.</summary>
        public string? GitCommit { get; init; }

        /// <summary>Process id of the producing run.</summary>
        public required int ProcessId { get; init; }

        /// <summary>Operating system description.</summary>
        public required string Os { get; init; }

        /// <summary>Runtime framework description.</summary>
        public required string RuntimeVersion { get; init; }

        /// <summary>Command-line flags with secrets removed.</summary>
        public IReadOnlyList<string> CommandLine { get; init; } = [];

        /// <summary>Segments in write order with their hash chain.</summary>
        public IReadOnlyList<JournalSegment> Segments { get; init; } = [];

        /// <summary>UTC timestamp the manifest was last written.</summary>
        public required DateTimeOffset WrittenAt { get; init; }
    }
}
