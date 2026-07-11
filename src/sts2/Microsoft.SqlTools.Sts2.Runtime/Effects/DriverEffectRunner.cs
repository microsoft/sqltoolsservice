//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;

namespace Microsoft.SqlTools.Sts2.Runtime.Effects
{
    /// <summary>
    /// The driver-facing effect runner (SPEC §9.4): owns live sessions and per-open
    /// cancellation, resolves SecretRef tokens at the very edge, classifies failures to
    /// stable codes, and removes secrets from the side table the moment an open attempt
    /// completes (SPEC §8.5 lifecycle). Every observation re-enters the coordinator as
    /// an <c>effect.res</c> envelope.
    /// </summary>
    public sealed class DriverEffectRunner : ISts2EffectRunner, IEffectRunnerDiagnostics, IAsyncDisposable
    {
        private readonly IReadOnlyDictionary<string, IDbDriver> drivers;
        private readonly SecretSideTable secrets;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> opensInFlight = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, IDbSession> sessions = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, QueryPump> queryPumps = new(StringComparer.Ordinal);

        /// <summary>Live pump state for one streaming query.</summary>
        private sealed class QueryPump
        {
            public required IDbSession Session { get; init; }

            /// <summary>Backpressure credits (SPEC §7.8): one rows page may post per credit.</summary>
            public required SemaphoreSlim Credits { get; init; }

            public CancellationTokenSource Cancellation { get; } = new();

            /// <summary>The streaming task; awaited on dispose so the reader fully unwinds (R009).</summary>
            public Task? PumpTask;

            /// <summary>True after dispose: no further events may post (I3).</summary>
            public volatile bool Suppressed;

            // Row-pipeline attribution accumulators (QO-2): per-page stats ride the
            // journaled rows event; these totals ride the completed event and become
            // the sts2.query.stats diagnostic.
            public long StatsPages;
            public long StatsRows;
            public long StatsEncodedBytes;
            public double StatsReadMsTotal;
            public double StatsCreditWaitMsTotal;
            public double StatsEncodeMsTotal;
        }

        private readonly Export.ExportBundleRequest? exportTemplate;
        private readonly TimeProvider timeProvider;

