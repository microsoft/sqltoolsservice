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
                "v2/diagnostics.health" => DecideHealth(state, envelope),
                "v2/diagnostics.state" => DecideState(state, envelope),
                "v2/diagnostics.setCapture" => DecideSetCapture(state, envelope),
                "v2/diagnostics.exportLog" => DecideExportLog(state, envelope),
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
                    ["exportLog"] = true, // v2/diagnostics.exportLog is registered and implemented
                    ["setCapture"] = true,
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
                    ["capture"] = state.RowCapture,
                    ["sqlCapture"] = state.SqlCapture,
                    // The host capture policy ceiling a client may request via setCapture (D-0012).
                    ["maxCapture"] = state.MaxRowCapture,
                    ["maxSqlCapture"] = state.MaxSqlCapture,
                    ["configVersion"] = state.ConfigVersion,
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

        private static CoreDecision DecideHealth(CoreState state, CoreEnvelope envelope)
        {
            // Pure-Core facts only (deterministic, replay-comparable). The coordinator
            // overlays Runtime facts — queue depth, leases, fatal status, dropped-diagnostic
            // counts, configVersion, error histogram — onto the wire response at the emit
            // edge (SPEC §12.1), so the journaled result stays pure and I7 holds.
            int activeQueries = state.Queries.Count(q => q.Value.Phase is QueryPhase.Running or QueryPhase.CancelRequested);
            int unackedPages = state.Queries.Values
                .Where(q => q.Phase is QueryPhase.Running or QueryPhase.CancelRequested)
                .Sum(q => q.PagesSent - q.PagesAcked);
            var result = new JsonObject
            {
                ["latestJournalSeq"] = envelope.Seq,
                ["activeConnections"] = state.Connections.Count,
                ["activeQueries"] = activeQueries,
                ["totalQueries"] = state.Queries.Count,
                ["unackedPages"] = unackedPages,
                ["shuttingDown"] = state.ShuttingDown,
            };
            return new CoreDecision(state, [new RpcResultOutput(envelope.Corr!, Json(result.ToJsonString()))]);
        }

        private static CoreDecision DecideState(CoreState state, CoreEnvelope envelope)
        {
            // The one shared redacted dump (SPEC §12.2, I16): ids/phases/counters/flags,
            // never secrets, row cells, or SQL text. The coordinator overlays Runtime handle
            // summaries on the wire response; the journaled result stays pure for replay.
            return new CoreDecision(state, [new RpcResultOutput(envelope.Corr!, Json(CoreStateDump.ToJson(state, envelope.Seq)))]);
        }

        private static CoreDecision DecideSetCapture(CoreState state, CoreEnvelope envelope)
        {
            string corr = envelope.Corr!;
            string rowCapture = GetString(envelope.Payload, "rowCapture") ?? state.RowCapture;
            string sqlCapture = GetString(envelope.Payload, "sqlCapture") ?? state.SqlCapture;

            if (rowCapture is not ("full" or "digest"))
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "rowCapture must be 'full' or 'digest'.");
            }
            if (sqlCapture is not ("text" or "digest"))
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest, "sqlCapture must be 'text' or 'digest'.");
            }

            // Host capture policy (D-0012): a client may not request a mode more revealing than
            // the host allows. 'digest' is always permitted (the safe floor); the revealing
            // modes ('full' rows, 'text' SQL) are permitted only when the policy ceiling allows
            // them. In the product composition the ceiling is digest/digest, so this denies an
            // untrusted client persisting real row/SQL data.
            if (!IsWithinCapturePolicy(rowCapture, state.MaxRowCapture))
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest,
                    "rowCapture '" + rowCapture + "' exceeds the host capture policy (max '" + state.MaxRowCapture + "').");
            }
            if (!IsWithinCapturePolicy(sqlCapture, state.MaxSqlCapture))
            {
                return Error(state, corr, Sts2ErrorCodes.InvalidRequest,
                    "sqlCapture '" + sqlCapture + "' exceeds the host capture policy (max '" + state.MaxSqlCapture + "').");
            }

            // Idempotent: an unchanged request echoes the current config without bumping the
            // version or journaling a config.changed (SPEC §11.1).
            bool unchanged = rowCapture == state.RowCapture && sqlCapture == state.SqlCapture;
            int newVersion = unchanged ? state.ConfigVersion : state.ConfigVersion + 1;
            string config = string.Create(CultureInfo.InvariantCulture,
                $$"""{"rowCapture":{{JsonSerializer.Serialize(rowCapture)}},"sqlCapture":{{JsonSerializer.Serialize(sqlCapture)}},"configVersion":{{newVersion}}}""");

            if (unchanged)
            {
                return new CoreDecision(state, [new RpcResultOutput(corr, Json(config))]);
            }

            CoreState next = state with { RowCapture = rowCapture, SqlCapture = sqlCapture, ConfigVersion = newVersion };
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json(config)),
                new ConfigChangedOutput(Json(config)),
            ]);
        }

        private static CoreDecision DecideExportLog(CoreState state, CoreEnvelope envelope)
        {
            // Export is an edge effect (file I/O); the runner writes the bundle and the
            // result corr is resolved when diag.export returns.
            bool includeSql = envelope.Payload is { ValueKind: JsonValueKind.Object } p
                && p.TryGetProperty("includeSqlText", out JsonElement sqlText) && sqlText.ValueKind == JsonValueKind.True;
            string effectId = string.Create(CultureInfo.InvariantCulture, $"diag-export-{envelope.Seq}");
            // The request corr travels in the args so the runner can echo it back (the
            // effect encoding does not carry the originating request corr).
            string args = string.Create(CultureInfo.InvariantCulture,
                $$"""{"corr":{{JsonSerializer.Serialize(envelope.Corr)}},"includeSqlText":{{(includeSql ? "true" : "false")}},"atSeq":{{envelope.Seq}}}""");
            return new CoreDecision(state, [new EffectRequestOutput(effectId, "diag.export", Json(args), envelope.Corr)]);
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
                    // Close during open: reply {} now, cancel the open. CloseAfterQuery
                    // also marks "close once opened" so that if the open WINS the cancel
                    // race and becomes Open, the open-result handler closes it instead of
                    // orphaning the session (leak found by simulator seed 47).
                    string cancelEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-cancel-{envelope.Seq}");
                    string cancelArgs = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"openId":{{JsonSerializer.Serialize(connection.OpenId)}}}
                        """);
                    CoreState next = state with
                    {
                        // CloseCorr stays null: the close was already answered with {}.
                        Connections = state.Connections.SetItem(connectionId,
                            connection with { CancelRequested = true, CloseAfterQuery = true }),
                    };
                    return new CoreDecision(next,
                    [
                        new RpcResultOutput(corr, Json("{}")),
                        new EffectRequestOutput(cancelEffectId, "driver.cancelOpen", Json(cancelArgs), corr),
                    ]);
                }

                case ConnectionPhase.Open when connection.ActiveQueryId is string activeQueryId
                    && state.Queries.TryGetValue(activeQueryId, out QueryInfo? activeQuery)
                    && activeQuery.Phase is QueryPhase.Running or QueryPhase.CancelRequested or QueryPhase.Disposing:
                {
                    // A close is already parked on this connection waiting for the query to
                    // terminate. A second close must NOT overwrite the first waiter's corr
                    // (I1: the first close still owes exactly one terminal response). Answer
                    // the duplicate idempotently with {} and keep the original CloseCorr (R010).
                    if (connection.CloseAfterQuery)
                    {
                        return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
                    }

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
                && through.ValueKind == JsonValueKind.Number && through.TryGetInt32(out int throughPageSeq))
            {
                // High-water (0-based pageSeq). Clamp to [current, PagesSent] so a duplicate,
                // out-of-order, or impossibly-large client ack can never push pagesAcked past
                // what was actually sent and over-grant credit beyond the window (I9, R011).
                int requested = throughPageSeq >= 0 ? throughPageSeq + 1 : pagesAcked;
                pagesAcked = Math.Min(query.PagesSent, Math.Max(pagesAcked, requested));
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

            // Idempotent for unknown, already-disposing, or already-disposed queries.
            if (!state.Queries.TryGetValue(queryId, out QueryInfo? query)
                || query.Phase is QueryPhase.Disposing or QueryPhase.Disposed)
            {
                return new CoreDecision(state, [new RpcResultOutput(corr, Json("{}"))]);
            }

            string effectId = string.Create(CultureInfo.InvariantCulture, $"drv-qdispose-{envelope.Seq}");
            string args = string.Create(CultureInfo.InvariantCulture, $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}}}""");

            // The query already emitted its terminal (completed/error/canceled). Dispose just
            // releases resources — mark Disposed, free the connection, no second complete.
            if (query.CompleteSent)
            {
                CoreState done = state with
                {
                    Queries = state.Queries.SetItem(queryId, query with { Phase = QueryPhase.Disposed }),
                    Connections = ClearActiveQuery(state.Connections, query.ConnectionId, queryId),
                };
                return new CoreDecision(done,
                [
                    new RpcResultOutput(corr, Json("{}")),
                    new EffectRequestOutput(effectId, "driver.queryDispose", Json(args), corr),
                ]);
            }

            // Active query (D-0011, R009): answer dispose {} now and ask the runner to stop the
            // pump, but HOLD the connection (keep ActiveQueryId) and emit NO terminal yet. The
            // driver.queryDispose result confirms the pump fully stopped; only then does Core
            // emit the single query.complete(disposed) and release the connection — so a new
            // query can never race the old reader on the same session.
            CoreState next = state with
            {
                Queries = state.Queries.SetItem(queryId, query with { Phase = QueryPhase.Disposing }),
            };
            return new CoreDecision(next,
            [
                new RpcResultOutput(corr, Json("{}")),
                new EffectRequestOutput(effectId, "driver.queryDispose", Json(args), corr),
            ]);
        }

        private static CoreDecision DecideDriverQueryDisposeResult(CoreState state, CoreEnvelope envelope)
        {
            string? queryId = GetString(envelope.Payload, "queryId");
            // Only an active dispose (Disposing) emits the terminal; the already-terminated
            // dispose path (CompleteSent) is just an ack.
            if (queryId is null || !state.Queries.TryGetValue(queryId, out QueryInfo? query)
                || query.Phase != QueryPhase.Disposing)
            {
                return CoreDecision.StateOnly(state);
            }

            string notify = string.Create(CultureInfo.InvariantCulture,
                $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}},"status":"disposed","rowsAffected":null}""");
            QueryInfo terminal = query with { Phase = QueryPhase.Disposed, CompleteSent = true };
            CoreState next = state with
            {
                Queries = state.Queries.SetItem(queryId, terminal),
                Connections = ClearActiveQuery(state.Connections, query.ConnectionId, queryId),
            };
            var outputs = new List<CoreOutput> { new RpcNotifyOutput("v2/query.complete", Json(notify)) };

            // The connection is free now; proceed any close that was parked behind the query.
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

        private static CoreDecision DecideQueryEvent(CoreState state, CoreEnvelope envelope)
        {
            string? queryId = GetString(envelope.Payload, "queryId");
            string? eventType = GetString(envelope.Payload, "eventType");
            if (queryId is null || eventType is null || !state.Queries.TryGetValue(queryId, out QueryInfo? query))
            {
                return Unexpected(state, envelope, "query event for unknown query");
            }

            // I3: no output after complete; disposed/disposing queries are silent too — the
            // single terminal for a dispose is the synthetic query.complete(disposed) emitted
            // when the runner confirms the pump stopped, so driver events here must not race it.
            if (query.CompleteSent || query.Phase is QueryPhase.Disposed or QueryPhase.Disposing)
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
                        {"queryId":{{JsonSerializer.Serialize(queryId)}},"messageClass":{{GetRaw(payload, "messageClass", "\"info\"")}},"number":{{GetRaw(payload, "number", "0")}},"severity":{{GetRaw(payload, "severity", "0")}},"text":{{GetRaw(payload, "text", "\"\"")}},"line":{{GetRaw(payload, "line", "null")}}}
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
            "driver.queryDispose" => DecideDriverQueryDisposeResult(state, envelope),
            "driver.queryEvent" => DecideQueryEvent(state, envelope),
            "diag.export" => DecideExportResult(state, envelope),
            _ => Unexpected(state, envelope, "unknown effect response type"),
        };

        private static CoreDecision DecideExportResult(CoreState state, CoreEnvelope envelope)
        {
            // The runner echoes the originating request corr in the payload.
            string? corr = GetString(envelope.Payload, "corr");
            if (corr is null)
            {
                return Unexpected(state, envelope, "diag.export result without corr");
            }
            string status = GetString(envelope.Payload, "status") ?? "error";
            if (status == "ok")
            {
                string result = string.Create(CultureInfo.InvariantCulture,
                    $$"""{"bundlePath":{{JsonSerializer.Serialize(GetString(envelope.Payload, "bundlePath"))}},"bytes":{{GetRaw(envelope.Payload!.Value, "bytes", "0")}}}""");
                return new CoreDecision(state, [new RpcResultOutput(corr, Json(result))]);
            }
            return Error(state, corr, Sts2ErrorCodes.Internal, GetString(envelope.Payload, "message") ?? "Export failed.");
        }

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

                // The open WON a cancel/close race (CloseAfterQuery). The open request
                // still terminates with success, but the connection must close, not orphan.
                if (connection.CloseAfterQuery)
                {
                    string closeEffectId = string.Create(CultureInfo.InvariantCulture, $"drv-close-{envelope.Seq}");
                    string closeArgs = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"handleId":{{JsonSerializer.Serialize(handleId)}}}
                        """);
                    CoreState closing = state with
                    {
                        Connections = state.Connections.SetItem(connectionId,
                            connection with { Phase = ConnectionPhase.Closing, HandleId = handleId }),
                        OpenIdToConnectionId = state.OpenIdToConnectionId.Remove(connection.OpenId),
                    };
                    string raceResult = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"serverInfo":{{serverInfo}}}
                        """);
                    return new CoreDecision(closing,
                    [
                        new RpcResultOutput(connection.OpenCorr, Json(raceResult)),
                        new EffectRequestOutput(closeEffectId, "driver.close", Json(closeArgs), null),
                    ]);
                }

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
                || connection.Phase != ConnectionPhase.Closing)
            {
                return Unexpected(state, envelope, "driver.close result for unknown or non-closing connection");
            }

            CoreState next = state with { Connections = state.Connections.Remove(connectionId) };
            // CloseCorr is null for a close that was already answered with {} (the
            // close-during-open race); only emit a result when a caller is waiting.
            return connection.CloseCorr is null
                ? CoreDecision.StateOnly(next)
                : new CoreDecision(next, [new RpcResultOutput(connection.CloseCorr, Json("{}"))]);
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
                && maxConn.ValueKind == JsonValueKind.Number
                && maxConn.TryGetInt32(out int parsedMax) && parsedMax > 0)
            {
                maxConnections = parsedMax;
            }

            // Initial capture modes AND the host capture policy enter through the journaled
            // session.start so replay starts from the same capture state the live run did
            // (SPEC §8.4, I7, D-0012). The policy (maxRow/maxSql) is the ceiling setCapture
            // may not exceed; the product composition pins it to digest/digest.
            string rowCapture = state.RowCapture;
            string sqlCapture = state.SqlCapture;
            string maxRowCapture = state.MaxRowCapture;
            string maxSqlCapture = state.MaxSqlCapture;
            if (envelope.Payload is { ValueKind: JsonValueKind.Object } capturePayload
                && capturePayload.TryGetProperty("capture", out JsonElement capture)
                && capture.ValueKind == JsonValueKind.Object)
            {
                rowCapture = GetString(capture, "row") ?? rowCapture;
                sqlCapture = GetString(capture, "sql") ?? sqlCapture;
                maxRowCapture = GetString(capture, "maxRow") ?? maxRowCapture;
                maxSqlCapture = GetString(capture, "maxSql") ?? maxSqlCapture;
            }

            return CoreDecision.StateOnly(state with
            {
                ServiceVersion = serviceVersion,
                Drivers = drivers.ToImmutable(),
                MaxConnections = maxConnections,
                RowCapture = rowCapture,
                SqlCapture = sqlCapture,
                MaxRowCapture = maxRowCapture,
                MaxSqlCapture = maxSqlCapture,
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

        // 'digest' is the safe floor (always allowed); a more-revealing mode is allowed only
        // when it equals the policy ceiling (D-0012).
        private static bool IsWithinCapturePolicy(string requested, string max) =>
            requested == "digest" || requested == max;

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
