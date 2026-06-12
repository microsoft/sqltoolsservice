//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Sts2.Contracts.Diagnostics
{
    /// <summary>Parameters of <c>v2/diagnostics.ping</c> (SPEC §7.2). All fields optional.</summary>
    public sealed class DiagnosticsPingParams
    {
        /// <summary>Optional text echoed back verbatim in the result.</summary>
        public string? Echo { get; init; }
    }

    /// <summary>Result of <c>v2/diagnostics.ping</c>: echo, health summary, latest journal seq.</summary>
    public sealed class DiagnosticsPingResult
    {
        /// <summary>The STS2 spec version (<see cref="Sts2WireConstants.SpecVersion"/>).</summary>
        public required string SpecVersion { get; init; }

        /// <summary>The service assembly version.</summary>
        public required string ServiceVersion { get; init; }

        /// <summary>The request's <see cref="DiagnosticsPingParams.Echo"/>, returned verbatim.</summary>
        public string? Echo { get; init; }

        /// <summary>Latest journal sequence number; 0 until the journal exists (M1).</summary>
        public long LatestJournalSeq { get; init; }

        /// <summary>One-word health summary: <c>ok</c> or <c>fatal</c>.</summary>
        public required string Health { get; init; }
    }
}
