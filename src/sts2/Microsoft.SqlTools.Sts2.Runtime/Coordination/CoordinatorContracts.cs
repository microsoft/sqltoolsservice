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
        /// Journal a <c>metric</c> snapshot envelope every N processed inputs (SPEC §12.3).
        /// 0 (default) disables metric journaling — the live metrics channel and health
        /// still report counts. The cadence is counted in inputs, so it is deterministic
        /// per run; metric envelopes are journaled-only (never dispatched, replay-skipped).
        /// </summary>
        public int MetricSampleEvery { get; init; }
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

    /// <summary>
    /// Optional runtime-handle counters an effect runner can expose for the health snapshot
    /// (SPEC §12.1). These are Runtime facts Core cannot see across the pure boundary, so
    /// they are merged into the health response at the coordinator edge.
    /// </summary>
    public interface IEffectRunnerDiagnostics
    {
        /// <summary>Live driver-session leases currently held (I8).</summary>
        int OpenLeases { get; }

        /// <summary>Open attempts whose driver call has not yet resolved.</summary>
        int OpensInFlight { get; }

        /// <summary>Query pumps still streaming.</summary>
        int ActiveQueryPumps { get; }
    }
}
