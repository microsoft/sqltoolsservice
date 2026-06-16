//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text.Json;
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
        private sealed record PendingInput(string Kind, string Type, string? SessionId, string? Corr, JsonElement? Payload, long? Cause);

        private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

        private readonly JournalWriter journal;
        private readonly CompositeEnvelopeSink auxSinks;
        private readonly CoordinatorOptions options;
        private readonly ISts2EffectRunner effectRunner;
        private readonly Action<OutboundRpcMessage> emitRpc;
        private readonly Channel<PendingInput> inputs;
        private readonly Task pump;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonElement> elidedFragments = new(StringComparer.Ordinal);
        private CoreState state;
        private long seqCounter;
        private long emitFaults;
        private long effectFaults;

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
            this.auxSinks = new CompositeEnvelopeSink(auxSinks ?? Array.Empty<IEnvelopeSink>());
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

        /// <summary>Posts an inbound JSON-RPC request.</summary>
        public ValueTask PostRpcRequestAsync(string method, string corr, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInRequest, method, sessionId, corr, payload, Cause: null));

        /// <summary>Posts an inbound JSON-RPC notification.</summary>
        public ValueTask PostRpcNotificationAsync(string method, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInNotify, method, sessionId, null, payload, Cause: null));

        /// <summary>Posts a root control signal (for example <c>session.start</c> or <c>lifecycle.shutdown</c>).</summary>
        public ValueTask PostControlAsync(string signal, JsonElement? payload = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.Control, signal, null, null, payload, Cause: null));

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
            finally
            {
                await journal.DisposeAsync().ConfigureAwait(false);
            }
        }

        private async Task RunPumpAsync()
        {
            await foreach (PendingInput input in inputs.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                JsonElement? captured = CaptureElision.ElideInput(options, input.Kind, input.Type, input.Payload, elidedFragments);
                Sts2Envelope envelope = BuildEnvelope(input.Kind, input.Type, input.SessionId, input.Corr, captured, input.Cause);
                await JournalAsync(envelope, flush: IsFlushPoint(envelope.Kind)).ConfigureAwait(false);

                CoreDecision decision = Sts2CoreReducer.Decide(state, new CoreEnvelope
                {
                    Seq = envelope.Seq,
                    Kind = envelope.Kind,
                    Type = envelope.Type,
                    SessionId = envelope.SessionId,
                    Corr = envelope.Corr,
                    Payload = envelope.Payload,
                });
                state = decision.NewState;

                foreach (CoreOutput output in decision.Outputs)
                {
                    await JournalAndActAsync(output, causeSeq: envelope.Seq, requestType: envelope.Type).ConfigureAwait(false);
                }
            }
        }

        private async Task JournalAndActAsync(CoreOutput output, long causeSeq, string requestType)
        {
            CoreOutputEncoder.EncodedOutput encoded = CoreOutputEncoder.Encode(output, requestType);
            Sts2Envelope envelope = BuildEnvelope(encoded.Kind, encoded.Type, null, encoded.Corr, encoded.Payload, causeSeq);
            await JournalAsync(envelope, flush: IsFlushPoint(encoded.Kind)).ConfigureAwait(false);

            switch (encoded.Kind)
            {
                case EnvelopeKinds.RpcOutResult:
                case EnvelopeKinds.RpcOutError:
                case EnvelopeKinds.RpcOutNotify:
                    Emit(new OutboundRpcMessage { Kind = encoded.Kind, Corr = encoded.Corr, Type = encoded.Type, Body = encoded.Payload });
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
                    break; // journaled; nothing to emit

                default:
                    throw new InvalidOperationException("Unhandled encoded output kind: " + encoded.Kind);
            }
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
            ConfigVersion = 1, // config machinery arrives with setCapture (M1 later slice / M2)
            Digest = CanonicalJson.DigestOf(payload ?? NullElement),
            Payload = payload,
        };

        private static bool IsFlushPoint(string kind) =>
            kind is EnvelopeKinds.RpcOutResult or EnvelopeKinds.RpcOutError
                 or EnvelopeKinds.Diagnostic or EnvelopeKinds.Control;

        private void Emit(OutboundRpcMessage message)
        {
            try
            {
                // The journal records elided payloads; the wire gets the originals back.
                JsonElement? restored = CaptureElision.Substitute(message.Body, elidedFragments);
                emitRpc(restored is null ? message : message with { Body = restored });
            }
            catch (Exception)
            {
                // The gateway owns transport failures; the journal already has the truth.
                // Count it so health can surface dropped outbound diagnostics (§12.1).
                System.Threading.Interlocked.Increment(ref emitFaults);
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
