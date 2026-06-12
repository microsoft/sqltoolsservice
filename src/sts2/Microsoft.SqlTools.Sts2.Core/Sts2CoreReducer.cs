//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// The pure synchronous reducer (SPEC §9.2): no I/O, no time, no randomness, no
    /// exceptions as control flow. At M1 it implements the toy machine that proves the
    /// coordinator/journal/replay loop; real verticals replace the toy in M2/M3.
    /// </summary>
    public static class Sts2CoreReducer
    {
        private const int JsonRpcInvalidRequest = -32600;

        /// <summary>Decides the next state and outputs for one journaled input envelope.</summary>
        public static CoreDecision Decide(CoreState state, CoreEnvelope envelope)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(envelope);

            CoreState advanced = state with { LastSeq = envelope.Seq };
            return envelope.Kind switch
            {
                "rpc.in.request" => DecideRequest(advanced, envelope),
                "effect.res" => DecideEffectResponse(advanced, envelope),
                "control" => DecideControl(advanced, envelope),
                "rpc.in.notify" => CoreDecision.StateOnly(advanced), // no toy notifications yet
                _ => Unexpected(advanced, envelope, "unhandled envelope kind"),
            };
        }

        private static CoreDecision DecideRequest(CoreState state, CoreEnvelope envelope)
        {
            if (envelope.Corr is null)
            {
                return Unexpected(state, envelope, "request without corr");
            }

            switch (envelope.Type)
            {
                case "v2/toy.echo":
                {
                    CoreState next = state with { ToyCounter = state.ToyCounter + 1 };
                    string text = envelope.Payload is { } p && p.TryGetProperty("text", out JsonElement t) && t.ValueKind == JsonValueKind.String
                        ? t.GetString()!
                        : string.Empty;
                    JsonElement result = JsonDocument.Parse(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{{\"echo\":{JsonSerializer.Serialize(text)},\"counter\":{next.ToyCounter}}}")).RootElement;
                    return new CoreDecision(next, [new RpcResultOutput(envelope.Corr, result)]);
                }

                case "v2/toy.effect":
                {
                    // Deterministic id derived from the journal seq (SPEC §9.3.2).
                    string effectId = string.Create(CultureInfo.InvariantCulture, $"eff-{envelope.Seq}");
                    CoreState next = state with
                    {
                        PendingToyEffects = state.PendingToyEffects.Add(effectId, envelope.Corr),
                    };
                    JsonElement args = envelope.Payload ?? JsonDocument.Parse("{}").RootElement;
                    return new CoreDecision(next, [new EffectRequestOutput(effectId, "toy.delay", args, envelope.Corr)]);
                }

                default:
                    return new CoreDecision(state,
                    [
                        new RpcErrorOutput(envelope.Corr, JsonRpcInvalidRequest,
                            "Unknown v2 method.", "Sts2.InvalidRequest"),
                    ]);
            }
        }

        private static CoreDecision DecideEffectResponse(CoreState state, CoreEnvelope envelope)
        {
            if (envelope.Corr is null || !state.PendingToyEffects.TryGetValue(envelope.Corr, out string? rpcCorr))
            {
                return Unexpected(state, envelope, "effect response for unknown effect id");
            }

            CoreState next = state with { PendingToyEffects = state.PendingToyEffects.Remove(envelope.Corr) };
            JsonElement result = JsonDocument.Parse(string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"effectId\":{JsonSerializer.Serialize(envelope.Corr)},\"observed\":{(envelope.Payload?.GetRawText() ?? "null")}}}")).RootElement;
            return new CoreDecision(next, [new RpcResultOutput(rpcCorr, result)]);
        }

        private static CoreDecision DecideControl(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
            "lifecycle.shutdown" or "lifecycle.exit" => CoreDecision.StateOnly(state with { ShuttingDown = true }),
            _ => Unexpected(state, envelope, "unknown control signal"),
        };

        private static CoreDecision Unexpected(CoreState state, CoreEnvelope envelope, string reason)
        {
            // Invalid input is a stable diagnostic output, never an exception (SPEC §9.2).
            JsonElement data = JsonDocument.Parse(string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"reason\":{JsonSerializer.Serialize(reason)},\"kind\":{JsonSerializer.Serialize(envelope.Kind)},\"type\":{JsonSerializer.Serialize(envelope.Type)},\"seq\":{envelope.Seq}}}")).RootElement;
            return new CoreDecision(state, [new DiagnosticOutput("core.unexpectedInput", data)]);
        }
    }
}
