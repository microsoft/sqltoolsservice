//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Core;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Observability;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>
    /// The single coordinator pump (SPEC §9.1). For every input envelope, in order:
    /// assign seq/ts/runId/configVersion, journal it (write-ahead), dispatch to the pure
    /// Core reducer, journal every produced output, then emit RPC output and schedule
    /// effects. The journal order is the truth.
    /// </summary>
    public sealed class Coordinator : ICoordinatorInbox, IAsyncDisposable
    {
        private sealed record PendingInput(string Kind, string Type, string? SessionId, string? Corr, JsonElement? Payload, long? Cause,
            TaskCompletionSource? Committed = null)
        {
            public long EnqueuedTicks { get; } = Stopwatch.GetTimestamp();
        }

        private readonly record struct OutputProcessStats(
            double EncodeMs,
            long EncodeAllocatedBytes,
            double EnvelopeBuildMs,
            long EnvelopeBuildAllocatedBytes,
            double JournalMs,
            double ActionMs,
            long ActionAllocatedBytes,
            double SubstitutionMs,
            long SubstitutionAllocatedBytes,
            double GatewayEmitMs,
            long GatewayEmitAllocatedBytes);

        private readonly record struct EmitProcessStats(
            double SubstitutionMs,
            long SubstitutionAllocatedBytes,
            double GatewayEmitMs,
            long GatewayEmitAllocatedBytes);

        private sealed class QueryCoordinatorPipelineStats
        {
            public long Pages;
            public long CaptureCanonicalBytes;
            public double QueueWaitMsTotal;
            public double CaptureMsTotal;
            public long CaptureAllocatedBytes;
            public double InputEnvelopeBuildMsTotal;
            public long InputEnvelopeBuildAllocatedBytes;
            public double InputJournalMsTotal;
            public double CoreMsTotal;
            public long CoreAllocatedBytes;
            public double OutputEncodeMsTotal;
            public long OutputEncodeAllocatedBytes;
            public double OutputEnvelopeBuildMsTotal;
            public long OutputEnvelopeBuildAllocatedBytes;
            public double OutputJournalMsTotal;
            public double OutputActionMsTotal;
            public long OutputActionAllocatedBytes;
            public double OutputSubstitutionMsTotal;
            public long OutputSubstitutionAllocatedBytes;
            public double OutputGatewayEmitMsTotal;
            public long OutputGatewayEmitAllocatedBytes;
        }

        private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

        private readonly JournalWriter journal;
        private readonly MetricsEnvelopeSink metrics;
        private readonly CompositeEnvelopeSink auxSinks;
        private readonly CoordinatorOptions options;
        private readonly ISts2EffectRunner effectRunner;
        private readonly Action<OutboundRpcMessage> emitRpc;
        private readonly Channel<PendingInput> inputs;
        private readonly Task pump;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonElement> elidedFragments = new(StringComparer.Ordinal);
        private readonly List<string> capturedKeys = new(); // per-turn elided-fragment keys (R005)
        private readonly Dictionary<string, QueryCoordinatorPipelineStats> queryPipelineStats = new(StringComparer.Ordinal);
        private CoreState state;
        private long seqCounter;
        private long emitFaults;
        private long effectFaults;
        private long inputsProcessed;
        private volatile string? fatalReason;

        /// <summary>Creates the coordinator and starts its pump. Takes ownership of <paramref name="journal"/>.</summary>
        /// <param name="journal">The write-ahead journal — the privileged first sink.</param>
        /// <param name="options">Coordinator configuration.</param>
        /// <param name="effectRunner">The async edge that executes journaled effects.</param>
        /// <param name="emitRpc">Sends an outbound RPC message to the gateway.</param>
        /// <param name="auxSinks">
        /// Auxiliary envelope observers (metrics, live tail, test capture). They see every
        /// envelope AFTER journaling, in seq order, best-effort: a faulty sink is counted
        /// and skipped, never stalling the pump or breaking write-ahead (SPEC §12).
        /// </param>
        public Coordinator(
            JournalWriter journal,
            CoordinatorOptions options,
            ISts2EffectRunner effectRunner,
            Action<OutboundRpcMessage> emitRpc,
            IReadOnlyList<IEnvelopeSink>? auxSinks = null)
        {
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.effectRunner = effectRunner ?? throw new ArgumentNullException(nameof(effectRunner));
            this.emitRpc = emitRpc ?? throw new ArgumentNullException(nameof(emitRpc));

            // Metrics are first-class coordinator state: the metrics sink always runs ahead
            // of caller-supplied observers, and health reads its tallies directly.
            metrics = new MetricsEnvelopeSink();
            var allSinks = new List<IEnvelopeSink> { metrics };
            if (auxSinks is not null)
            {
                allSinks.AddRange(auxSinks);
            }
            this.auxSinks = new CompositeEnvelopeSink(allSinks, onFault: (_, _) => Sts2EventSource.Log.SinkFaultObserved());
            state = CoreState.Initial; // session config enters via a journaled session.start envelope

            inputs = Channel.CreateBounded<PendingInput>(new BoundedChannelOptions(options.QueueCapacity)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait,
            });
            pump = Task.Run(RunPumpAsync);
        }

        /// <summary>Completes when the pump drains after the inbox is completed.</summary>
        public Task Completion => pump;

        /// <summary>The current Core state (already redacted by construction).</summary>
        public CoreState CurrentState => state;

        /// <summary>Latest journaled sequence number.</summary>
        public long LatestSeq => journal.LatestSeq;

        /// <summary>Current pending depth of the bounded input queue (SPEC §12.1).</summary>
        public int QueueDepth => inputs.Reader.Count;

        /// <summary>Auxiliary-sink faults swallowed (a misbehaving observer, SPEC §12.1).</summary>
        public long SinkFaultCount => auxSinks.FaultCount;

        /// <summary>Outbound emissions dropped by a transport fault (SPEC §12.1 dropped diagnostics).</summary>
        public long EmitFaultCount => System.Threading.Interlocked.Read(ref emitFaults);

        /// <summary>Effect dispatches that faulted before reaching the runner.</summary>
        public long EffectFaultCount => System.Threading.Interlocked.Read(ref effectFaults);

        /// <summary>The metrics tallies over this session's envelope stream (SPEC §12.3).</summary>
        public MetricsEnvelopeSink Metrics => metrics;

        /// <summary>Current config snapshot version stamped on envelopes (SPEC §8.4), owned by Core state.</summary>
        public int ConfigVersion => state.ConfigVersion;

        /// <summary>Non-null once the pump has faulted fatally; the redacted reason (SPEC §12.1).</summary>
        public string? FatalReason => fatalReason;

        /// <summary>Posts an inbound JSON-RPC request.</summary>
        public ValueTask PostRpcRequestAsync(string method, string corr, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInRequest, method, sessionId, corr, payload, Cause: null));

        /// <summary>Posts an inbound JSON-RPC notification.</summary>
        public ValueTask PostRpcNotificationAsync(string method, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInNotify, method, sessionId, null, payload, Cause: null));

        /// <summary>Posts a root control signal (for example <c>session.start</c> or <c>lifecycle.shutdown</c>).</summary>
        public ValueTask PostControlAsync(string signal, JsonElement? payload = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.Control, signal, null, null, payload, Cause: null));

        /// <summary>
        /// Posts a control signal and returns a task that completes only after the PUMP has
        /// journaled it, drained its outputs, and flushed (a real write-ahead barrier, R002).
        /// Enqueuing alone does not prove the tail is durable — the multiplexer's bounded
        /// shutdown/exit wait depends on this actually meaning "committed to disk".
        /// </summary>
        public async Task PostControlBarrierAsync(string signal, JsonElement? payload = null)
        {
            var committed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await inputs.Writer.WriteAsync(
                new PendingInput(EnvelopeKinds.Control, signal, null, null, payload, Cause: null, Committed: committed)).ConfigureAwait(false);
            await committed.Task.ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public ValueTask PostEffectResponseAsync(string effectId, string effectName, JsonElement? payload, long causeSeq) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.EffectResponse, effectName, null, effectId, payload, causeSeq));

        /// <summary>Flushes the journal to disk (lifecycle flush, SPEC §6.2).</summary>
        public ValueTask FlushJournalAsync() => journal.FlushAsync();

        /// <summary>Stops accepting input, drains the pump, and closes the journal.</summary>
        public async ValueTask DisposeAsync()
        {
            inputs.Writer.TryComplete();
            try
            {
                await pump.ConfigureAwait(false);
            }
            catch
            {
                // A fatal pump fault is already recorded in FatalReason; disposing must still
                // close the journal, so swallow here rather than masking it.
            }
            finally
            {
                // Fail any barrier the (possibly faulted) pump never reached, so a lifecycle
                // wait cannot hang on a dead pump (R002). The pump is the only reader and has
                // now stopped, so draining here is race-free.
                while (inputs.Reader.TryRead(out PendingInput? leftover))
                {
                    leftover.Committed?.TrySetException(
                        new InvalidOperationException("STS2 stopped before the barrier committed."));
                }
                elidedFragments.Clear(); // bound the side table: drop any unsubstituted fragments
                queryPipelineStats.Clear();
                await journal.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task RunPumpAsync()
        {
            try
            {
                await foreach (PendingInput input in inputs.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    string? queryId = GetQueryEventString(input, "queryId");
                    string? queryEventType = GetQueryEventString(input, "eventType");
                    bool isRowsEvent = queryId is not null && queryEventType == "rows";
                    bool isTerminalQueryEvent = queryId is not null
                        && queryEventType is "completed" or "error" or "canceled";
                    double queueWaitMs = ElapsedMs(input.EnqueuedTicks);

                    // Capture modes come from Core state, so a runtime setCapture applies to
                    // the next envelope (the state field still holds the pre-decision state).
                    // capturedKeys records the elided fragments this turn added, so any the
                    // outputs do not substitute back (a rejected execute, a suppressed row
                    // event) are released at the end of the turn instead of lingering (R005).
                    capturedKeys.Clear();
                    long captureAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                    long captureStartTicks = Stopwatch.GetTimestamp();
                    JsonElement? captured = CaptureElision.ElideInput(
                        state.RowCapture, state.SqlCapture, input.Kind, input.Type, input.Payload, elidedFragments, capturedKeys);
                    double captureMs = ElapsedMs(captureStartTicks);
                    long captureAllocatedBytes = CurrentThreadAllocatedSince(captureAllocationStart);

                    long envelopeAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                    long envelopeStartTicks = Stopwatch.GetTimestamp();
                    Sts2Envelope envelope = BuildEnvelope(input.Kind, input.Type, input.SessionId, input.Corr, captured, input.Cause);
                    double inputEnvelopeBuildMs = ElapsedMs(envelopeStartTicks);
                    long inputEnvelopeBuildAllocatedBytes = CurrentThreadAllocatedSince(envelopeAllocationStart);

                    long inputJournalStartTicks = Stopwatch.GetTimestamp();
                    await JournalAsync(envelope, flush: DurabilityPolicy.IsCheckpoint(envelope.Kind, envelope.Type)).ConfigureAwait(false);
                    double inputJournalMs = ElapsedMs(inputJournalStartTicks);

                    long coreAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                    long coreStartTicks = Stopwatch.GetTimestamp();
                    CoreDecision decision = Sts2CoreReducer.Decide(state, new CoreEnvelope
                    {
                        Seq = envelope.Seq,
                        Kind = envelope.Kind,
                        Type = envelope.Type,
                        SessionId = envelope.SessionId,
                        Corr = envelope.Corr,
                        Payload = envelope.Payload,
                    });
                    double coreMs = ElapsedMs(coreStartTicks);
                    long coreAllocatedBytes = CurrentThreadAllocatedSince(coreAllocationStart);
                    state = decision.NewState;

                    OutputProcessStats outputProcessStats = default;
                    foreach (CoreOutput output in decision.Outputs)
                    {
                        OutputProcessStats current = await JournalAndActAsync(
                            output,
                            causeSeq: envelope.Seq,
                            requestType: envelope.Type).ConfigureAwait(false);
                        outputProcessStats = Add(outputProcessStats, current);
                    }

                    if (isRowsEvent)
                    {
                        QueryCoordinatorPipelineStats stats = GetOrCreateQueryPipelineStats(queryId!);
                        stats.Pages++;
                        stats.CaptureCanonicalBytes += GetCaptureCanonicalBytes(captured);
                        stats.QueueWaitMsTotal += queueWaitMs;
                        stats.CaptureMsTotal += captureMs;
                        stats.CaptureAllocatedBytes += captureAllocatedBytes;
                        stats.InputEnvelopeBuildMsTotal += inputEnvelopeBuildMs;
                        stats.InputEnvelopeBuildAllocatedBytes += inputEnvelopeBuildAllocatedBytes;
                        stats.InputJournalMsTotal += inputJournalMs;
                        stats.CoreMsTotal += coreMs;
                        stats.CoreAllocatedBytes += coreAllocatedBytes;
                        stats.OutputEncodeMsTotal += outputProcessStats.EncodeMs;
                        stats.OutputEncodeAllocatedBytes += outputProcessStats.EncodeAllocatedBytes;
                        stats.OutputEnvelopeBuildMsTotal += outputProcessStats.EnvelopeBuildMs;
                        stats.OutputEnvelopeBuildAllocatedBytes += outputProcessStats.EnvelopeBuildAllocatedBytes;
                        stats.OutputJournalMsTotal += outputProcessStats.JournalMs;
                        stats.OutputActionMsTotal += outputProcessStats.ActionMs;
                        stats.OutputActionAllocatedBytes += outputProcessStats.ActionAllocatedBytes;
                        stats.OutputSubstitutionMsTotal += outputProcessStats.SubstitutionMs;
                        stats.OutputSubstitutionAllocatedBytes += outputProcessStats.SubstitutionAllocatedBytes;
                        stats.OutputGatewayEmitMsTotal += outputProcessStats.GatewayEmitMs;
                        stats.OutputGatewayEmitAllocatedBytes += outputProcessStats.GatewayEmitAllocatedBytes;
                    }

                    // Release fragments no output reclaimed (substitution removes consumed ones).
                    foreach (string key in capturedKeys)
                    {
                        elidedFragments.TryRemove(key, out _);
                    }

                    // Barrier (R002): this input and all causally-prior outputs are now journaled;
                    // flush and signal the caller that the tail is durable.
                    if (input.Committed is { } committed)
                    {
                        await journal.FlushAsync().ConfigureAwait(false);
                        committed.TrySetResult();
                    }

                    await MaybeSampleMetricsAsync(triggeringSeq: envelope.Seq).ConfigureAwait(false);

                    if (isTerminalQueryEvent
                        && queryPipelineStats.Remove(queryId!, out QueryCoordinatorPipelineStats? pipelineStats))
                    {
                        await JournalQueryPipelineStatsAsync(
                            queryId!,
                            queryEventType!,
                            envelope.Seq,
                            pipelineStats).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                // The pump is the single-threaded heart of the machine. A fault here is
                // fatal and must be visible, not silent (SPEC §12.1, §18.10) — record a
                // redacted reason for health and re-throw so Completion faults for crash
                // containment.
                fatalReason = ex.GetType().Name;
                throw;
            }
        }

        /// <summary>
        /// Journals a <c>metric</c> snapshot envelope on the configured input cadence
        /// (SPEC §12.3). The envelope is journaled-only (never dispatched to Core and
        /// skipped on replay), so it carries the metric history into the trace and exports
        /// without affecting replay determinism.
        /// </summary>
        private async ValueTask MaybeSampleMetricsAsync(long triggeringSeq)
        {
            inputsProcessed++;
            if (options.MetricSampleEvery <= 0 || inputsProcessed % options.MetricSampleEvery != 0)
            {
                return;
            }
            // The sample is caused by the input whose processing reached the cadence, so it
            // carries that cause rather than being a spurious root (R040, I5/trace-schema).
            JsonElement snapshot = JsonDocument.Parse(BuildMetricSnapshot().ToJsonString()).RootElement;
            Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.Metric, "sts2.snapshot", null, null, snapshot, cause: triggeringSeq);
            await JournalAsync(envelope, flush: false).ConfigureAwait(false);
        }

        /// <summary>
        /// Journals one replay-ignored, privacy-safe runtime metric after a query's terminal
        /// event. This prices the coordinator stages that occur after the driver page stats:
        /// queue wait, capture elision, envelope digests, journal appends, Core routing, and
        /// wire-edge restoration. Values are aggregate counts/bytes/timings only.
        /// </summary>
        private async ValueTask JournalQueryPipelineStatsAsync(
            string queryId,
            string terminalEventType,
            long causeSeq,
            QueryCoordinatorPipelineStats stats)
        {
            var snapshot = new JsonObject
            {
                ["queryId"] = queryId,
                ["status"] = terminalEventType,
                ["pages"] = stats.Pages,
                ["captureCanonicalBytes"] = stats.CaptureCanonicalBytes,
                ["queueWaitMsTotal"] = RoundMs(stats.QueueWaitMsTotal),
                ["captureMsTotal"] = RoundMs(stats.CaptureMsTotal),
                ["captureAllocatedBytes"] = stats.CaptureAllocatedBytes,
                ["inputEnvelopeBuildMsTotal"] = RoundMs(stats.InputEnvelopeBuildMsTotal),
                ["inputEnvelopeBuildAllocatedBytes"] = stats.InputEnvelopeBuildAllocatedBytes,
                ["inputJournalMsTotal"] = RoundMs(stats.InputJournalMsTotal),
                ["coreMsTotal"] = RoundMs(stats.CoreMsTotal),
                ["coreAllocatedBytes"] = stats.CoreAllocatedBytes,
                ["outputEncodeMsTotal"] = RoundMs(stats.OutputEncodeMsTotal),
                ["outputEncodeAllocatedBytes"] = stats.OutputEncodeAllocatedBytes,
                ["outputEnvelopeBuildMsTotal"] = RoundMs(stats.OutputEnvelopeBuildMsTotal),
                ["outputEnvelopeBuildAllocatedBytes"] = stats.OutputEnvelopeBuildAllocatedBytes,
                ["outputJournalMsTotal"] = RoundMs(stats.OutputJournalMsTotal),
                ["outputActionMsTotal"] = RoundMs(stats.OutputActionMsTotal),
                ["outputActionAllocatedBytes"] = stats.OutputActionAllocatedBytes,
                ["outputSubstitutionMsTotal"] = RoundMs(stats.OutputSubstitutionMsTotal),
                ["outputSubstitutionAllocatedBytes"] = stats.OutputSubstitutionAllocatedBytes,
                ["outputGatewayEmitMsTotal"] = RoundMs(stats.OutputGatewayEmitMsTotal),
                ["outputGatewayEmitAllocatedBytes"] = stats.OutputGatewayEmitAllocatedBytes,
            };
            JsonElement payload = JsonDocument.Parse(snapshot.ToJsonString()).RootElement;
            Sts2Envelope envelope = BuildEnvelope(
                EnvelopeKinds.Metric,
                "sts2.query.coordinator.stats",
                null,
                null,
                payload,
                cause: causeSeq);
            await JournalAsync(envelope, flush: false).ConfigureAwait(false);
        }

        private QueryCoordinatorPipelineStats GetOrCreateQueryPipelineStats(string queryId)
        {
            if (!queryPipelineStats.TryGetValue(queryId, out QueryCoordinatorPipelineStats? stats))
            {
                stats = new QueryCoordinatorPipelineStats();
                queryPipelineStats.Add(queryId, stats);
            }
            return stats;
        }

        private static string? GetQueryEventString(PendingInput input, string propertyName) =>
            input.Kind == EnvelopeKinds.EffectResponse
            && input.Type == "driver.queryEvent"
            && input.Payload is { ValueKind: JsonValueKind.Object } payload
            && payload.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static long GetCaptureCanonicalBytes(JsonElement? captured)
        {
            if (captured is not { ValueKind: JsonValueKind.Object } payload)
            {
                return 0;
            }
            foreach (string propertyName in new[] { "compact", "rows" })
            {
                if (payload.TryGetProperty(propertyName, out JsonElement wrapper)
                    && wrapper.ValueKind == JsonValueKind.Object
                    && wrapper.TryGetProperty("$redacted", out JsonElement redacted)
                    && redacted.ValueKind == JsonValueKind.True
                    && wrapper.TryGetProperty("bytes", out JsonElement bytes)
                    && bytes.TryGetInt64(out long result))
                {
                    return result;
                }
            }
            return 0;
        }

        private static OutputProcessStats Add(OutputProcessStats left, OutputProcessStats right) => new(
            left.EncodeMs + right.EncodeMs,
            left.EncodeAllocatedBytes + right.EncodeAllocatedBytes,
            left.EnvelopeBuildMs + right.EnvelopeBuildMs,
            left.EnvelopeBuildAllocatedBytes + right.EnvelopeBuildAllocatedBytes,
            left.JournalMs + right.JournalMs,
            left.ActionMs + right.ActionMs,
            left.ActionAllocatedBytes + right.ActionAllocatedBytes,
            left.SubstitutionMs + right.SubstitutionMs,
            left.SubstitutionAllocatedBytes + right.SubstitutionAllocatedBytes,
            left.GatewayEmitMs + right.GatewayEmitMs,
            left.GatewayEmitAllocatedBytes + right.GatewayEmitAllocatedBytes);

        private static double ElapsedMs(long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;

        private static double RoundMs(double value) => Math.Round(value, 2);

        private static long CurrentThreadAllocatedSince(long start) =>
            Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - start);

        private async Task<OutputProcessStats> JournalAndActAsync(CoreOutput output, long causeSeq, string requestType)
        {
            long encodeAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            long encodeStartTicks = Stopwatch.GetTimestamp();
            CoreOutputEncoder.EncodedOutput encoded = CoreOutputEncoder.Encode(output, requestType);
            double encodeMs = ElapsedMs(encodeStartTicks);
            long encodeAllocatedBytes = CurrentThreadAllocatedSince(encodeAllocationStart);

            long envelopeAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            long envelopeStartTicks = Stopwatch.GetTimestamp();
            Sts2Envelope envelope = BuildEnvelope(encoded.Kind, encoded.Type, null, encoded.Corr, encoded.Payload, causeSeq);
            double envelopeBuildMs = ElapsedMs(envelopeStartTicks);
            long envelopeBuildAllocatedBytes = CurrentThreadAllocatedSince(envelopeAllocationStart);

            long journalStartTicks = Stopwatch.GetTimestamp();
            await JournalAsync(envelope, flush: DurabilityPolicy.IsCheckpoint(encoded.Kind, encoded.Type)).ConfigureAwait(false);
            double journalMs = ElapsedMs(journalStartTicks);

            long actionAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            long actionStartTicks = Stopwatch.GetTimestamp();
            EmitProcessStats emitStats = default;
            switch (encoded.Kind)
            {
                case EnvelopeKinds.RpcOutResult:
                {
                    // The journaled health result is pure Core (replay-deterministic); the
                    // wire response carries the live Runtime overlay (SPEC §12.1), mirroring
                    // how capture elision keeps the journal authoritative while the wire is
                    // enriched.
                    JsonElement? body = encoded.Payload;
                    if (encoded.Payload is { } core)
                    {
                        if (encoded.Type == "v2/diagnostics.health")
                        {
                            body = OverlayRuntimeHealth(core);
                        }
                        else if (encoded.Type == "v2/diagnostics.state")
                        {
                            body = OverlayRuntimeState(core);
                        }
                    }
                    emitStats = Emit(new OutboundRpcMessage { Kind = encoded.Kind, Corr = encoded.Corr, Type = encoded.Type, Body = body });
                    break;
                }

                case EnvelopeKinds.RpcOutError:
                case EnvelopeKinds.RpcOutNotify:
                    emitStats = Emit(new OutboundRpcMessage { Kind = encoded.Kind, Corr = encoded.Corr, Type = encoded.Type, Body = encoded.Payload });
                    break;

                case EnvelopeKinds.EffectRequest:
                    RunEffect(new EffectWorkItem
                    {
                        CauseSeq = envelope.Seq,
                        EffectId = encoded.Corr!,
                        EffectName = encoded.Type,
                        Args = encoded.Payload!.Value,
                    });
                    break;

                case EnvelopeKinds.Diagnostic:
                case EnvelopeKinds.ConfigChanged:
                    break; // journaled; Core already applied the change to its state

                default:
                    throw new InvalidOperationException("Unhandled encoded output kind: " + encoded.Kind);
            }
            return new OutputProcessStats(
                encodeMs,
                encodeAllocatedBytes,
                envelopeBuildMs,
                envelopeBuildAllocatedBytes,
                journalMs,
                ElapsedMs(actionStartTicks),
                CurrentThreadAllocatedSince(actionAllocationStart),
                emitStats.SubstitutionMs,
                emitStats.SubstitutionAllocatedBytes,
                emitStats.GatewayEmitMs,
                emitStats.GatewayEmitAllocatedBytes);
        }

        /// <summary>
        /// Journals an envelope (write-ahead primary) then fans it out to auxiliary
        /// observers. The journal append is awaited before any observer runs and before
        /// the caller dispatches the envelope, preserving the write-ahead rule (§8.3).
        /// </summary>
        private async ValueTask JournalAsync(Sts2Envelope envelope, bool flush)
        {
            await journal.AppendAsync(envelope, flush).ConfigureAwait(false);
            await auxSinks.OnEnvelopeAsync(envelope, flush).ConfigureAwait(false);
        }

        private Sts2Envelope BuildEnvelope(string kind, string type, string? sessionId, string? corr, JsonElement? payload, long? cause) => new()
        {
            RunId = options.RunId,
            Seq = ++seqCounter,
            Ts = options.TimeProvider.GetUtcNow(),
            Kind = kind,
            SessionId = sessionId,
            Corr = corr,
            Cause = cause,
            Type = type,
            ConfigVersion = state.ConfigVersion,
            Digest = CanonicalJson.DigestOf(payload ?? NullElement),
            Payload = payload,
        };

        /// <summary>
        /// Merges the live Runtime health facts (SPEC §12.1) onto the pure-Core health
        /// result: queue depth, driver-handle leases, dropped-diagnostic counts, fatal
        /// status, config version, and the recent error histogram. None of this is in the
        /// journaled result, so replay stays deterministic.
        /// </summary>
        private JsonElement OverlayRuntimeHealth(JsonElement coreHealth)
        {
            JsonObject obj = JsonNode.Parse(coreHealth.GetRawText())!.AsObject();
            obj["configVersion"] = state.ConfigVersion;
            obj["queueDepth"] = QueueDepth;
            obj["fatal"] = fatalReason is not null;
            if (fatalReason is not null)
            {
                obj["fatalReason"] = fatalReason;
            }
            if (effectRunner is IEffectRunnerDiagnostics diag)
            {
                obj["openLeases"] = diag.OpenLeases;
                obj["opensInFlight"] = diag.OpensInFlight;
                obj["activeQueryPumps"] = diag.ActiveQueryPumps;
            }
            obj["droppedDiagnostics"] = new JsonObject
            {
                ["emit"] = EmitFaultCount,
                ["effect"] = EffectFaultCount,
                ["sink"] = SinkFaultCount,
            };
            obj["envelopesObserved"] = metrics.Total;
            // Lifetime (session-cumulative) error histogram — named precisely so operators do
            // not misread it as a recent/windowed count (R047).
            var histogram = new JsonObject();
            foreach (KeyValuePair<string, long> entry in metrics.ErrorsByCode())
            {
                histogram[entry.Key] = entry.Value;
            }
            obj["errorsByCodeTotal"] = histogram;
            return JsonDocument.Parse(obj.ToJsonString()).RootElement;
        }

        /// <summary>
        /// Attaches a <c>runtime</c> section to the live state response (SPEC §12.2): the
        /// driver-handle summaries and config version replay cannot know. The journaled
        /// state result stays the pure Core dump, so replay state matches it exactly.
        /// </summary>
        private JsonElement OverlayRuntimeState(JsonElement coreState)
        {
            JsonObject obj = JsonNode.Parse(coreState.GetRawText())!.AsObject();
            var runtime = new JsonObject
            {
                ["configVersion"] = state.ConfigVersion,
                ["queueDepth"] = QueueDepth,
                ["fatal"] = fatalReason is not null,
            };
            if (effectRunner is IEffectRunnerDiagnostics diag)
            {
                runtime["openLeases"] = diag.OpenLeases;
                runtime["opensInFlight"] = diag.OpensInFlight;
                runtime["activeQueryPumps"] = diag.ActiveQueryPumps;
            }
            obj["runtime"] = runtime;
            return JsonDocument.Parse(obj.ToJsonString()).RootElement;
        }

        /// <summary>Builds the <c>metric</c> envelope payload from the live tallies (SPEC §12.3).</summary>
        private JsonObject BuildMetricSnapshot()
        {
            var byKind = new JsonObject();
            foreach (KeyValuePair<string, long> entry in metrics.EnvelopesByKind())
            {
                byKind[entry.Key] = entry.Value;
            }
            var errors = new JsonObject();
            foreach (KeyValuePair<string, long> entry in metrics.ErrorsByCode())
            {
                errors[entry.Key] = entry.Value;
            }
            return new JsonObject
            {
                ["envelopes"] = metrics.Total,
                ["errors"] = metrics.Errors,
                ["queueDepth"] = QueueDepth,
                ["openLeases"] = effectRunner is IEffectRunnerDiagnostics d ? d.OpenLeases : 0,
                ["droppedSink"] = SinkFaultCount,
                ["byKind"] = byKind,
                ["errorsByCode"] = errors,
            };
        }

        private EmitProcessStats Emit(OutboundRpcMessage message)
        {
            long substitutionAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            long substitutionStartTicks = Stopwatch.GetTimestamp();
            try
            {
                // The journal records elided payloads; the wire gets the originals back.
                object? parameters = CaptureElision.SubstituteParameterObject(message.Body, elidedFragments);
                double substitutionMs = ElapsedMs(substitutionStartTicks);
                long substitutionAllocatedBytes = CurrentThreadAllocatedSince(substitutionAllocationStart);

                long gatewayAllocationStart = GC.GetAllocatedBytesForCurrentThread();
                long gatewayStartTicks = Stopwatch.GetTimestamp();
                emitRpc(parameters is null ? message : message with { ParameterObject = parameters });
                return new EmitProcessStats(
                    substitutionMs,
                    substitutionAllocatedBytes,
                    ElapsedMs(gatewayStartTicks),
                    CurrentThreadAllocatedSince(gatewayAllocationStart));
            }
            catch (Exception)
            {
                // The gateway owns transport failures; the journal already has the truth.
                // Count it so health can surface dropped outbound diagnostics (§12.1).
                System.Threading.Interlocked.Increment(ref emitFaults);
                return new EmitProcessStats(
                    ElapsedMs(substitutionStartTicks),
                    CurrentThreadAllocatedSince(substitutionAllocationStart),
                    0,
                    0);
            }
        }

        private void RunEffect(EffectWorkItem effect)
        {
            try
            {
                JsonElement? restored = CaptureElision.Substitute(effect.Args, elidedFragments);
                effectRunner.Run(restored is null ? effect : effect with { Args = restored.Value }, this);
            }
            catch (Exception)
            {
                // Effect runner faults surface as missing effect.res; counted for health (§12.1).
                System.Threading.Interlocked.Increment(ref effectFaults);
            }
        }
    }
}
