//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// Last-line frame guard (D-0019): the pinned transport frame bound minus
        /// 1 MiB of slack for the event/envelope wrapper around the rows JSON.
        /// Page construction (pageBytes + accurate cell estimates) should keep
        /// pages far below this; the guard turns the pathological case (one row
        /// of many maximum-size cells) into a typed failure instead of an
        /// oversized frame.
        /// </summary>
        private const int FrameGuardBytes = Sts2Defaults.MaxFrameBytes - 1048576;

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
            public long StatsCellSlots;
            public long StatsNullCells;
            public long StatsEventPayloadBytes;
            public long StatsMaxEventPayloadBytes;
            public long StatsEncodePrepAllocatedBytes;
            public long StatsEventBuildAllocatedBytes;
            public long StatsPostBuildAllocatedBytes;
            public double StatsRowsSerializeMsTotal;
            public double StatsUtf8MeasureMsTotal;
            public double StatsNullBitmapMsTotal;
            public double StatsPageBodyBuildMsTotal;
            public double StatsEventBuildMsTotal;
            public double StatsPostBuildMsTotal;
            public double StatsPostMsTotal;
        }

        private readonly struct QueryEventPostStats
        {
            public QueryEventPostStats(long payloadBytes, double buildMs, long allocatedBytes)
            {
                PayloadBytes = payloadBytes;
                BuildMs = buildMs;
                AllocatedBytes = allocatedBytes;
            }

            public long PayloadBytes { get; }
            public double BuildMs { get; }
            public long AllocatedBytes { get; }
        }

        private readonly record struct RowsEventPostStats(
            long CellSlots,
            long NullCells,
            long EncodedBytes,
            double RowsSerializeMs,
            double Utf8MeasureMs,
            double NullBitmapMs,
            double PageBodyBuildMs,
            double EncodeMs,
            long EncodePrepAllocatedBytes,
            double EventBuildMs,
            long EventBuildAllocatedBytes,
            QueryEventPostStats Post);

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
                if (!effect.Args.TryGetProperty("profile", out JsonElement profile)
                    || profile.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidAuthRequest("Connection profile is required.");
                }
                string driverName = GetString(profile, "driver") ?? "fake";
                ConnectionOpenRequest request = BuildOpenRequest(profile, resolvedTokens);
                if (!drivers.TryGetValue(driverName, out IDbDriver? driver))
                {
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Unavailable)}},"message":{{JsonSerializer.Serialize("No driver registered with name '" + driverName + "'.")}}}""").ConfigureAwait(false);
                    return;
                }

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
                    await PostOpenDriverErrorAsync(inbox, effect, connectionId, openId, ex).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":{{JsonSerializer.Serialize("Driver threw an unclassified exception: " + ex.GetType().Name)}}}""").ConfigureAwait(false);
                }
            }
            catch (DbDriverException ex)
            {
                // Request construction happens before the driver try/catch above. Validation
                // failures must still complete the open effect instead of faulting this task.
                await PostOpenDriverErrorAsync(inbox, effect, connectionId, openId, ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await PostOpenResultAsync(inbox, effect, connectionId, openId,
                    $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":{{JsonSerializer.Serialize("Open request preparation threw an unclassified exception: " + ex.GetType().Name)}}}""").ConfigureAwait(false);
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
            // D-0019: Core normalized options.vectorEncoding="binary-v1" into this
            // journaled bool; the driver emits typed vector cells only when set.
            bool vectorBinary = effect.Args.TryGetProperty("vectorBinary", out JsonElement vectorFlag)
                && vectorFlag.ValueKind == JsonValueKind.True;
            bool spatialWkb = effect.Args.TryGetProperty("spatialWkb", out JsonElement spatialFlag)
                && spatialFlag.ValueKind == JsonValueKind.True;

            var pump = new QueryPump { Session = session, Credits = new SemaphoreSlim(initialCredit) };
            queryPumps[queryId] = pump;
            pump.PumpTask = Task.Run(() => StreamQueryPumpAsync(effect, inbox, queryId, sql, maxCellBytes, pageRows, pageBytes, queryTimeoutMs, compactRows, vectorBinary, spatialWkb, pump));
        }

        private async Task StreamQueryPumpAsync(EffectWorkItem effect, ICoordinatorInbox inbox, string queryId, string sql, int maxCellBytes, int pageRows, int pageBytes, int queryTimeoutMs, bool compactRows, bool vectorBinary, bool spatialWkb, QueryPump pump)
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
                    // D-0019: typed vector cells only for opted-in queries.
                    VectorBinary = vectorBinary,
                    SpatialWkb = spatialWkb,
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
                                typeHintsBySet[resultSet.ResultSetId] = SerializeTypeHints(resultSet.Columns, vectorBinary, spatialWkb);
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
                            long postStartTicks = Stopwatch.GetTimestamp();
                            await PostRowsPageAsync(
                                inbox,
                                effect,
                                queryId,
                                page,
                                compactRows,
                                maxCellBytes,
                                typeHintsBySet.GetValueOrDefault(page.ResultSetId, "[]"),
                                readMs,
                                creditWaitMs,
                                out RowsEventPostStats pageStats).ConfigureAwait(false);
                            double postMs = ElapsedMs(postStartTicks);
                            pump.StatsPages++;
                            pump.StatsRows += page.Cells.Count;
                            pump.StatsCellSlots += pageStats.CellSlots;
                            pump.StatsNullCells += pageStats.NullCells;
                            pump.StatsEncodedBytes += pageStats.EncodedBytes;
                            pump.StatsReadMsTotal += readMs;
                            pump.StatsCreditWaitMsTotal += creditWaitMs;
                            pump.StatsEncodeMsTotal += pageStats.EncodeMs;
                            pump.StatsRowsSerializeMsTotal += pageStats.RowsSerializeMs;
                            pump.StatsUtf8MeasureMsTotal += pageStats.Utf8MeasureMs;
                            pump.StatsNullBitmapMsTotal += pageStats.NullBitmapMs;
                            pump.StatsPageBodyBuildMsTotal += pageStats.PageBodyBuildMs;
                            pump.StatsEventBuildMsTotal += pageStats.EventBuildMs;
                            pump.StatsEncodePrepAllocatedBytes += pageStats.EncodePrepAllocatedBytes;
                            pump.StatsEventBuildAllocatedBytes += pageStats.EventBuildAllocatedBytes;
                            pump.StatsEventPayloadBytes += pageStats.Post.PayloadBytes;
                            pump.StatsMaxEventPayloadBytes = Math.Max(
                                pump.StatsMaxEventPayloadBytes,
                                pageStats.Post.PayloadBytes);
                            pump.StatsPostBuildMsTotal += pageStats.Post.BuildMs;
                            pump.StatsPostBuildAllocatedBytes += pageStats.Post.AllocatedBytes;
                            pump.StatsPostMsTotal += postMs;
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
                                $$"""{"eventType":"completed","rowsAffected":{{completed.RowsAffected.Sum()}},"database":{{JsonSerializer.Serialize(completed.Database)}},"stats":{"pages":{{pump.StatsPages}},"rows":{{pump.StatsRows}},"cellSlots":{{pump.StatsCellSlots}},"nullCells":{{pump.StatsNullCells}},"encodedBytes":{{pump.StatsEncodedBytes}},"eventPayloadBytes":{{pump.StatsEventPayloadBytes}},"maxEventPayloadBytes":{{pump.StatsMaxEventPayloadBytes}},"readMsTotal":{{FormatMs(pump.StatsReadMsTotal)}},"creditWaitMsTotal":{{FormatMs(pump.StatsCreditWaitMsTotal)}},"encodeMsTotal":{{FormatMs(pump.StatsEncodeMsTotal)}},"rowsSerializeMsTotal":{{FormatMs(pump.StatsRowsSerializeMsTotal)}},"utf8MeasureMsTotal":{{FormatMs(pump.StatsUtf8MeasureMsTotal)}},"nullBitmapMsTotal":{{FormatMs(pump.StatsNullBitmapMsTotal)}},"pageBodyBuildMsTotal":{{FormatMs(pump.StatsPageBodyBuildMsTotal)}},"eventBuildMsTotal":{{FormatMs(pump.StatsEventBuildMsTotal)}},"postBuildMsTotal":{{FormatMs(pump.StatsPostBuildMsTotal)}},"postMsTotal":{{FormatMs(pump.StatsPostMsTotal)}},"encodePrepAllocatedBytes":{{pump.StatsEncodePrepAllocatedBytes}},"eventBuildAllocatedBytes":{{pump.StatsEventBuildAllocatedBytes}},"postBuildAllocatedBytes":{{pump.StatsPostBuildAllocatedBytes}} } }""")).ConfigureAwait(false);
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
        /// D-0019: vector columns hint "vector:f32le:v1" only for queries that
        /// negotiated the typed encoding (the client's mapping changes in
        /// lockstep behind the same negotiation); otherwise they stay "string"
        /// (JSON text cells). Hints route fast paths but are never trusted
        /// without structural validation — the cell tag is self-describing.
        /// </summary>
        private static string SerializeTypeHints(IReadOnlyList<ColumnInfo> columns, bool vectorBinary, bool spatialWkb)
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
                    "vector" when vectorBinary => "vector:f32le:v1",
                    _ when spatialWkb && column.SpatialEncoding == "wkb-v1" => "spatial:wkb:v1",
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
                var obj = new System.Text.Json.Nodes.JsonObject
                {
                    ["name"] = column.Name,
                    ["type"] = column.EngineType,
                    ["nullable"] = column.Nullable,
                };
                // D-0019 (additive, SPEC §7.7 already promised these): normalized
                // facts serialized only when the driver knows them. For vector
                // columns, length = 8 + 4*dimensions, so clients can derive the
                // dimension count from metadata alone.
                if (column.Precision is int precision)
                {
                    obj["precision"] = precision;
                }
                if (column.Scale is int scale)
                {
                    obj["scale"] = scale;
                }
                if (column.Length is int length)
                {
                    obj["length"] = length;
                }
                if (column.Collation is string collation)
                {
                    obj["collation"] = collation;
                }
                if (column.SpatialKind is string spatialKind && column.SpatialEncoding == "wkb-v1")
                {
                    obj["spatial"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["kind"] = spatialKind,
                        ["encoding"] = "wkb-v1",
                    };
                }
                array.Add(obj);
            }
            return array.ToJsonString();
        }

        private static long WriteRows(
            Utf8JsonWriter writer,
            IReadOnlyList<IReadOnlyList<object?>> rows,
            int maxCellBytes,
            out long cellSlots,
            out long nullCells)
        {
            cellSlots = 0;
            nullCells = 0;
            long bytesBefore = writer.BytesCommitted + writer.BytesPending;
            writer.WriteStartArray();
            foreach (IReadOnlyList<object?> row in rows)
            {
                writer.WriteStartArray();
                foreach (object? cell in row)
                {
                    cellSlots++;
                    if (cell is null or DBNull)
                    {
                        nullCells++;
                    }
                    // SPEC §7.7 wire encoding at the query's effective bound
                    // (R024/STS2-3). Write directly into the final event buffer:
                    // building a JsonNode for every cell duplicated the page graph.
                    WireValueEncoder.Write(writer, cell, maxCellBytes);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            return writer.BytesCommitted + writer.BytesPending - bytesBefore;
        }

        private static Task PostRowsPageAsync(
            ICoordinatorInbox inbox,
            EffectWorkItem effect,
            string queryId,
            RowsPage page,
            bool compactRows,
            int maxCellBytes,
            string typeHintsJson,
            double readMs,
            double creditWaitMs,
            out RowsEventPostStats stats)
        {
            long encodeStartTicks = Stopwatch.GetTimestamp();
            long encodePrepAllocationStart = GC.GetAllocatedBytesForCurrentThread();

            // QO-5: compact pages carry one server-built null bitmap plus type
            // hints computed once per result set; the client consumes both verbatim.
            string? nullBitmap = null;
            double nullBitmapMs = 0;
            if (compactRows)
            {
                long nullBitmapStartTicks = Stopwatch.GetTimestamp();
                nullBitmap = PackNullBitmap(page.Cells);
                nullBitmapMs = ElapsedMs(nullBitmapStartTicks);
            }

            var buffer = new ArrayBufferWriter<byte>(EstimateRowsEventCapacity(page.Cells, maxCellBytes));
            using var writer = new Utf8JsonWriter(buffer);
            writer.WriteStartObject();
            writer.WriteString("queryId", queryId);
            writer.WriteString("eventType", "rows");
            writer.WriteNumber("resultSetId", page.ResultSetId);
            writer.WriteNumber("pageSeq", page.PageSeq);
            writer.WriteNumber("rowOffset", page.RowOffset);

            long pageBodyPrefixStartTicks = Stopwatch.GetTimestamp();
            if (compactRows)
            {
                writer.WritePropertyName("compact");
                writer.WriteStartObject();
                writer.WritePropertyName("values");
            }
            else
            {
                writer.WritePropertyName("rows");
            }
            double pageBodyBuildMs = ElapsedMs(pageBodyPrefixStartTicks);

            long rowsSerializeStartTicks = Stopwatch.GetTimestamp();
            long encodedBytes = WriteRows(
                writer,
                page.Cells,
                maxCellBytes,
                out long cellSlots,
                out long nullCells);
            double rowsSerializeMs = ElapsedMs(rowsSerializeStartTicks);

            // Honest byte accounting comes from the UTF-8 writer itself. The
            // separate full-string measurement pass is gone, hence zero here.
            const double utf8MeasureMs = 0;
            if (encodedBytes > FrameGuardBytes)
            {
                throw new DbDriverException(
                    Sts2ErrorCodes.QueryFailedTransport,
                    string.Create(CultureInfo.InvariantCulture,
                        $"Encoded rows page ({encodedBytes} bytes) exceeds the frame budget ({FrameGuardBytes} bytes); a single row is too large to transport."));
            }

            long pageBodySuffixStartTicks = Stopwatch.GetTimestamp();
            if (compactRows)
            {
                writer.WriteString("nullBitmap", nullBitmap);
                writer.WritePropertyName("typeHints");
                writer.WriteRawValue(typeHintsJson, skipInputValidation: true);
                writer.WriteEndObject();
                writer.WriteNumber("approxBytes", encodedBytes);
                writer.WriteNumber("encodedBytes", encodedBytes);
            }
            pageBodyBuildMs += ElapsedMs(pageBodySuffixStartTicks);
            double encodeMs = ElapsedMs(encodeStartTicks);
            long encodePrepAllocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - encodePrepAllocationStart;

            long eventBuildStartTicks = Stopwatch.GetTimestamp();
            long eventBuildAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            writer.WritePropertyName("stats");
            writer.WriteStartObject();
            writer.WriteNumber("rowCount", page.Cells.Count);
            writer.WriteNumber("cellSlots", cellSlots);
            writer.WriteNumber("nullCells", nullCells);
            writer.WriteNumber("encodedBytes", encodedBytes);
            writer.WriteNumber("readMs", Math.Round(readMs, 2));
            writer.WriteNumber("creditWaitMs", Math.Round(creditWaitMs, 2));
            writer.WriteNumber("encodeMs", Math.Round(encodeMs, 2));
            writer.WriteNumber("rowsSerializeMs", Math.Round(rowsSerializeMs, 2));
            writer.WriteNumber("utf8MeasureMs", utf8MeasureMs);
            writer.WriteNumber("nullBitmapMs", Math.Round(nullBitmapMs, 2));
            writer.WriteNumber("pageBodyBuildMs", Math.Round(pageBodyBuildMs, 2));
            writer.WriteNumber("encodePrepAllocatedBytes", encodePrepAllocatedBytes);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            double eventBuildMs = ElapsedMs(eventBuildStartTicks);
            long eventBuildAllocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - eventBuildAllocationStart;

            long postBuildStartTicks = Stopwatch.GetTimestamp();
            long postBuildAllocationStart = GC.GetAllocatedBytesForCurrentThread();
            long payloadBytes = buffer.WrittenCount;
            JsonElement root = JsonDocument.Parse(buffer.WrittenMemory).RootElement;
            var postStats = new QueryEventPostStats(
                payloadBytes,
                ElapsedMs(postBuildStartTicks),
                GC.GetAllocatedBytesForCurrentThread() - postBuildAllocationStart);
            stats = new RowsEventPostStats(
                cellSlots,
                nullCells,
                encodedBytes,
                rowsSerializeMs,
                utf8MeasureMs,
                nullBitmapMs,
                pageBodyBuildMs,
                encodeMs,
                encodePrepAllocatedBytes,
                eventBuildMs,
                eventBuildAllocatedBytes,
                postStats);
            return inbox.PostEffectResponseAsync(
                "evt-" + queryId,
                "driver.queryEvent",
                root,
                effect.CauseSeq).AsTask();
        }

        private static int EstimateRowsEventCapacity(
            IReadOnlyList<IReadOnlyList<object?>> rows,
            int maxCellBytes)
        {
            long estimate = 4096;
            foreach (IReadOnlyList<object?> row in rows)
            {
                estimate += 2;
                foreach (object? cell in row)
                {
                    estimate += 1 + EstimateCellJsonBytes(cell, maxCellBytes);
                    if (estimate >= FrameGuardBytes)
                    {
                        return FrameGuardBytes;
                    }
                }
            }
            return (int)Math.Clamp(estimate, 4096, FrameGuardBytes);
        }

        private static long EstimateCellJsonBytes(object? cell, int maxCellBytes)
        {
            static long EscapedText(int chars) => chars + chars / 4L + 2;
            int prefixCap = maxCellBytes > 0
                ? Math.Min(maxCellBytes, Sts2Defaults.TruncatedPrefixBytes)
                : Sts2Defaults.TruncatedPrefixBytes;
            return cell switch
            {
                null or DBNull => 4,
                string value => EscapedText(Math.Min(value.Length, prefixCap)) + 160,
                byte[] value => (long)Math.Min(value.Length, prefixCap) * 4 / 3 + 160,
                Abstractions.DriverTruncatedValue value when value.Kind == "binary" =>
                    (long)Math.Min(value.PrefixBytes?.Length ?? 0, prefixCap) * 4 / 3 + 160,
                Abstractions.DriverTruncatedValue value =>
                    EscapedText(Math.Min(value.PrefixText?.Length ?? 0, prefixCap)) + 160,
                Abstractions.DriverVectorValue value => (long)value.ComponentBytes.Length * 4 / 3 + 192,
                Abstractions.DriverSpatialValue value => (long)value.Wkb.Length * 4 / 3 + 192,
                Abstractions.DriverVectorUnavailableValue or
                Abstractions.DriverSpatialUnavailableValue => 256,
                bool => 5,
                Guid or DateTime or DateTimeOffset => 64,
                TimeSpan => 32,
                decimal or double or float or long or int or short or byte => 32,
                System.Text.Json.Nodes.JsonNode => 256,
                _ => 128,
            };
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

        private static Task PostQueryEventAsync(
            ICoordinatorInbox inbox,
            EffectWorkItem effect,
            string queryId,
            string eventCore) =>
            PostQueryEventAsync(inbox, effect, queryId, eventCore, out _);

        private static Task PostQueryEventAsync(
            ICoordinatorInbox inbox,
            EffectWorkItem effect,
            string queryId,
            string eventCore,
            out QueryEventPostStats stats)
        {
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long buildStartTicks = Stopwatch.GetTimestamp();
            // queryId is merged in so Core routes without tracking effect ids.
            string payload = string.Create(CultureInfo.InvariantCulture,
                $$"""{"queryId":{{JsonSerializer.Serialize(queryId)}},{{eventCore[1..]}}""");
            long payloadBytes = Encoding.UTF8.GetByteCount(payload);
            JsonElement root = JsonDocument.Parse(payload).RootElement;
            stats = new QueryEventPostStats(
                payloadBytes,
                ElapsedMs(buildStartTicks),
                GC.GetAllocatedBytesForCurrentThread() - allocationStart);
            return inbox.PostEffectResponseAsync(
                "evt-" + queryId,
                "driver.queryEvent",
                root,
                effect.CauseSeq).AsTask();
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
            JsonElement auth = default;
            if (profile.TryGetProperty("auth", out JsonElement authValue))
            {
                if (authValue.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidAuthRequest("Connection profile auth must be an object.");
                }
                auth = authValue;
            }
            string kind = auth.ValueKind == JsonValueKind.Object ? GetString(auth, "kind") ?? "integrated" : "integrated";
            string? user = auth.ValueKind == JsonValueKind.Object ? GetString(auth, "user") : null;
            string? secret = ResolveAuthSecret(auth, kind, resolvedTokens);

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

        /// <summary>
        /// Resolves only the credential field allowed by <paramref name="kind"/>. The
        /// canonical access-token field is <c>token</c>; <c>accessToken</c> remains a
        /// compatibility alias. All side-table references are collected before validation
        /// so malformed mixed payloads are scrubbed on the same lifecycle as valid opens.
        /// </summary>
        private string? ResolveAuthSecret(JsonElement auth, string kind, List<string> resolvedTokens)
        {
            CollectResolvedSecretReferences(auth, resolvedTokens);

            var fields = new Dictionary<string, string?>(StringComparer.Ordinal);
            string? validationError = null;
            if (auth.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in auth.EnumerateObject())
                {
                    if (property.NameEquals("kind") || property.NameEquals("user"))
                    {
                        continue;
                    }

                    bool isCredential = IsAuthCredentialField(property.Name);
                    if (!isCredential)
                    {
                        validationError ??= "Authentication contains an unsupported field.";
                        continue;
                    }
                    string? resolved = null;
                    if (property.Value.ValueKind == JsonValueKind.String
                        && property.Value.GetString() is string tokenReference)
                    {
                        if (tokenReference.StartsWith("secret:", StringComparison.Ordinal)
                            && secrets.TryResolve(tokenReference, out string secretValue))
                        {
                            resolved = secretValue;
                        }
                        else if (isCredential)
                        {
                            validationError ??= "Authentication credential reference could not be resolved.";
                        }
                    }
                    else if (isCredential)
                    {
                        validationError ??= "Authentication credentials must be strings.";
                    }

                    if (!fields.TryAdd(property.Name, resolved))
                    {
                        validationError ??= "Authentication credential fields must not be repeated.";
                    }
                }
            }

            if (validationError is not null)
            {
                throw InvalidAuthRequest(validationError);
            }

            bool hasPassword = fields.TryGetValue("password", out string? password);
            bool hasToken = fields.TryGetValue("token", out string? token);
            bool hasAccessTokenAlias = fields.TryGetValue("accessToken", out string? accessTokenAlias);

            switch (kind)
            {
                case "sqlLogin":
                    if (hasToken || hasAccessTokenAlias)
                    {
                        throw InvalidAuthRequest("SQL login authentication cannot include access-token credentials.");
                    }
                    // Empty SQL passwords are valid and intentionally remain distinct from
                    // an unresolved credential reference, which was rejected above.
                    return hasPassword ? password : null;

                case "accessToken":
                    if (hasPassword)
                    {
                        throw InvalidAuthRequest("Access-token authentication cannot include a password.");
                    }
                    if (hasToken && hasAccessTokenAlias)
                    {
                        throw InvalidAuthRequest("Specify either auth.token or auth.accessToken, not both.");
                    }
                    if (!hasToken && !hasAccessTokenAlias)
                    {
                        throw InvalidAuthRequest("Access-token authentication requires auth.token.");
                    }
                    string? accessToken = hasToken ? token : accessTokenAlias;
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        throw InvalidAuthRequest("Access token must not be empty.");
                    }
                    return accessToken;

                case "integrated":
                    if (hasPassword || hasToken || hasAccessTokenAlias)
                    {
                        throw InvalidAuthRequest("Integrated authentication cannot include credentials.");
                    }
                    return null;

                default:
                    throw InvalidAuthRequest("Unsupported auth kind: " + kind);
            }
        }

        private void CollectResolvedSecretReferences(JsonElement value, List<string> resolvedTokens)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in value.EnumerateObject())
                    {
                        // SecretRedactor leaves these auth metadata keys in clear text at
                        // every nesting level; mirror that boundary here.
                        if (!property.NameEquals("kind") && !property.NameEquals("user"))
                        {
                            CollectResolvedSecretReferences(property.Value, resolvedTokens);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (JsonElement item in value.EnumerateArray())
                    {
                        CollectResolvedSecretReferences(item, resolvedTokens);
                    }
                    break;

                case JsonValueKind.String:
                    if (value.GetString() is string tokenReference
                        && tokenReference.StartsWith("secret:", StringComparison.Ordinal)
                        && secrets.TryResolve(tokenReference, out _))
                    {
                        resolvedTokens.Add(tokenReference);
                    }
                    break;
            }
        }

        private static bool IsAuthCredentialField(string name) =>
            name is "password" or "token" or "accessToken";

        private static DbDriverException InvalidAuthRequest(string message) =>
            new(Sts2ErrorCodes.InvalidRequest, message);

        private static Task PostOpenDriverErrorAsync(
            ICoordinatorInbox inbox,
            EffectWorkItem effect,
            string connectionId,
            string openId,
            DbDriverException error)
        {
            string server = error.Server is null
                ? "null"
                : string.Create(CultureInfo.InvariantCulture,
                    $$"""{"number":{{error.Server.Number}},"severity":{{error.Server.Severity}},"state":{{error.Server.State}},"line":{{(error.Server.Line?.ToString(CultureInfo.InvariantCulture) ?? "null")}}}""");
            return PostOpenResultAsync(inbox, effect, connectionId, openId,
                $$"""{"status":"error","code":{{JsonSerializer.Serialize(error.Code)}},"message":{{JsonSerializer.Serialize(error.Message)}},"server":{{server}}}""");
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
