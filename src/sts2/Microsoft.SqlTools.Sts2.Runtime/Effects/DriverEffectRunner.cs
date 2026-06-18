//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

            /// <summary>True after dispose: no further events may post (I3).</summary>
            public volatile bool Suppressed;
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
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await pump.Session.CancelAsync(queryId, CancellationToken.None).ConfigureAwait(false);
                            }
                            catch (Exception ex) when (ex is DbDriverException or ObjectDisposedException)
                            {
                            }
                            pump.Cancellation.Cancel();
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
                    }
                    _ = PostAsync(inbox, effect, """{"status":"ok"}""");
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
                        {"product":{{JsonSerializer.Serialize(session.Server.Product)}},"version":{{JsonSerializer.Serialize(session.Server.Version)}},"engineEdition":{{JsonSerializer.Serialize(session.Server.EngineEdition)}},"dialect":{{JsonSerializer.Serialize(session.Server.Dialect)}}}
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

            if (handleId is null || !sessions.TryGetValue(handleId, out IDbSession? session))
            {
                _ = PostQueryEventAsync(inbox, effect, queryId,
                    $$"""{"eventType":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.NotFound)}},"message":"No session for handle."}""");
                return;
            }

            var pump = new QueryPump { Session = session, Credits = new SemaphoreSlim(initialCredit) };
            queryPumps[queryId] = pump;
            _ = Task.Run(() => StreamQueryPumpAsync(effect, inbox, queryId, sql, pump));
        }

        private async Task StreamQueryPumpAsync(EffectWorkItem effect, ICoordinatorInbox inbox, string queryId, string sql, QueryPump pump)
        {
            await PostQueryEventAsync(inbox, effect, queryId, """{"eventType":"started"}""").ConfigureAwait(false);

            try
            {
                var request = new QueryExecuteRequest
                {
                    QueryId = queryId,
                    Sql = sql,
                    PageRows = Sts2Defaults.PageRows,
                    PageBytes = Sts2Defaults.PageBytes,
                };
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
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"resultSet","resultSetId":{{resultSet.ResultSetId}},"columns":{{SerializeColumns(resultSet.Columns)}}}""")).ConfigureAwait(false);
                            break;

                        case RowsPage page:
                            await pump.Credits.WaitAsync(pump.Cancellation.Token).ConfigureAwait(false);
                            if (pump.Suppressed)
                            {
                                break;
                            }
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"rows","resultSetId":{{page.ResultSetId}},"pageSeq":{{page.PageSeq}},"rowOffset":{{page.RowOffset}},"rows":{{SerializeRows(page.Cells)}}}""")).ConfigureAwait(false);
                            break;

                        case ServerMessage message:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"message","messageClass":{{JsonSerializer.Serialize(message.MessageClass)}},"number":{{message.Number}},"severity":{{message.Severity}},"text":{{JsonSerializer.Serialize(message.Text)}}}""")).ConfigureAwait(false);
                            break;

                        case ResultSetCompleted done:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"resultSetDone","resultSetId":{{done.ResultSetId}},"rowCount":{{done.RowCount}}}""")).ConfigureAwait(false);
                            break;

                        case ExecCompleted completed:
                            await PostQueryEventAsync(inbox, effect, queryId, string.Create(CultureInfo.InvariantCulture,
                                $$"""{"eventType":"completed","rowsAffected":{{completed.RowsAffected.Sum()}}}""")).ConfigureAwait(false);
                            break;

                        default:
                            break;
                    }
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

        private static string SerializeRows(IReadOnlyList<IReadOnlyList<object?>> rows)
        {
            var array = new System.Text.Json.Nodes.JsonArray();
            foreach (IReadOnlyList<object?> row in rows)
            {
                var cells = new System.Text.Json.Nodes.JsonArray();
                foreach (object? cell in row)
                {
                    cells.Add(WireValueEncoder.Encode(cell)); // SPEC §7.7 wire encoding
                }
                array.Add(cells);
            }
            return array.ToJsonString();
        }

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
