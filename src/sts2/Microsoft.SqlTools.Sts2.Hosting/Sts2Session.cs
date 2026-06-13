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
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using StreamJsonRpc;

namespace Microsoft.SqlTools.Sts2.Hosting
{
    /// <summary>Configuration for one STS2 session.</summary>
    public sealed record Sts2SessionOptions
    {
        /// <summary>Stream the session reads inbound messages from (multiplexer STS2 input).</summary>
        public required Stream Input { get; init; }

        /// <summary>Stream the session writes outbound messages to (multiplexer STS2 output).</summary>
        public required Stream Output { get; init; }

        /// <summary>Run id stamped on every envelope and journal file.</summary>
        public required string RunId { get; init; }

        /// <summary>Directory for journal segments and manifest (<c>&lt;log-dir&gt;/sts2</c>).</summary>
        public required string JournalDirectory { get; init; }

        /// <summary>Service version reported by initialize/ping.</summary>
        public required string ServiceVersion { get; init; }

        /// <summary>Registered drivers; empty until the real adapters land (M4/M5).</summary>
        public IReadOnlyDictionary<string, IDbDriver> Drivers { get; init; } = new Dictionary<string, IDbDriver>();

        /// <summary>Directory export bundles are written to; defaults to the journal directory.</summary>
        public string? ExportDirectory { get; init; }

        /// <summary>Command line with secrets removed, recorded in the journal manifest.</summary>
        public IReadOnlyList<string> CommandLine { get; init; } = [];
    }

    /// <summary>
    /// One composed STS2 session (SPEC §3): the StreamJsonRpc gateway plus the
    /// coordinator, journal, secret side table, and driver effect runner behind it.
    /// Every v2 request is redacted, journaled, decided by Core, and answered from the
    /// journaled output — the gateway holds no domain logic.
    /// </summary>
    public sealed class Sts2Session : IAsyncDisposable
    {
        private readonly JsonRpc rpc;
        private readonly Coordinator coordinator;
        private readonly SecretSideTable secrets;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<OutboundRpcMessage>> pendingRequests = new(StringComparer.Ordinal);
        private int corrCounter;

        private Sts2Session(JsonRpc rpc, Coordinator coordinator, SecretSideTable secrets)
        {
            this.rpc = rpc;
            this.coordinator = coordinator;
            this.secrets = secrets;
        }

        /// <summary>Faults when the RPC connection or host dies; used for crash containment.</summary>
        public Task Completion => rpc.Completion;

        /// <summary>The coordinator, exposed for lifecycle signals and diagnostics.</summary>
        public Coordinator Coordinator => coordinator;

        /// <summary>Builds and starts a session over the given streams.</summary>
        public static Sts2Session Start(Sts2SessionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var secrets = new SecretSideTable();
            var exportTemplate = new Runtime.Export.ExportBundleRequest
            {
                RunId = options.RunId,
                JournalDirectory = options.JournalDirectory,
                OutputDirectory = options.ExportDirectory ?? options.JournalDirectory,
            };
            var effectRunner = new DriverEffectRunner(options.Drivers, secrets, exportTemplate);
            var journal = new JournalWriter(options.RunId,
                new JournalOptions { Directory = options.JournalDirectory },
                new JournalRunInfo { ServiceVersion = options.ServiceVersion, CommandLine = options.CommandLine });

            Sts2Session? session = null;
            var coordinator = new Coordinator(
                journal,
                new CoordinatorOptions { RunId = options.RunId },
                effectRunner,
                message => session?.HandleOutbound(message));

            var formatter = new SystemTextJsonFormatter();
            formatter.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var handler = new HeaderDelimitedMessageHandler(options.Output, options.Input, formatter);
            var rpc = new JsonRpc(handler);
            session = new Sts2Session(rpc, coordinator, secrets);
            rpc.AddLocalRpcTarget(new GatewayTarget(session), null);
            rpc.StartListening();

            // Session config is a journaled root envelope so replay starts identically.
            var drivers = new JsonArray(options.Drivers.Values
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .Select(d => (JsonNode)new JsonObject
                {
                    ["name"] = d.Name,
                    ["dialects"] = new JsonArray(d.Capabilities.Dialects.Select(x => (JsonNode)JsonValue.Create(x)).ToArray()),
                    ["production"] = d.Capabilities.Production,
                }).ToArray());
            var sessionStart = new JsonObject
            {
                ["serviceVersion"] = options.ServiceVersion,
                ["drivers"] = drivers,
            };
            coordinator.PostControlAsync("session.start", ToElement(sessionStart)).AsTask().GetAwaiter().GetResult();
            return session;
        }

