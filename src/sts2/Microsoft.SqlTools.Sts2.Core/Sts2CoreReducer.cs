//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// The pure synchronous reducer (SPEC §9.2): no I/O, no time, no randomness, no
    /// exceptions as control flow. M2 implements initialize, ping, and the connection
    /// machine; the toy machine remains as spine scaffolding until M3.
    /// </summary>
    public static class Sts2CoreReducer
    {
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
                "rpc.in.notify" => CoreDecision.StateOnly(advanced), // ack notifications arrive in M3
                _ => Unexpected(advanced, envelope, "unhandled envelope kind"),
            };
        }

        private static CoreDecision DecideRequest(CoreState state, CoreEnvelope envelope)
        {
            if (envelope.Corr is null)
            {
                return Unexpected(state, envelope, "request without corr");
            }

            // SPEC §7.1: unknown fields are ignored unless they demand understanding.
            if (envelope.Payload is { ValueKind: JsonValueKind.Object } payload)
            {
                foreach (JsonProperty property in payload.EnumerateObject())
                {
                    if (property.Name.StartsWith("mustUnderstand_", StringComparison.Ordinal))
                    {
                        return Error(state, envelope.Corr, Sts2ErrorCodes.InvalidRequest,
                            "Request contains an unsupported mustUnderstand_ field: " + property.Name);
                    }
                }
            }

            return envelope.Type switch
            {
                "v2/initialize" => DecideInitialize(state, envelope),
                "v2/diagnostics.ping" => DecidePing(state, envelope),
                "v2/connection.open" => DecideConnectionOpen(state, envelope),
                "v2/connection.cancel" => DecideConnectionCancel(state, envelope),
                "v2/connection.close" => DecideConnectionClose(state, envelope),
                "v2/toy.echo" => DecideToyEcho(state, envelope),
                "v2/toy.effect" => DecideToyEffect(state, envelope),
                _ => Error(state, envelope.Corr, Sts2ErrorCodes.InvalidRequest, "Unknown v2 method."),
            };
        }

        // ---------------- initialize & ping ----------------

        private static CoreDecision DecideInitialize(CoreState state, CoreEnvelope envelope)
        {
            // Idempotent: repeated calls return the current summary, never reset state.
            var driverArray = new JsonArray();
            foreach (DriverDescriptor driver in state.Drivers)
            {
                driverArray.Add(new JsonObject
                {
                    ["name"] = driver.Name,
                    ["dialects"] = new JsonArray(driver.Dialects.Select(d => (JsonNode)JsonValue.Create(d)).ToArray()),
                    ["production"] = driver.Production,
                });
            }

            var result = new JsonObject
            {
                ["specVersion"] = Sts2WireConstants.SpecVersion,
                ["serviceVersion"] = state.ServiceVersion,
                ["capabilities"] = new JsonObject
                {
                    ["forwardOnlyStreaming"] = true,
                    ["oneActiveQueryPerConnection"] = true,
                    ["redactedReplay"] = true,
                    ["exportLog"] = false,
                },
                ["drivers"] = driverArray,
                ["limits"] = new JsonObject
                {
                    ["pageRows"] = Sts2Defaults.PageRows,
                    ["pageBytes"] = Sts2Defaults.PageBytes,
                    ["windowPages"] = Sts2Defaults.WindowPages,
                    ["maxCellBytes"] = Sts2Defaults.MaxCellBytes,
                    ["maxFrameBytes"] = Sts2Defaults.MaxFrameBytes,
                    ["maxConnections"] = Sts2Defaults.MaxConnections,
                },
                ["journal"] = new JsonObject
                {
                    ["capture"] = "digest",
                    ["sqlCapture"] = "digest",
                    ["latestSeq"] = envelope.Seq,
                },
            };
            return new CoreDecision(state with { Initialized = true },
                [new RpcResultOutput(envelope.Corr!, Json(result.ToJsonString()))]);
        }

        private static CoreDecision DecidePing(CoreState state, CoreEnvelope envelope)
        {
            string? echo = envelope.Payload is { ValueKind: JsonValueKind.Object } p
                && p.TryGetProperty("echo", out JsonElement e) && e.ValueKind == JsonValueKind.String
                ? e.GetString()
                : null;
            string result = string.Create(CultureInfo.InvariantCulture, $$"""
                {"specVersion":{{JsonSerializer.Serialize(Sts2WireConstants.SpecVersion)}},"serviceVersion":{{JsonSerializer.Serialize(state.ServiceVersion)}},"echo":{{JsonSerializer.Serialize(echo)}},"latestJournalSeq":{{envelope.Seq}},"health":{{JsonSerializer.Serialize(state.ShuttingDown ? "shuttingDown" : "ok")}}}
                """);
            return new CoreDecision(state, [new RpcResultOutput(envelope.Corr!, Json(result))]);
        }

        // ---------------- connection machine ----------------

        private static CoreDecision DecideConnectionOpen(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            if (envelope.Payload is not { ValueKind: JsonValueKind.Object } payload
                || !payload.TryGetProperty("openId", out JsonElement openIdElement)
                || openIdElement.ValueKind != JsonValueKind.String
                || !payload.TryGetProperty("profile", out JsonElement profile)
                || profile.ValueKind != JsonValueKind.Object)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "connection.open requires openId and profile.");
            }

            string openId = openIdElement.GetString()!;
            if (state.OpenIdToConnectionId.ContainsKey(openId))
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "openId is already in use: " + openId);
            }
            if (state.Connections.Count >= Sts2Defaults.MaxConnections)
            {
                return Error(state, corr, Sts2ErrorCodes.Busy, "Connection limit reached.");
            }

            string connectionId = string.Create(CultureInfo.InvariantCulture, $"c-{envelope.Seq}");
            string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-open-{envelope.Seq}");
            string args = string.Create(CultureInfo.InvariantCulture, $$"""
                {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"openId":{{JsonSerializer.Serialize(openId)}},"profile":{{profile.GetRawText()}}}
                """);

            CoreState next = state with
            {
                Connections = state.Connections.Add(connectionId, new ConnectionInfo
                {
                    ConnectionId = connectionId,
                    OpenId = openId,
                    Phase = ConnectionPhase.Opening,
                    OpenCorr = corr,
                }),
                OpenIdToConnectionId = state.OpenIdToConnectionId.Add(openId, connectionId),
            };
            return new CoreDecision(next, [new EffectRequestOutput(effectId, "driver.open", Json(args), corr)]);
        }

        private static CoreDecision DecideConnectionCancel(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string? openId = GetString(envelope.Payload, "openId");
            if (openId is null)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "connection.cancel requires openId.");
            }

            // Idempotent: unknown or completed openId returns {} (SPEC §7.9).
            if (!state.OpenIdToConnectionId.TryGetValue(openId, out string? connectionId)
                || !state.Connections.TryGetValue(connectionId, out ConnectionInfo? connection)
                || connection.Phase != ConnectionPhase.Opening
                || connection.CancelRequested)
            {
                return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }

            string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-cancel-{envelope.Seq}");
            string args = string.Create(CultureInfo.InvariantCulture, $$"""
                {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"openId":{{JsonSerializer.Serialize(openId)}}}
                """);
            CoreState next = state with
            {
                Connections = state.Connections.SetItem(connectionId, connection with { CancelRequested = true }),
            };
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json("{}")),
                new EffectRequestOutput(effectId, "driver.cancelOpen", Json(args), corr),
            ]);
        }

        private static CoreDecision DecideConnectionClose(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string? connectionId = GetString(envelope.Payload, "connectionId");
            if (connectionId is null)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "connection.close requires connectionId.");
            }

            if (!state.Connections.TryGetValue(connectionId, out ConnectionInfo? connection))
            {
                // Idempotent for unknown/already-closed connections (SPEC §7.9).
                return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }

            switch (connection.Phase)
            {
                case ConnectionPhase.Opening:
                {
                    // Close during open: reply {} now, cancel the open; the open request
                    // itself terminates with Sts2.Canceled when the effect resolves.
                    string cancelEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-cancel-{envelope.Seq}");
                    string cancelArgs = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"openId":{{JsonSerializer.Serialize(connection.OpenId)}}}
                        """);
                    CoreState next = state with
                    {
                        Connections = state.Connections.SetItem(connectionId, connection with { CancelRequested = true }),
                    };
                    return new CoreDecision(next,
                    [
                        new RpcResultOutput(corr, Json("{}")),
                        new EffectRequestOutput(cancelEffectId, "driver.cancelOpen", Json(cancelArgs), corr),
                    ]);
                }

                case ConnectionPhase.Open:
                {
                    string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-close-{envelope.Seq}");
                    string args = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"handleId":{{JsonSerializer.Serialize(connection.HandleId)}}}
                        """);
                    CoreState next = state with
                    {
                        Connections = state.Connections.SetItem(connectionId,
                            connection with { Phase = ConnectionPhase.Closing, CloseCorr = corr }),
                    };
                    return new CoreDecision(next, [new EffectRequestOutput(effectId, "driver.close", Json(args), corr)]);
                }

                case ConnectionPhase.Closing:
                default:
                    // A close is already in flight; idempotent {}.
                    return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }
        }

        // ---------------- effect responses ----------------

        private static CoreDecision DecideEffectResponse(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
            "driver.open" => DecideDriverOpenResult(state, envelope),
            "driver.cancelOpen" => CoreDecision.StateOnly(state), // ack only; the open's own result resolves it
            "driver.close" => DecideDriverCloseResult(state, envelope),
            "toy.delay" => DecideToyEffectResponse(state, envelope),
            _ => Unexpected(state, envelope, "unknown effect response type"),
        };

        private static CoreDecision DecideDriverOpenResult(CoreState state, CoreEnvelope envelope)
        {
            string? connectionId = GetString(envelope.Payload, "connectionId");
            if (connectionId is null || !state.Connections.TryGetValue(connectionId, out ConnectionInfo? connection)
                || connection.Phase != ConnectionPhase.Opening)
            {
                return Unexpected(state, envelope, "driver.open result for unknown or non-opening connection");
            }

            string status = GetString(envelope.Payload, "status") ?? "error";
            if (status == "ok")
            {
                string? handleId = GetString(envelope.Payload, "handleId");
                string serverInfo = envelope.Payload!.Value.TryGetProperty("serverInfo", out JsonElement si)
                    ? si.GetRawText()
                    : "null";
                CoreState next = state with
                {
                    Connections = state.Connections.SetItem(connectionId,
                        connection with { Phase = ConnectionPhase.Open, HandleId = handleId }),
                    OpenIdToConnectionId = state.OpenIdToConnectionId.Remove(connection.OpenId),
                };
                string result = string.Create(CultureInfo.InvariantCulture, $$"""
                    {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"serverInfo":{{serverInfo}}}
                    """);
                return new CoreDecision(next, [new RpcResultOutput(connection.OpenCorr, Json(result))]);
            }

            // error or canceled: the connection is gone; the open request terminates with an error (I1).
            CoreState removed = state with
            {
                Connections = state.Connections.Remove(connectionId),
                OpenIdToConnectionId = state.OpenIdToConnectionId.Remove(connection.OpenId),
            };
            string code = status == "canceled"
                ? Sts2ErrorCodes.Canceled
                : GetString(envelope.Payload, "code") ?? Sts2ErrorCodes.Internal;
            string message = status == "canceled"
                ? "The connection open was canceled."
                : GetString(envelope.Payload, "message") ?? "Connection failed.";
            return new CoreDecision(removed, [BuildError(connection.OpenCorr, code, message, envelope.Payload)]);
        }

        private static CoreDecision DecideDriverCloseResult(CoreState state, CoreEnvelope envelope)
        {
            string? connectionId = GetString(envelope.Payload, "connectionId");
            if (connectionId is null || !state.Connections.TryGetValue(connectionId, out ConnectionInfo? connection)
                || connection.Phase != ConnectionPhase.Closing || connection.CloseCorr is null)
            {
                return Unexpected(state, envelope, "driver.close result for unknown or non-closing connection");
            }

            CoreState next = state with { Connections = state.Connections.Remove(connectionId) };
            return new CoreDecision(next, [new RpcResultOutput(connection.CloseCorr, Json("{}"))]);
        }

        // ---------------- toy machine (spine scaffolding; removed in M3) ----------------

        private static CoreDecision DecideToyEcho(CoreState state, CoreEnvelope envelope)
        {
            CoreState next = state with { ToyCounter = state.ToyCounter + 1 };
            string text = GetString(envelope.Payload, "text") ?? string.Empty;
            string result = string.Create(CultureInfo.InvariantCulture,
                $$"""{"echo":{{JsonSerializer.Serialize(text)}},"counter":{{next.ToyCounter}}}""");
            return new CoreDecision(next, [new RpcResultOutput(envelope.Corr!, Json(result))]);
        }

        private static CoreDecision DecideToyEffect(CoreState state, CoreEnvelope envelope)
        {
            string effectId = string.Create(CultureInfo.InvariantCulture, $"eff-{envelope.Seq}");
            CoreState next = state with
            {
                PendingToyEffects = state.PendingToyEffects.Add(effectId, envelope.Corr!),
            };
            JsonElement args = envelope.Payload ?? Json("{}");
            return new CoreDecision(next, [new EffectRequestOutput(effectId, "toy.delay", args, envelope.Corr)]);
        }

        private static CoreDecision DecideToyEffectResponse(CoreState state, CoreEnvelope envelope)
        {
            if (envelope.Corr is null || !state.PendingToyEffects.TryGetValue(envelope.Corr, out string? rpcCorr))
            {
                return Unexpected(state, envelope, "effect response for unknown effect id");
            }
            CoreState next = state with { PendingToyEffects = state.PendingToyEffects.Remove(envelope.Corr) };
            string result = string.Create(CultureInfo.InvariantCulture,
                $$"""{"effectId":{{JsonSerializer.Serialize(envelope.Corr)}},"observed":{{envelope.Payload?.GetRawText() ?? "null"}}}""");
            return new CoreDecision(next, [new RpcResultOutput(rpcCorr, Json(result))]);
        }

        // ---------------- control & helpers ----------------

        private static CoreDecision DecideControl(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
            // Session config arrives as a journaled root envelope so live and replayed
            // runs start from the identical CoreState.Initial (replay safety: state must
            // never enter from outside the journal).
            "session.start" => DecideSessionStart(state, envelope),
            "lifecycle.shutdown" or "lifecycle.exit" => CoreDecision.StateOnly(state with { ShuttingDown = true }),
            _ => Unexpected(state, envelope, "unknown control signal"),
        };

        private static CoreDecision DecideSessionStart(CoreState state, CoreEnvelope envelope)
        {
            string serviceVersion = GetString(envelope.Payload, "serviceVersion") ?? state.ServiceVersion;
            var drivers = System.Collections.Immutable.ImmutableArray.CreateBuilder<DriverDescriptor>();
            if (envelope.Payload is { ValueKind: JsonValueKind.Object } payload
                && payload.TryGetProperty("drivers", out JsonElement driverArray)
                && driverArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement driver in driverArray.EnumerateArray())
                {
                    drivers.Add(new DriverDescriptor
                    {
                        Name = GetString(driver, "name") ?? "unknown",
                        Dialects = driver.TryGetProperty("dialects", out JsonElement dialects) && dialects.ValueKind == JsonValueKind.Array
                            ? [.. dialects.EnumerateArray().Where(d => d.ValueKind == JsonValueKind.String).Select(d => d.GetString()!)]
                            : [],
                        Production = driver.TryGetProperty("production", out JsonElement production) && production.ValueKind == JsonValueKind.True,
                    });
                }
            }
            return CoreDecision.StateOnly(state with { ServiceVersion = serviceVersion, Drivers = drivers.ToImmutable() });
        }

        private static CoreDecision Error(CoreState state, string corr, string dataCode, string message) =>
            new(state, [new RpcErrorOutput(corr, Sts2JsonRpcCodes.For(dataCode), message, dataCode)]);

        private static RpcErrorOutput BuildError(string corr, string dataCode, string message, JsonElement? _) =>
            new(corr, Sts2JsonRpcCodes.For(dataCode), message, dataCode);

        private static CoreDecision Unexpected(CoreState state, CoreEnvelope envelope, string reason)
        {
            // Invalid input is a stable diagnostic output, never an exception (SPEC §9.2).
            string data = string.Create(CultureInfo.InvariantCulture,
                $$"""{"reason":{{JsonSerializer.Serialize(reason)}},"kind":{{JsonSerializer.Serialize(envelope.Kind)}},"type":{{JsonSerializer.Serialize(envelope.Type)}},"seq":{{envelope.Seq}}}""");
            return new CoreDecision(state, [new DiagnosticOutput("core.unexpectedInput", Json(data))]);
        }

        private static string? GetString(JsonElement? payload, string property) =>
            payload is { ValueKind: JsonValueKind.Object } p
            && p.TryGetProperty(property, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;
    }
}
