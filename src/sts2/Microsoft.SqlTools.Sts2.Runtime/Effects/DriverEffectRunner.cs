//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class DriverEffectRunner : ISts2EffectRunner
    {
        private readonly IReadOnlyDictionary<string, IDbDriver> drivers;
        private readonly SecretSideTable secrets;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> opensInFlight = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, IDbSession> sessions = new(StringComparer.Ordinal);

        /// <summary>Creates a runner over the registered drivers.</summary>
        public DriverEffectRunner(IReadOnlyDictionary<string, IDbDriver> drivers, SecretSideTable secrets)
        {
            this.drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
            this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        }

        /// <summary>Live driver-session lease count (I8).</summary>
        public int OpenSessionCount => sessions.Count;

        /// <inheritdoc/>
        public void Run(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            ArgumentNullException.ThrowIfNull(effect);
            ArgumentNullException.ThrowIfNull(inbox);
            switch (effect.EffectName)
            {
                case "driver.open":
                    _ = Task.Run(() => OpenAsync(effect, inbox));
                    break;

                case "driver.cancelOpen":
                {
                    string? openId = GetString(effect.Args, "openId");
                    if (openId is not null && opensInFlight.TryGetValue(openId, out CancellationTokenSource? cts))
                    {
                        cts.Cancel();
                    }
                    _ = PostAsync(inbox, effect, """{"status":"ok"}""");
                    break;
                }

                case "driver.close":
                    _ = Task.Run(() => CloseAsync(effect, inbox));
                    break;

                case "toy.delay": // M1 spine scaffolding; removed in M3
                    _ = PostAsync(inbox, effect, effect.Args.GetRawText());
                    break;

                default:
                    _ = PostAsync(inbox, effect, string.Create(CultureInfo.InvariantCulture,
                        $$"""{"status":"error","code":{{JsonSerializer.Serialize(Sts2ErrorCodes.Internal)}},"message":"Unknown effect name."}"""));
                    break;
            }
        }

        private async Task OpenAsync(EffectWorkItem effect, ICoordinatorInbox inbox)
        {
            string connectionId = GetString(effect.Args, "connectionId") ?? "?";
            string openId = GetString(effect.Args, "openId") ?? "?";
            var resolvedTokens = new List<string>();
            var cancelSource = new CancellationTokenSource();
            opensInFlight[openId] = cancelSource;

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
                    await PostOpenResultAsync(inbox, effect, connectionId, openId,
                        $$"""{"status":"ok","handleId":{{JsonSerializer.Serialize(handleId)}},"serverInfo":{{serverInfo}}}""").ConfigureAwait(false);
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
                        && token.StartsWith("secret:sha256:", StringComparison.Ordinal)
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