        /// <summary>Posts a lifecycle signal and flushes the journal (mux flush wait, SPEC §6.2).</summary>
        public async Task SignalLifecycleAsync(string signal)
        {
            await coordinator.PostControlAsync(signal).ConfigureAwait(false);
            await coordinator.FlushJournalAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            rpc.Dispose();
            await coordinator.DisposeAsync().ConfigureAwait(false);
        }

        private void HandleOutbound(OutboundRpcMessage message)
        {
            if (message.Corr is not null && pendingRequests.TryRemove(message.Corr, out TaskCompletionSource<OutboundRpcMessage>? tcs))
            {
                tcs.TrySetResult(message);
                return;
            }
            if (message.Kind == "rpc.out.notify")
            {
                _ = rpc.NotifyWithParameterObjectAsync(message.Type, message.Body);
            }
        }

        private async Task<JsonElement?> InvokeAsync(string method, JsonElement? parameters)
        {
            string corr = "r-" + Interlocked.Increment(ref corrCounter).ToString(CultureInfo.InvariantCulture);
            var tcs = new TaskCompletionSource<OutboundRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            pendingRequests[corr] = tcs;

            // Secrets are tokenized BEFORE the envelope exists (SPEC §8.5).
            JsonElement? redacted = parameters is { } p
                ? ToElement(SecretRedactor.Redact(JsonNode.Parse(p.GetRawText()), secrets))
                : null;
            await coordinator.PostRpcRequestAsync(method, corr, redacted).ConfigureAwait(false);

            OutboundRpcMessage outcome = await tcs.Task.ConfigureAwait(false);
            if (outcome.Kind == "rpc.out.error")
            {
                JsonElement body = outcome.Body!.Value;
                throw new LocalRpcException(body.GetProperty("message").GetString())
                {
                    ErrorCode = body.GetProperty("code").GetInt32(),
                    ErrorData = body.GetProperty("data"),
                };
            }
            return outcome.Body;
        }

        private static JsonElement? ToElement(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }
            return JsonDocument.Parse(node.ToJsonString()).RootElement;
        }

        /// <summary>The thin RPC surface; every method funnels into the coordinator.</summary>
        private sealed class GatewayTarget
        {
            private readonly Sts2Session session;

            internal GatewayTarget(Sts2Session session)
            {
                this.session = session;
            }

            [JsonRpcMethod("v2/initialize", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> InitializeAsync(JsonElement? args = null) => session.InvokeAsync("v2/initialize", args);

            [JsonRpcMethod("v2/connection.open", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> ConnectionOpenAsync(JsonElement? args = null) => session.InvokeAsync("v2/connection.open", args);

            [JsonRpcMethod("v2/connection.cancel", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> ConnectionCancelAsync(JsonElement? args = null) => session.InvokeAsync("v2/connection.cancel", args);

            [JsonRpcMethod("v2/connection.close", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> ConnectionCloseAsync(JsonElement? args = null) => session.InvokeAsync("v2/connection.close", args);

            [JsonRpcMethod("v2/query.execute", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> QueryExecuteAsync(JsonElement? args = null) => session.InvokeAsync("v2/query.execute", args);

            [JsonRpcMethod("v2/query.cancel", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> QueryCancelAsync(JsonElement? args = null) => session.InvokeAsync("v2/query.cancel", args);

            [JsonRpcMethod("v2/query.dispose", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> QueryDisposeAsync(JsonElement? args = null) => session.InvokeAsync("v2/query.dispose", args);

            [JsonRpcMethod("v2/query.ack", UseSingleObjectParameterDeserialization = true)]
            public Task QueryAckAsync(JsonElement? args = null) =>
                session.coordinator.PostRpcNotificationAsync("v2/query.ack", args).AsTask();

            [JsonRpcMethod("v2/diagnostics.ping", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> PingAsync(JsonElement? args = null) => session.InvokeAsync("v2/diagnostics.ping", args);

            [JsonRpcMethod("v2/diagnostics.health", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> HealthAsync(JsonElement? args = null) => session.InvokeAsync("v2/diagnostics.health", args);

            [JsonRpcMethod("v2/diagnostics.state", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> StateAsync(JsonElement? args = null) => session.InvokeAsync("v2/diagnostics.state", args);

            [JsonRpcMethod("v2/diagnostics.exportLog", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> ExportLogAsync(JsonElement? args = null) => session.InvokeAsync("v2/diagnostics.exportLog", args);
        }
    }
}
