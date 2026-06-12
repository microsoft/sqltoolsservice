//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// The pure synchronous reducer (SPEC §9.2): no I/O, no time, no randomness, no
    /// exceptions as control flow. M3 implements the connection and query machines;
    /// backpressure credit lives here, the enumerator pull loop lives in the runner.
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
                "rpc.in.notify" => DecideNotification(advanced, envelope),
                "effect.res" => DecideEffectResponse(advanced, envelope),
                "control" => DecideControl(advanced, envelope),
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
                "v2/query.execute" => DecideQueryExecute(state, envelope),
                "v2/query.cancel" => DecideQueryCancel(state, envelope),
                "v2/query.dispose" => DecideQueryDispose(state, envelope),
                _ => Error(state, envelope.Corr, Sts2ErrorCodes.InvalidRequest, "Unknown v2 method."),
            };
        }

        private static CoreDecision DecideNotification(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
            "v2/query.ack" => DecideQueryAck(state, envelope),
            _ => CoreDecision.StateOnly(state), // unknown client notifications are ignored
        };

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
                    ["maxConnections"] = state.MaxConnections,
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
            string? echo = GetString(envelope.Payload, "echo");
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
            if (state.Connections.Count >= state.MaxConnections)
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

                case ConnectionPhase.Open when connection.ActiveQueryId is string activeQueryId
                    && state.Queries.TryGetValue(activeQueryId, out QueryInfo? activeQuery)
                    && activeQuery.Phase is QueryPhase.Running or QueryPhase.CancelRequested:
                {
                    // SPEC §7.9: close cancels the active query first; the connection
                    // closes when the query reaches a terminal state.
                    var outputs = new List<CoreOutput>();
                    CoreState next = state with
                    {
                        Connections = state.Connections.SetItem(connectionId,
                            connection with { CloseCorr = corr, CloseAfterQuery = true }),
                    };
                    if (activeQuery.Phase == QueryPhase.Running)
                    {
                        string cancelEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-qcancel-{envelope.Seq}");
                        string cancelArgs = string.Create(CultureInfo.InvariantCulture,
                            $$"""{"queryId":{{JsonSerializer.Serialize(activeQueryId)}}}""");
                        outputs.Add(new EffectRequestOutput(cancelEffectId, "driver.queryCancel", Json(cancelArgs), corr));
                        next = next with
                        {
                            Queries = next.Queries.SetItem(activeQueryId,
                                activeQuery with { Phase = QueryPhase.CancelRequested }),
                        };
                    }
                    return new CoreDecision(next, [.. outputs]);
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
                    return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }
        }

        // ---------------- query machine ----------------

        private static CoreDecision DecideQueryExecute(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string? connectionId = GetString(envelope.Payload, "connectionId");
            // SQL may be plain text or a $redacted digest wrapper (sqlCapture=digest);
            // Core relays it verbatim either way and never parses it (SPEC §13.3).
            string? sqlRaw = envelope.Payload is { ValueKind: JsonValueKind.Object } pl
                && pl.TryGetProperty("sql", out JsonElement sqlElement)
                && sqlElement.ValueKind is JsonValueKind.String or JsonValueKind.Object
                    ? sqlElement.GetRawText()
                    : null;
            if (connectionId is null || sqlRaw is null)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "query.execute requires connectionId and sql.");
            }
            if (!state.Connections.TryGetValue(connectionId, out ConnectionInfo? connection)
                || connection.Phase != ConnectionPhase.Open)
            {
                return Error(state, corr, Sts2ErrorCodes.NotFound, "No open connection with id " + connectionId + ".");
            }
            if (connection.ActiveQueryId is not null)
            {
                return Error(state, corr, Sts2ErrorCodes.Busy, "A query is already active on this connection.");
            }

            string queryId = string.Create(CultureInfo.InvariantCulture, $"q-{envelope.Seq}");
            string startEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-qstart-{envelope.Seq}");
            string startArgs = string.Create(CultureInfo.InvariantCulture, $$"""
                {"queryId":{{JsonSerializer.Serialize(queryId)}},"connectionId":{{JsonSerializer.Serialize(connectionId)}},"handleId":{{JsonSerializer.Serialize(connection.HandleId)}},"sql":{{sqlRaw}},"credit":{{Sts2Defaults.WindowPages}}}
                """);

            CoreState next = state with
            {
                Connections = state.Connections.SetItem(connectionId, connection with { ActiveQueryId = queryId }),
                Queries = state.Queries.Add(queryId, new QueryInfo
                {
                    QueryId = queryId,
                    ConnectionId = connectionId,
                    Phase = QueryPhase.Running,
                    PagesSent = 0,
                    PagesAcked = 0,
                    CreditOutstanding = Sts2Defaults.WindowPages,
                    CompleteSent = false,
                }),
            };
            string result = string.Create(CultureInfo.InvariantCulture, $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}}}""");
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json(result)),
                new EffectRequestOutput(startEffectId, "driver.queryStart", Json(startArgs), corr),
            ]);
        }

        private static CoreDecision DecideQueryAck(CoreState state, CoreEnvelope envelope)
        {
            string? queryId = GetString(envelope.Payload, "queryId");
            if (queryId is null || !state.Queries.TryGetValue(queryId, out QueryInfo? query)
                || query.Phase is QueryPhase.Completed or QueryPhase.Disposed)
            {
                return CoreDecision.StateOnly(state); // late/unknown acks are ignored (idempotent)
            }

            int pagesAcked = query.PagesAcked;
            if (envelope.Payload is { } p && p.TryGetProperty("throughPageSeq", out JsonElement through)
                && through.ValueKind == JsonValueKind.Number)
            {
                pagesAcked = Math.Max(pagesAcked, through.GetInt32() + 1); // high-water (0-based pageSeq)
            }
            else
            {
                pagesAcked = Math.Min(query.PagesSent, pagesAcked + 1); // per-page credit
            }

            int unacked = query.PagesSent - pagesAcked;
            int creditToGrant = Sts2Defaults.WindowPages - unacked - query.CreditOutstanding;
            QueryInfo updated = query with
            {
                PagesAcked = pagesAcked,
                CreditOutstanding = query.CreditOutstanding + Math.Max(0, creditToGrant),
            };
            CoreState next = state with { Queries = state.Queries.SetItem(queryId, updated) };

            if (creditToGrant > 0 && query.Phase == QueryPhase.Running)
            {
                string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-qadvance-{envelope.Seq}");
                string args = string.Create(CultureInfo.InvariantCulture,
                    $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}},"credit":{{creditToGrant}}}""");
                return new CoreDecision(next, [new EffectRequestOutput(effectId, "driver.queryAdvance", Json(args), envelope.Corr)]);
            }
            return CoreDecision.StateOnly(next);
        }

        private static CoreDecision DecideQueryCancel(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string? queryId = GetString(envelope.Payload, "queryId");
            if (queryId is null)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "query.cancel requires queryId.");
            }

            // Idempotent: unknown, completed, or disposed queries return {} (SPEC §7.9).
            if (!state.Queries.TryGetValue(queryId, out QueryInfo? query)
                || query.Phase is QueryPhase.Completed or QueryPhase.Disposed or QueryPhase.CancelRequested)
            {
                return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }

            string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-qcancel-{envelope.Seq}");
            string args = string.Create(CultureInfo.InvariantCulture, $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}}}""");
            CoreState next = state with
            {
                Queries = state.Queries.SetItem(queryId, query with { Phase = QueryPhase.CancelRequested }),
            };
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json("{}")),
                new EffectRequestOutput(effectId, "driver.queryCancel", Json(args), corr),
            ]);
        }

        private static CoreDecision DecideQueryDispose(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string? queryId = GetString(envelope.Payload, "queryId");
            if (queryId is null)
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "query.dispose requires queryId.");
            }

            if (!state.Queries.TryGetValue(queryId, out QueryInfo? query) || query.Phase == QueryPhase.Disposed)
            {
                return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }

            CoreState next = state with
            {
                Queries = state.Queries.SetItem(queryId, query with { Phase = QueryPhase.Disposed }),
                Connections = ClearActiveQuery(state.Connections, query.ConnectionId, queryId),
            };
            string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-qdispose-{envelope.Seq}");
            string args = string.Create(CultureInfo.InvariantCulture, $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}}}""");
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json("{}")),
                new EffectRequestOutput(effectId, "driver.queryDispose", Json(args), corr),
            ]);
        }

        private static CoreDecision DecideQueryEvent(CoreState state, CoreEnvelope envelope)
        {
            string? queryId = GetString(envelope.Payload, "queryId");
            string? eventType = GetString(envelope.Payload, "eventType");
            if (queryId is null || eventType is null || !state.Queries.TryGetValue(queryId, out QueryInfo? query))
            {
                return Unexpected(state, envelope, "query event for unknown query");
            }

            // I3: no output after complete; disposed queries are silent too.
            if (query.CompleteSent || query.Phase == QueryPhase.Disposed)
            {
                return CoreDecision.StateOnly(state);
            }

            JsonElement payload = envelope.Payload!.Value;
            switch (eventType)
            {
                case "started":
                    return CoreDecision.StateOnly(state);

                case "resultSet":
                {
                    string notify = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"queryId":{{JsonSerializer.Serialize(queryId)}},"resultSetId":{{GetRaw(payload, "resultSetId", "0")}},"columns":{{GetRaw(payload, "columns", "[]")}}}
                        """);
                    return new CoreDecision(state, [new RpcNotifyOutput("v2/query.resultSet", Json(notify))]);
                }

                case "rows":
                {
                    QueryInfo updated = query with
                    {
                        PagesSent = query.PagesSent + 1,
                        CreditOutstanding = Math.Max(0, query.CreditOutstanding - 1),
                    };
                    CoreState next = state with { Queries = state.Queries.SetItem(queryId, updated) };
                    string notify = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"queryId":{{JsonSerializer.Serialize(queryId)}},"resultSetId":{{GetRaw(payload, "resultSetId", "0")}},"pageSeq":{{GetRaw(payload, "pageSeq", "0")}},"rowOffset":{{GetRaw(payload, "rowOffset", "0")}},"rows":{{GetRaw(payload, "rows", "[]")}},"last":false}
                        """);
                    return new CoreDecision(next, [new RpcNotifyOutput("v2/query.rows", Json(notify))]);
                }

                case "message":
                {
                    string notify = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"queryId":{{JsonSerializer.Serialize(queryId)}},"messageClass":{{GetRaw(payload, "messageClass", "\"info\"")}},"number":{{GetRaw(payload, "number", "0")}},"severity":{{GetRaw(payload, "severity", "0")}},"text":{{GetRaw(payload, "text", "\"\"")}}}
                        """);
                    return new CoreDecision(state, [new RpcNotifyOutput("v2/query.message", Json(notify))]);
                }

                case "resultSetDone":
                    return CoreDecision.StateOnly(state); // row counts arrive in complete

                case "completed":
                case "error":
                case "canceled":
                {
                    string status = eventType switch
                    {
                        "completed" => "succeeded",
                        "canceled" => "canceled",
                        _ => "error",
                    };
                    string errorPart = eventType == "error"
                        ? string.Create(CultureInfo.InvariantCulture,
                            $$""","error":{"code":{{GetRaw(payload, "code", "\"Sts2.QueryFailed.Server\"")}},"message":{{GetRaw(payload, "message", "\"Query failed.\"")}},"server":{{GetRaw(payload, "server", "null")}}}""")
                        : string.Empty;
                    string notify = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"queryId":{{JsonSerializer.Serialize(queryId)}},"status":{{JsonSerializer.Serialize(status)}},"rowsAffected":{{GetRaw(payload, "rowsAffected", "null")}}{{errorPart}}}
                        """);

                    QueryInfo terminal = query with { Phase = QueryPhase.Completed, CompleteSent = true };
                    CoreState next = state with
                    {
                        Queries = state.Queries.SetItem(queryId, terminal),
                        Connections = ClearActiveQuery(state.Connections, query.ConnectionId, queryId),
                    };
                    var outputs = new List<CoreOutput> { new RpcNotifyOutput("v2/query.complete", Json(notify)) };

                    // A close was waiting for this query (SPEC §7.9).
                    if (next.Connections.TryGetValue(query.ConnectionId, out ConnectionInfo? connection)
                        && connection.CloseAfterQuery && connection.Phase == ConnectionPhase.Open)
                    {
                        string closeEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-close-{envelope.Seq}");
                        string closeArgs = string.Create(CultureInfo.InvariantCulture, $$"""
                            {"connectionId":{{JsonSerializer.Serialize(connection.ConnectionId)}},"handleId":{{JsonSerializer.Serialize(connection.HandleId)}}}
                            """);
                        outputs.Add(new EffectRequestOutput(closeEffectId, "driver.close", Json(closeArgs), connection.CloseCorr));
                        next = next with
                        {
                            Connections = next.Connections.SetItem(connection.ConnectionId,
                                connection with { Phase = ConnectionPhase.Closing }),
                        };
                    }
                    return new CoreDecision(next, [.. outputs]);
                }

                default:
                    return Unexpected(state, envelope, "unknown query event type " + eventType);
            }
        }

        // ---------------- effect responses ----------------

        private static CoreDecision DecideEffectResponse(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
            "driver.open" => DecideDriverOpenResult(state, envelope),
            "driver.cancelOpen" => CoreDecision.StateOnly(state), // ack only; the open's own result resolves it
            "driver.close" => DecideDriverCloseResult(state, envelope),
            "driver.queryStart" => CoreDecision.StateOnly(state), // pump started; events follow
            "driver.queryAdvance" => CoreDecision.StateOnly(state),
            "driver.queryCancel" => CoreDecision.StateOnly(state),
            "driver.queryDispose" => CoreDecision.StateOnly(state),
            "driver.queryEvent" => DecideQueryEvent(state, envelope),
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
            return new CoreDecision(removed,
                [new RpcErrorOutput(connection.OpenCorr, Sts2JsonRpcCodes.For(code), message, code)]);
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

        // ---------------- control & helpers ----------------

        private static CoreDecision DecideControl(CoreState state, CoreEnvelope envelope) => envelope.Type switch
        {
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

            int maxConnections = state.MaxConnections;
            if (envelope.Payload is { ValueKind: JsonValueKind.Object } payloadWithLimits
                && payloadWithLimits.TryGetProperty("limits", out JsonElement limits)
                && limits.ValueKind == JsonValueKind.Object
                && limits.TryGetProperty("maxConnections", out JsonElement maxConn)
                && maxConn.ValueKind == JsonValueKind.Number)
            {
                maxConnections = maxConn.GetInt32();
            }

            return CoreDecision.StateOnly(state with
            {
                ServiceVersion = serviceVersion,
                Drivers = drivers.ToImmutable(),
                MaxConnections = maxConnections,
            });
        }

        private static System.Collections.Immutable.ImmutableSortedDictionary<string, ConnectionInfo> ClearActiveQuery(
            System.Collections.Immutable.ImmutableSortedDictionary<string, ConnectionInfo> connections,
            string connectionId,
            string queryId)
        {
            return connections.TryGetValue(connectionId, out ConnectionInfo? connection) && connection.ActiveQueryId == queryId
                ? connections.SetItem(connectionId, connection with { ActiveQueryId = null })
                : connections;
        }

        private static CoreDecision Error(CoreState state, string corr, string dataCode, string message) =>
            new(state, [new RpcErrorOutput(corr, Sts2JsonRpcCodes.For(dataCode), message, dataCode)]);

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

        private static string GetRaw(JsonElement payload, string property, string fallback) =>
            payload.TryGetProperty(property, out JsonElement value) ? value.GetRawText() : fallback;

        private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;
    }
}
