//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Core;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;

namespace Microsoft.SqlTools.Sts2.Runtime.Replay
{
    /// <summary>Where and how a replay diverged from the recorded journal (SPEC §13.2).</summary>
    public sealed record ReplayDivergence
    {
        /// <summary>Journal seq at which the divergence was detected.</summary>
        public required long Seq { get; init; }

        /// <summary>What the journal recorded (kind/type/digest).</summary>
        public required string Recorded { get; init; }

        /// <summary>What replay produced instead.</summary>
        public required string Replayed { get; init; }

        /// <summary>Cause chain from the divergent envelope back to its root.</summary>
        public required IReadOnlyList<long> CauseChain { get; init; }
    }

    /// <summary>Outcome of replaying one journal.</summary>
    public sealed record ReplayResult
    {
        /// <summary>True when the outbound digest sequence matched exactly (I7).</summary>
        public required bool Identical { get; init; }

        /// <summary>Digest of every outbound RPC envelope, in order, as produced by replay.</summary>
        public required IReadOnlyList<string> OutboundDigests { get; init; }

        /// <summary>First divergence, when not identical.</summary>
        public ReplayDivergence? Divergence { get; init; }

        /// <summary>Core state after the last replayed envelope (redacted by construction).</summary>
        public required CoreState FinalState { get; init; }

        /// <summary>Seq of the last envelope replayed.</summary>
        public required long LastSeq { get; init; }
    }

    /// <summary>
    /// Replays a journal through the pure reducer without re-executing effects
    /// (SPEC §13.2): recorded <c>effect.res</c> envelopes are fed back in, and every
    /// recorded output envelope is matched by causal position and digest against what
    /// the reducer produces now.
    /// </summary>
    public static class JournalReplayer
    {
        private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement;

        /// <summary>Replays <paramref name="envelopes"/>, optionally stopping after <paramref name="untilSeq"/>.</summary>
        public static ReplayResult Replay(IEnumerable<Sts2Envelope> envelopes, long? untilSeq = null)
        {
            ArgumentNullException.ThrowIfNull(envelopes);

            CoreState state = CoreState.Initial;
            var outboundDigests = new List<string>();
            var pendingOutputs = new Queue<(string Kind, string Type, string? Corr, string Digest)>();
            var causeBySeq = new Dictionary<long, long?>();
            long lastSeq = 0;

            foreach (Sts2Envelope envelope in envelopes)
            {
                if (untilSeq is long limit && envelope.Seq > limit)
                {
                    break;
                }
                lastSeq = envelope.Seq;
                causeBySeq[envelope.Seq] = envelope.Cause;

                switch (envelope.Kind)
                {
                    case EnvelopeKinds.RpcInRequest:
                    case EnvelopeKinds.RpcInNotify:
                    case EnvelopeKinds.EffectResponse:
                    case EnvelopeKinds.Control:
                    case EnvelopeKinds.TimerDue:
                    {
                        if (pendingOutputs.Count > 0)
                        {
                            (string kind, string type, _, string digest) = pendingOutputs.Dequeue();
                            return Diverged(envelope.Seq, causeBySeq, outboundDigests, state, lastSeq,
                                recorded: $"next input {envelope.Kind}/{envelope.Type}",
                                replayed: $"an additional output {kind}/{type} digest={digest} the journal does not record");
                        }

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
                            CoreOutputEncoder.EncodedOutput encoded = CoreOutputEncoder.Encode(output, envelope.Type);
                            pendingOutputs.Enqueue((encoded.Kind, encoded.Type, encoded.Corr,
                                CanonicalJson.DigestOf(encoded.Payload ?? NullElement)));
                        }
                        break;
                    }

                    case EnvelopeKinds.RpcOutResult:
                    case EnvelopeKinds.RpcOutError:
                    case EnvelopeKinds.RpcOutNotify:
                    case EnvelopeKinds.EffectRequest:
                    case EnvelopeKinds.Diagnostic:
                    {
                        if (pendingOutputs.Count == 0)
                        {
                            return Diverged(envelope.Seq, causeBySeq, outboundDigests, state, lastSeq,
                                recorded: $"{envelope.Kind}/{envelope.Type} digest={envelope.Digest}",
                                replayed: "no output at this causal position");
                        }
                        (string kind, string type, _, string digest) = pendingOutputs.Dequeue();
                        if (kind != envelope.Kind || type != envelope.Type || digest != envelope.Digest)
                        {
                            return Diverged(envelope.Seq, causeBySeq, outboundDigests, state, lastSeq,
                                recorded: $"{envelope.Kind}/{envelope.Type} digest={envelope.Digest}",
                                replayed: $"{kind}/{type} digest={digest}");
                        }
                        if (kind is EnvelopeKinds.RpcOutResult or EnvelopeKinds.RpcOutError or EnvelopeKinds.RpcOutNotify)
                        {
                            outboundDigests.Add(digest);
                        }
                        break;
                    }

                    default:
                        break; // metric, config.changed, state.snapshot: not replay-relevant at M1
                }
            }

            return new ReplayResult
            {
                Identical = true,
                OutboundDigests = outboundDigests,
                FinalState = state,
                LastSeq = lastSeq,
            };
        }

        /// <summary>Walks the cause chain from <paramref name="seq"/> to its root using recorded causes.</summary>
        public static IReadOnlyList<long> CauseChainOf(IReadOnlyDictionary<long, long?> causeBySeq, long seq)
        {
            var chain = new List<long> { seq };
            long current = seq;
            while (causeBySeq.TryGetValue(current, out long? cause) && cause is long parent && !chain.Contains(parent))
            {
                chain.Add(parent);
                current = parent;
            }
            return chain;
        }

        private static ReplayResult Diverged(
            long seq,
            Dictionary<long, long?> causeBySeq,
            List<string> outboundDigests,
            CoreState state,
            long lastSeq,
            string recorded,
            string replayed) => new()
        {
            Identical = false,
            OutboundDigests = outboundDigests,
            FinalState = state,
            LastSeq = lastSeq,
            Divergence = new ReplayDivergence
            {
                Seq = seq,
                Recorded = recorded,
                Replayed = replayed,
                CauseChain = CauseChainOf(causeBySeq, seq),
            },
        };

        /// <summary>
        /// Deterministic redacted JSON dump of a Core state (SPEC §12.2, I16), in the one
        /// shared <see cref="CoreStateDump"/> format so replay state compares byte-for-byte
        /// against the live <c>diagnostics.state</c> Core portion.
        /// </summary>
        public static string DumpState(CoreState state, long atSeq) => CoreStateDump.ToJson(state, atSeq);
    }
}
