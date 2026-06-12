//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// Core's view of one journaled input envelope (SPEC §9.2). Pure data: ids, names,
    /// and an immutable JSON payload — never driver handles, tokens, or live resources.
    /// </summary>
    public sealed record CoreEnvelope
    {
        /// <summary>Journal sequence number; Core derives all generated ids from it.</summary>
        public required long Seq { get; init; }

        /// <summary>Envelope kind (<c>rpc.in.request</c>, <c>effect.res</c>, <c>control</c>, ...).</summary>
        public required string Kind { get; init; }

        /// <summary>RPC method, effect name, or control signal name.</summary>
        public required string Type { get; init; }

        /// <summary>Logical session id when applicable.</summary>
        public string? SessionId { get; init; }

        /// <summary>Correlation id (JSON-RPC id or effect id).</summary>
        public string? Corr { get; init; }

        /// <summary>Sanitized payload; secrets are already SecretRef tokens.</summary>
        public JsonElement? Payload { get; init; }
    }
}
