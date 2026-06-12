//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>Coordinator configuration.</summary>
    public sealed record CoordinatorOptions
    {
        /// <summary>The run id stamped on every envelope.</summary>
        public required string RunId { get; init; }

        /// <summary>Clock for envelope timestamps; replay uses recorded values instead.</summary>
        public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

        /// <summary>Bounded input queue capacity.</summary>
        public int QueueCapacity { get; init; } = 1024;

        /// <summary>
        /// Row capture (SPEC §8.4): <c>full</c> journals row cells inline (test default);
        /// <c>digest</c> replaces them with an authoritative-digest wrapper before
        /// journaling — the wire still carries full rows via emission substitution.
        /// </summary>
        public string RowCapture { get; init; } = "full";

        /// <summary>SQL capture (SPEC §8.4): <c>text</c> journals SQL inline; <c>digest</c> elides it the same way.</summary>
        public string SqlCapture { get; init; } = "text";
    }

    /// <summary>An outbound JSON-RPC message decided by Core, already journaled.</summary>
    public sealed record OutboundRpcMessage
    {
        /// <summary>Envelope kind: <c>rpc.out.result</c>, <c>rpc.out.error</c>, or <c>rpc.out.notify</c>.</summary>
        public required string Kind { get; init; }

        /// <summary>JSON-RPC id for results/errors; null for notifications.</summary>
        public string? Corr { get; init; }

        /// <summary>The request method answered, or the notification method.</summary>
        public required string Type { get; init; }

        /// <summary>Result body, error body (code/message/data), or notification params.</summary>
        public JsonElement? Body { get; init; }
    }

    /// <summary>A journaled effect request handed to the effect runner.</summary>
    public sealed record EffectWorkItem
    {
        /// <summary>Seq of the journaled <c>effect.req</c> envelope (the cause of the response).</summary>
        public required long CauseSeq { get; init; }

        /// <summary>Deterministic effect id from Core.</summary>
        public required string EffectId { get; init; }

        /// <summary>Effect name (for example <c>toy.delay</c>).</summary>
        public required string EffectName { get; init; }

        /// <summary>Effect arguments (sanitized; secrets resolve through the side table only).</summary>
        public required JsonElement Args { get; init; }
    }

    /// <summary>Re-entry point for effect results: everything comes back as an envelope (SPEC §9.3.7).</summary>
    public interface ICoordinatorInbox
    {
        /// <summary>Posts an <c>effect.res</c> envelope for a completed effect observation.</summary>
        ValueTask PostEffectResponseAsync(string effectId, string effectName, JsonElement? payload, long causeSeq);
    }

    /// <summary>The async edge that executes journaled effect requests (SPEC §9.4).</summary>
    public interface ISts2EffectRunner
    {
        /// <summary>Runs one effect; implementations post observations back through <paramref name="inbox"/>.</summary>
        void Run(EffectWorkItem effect, ICoordinatorInbox inbox);
    }
}
