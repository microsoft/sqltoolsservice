//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Core;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;

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
        private readonly CoordinatorOptions options;
        private readonly ISts2EffectRunner effectRunner;
        private readonly Action<OutboundRpcMessage> emitRpc;
        private readonly Channel<PendingInput> inputs;
        private readonly Task pump;

        private CoreState state = CoreState.Initial;
        private long seqCounter;

        /// <summary>Creates the coordinator and starts its pump. Takes ownership of <paramref name="journal"/>.</summary>
        public Coordinator(
            JournalWriter journal,
            CoordinatorOptions options,
            ISts2EffectRunner effectRunner,
            Action<OutboundRpcMessage> emitRpc)
        {
            this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.effectRunner = effectRunner ?? throw new ArgumentNullException(nameof(effectRunner));
            this.emitRpc = emitRpc ?? throw new ArgumentNullException(nameof(emitRpc));

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

        /// <summary>Posts an inbound JSON-RPC request.</summary>
        public ValueTask PostRpcRequestAsync(string method, string corr, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInRequest, method, sessionId, corr, payload, Cause: null));

        /// <summary>Posts an inbound JSON-RPC notification.</summary>
        public ValueTask PostRpcNotificationAsync(string method, JsonElement? payload, string? sessionId = null) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.RpcInNotify, method, sessionId, null, payload, Cause: null));

        /// <summary>Posts a root control signal (for example <c>lifecycle.shutdown</c>).</summary>
        public ValueTask PostControlAsync(string signal) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.Control, signal, null, null, null, Cause: null));

        /// <inheritdoc/>
        public ValueTask PostEffectResponseAsync(string effectId, string effectName, JsonElement? payload, long causeSeq) =>
            inputs.Writer.WriteAsync(new PendingInput(EnvelopeKinds.EffectResponse, effectName, null, effectId, payload, causeSeq));

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
                Sts2Envelope envelope = BuildEnvelope(input.Kind, input.Type, input.SessionId, input.Corr, input.Payload, input.Cause);
                await journal.AppendAsync(envelope, flush: IsFlushPoint(envelope.Kind)).ConfigureAwait(false);

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
            switch (output)
            {
                case RpcResultOutput result:
                {
                    Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.RpcOutResult, requestType, null, result.Corr, result.Result, causeSeq);
                    await journal.AppendAsync(envelope, flush: true).ConfigureAwait(false);
                    Emit(new OutboundRpcMessage { Kind = envelope.Kind, Corr = result.Corr, Type = requestType, Body = result.Result });
                    break;
                }

                case RpcErrorOutput error:
                {
                    JsonElement body = JsonDocument.Parse(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{{\"code\":{error.JsonRpcCode},\"message\":{JsonSerializer.Serialize(error.Message)},\"data\":{{\"code\":{JsonSerializer.Serialize(error.DataCode)},\"retryable\":false,\"corr\":{JsonSerializer.Serialize(error.Corr)}}}}}")).RootElement;
                    Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.RpcOutError, requestType, null, error.Corr, body, causeSeq);
                    await journal.AppendAsync(envelope, flush: true).ConfigureAwait(false);
                    Emit(new OutboundRpcMessage { Kind = envelope.Kind, Corr = error.Corr, Type = requestType, Body = body });
                    break;
                }

                case RpcNotifyOutput notify:
                {
                    Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.RpcOutNotify, notify.Method, null, null, notify.Params, causeSeq);
                    await journal.AppendAsync(envelope, flush: false).ConfigureAwait(false);
                    Emit(new OutboundRpcMessage { Kind = envelope.Kind, Corr = null, Type = notify.Method, Body = notify.Params });
                    break;
                }

                case EffectRequestOutput effect:
                {
                    Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.EffectRequest, effect.EffectName, null, effect.EffectId, effect.Args, causeSeq);
                    await journal.AppendAsync(envelope, flush: false).ConfigureAwait(false);
                    RunEffect(new EffectWorkItem
                    {
                        CauseSeq = envelope.Seq,
                        EffectId = effect.EffectId,
                        EffectName = effect.EffectName,
                        Args = effect.Args,
                    });
                    break;
                }

                case DiagnosticOutput diagnostic:
                {
                    Sts2Envelope envelope = BuildEnvelope(EnvelopeKinds.Diagnostic, diagnostic.Name, null, null, diagnostic.Data, causeSeq);
                    await journal.AppendAsync(envelope, flush: true).ConfigureAwait(false);
                    break;
                }

                default:
                    throw new InvalidOperationException("Unknown CoreOutput type: " + output.GetType().Name);
            }
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
                emitRpc(message);
            }
            catch (Exception)
            {
                // The gateway owns transport failures; the journal already has the truth.
            }
        }

        private void RunEffect(EffectWorkItem effect)
        {
            try
            {
                effectRunner.Run(effect, this);
            }
            catch (Exception)
            {
                // Effect runner faults surface as missing effect.res; M2 adds fault envelopes.
            }
        }
    }
}
