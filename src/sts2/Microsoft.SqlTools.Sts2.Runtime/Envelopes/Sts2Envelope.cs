//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Runtime.Envelopes
{
    /// <summary>
    /// The universal communication unit (SPEC §8.1). Every RPC frame, command, event,
    /// effect, timer, config change, metric, and diagnostic becomes one of these, and it
    /// is journaled before it is dispatched.
    /// </summary>
    public sealed record Sts2Envelope
    {
        /// <summary>Envelope schema identifier; only <c>sts2.envelope/1</c> exists.</summary>
        public string Schema { get; init; } = EnvelopeJsonCodec.SchemaId;

        /// <summary>Stable id of the process run that produced this envelope.</summary>
        public required string RunId { get; init; }

        /// <summary>Gapless monotonic sequence number assigned by the coordinator (I5).</summary>
        public required long Seq { get; init; }

        /// <summary>UTC timestamp from the injected time provider; replay uses recorded values.</summary>
        public required DateTimeOffset Ts { get; init; }

        /// <summary>One of <see cref="EnvelopeKinds"/>.</summary>
        public required string Kind { get; init; }

        /// <summary>Logical connection/session id when applicable.</summary>
        public string? SessionId { get; init; }

        /// <summary>JSON-RPC id, effect id, or generated correlation id.</summary>
        public string? Corr { get; init; }

        /// <summary>Seq of the envelope that caused this one; null only for external inbound and root control events.</summary>
        public long? Cause { get; init; }

        /// <summary>RPC method, command name, event name, effect name, metric name, or diagnostic name.</summary>
        public required string Type { get; init; }

        /// <summary>Config snapshot version current when the envelope was created.</summary>
        public required int ConfigVersion { get; init; }

        /// <summary>SHA-256 of the canonical payload; present even when the payload is elided.</summary>
        public required string Digest { get; init; }

        /// <summary>Inline payload when the capture mode permits it.</summary>
        public JsonElement? Payload { get; init; }

        /// <summary>Redaction/elision metadata when the payload is omitted or partially opaque.</summary>
        public JsonElement? PayloadMeta { get; init; }
    }
}