        /// <summary>Creates a runner over the registered drivers.</summary>
        public DriverEffectRunner(
            IReadOnlyDictionary<string, IDbDriver> drivers,
            SecretSideTable secrets,
            Export.ExportBundleRequest? exportTemplate = null,
            TimeProvider? timeProvider = null)
        {
            this.drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
            this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
            this.exportTemplate = exportTemplate;
            this.timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>Live driver-session lease count (I8).</summary>
        public int OpenSessionCount => sessions.Count;

        /// <summary>
        /// Opens whose driver call has not yet resolved. A run must let these settle before
        /// teardown, or an open that completes post-dispose stores a session Core never
        /// closes (an I8 false-positive — the session is orphaned, not leaked by the machine).
        /// </summary>
        public int OpensInFlightCount => opensInFlight.Count;

        /// <summary>Query pumps still streaming.</summary>
        public int ActiveQueryPumpCount => queryPumps.Count;

        /// <inheritdoc/>
        int IEffectRunnerDiagnostics.OpenLeases => sessions.Count;

        /// <inheritdoc/>
        int IEffectRunnerDiagnostics.OpensInFlight => opensInFlight.Count;

        /// <inheritdoc/>
        int IEffectRunnerDiagnostics.ActiveQueryPumps => queryPumps.Count;

        /// <summary>
        /// Cancels any running query pumps and in-flight opens, disposes any sessions
        /// still held, and returns how many sessions there were. Called at run end:
        /// cancelling the pumps releases their credit-semaphore waits so no background
        /// task is orphaned (a leak found by the 200-seed simulator). A non-zero return
        /// is an I8 lease violation for scenarios that closed all their connections.
        /// </summary>
        public async ValueTask<int> DisposeLeakedSessionsAsync()
        {
            foreach (string queryId in queryPumps.Keys.ToArray())
            {
                if (queryPumps.TryRemove(queryId, out QueryPump? pump))
                {
                    pump.Suppressed = true;
                    pump.Cancellation.Cancel();
                }
            }
            foreach (string openId in opensInFlight.Keys.ToArray())
            {
                if (opensInFlight.TryGetValue(openId, out CancellationTokenSource? cts))
                {
                    cts.Cancel();
                }
            }

            int leaked = 0;
            foreach (string handleId in sessions.Keys.ToArray())
            {
                if (sessions.TryRemove(handleId, out IDbSession? session))
                {
                    leaked++;
                    try
                    {
                        await session.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is DbDriverException or ObjectDisposedException)
                    {
                    }
                }
            }
            return leaked;
        }

        /// <summary>
        /// Owns and releases all runner resources (R013): cancels pumps/opens and disposes
        /// every live session, semaphore, and cancellation source. The session disposes the
        /// runner after the coordinator pump has drained, so no new effects arrive during disposal.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeLeakedSessionsAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Run(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(inbox);
            // Run() executes synchronously on the single coordinator pump thread, in journal
            // order. Open/query handles are therefore REGISTERED here, synchronously, before
            // the async Task.Run that does the I/O — so any later effect on the same handle
            // (cancel/advance/dispose) always finds a live record. This structurally removes
            // the pre-arrival races the simulator once exposed (no reconciliation side tables).
            switch (effect.EffectName)
            {
                case "driver.open":
                {
                    string openId = GetString(effect.Args, "openId") ?? "?";
                    var cancelSource = new CancellationTokenSource();
                    opensInFlight[openId] = cancelSource;
                    _ = Task.Run(() => OpenAsync(effect, inbox, openId, cancelSource));
                    break;
                }

                case "driver.cancelOpen":
                {
                    string? openId = GetString(effect.Args, "openId");
                    if (openId is not null && opensInFlight.TryGetValue(openId, out CancellationTokenSource? cts))
                    {
                        cts.Cancel();
                    }
                    // A missing entry means the open already resolved; the cancel is a no-op.
                    _ = PostAsync(inbox, effect, """{"status":"ok"}""");
                    break;
                }

                case "driver.close":
                    _ = Task.Run(() => CloseAsync(effect, inbox));
                    break;

                case "driver.queryStart":
                    StartQueryPump(effect, inbox);
                    break;

                case "driver.queryAdvance":
                {
                    string? queryId = GetString(effect.Args, "queryId");
                    int credit = effect.Args.TryGetProperty("credit", out JsonElement c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                    if (queryId is not null && credit > 0 && queryPumps.TryGetValue(queryId, out QueryPump? pump))
                    {
                        pump.Credits.Release(credit);
                    }
                    // A missing pump means the query already terminated; the credit is moot.
                    _ = PostAsync(inbox, effect, """{"status":"ok"}""");
                    break;
                }

                case "driver.queryCancel":
                {
                    string? queryId = GetString(effect.Args, "queryId");
                    if (queryId is not null && queryPumps.TryGetValue(queryId, out QueryPump? pump))
                    {
                        // Cancel the LOCAL pump token FIRST (bounded, deterministic): this stops
                        // the enumerator regardless of the provider. Provider cancellation is a
                        // best-effort follow-up under a bounded token, so a driver whose
                        // CancelAsync hangs cannot wedge query.cancel (R015).
                        pump.Cancellation.Cancel();
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var bounded = new CancellationTokenSource(Sts2Defaults.CloseTimeoutMs);
                                await pump.Session.CancelAsync(queryId, bounded.Token).ConfigureAwait(false);
                            }
                            catch (Exception ex) when (ex is DbDriverException or ObjectDisposedException or OperationCanceledException)
                            {
                            }
                        });
                    }
                    _ = PostAsync(inbox, effect, """{"status":"ok"}""");
                    break;
                }

                case "driver.queryDispose":
                {
                    string? queryId = GetString(effect.Args, "queryId");
                    if (queryId is not null && queryPumps.TryRemove(queryId, out QueryPump? pump))
                    {
                        pump.Suppressed = true; // I3: no further events after dispose
                        pump.Cancellation.Cancel();
                        // Await the streaming task BEFORE acking so the driver reader/command is
                        // fully unwound before Core frees the connection — a new query can never
                        // race the old reader on the same session (R009/D-0011).
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (pump.PumpTask is Task task)
                                {
                                    await task.ConfigureAwait(false);
                                }
                            }
                            catch (Exception)
                            {
                                // The pump's own faults are already classified into events.
                            }
                            await PostDisposeAckAsync(inbox, effect, queryId).ConfigureAwait(false);
                        });
                    }
                    else
                    {
                        // No live pump (the query already terminated): ack immediately.
                        _ = PostDisposeAckAsync(inbox, effect, queryId ?? "?");
                    }
                    break;
                }

                case "diag.export":
                    _ = Task.Run(() => ExportAsync(effect, inbox));
                    break;

                default:
                    _ = PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":"Unknown effect name."}"""));
                    break;
            }
        }

        private async Task OpenAsync(EffectWorkItem effect, ICoordinatorInbox inbox, string openId, CancellationTokenSource cancelSource)
        {
            // cancelSource is registered in opensInFlight by Run() before this task starts, so
            // a racing cancelOpen has already cancelled it if it arrived first.
            string connectionId = GetString(effect.Args, "connectionId") ?? "?";
            var resolvedTokens = new List<string>();

            try
            {
                JsonElement profile = effect.Args.GetProperty("profile");
                string driverName = GetString(profile, "driver") ?? "fake";
                if (!drivers.TryGetValue(driverName, out IDbDriver? driver))
                {
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Unavailable)}},"message":{{JsonSerializer.Serialize("No driver registered with name '" + driverName + "'.")}}}""").ConfigureAwait(false);
                    return;
                }

                ConnectionOpenRequest request = BuildOpenRequest(profile, resolvedTokens);
                int timeoutMs = request.ConnectTimeoutMs > 0 ? request.ConnectTimeoutMs : Sts2Defaults.ConnectTimeoutMs;
                using var timeoutSource = new CancellationTokenSource(timeoutMs);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

                try
                {
                    IDbSession session = await driver.OpenAsync(request, linked.Token).ConfigureAwait(false);
                    string handleId = "h-" + openId;
                    sessions[handleId] = session;
                    string serverInfo = string.Create(CultureInfo.InvariantCulture, $$"""
                        {"product":{{JsonSerializer.Serialize(session.Server.Product)}},"version":{{JsonSerializer.Serialize(session.Server.Version)}},"engineEdition":{{JsonSerializer.Serialize(session.Server.EngineEdition)}},"engineEditionId":{{JsonSerializer.Serialize(session.Server.EngineEditionId)}},"dialect":{{JsonSerializer.Serialize(session.Server.Dialect)}}}
                        """);
                    try
                    {
                        await PostOpenResultAsync(inbox, effect, connectionId, openId,
                            $$"""{"status":"ok","handleId":{{JsonSerializer.Serialize(handleId)}},"serverInfo":{{serverInfo}}}""").ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // The coordinator could not accept the open result (e.g. it is shutting
                        // down / its inbox is closed). Core will never learn this handle, so the
                        // freshly-opened session would be owned by no one — dispose it (R014).
                        if (sessions.TryRemove(handleId, out IDbSession? orphan))
                        {
                            try
                            {
                                await orphan.DisposeAsync().ConfigureAwait(false);
                            }
                            catch (Exception ex) when (ex is DbDriverException or ObjectDisposedException)
                            {
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    string payload = cancelSource.IsCancellationRequested
                        ? """{"status":"canceled"}"""
                        : string.Create(CultureInfo.InvariantCulture,
                            $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.ConnectionFailedTimeout)}},"message":"Connection attempt exceeded {{timeoutMs}}ms."}""");
                    await PostOpenResultAsync(inbox, effect, connectionId, openId, payload).ConfigureAwait(false);
                }
                catch (DbDriverException ex)
                {
                    string server = ex.Server is null
                        ? "null"
                        : string.Create(CultureInfo.InvariantCulture,
                            $$"""{"number":{{ex.Server.Number}},"severity":{{ex.Server.Severity}},"state":{{ex.Server.State}},"line":{{(ex.Server.Line?.ToString(CultureInfo.InvariantCulture) ?? "null")}}}""");
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(ex.Code)}},"message":{{JsonSerializer.Serialize(ex.Message)}},"server":{{server}}}""").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":{{JsonSerializer.Serialize("Driver threw an unclassified exception: " + ex.GetType().Name)}}}""").ConfigureAwait(false);
                }
            }
            finally
            {
                // SPEC §8.5: secret entries are removed when the open attempt completes.
                secrets.RemoveAll(resolvedTokens);
                opensInFlight.TryRemove(openId, out _);
                cancelSource.Dispose();
            }
        }

        /// <summary>
        /// Synchronously creates and registers the query pump (on the pump thread) so racing
        /// advance/cancel/dispose effects find it, then kicks the async streaming task.
        /// </summary>
        private void StartQueryPump(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            string queryId = GetString(effect.Args, "queryId") ?? "?";
            string? handleId = GetString(effect.Args, "handleId");
            string sql = GetString(effect.Args, "sql") ?? string.Empty;
            int initialCredit = effect.Args.TryGetProperty("credit", out JsonElement c) && c.ValueKind == JsonValueKind.Number
                ? c.GetInt32()
                : 1;
            // Core normalized options.maxCellBytes into the journaled effect args (STS2-3);
            // an absent/invalid value falls back to the pinned default (older journals).
            // QO-3: pageRows/pageBytes/queryTimeoutMs ride the same journaled args.
            int maxCellBytes = ArgsInt(effect.Args, "maxCellBytes", Sts2Defaults.MaxCellBytes);
            int pageRows = ArgsInt(effect.Args, "pageRows", Sts2Defaults.PageRows);
            int pageBytes = ArgsInt(effect.Args, "pageBytes", Sts2Defaults.PageBytes);
            int queryTimeoutMs = ArgsInt(effect.Args, "queryTimeoutMs", 0);

            if (handleId is null || !sessions.TryGetValue(handleId, out IDbSession? session))
            {
                _ = PostQueryEventAsync(inbox, effect, queryId,
                    $$"""{"eventType":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.NotFound)}},"message":"No session for handle."}""");
                return;
            }

            bool compactRows = effect.Args.TryGetProperty("compactRows", out JsonElement compactFlag)
                && compactFlag.ValueKind == JsonValueKind.True;

            var pump = new QueryPump { Session = session, Credits = new SemaphoreSlim(initialCredit) };
            queryPumps[queryId] = pump;
            pump.PumpTask = Task.Run(() => StreamQueryPumpAsync(effect, inbox, queryId, sql, maxCellBytes, pageRows, pageBytes, queryTimeoutMs, compactRows, pump));
        }

        private async Task StreamQueryPumpAsync(EffectWorkItem effect, ICoordinatorInbox inbox, string queryId, string sql, int maxCellBytes, int pageRows, int pageBytes, int queryTimeoutMs, bool compactRows, QueryPump pump)
        {
            await PostQueryEventAsync(inbox, effect, queryId, """{"eventType":"started"}""").ConfigureAwait(false);

            try
            {
                // QO-3: page sizing and timeout come from the journaled effect args
                // (Core-normalized, lower-only for page limits) — the hardcoded
                // defaults are gone; older journals fall back via ArgsInt.
                var request = new QueryExecuteRequest
                {
                    QueryId = queryId,
                    Sql = sql,
                    PageRows = pageRows,
                    PageBytes = pageBytes,
                    QueryTimeoutMs = queryTimeoutMs,
                    // QO-4: the driver streams large values bounded by this;
                    // the encoder stays authoritative for anything it didn't.
                    MaxCellBytes = maxCellBytes,
                };
                // Row-pipeline attribution (QO-2): readMs approximates driver/enumerator time
                // as the gap since the previous event finished posting; credit wait and encode
                // are measured exactly. Stats ride the journaled event payloads only — the
                // reducer never forwards them onto v2/query.rows.
                long lastEventDoneTicks = Stopwatch.GetTimestamp();
                // QO-5 compact rows: type hints computed once per result set
                // (from the driver's column metadata) ride every compact page.
                var typeHintsBySet = new Dictionary<int, string>();
                // Backpressure (SPEC §7.8): a rows page may only POST when the window has
                // credit; the WaitAsync below is what parks the enumerator. NOTE (R012): the
                // enumerator materializes the next page during MoveNext before this gate, so
                // at most ONE page can be read beyond the window. That overrun is bounded and
                // intentional here — eliminating it cleanly requires a credit-gated page-pull
                // port (gating MoveNext itself would deadlock trailing non-row events such as
                // query.complete, which must flow without consuming page credit).
                await foreach (ExecEvent execEvent in pump.Session.ExecuteAsync(request, pump.Cancellation.Token).ConfigureAwait(false))
                {
                    if (pump.Suppressed)
                    {
                        break;
                    }
                    switch (execEvent)
                    {
                        case ExecStarted:
                            break; // already announced

                        case ResultSetStarted resultSet:
                            if (compactRows)
                            {
                                typeHintsBySet[resultSet.ResultSetId] = SerializeTypeHints(resultSet.Columns);
                            }
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"resultSet","resultSetId":{{resultSet.ResultSetId}},"columns":{{SerializeColumns(resultSet.Columns)}}}""")).ConfigureAwait(false);
                            break;

                        case RowsPage page:
                        {
                            double readMs = ElapsedMs(lastEventDoneTicks);
                            long creditStartTicks = Stopwatch.GetTimestamp();
                            await pump.Credits.WaitAsync(pump.Cancellation.Token).ConfigureAwait(false);
                            double creditWaitMs = ElapsedMs(creditStartTicks);
                            if (pump.Suppressed)
                            {
                                break;
                            }
                            long encodeStartTicks = Stopwatch.GetTimestamp();
                            string rowsJson = SerializeRows(page.Cells, maxCellBytes);
                            // QO-5: the compact shape carries the null bitmap +
                            // type hints computed HERE (once), so the client
                            // stops rebuilding pages and re-measuring bytes.
                            string pageBody = compactRows
                                ? string.Create(CultureInfo.InvariantCulture, $$"""
                                    "compact":{"values":{{rowsJson}},"nullBitmap":{{JsonSerializer.Serialize(PackNullBitmap(page.Cells))}},"typeHints":{{typeHintsBySet.GetValueOrDefault(page.ResultSetId, "[]")}} },"approxBytes":{{rowsJson.Length}},"encodedBytes":{{rowsJson.Length}}
                                    """)
                                : string.Create(CultureInfo.InvariantCulture, $$"""
                                    "rows":{{rowsJson}}
                                    """);
                            double encodeMs = ElapsedMs(encodeStartTicks);
                            pump.StatsPages++;
                            pump.StatsRows += page.Cells.Count;
                            pump.StatsEncodedBytes += rowsJson.Length;
                            pump.StatsReadMsTotal += readMs;
                            pump.StatsCreditWaitMsTotal += creditWaitMs;
                            pump.StatsEncodeMsTotal += encodeMs;
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"rows","resultSetId":{{page.ResultSetId}},"pageSeq":{{page.PageSeq}},"rowOffset":{{page.RowOffset}},{{pageBody}},"stats":{"rowCount":{{page.Cells.Count}},"encodedBytes":{{rowsJson.Length}},"readMs":{{FormatMs(readMs)}},"creditWaitMs":{{FormatMs(creditWaitMs)}},"encodeMs":{{FormatMs(encodeMs)}} } }""")).ConfigureAwait(false);
                            break;
                        }

                        case ServerMessage message:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"message","messageClass":{{JsonSerializer.Serialize(message.MessageClass)}},"number":{{message.Number}},"severity":{{message.Severity}},"text":{{JsonSerializer.Serialize(message.Text)}},"line":{{(message.Line?.ToString(CultureInfo.InvariantCulture) ?? "null")}}}""")).ConfigureAwait(false);
                            break;

                        case ResultSetCompleted done:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"resultSetDone","resultSetId":{{done.ResultSetId}},"rowCount":{{done.RowCount}}}""")).ConfigureAwait(false);
                            break;

                        case ExecCompleted completed:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"completed","rowsAffected":{{completed.RowsAffected.Sum()}},"database":{{JsonSerializer.Serialize(completed.Database)}},"stats":{"pages":{{pump.StatsPages}},"rows":{{pump.StatsRows}},"encodedBytes":{{pump.StatsEncodedBytes}},"readMsTotal":{{FormatMs(pump.StatsReadMsTotal)}},"creditWaitMsTotal":{{FormatMs(pump.StatsCreditWaitMsTotal)}},"encodeMsTotal":{{FormatMs(pump.StatsEncodeMsTotal)}} } }""")).ConfigureAwait(false);
                            break;

                        default:
                            break;
                    }
                    lastEventDoneTicks = Stopwatch.GetTimestamp();
                }
            }
            catch (OperationCanceledException)
            {
                if (!pump.Suppressed)
                {
                    await PostQueryEventAsync(inbox, effect, queryId, """{"eventType":"canceled"}""").ConfigureAwait(false);
                }
            }
            catch (DbDriverException ex)
            {
                if (!pump.Suppressed)
                {
                    string server = ex.Server is null
                        ? "null"
                        : string.Create(CultureInfo.InvariantCulture,
                            $$"""{"number":{{ex.Server.Number}},"severity":{{ex.Server.Severity}},"state":{{ex.Server.State}},"line":{{(ex.Server.Line?.ToString(CultureInfo.InvariantCulture) ?? "null")}}}""");
                    await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                        $$"""{"eventType":"error","code":{{JsonSerializer.Serialize(ex.Code)}},"message":{{JsonSerializer.Serialize(ex.Message)}},"server":{{server}}}""")).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (!pump.Suppressed)
                {
                    await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                        $$"""{"eventType":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":{{JsonSerializer.Serialize("Driver threw an unclassified exception: " + ex.GetType().Name)}}}""")).ConfigureAwait(false);
                }
            }
            finally
            {
                queryPumps.TryRemove(queryId, out _);
            }
        }

        /// <summary>
        /// Row-major LSB-first null bitmap over the page's cells (QO-5) —
        /// byte[i>>3] bit (i&7), matching the client's packBitmap layout
        /// exactly (sts2Backend.ts): the client consumes it verbatim.
        /// </summary>
        private static string PackNullBitmap(IReadOnlyList<IReadOnlyList<object?>> rows)
        {
            int columns = rows.Count > 0 ? rows[0].Count : 0;
            var bytes = new byte[(rows.Count * columns + 7) / 8];
            int index = 0;
            foreach (IReadOnlyList<object?> row in rows)
            {
                foreach (object? cell in row)
                {
                    if (cell is null or DBNull)
                    {
                        bytes[index >> 3] |= (byte)(1 << (index & 7));
                    }
                    index++;
                }
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// engineType → compact type hint (QO-5), the same taxonomy the client
        /// binding used to compute per page (typeHintFor in sts2Backend.ts) —
        /// computed ONCE per result set here. Keep the two mappings identical.
        /// </summary>
        private static string SerializeTypeHints(IReadOnlyList<ColumnInfo> columns)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (ColumnInfo column in columns)
            {
                string t = column.EngineType.ToLowerInvariant();
                string hint = t switch
                {
                    "bit" => "boolean",
                    "int" or "smallint" or "tinyint" or "float" or "real" => "number",
                    "bigint" or "decimal" or "numeric" or "money" or "smallmoney" => "number:approx",
                    "varbinary" or "binary" or "image" or "timestamp" or "rowversion" => "binary",
                    "xml" => "xml",
                    _ when t.StartsWith("date", StringComparison.Ordinal)
                        || t.StartsWith("time", StringComparison.Ordinal)
                        || t == "smalldatetime" => "datetime",
                    _ => "string",
                };
                array.Add((System.Text.Json.Nodes.JsonNode)hint);
            }
            return array.ToJsonString();
        }

        private static string SerializeColumns(IReadOnlyList<ColumnInfo> columns)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (ColumnInfo column in columns)
            {
                array.Add(new System.Text.Json.Nodes.JsonObject
                {
                    ["name"] = column.Name,
                    ["type"] = column.EngineType,
                    ["nullable"] = column.Nullable,
                });
            }
            return array.ToJsonString();
        }

        private static string SerializeRows(IReadOnlyList<IReadOnlyList<object?>> rows, int maxCellBytes)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (IReadOnlyList<object?> row in rows)
            {
                var cells = new System.Text.Json.Nodes.JsonArray();
                foreach (object? cell in row)
                {
                    // SPEC §7.7 wire encoding at the query's effective bound (R024/STS2-3):
                    // one giant cell cannot break the memory/frame bound, and an oversized
                    // cell arrives as an honest truncated wrapper, never silently clipped.
                    cells.Add(WireValueEncoder.Encode(cell, maxCellBytes));
                }
                array.Add(cells);
            }
            return array.ToJsonString();
        }

        /// <summary>Positive-int effect arg with fallback (absent/invalid/non-positive = default).</summary>
        private static int ArgsInt(JsonElement args, string name, int defaultValue) =>
            args.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int parsed)
            && parsed > 0
                ? parsed
                : defaultValue;

        private static double ElapsedMs(long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;

        /// <summary>Invariant, fixed-precision ms for JSON payloads (2 decimals).</summary>
        private static string FormatMs(double ms) =>
            Math.Round(ms, 2).ToString(CultureInfo.InvariantCulture);

        private static Task PostQueryEventAsync(ICoordinatorInbox inbox, EffectWorkItem effect, string queryId, string eventCore)
        {
            // queryId is merged in so Core routes without tracking effect ids.
            string payload = string.Create(CultureInfo.InvariantCulture,
                $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}},{{eventCore[1..]}}""");
            return inbox.PostEffectResponseAsync("evt-" + queryId, "driver.queryEvent",
                JsonDocument.Parse(payload).RootElement, effect.CauseSeq).AsTask();
        }

        private async Task ExportAsync(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            string? corr = GetString(effect.Args, "corr");
            if (exportTemplate is null)
            {
                await PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                    $$"""{"corr":{{JsonSerializer.Serialize(corr)}},"status":"error","message":"Export is not configured for this session."}""")).ConfigureAwait(false);
                return;
            }

            try
            {
                bool includeSql = effect.Args.TryGetProperty("includeSqlText", out JsonElement s) && s.ValueKind == JsonValueKind.True;
                Export.ExportBundleResult result = await Task.Run(() => Export.ExportBundleWriter.Write(
                    exportTemplate with { IncludeSqlText = includeSql }, timeProvider)).ConfigureAwait(false);
                await PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                    $$"""{"corr":{{JsonSerializer.Serialize(corr)}},"status":"ok","bundlePath":{{JsonSerializer.Serialize(result.BundlePath)}},"bytes":{{result.Bytes}}}""")).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                await PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                    $$"""{"corr":{{JsonSerializer.Serialize(corr)}},"status":"error","message":{{JsonSerializer.Serialize("Export failed: " + ex.Message)}}}""")).ConfigureAwait(false);
            }
        }

        private async Task CloseAsync(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            string connectionId = GetString(effect.Args, "connectionId") ?? "?";
            string? handleId = GetString(effect.Args, "handleId");
            if (handleId is not null && sessions.TryRemove(handleId, out IDbSession? session))
            {
                try
                {
                    // Bounded close (sts2.runtime.closeTimeoutMs): never wedge shutdown on a driver.
                    await session.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromMilliseconds(Sts2Defaults.CloseTimeoutMs)).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is TimeoutException or DbDriverException or ObjectDisposedException)
                {
                    // The handle is gone either way; the journal records the close.
                }
            }
            await PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                $$"""{"status":"ok","connectionId":{{JsonSerializer.Serialize(connectionId)}}}""")).ConfigureAwait(false);
        }

        private ConnectionOpenRequest BuildOpenRequest(JsonElement profile, List<string> resolvedTokens)
        {
            JsonElement auth = profile.TryGetProperty("auth", out JsonElement a) && a.ValueKind == JsonValueKind.Object
                ? a
                : default;
            string kind = auth.ValueKind == JsonValueKind.Object ? GetString(auth, "kind") ?? "integrated" : "integrated";
            string? user = auth.ValueKind == JsonValueKind.Object ? GetString(auth, "user") : null;

            string? secret = null;
            if (auth.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in auth.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String
                        && property.Value.GetString() is string token
                        && token.StartsWith("secret:", StringComparison.Ordinal)
                        && secrets.TryResolve(token, out string resolved))
                    {
                        resolvedTokens.Add(token);
                        secret ??= resolved; // first credential field wins (password or token)
                    }
                }
            }

            int connectTimeoutMs = 0;
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            if (profile.TryGetProperty("options", out JsonElement opts) && opts.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in opts.EnumerateObject())
                {
                    if (property.Name == "connectTimeoutMs" && property.Value.ValueKind == JsonValueKind.Number)
                    {
                        connectTimeoutMs = property.Value.GetInt32();
                    }
                    else
                    {
                        options[property.Name] = property.Value.ToString();
                    }
                }
            }

            return new ConnectionOpenRequest
            {
                Server = GetString(profile, "server") ?? string.Empty,
                Database = GetString(profile, "database"),
                Auth = new SecretMaterial { Kind = kind, User = user, Secret = secret },
                ConnectTimeoutMs = connectTimeoutMs,
                ApplicationName = options.TryGetValue("applicationName", out string? app) ? app : null,
                Options = options,
            };
        }

        private static Task PostOpenResultAsync(ICoordinatorInbox inbox, EffectWorkItem effect, string connectionId, string openId, string payloadCore)
        {
            // Merge connectionId/openId into the payload so Core can route without
            // tracking effect ids beyond the journal.
            string payload = string.Create(CultureInfo.InvariantCulture, $$"""
                {"connectionId":{{JsonSerializer.Serialize(connectionId)}},"openId":{{JsonSerializer.Serialize(openId)}},{{payloadCore[1..]}}
                """);
            return PostAsync(inbox, effect, payload);
        }

        private static Task PostDisposeAckAsync(ICoordinatorInbox inbox, EffectWorkItem effect, string queryId)
        {
            // The dispose result carries the queryId so Core routes it to the dispose handler
            // (which emits the single query.complete(disposed) once the pump has stopped).
            string payload = string.Create(CultureInfo.InvariantCulture,
                $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}},"status":"ok"}""");
            return PostAsync(inbox, effect, payload);
        }

        private static Task PostAsync(ICoordinatorInbox inbox, EffectWorkItem effect, string payloadJson)
        {
            JsonElement payload = JsonDocument.Parse(payloadJson).RootElement;
            return inbox.PostEffectResponseAsync(effect.EffectId, effect.EffectName, payload, effect.CauseSeq).AsTask();
        }

        private static string? GetString(JsonElement element, string property) =>
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
