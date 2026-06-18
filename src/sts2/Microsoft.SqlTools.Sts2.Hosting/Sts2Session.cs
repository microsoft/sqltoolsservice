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
using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Observability;
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

        /// <summary>
        /// Extra envelope observers to attach to this session (SPEC §12). They see every
        /// journaled envelope in seq order, best-effort. The session always registers a
        /// metrics sink and a live-tail broadcast sink ahead of these.
        /// </summary>
        public IReadOnlyList<IEnvelopeSink> EnvelopeSinks { get; init; } = [];
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
        private readonly DriverEffectRunner effectRunner;
        private readonly SecretSideTable secrets;
        private readonly BroadcastEnvelopeSink liveTail;
        private readonly IReadOnlyList<MailboxEnvelopeSink> mailboxSinks;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<OutboundRpcMessage>> pendingRequests = new(StringComparer.Ordinal);
        private readonly TaskCompletionSource sessionEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int corrCounter;
        private string? fatalReason;
        private int shuttingDown;

        private Sts2Session(JsonRpc rpc, Coordinator coordinator, DriverEffectRunner effectRunner,
            SecretSideTable secrets, BroadcastEnvelopeSink liveTail, IReadOnlyList<MailboxEnvelopeSink> mailboxSinks)
        {
            this.rpc = rpc;
            this.coordinator = coordinator;
            this.effectRunner = effectRunner;
            this.secrets = secrets;
            this.liveTail = liveTail;
            this.mailboxSinks = mailboxSinks;

            // Composite session lifetime (R001): a fault in ANY core component — the RPC
            // connection OR the coordinator pump (journal/core/sink failure) — transitions
            // STS2 to fatal, fails pending requests, and faults Completion so the multiplexer
            // marks the channel dead. The old code observed only rpc.Completion, so a pump
            // fault could silently strand every request.
            ObserveComponent(rpc.Completion, "rpc");
            ObserveComponent(coordinator.Completion, "coordinator");
        }

        /// <summary>Faults when any core component dies unexpectedly; used for crash containment (R001).</summary>
        public Task Completion => sessionEnded.Task;

        /// <summary>Non-null once the session has entered fatal containment; the redacted reason.</summary>
        public string? FatalReason => fatalReason;

        /// <summary>The coordinator, exposed for lifecycle signals and diagnostics.</summary>
        public Coordinator Coordinator => coordinator;

        /// <summary>
        /// The in-process live tail of the envelope stream (SPEC §12). Subscribe to feed a
        /// diagnostic viewer or attached tool with every journaled envelope in seq order.
        /// </summary>
        public BroadcastEnvelopeSink LiveTail => liveTail;

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

            // The coordinator owns the metrics sink; here we add a live tail for the
            // diagnostic viewer plus any caller-supplied observers (SPEC §12). Third-party
            // sinks are wrapped in a non-blocking mailbox (R003) so a slow/hanging/throwing
            // observer can never stall the pump or break write-ahead; the built-in live tail
            // is already non-blocking and runs inline.
            var liveTail = new BroadcastEnvelopeSink();
            var mailboxSinks = options.EnvelopeSinks.Select(s => new MailboxEnvelopeSink(s)).ToArray();
            var auxSinks = new List<IEnvelopeSink>(1 + mailboxSinks.Length) { liveTail };
            auxSinks.AddRange(mailboxSinks);

            Sts2Session? session = null;
            var coordinator = new Coordinator(
                journal,
                new CoordinatorOptions { RunId = options.RunId, MetricSampleEvery = 1000 },
                effectRunner,
                message => session?.HandleOutbound(message),
                auxSinks);

            var formatter = new SystemTextJsonFormatter();
            formatter.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var handler = new HeaderDelimitedMessageHandler(options.Output, options.Input, formatter);
            var rpc = new JsonRpc(handler);
            session = new Sts2Session(rpc, coordinator, effectRunner, secrets, liveTail, mailboxSinks);
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
                // Privacy-preserving capture by default (SPEC §8.4): row cells and SQL are
                // elided to digests before journaling. Changeable at runtime via setCapture.
                ["capture"] = new JsonObject { ["row"] = "digest", ["sql"] = "digest" },
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
            Interlocked.Exchange(ref shuttingDown, 1); // a clean dispose is not a fatal fault
            rpc.Dispose();
            // Stop intake and drain the pump first, then dispose the resources it owns: the
            // effect runner (sessions, pumps, CTS, semaphores — R013) and the observer
            // mailboxes (their worker tasks). Order matters: the runner must outlive the pump.
            await coordinator.DisposeAsync().ConfigureAwait(false);
            await effectRunner.DisposeAsync().ConfigureAwait(false);
            foreach (MailboxEnvelopeSink mailbox in mailboxSinks)
            {
                await mailbox.DisposeAsync().ConfigureAwait(false);
            }
            sessionEnded.TrySetResult(); // clean shutdown completes Completion without faulting
        }

        private void ObserveComponent(Task completion, string name)
        {
            _ = completion.ContinueWith(
                t => EnterFatal(name + " faulted: " + (t.Exception?.GetBaseException().Message ?? "unknown")),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Enters fatal containment exactly once: records the reason, fails every pending
        /// request with <c>Sts2.Unavailable</c>, and faults <see cref="Completion"/> so the
        /// multiplexer marks the channel dead while legacy traffic continues.
        /// </summary>
        private void EnterFatal(string reason)
        {
            if (Interlocked.CompareExchange(ref fatalReason, reason, null) is not null || Volatile.Read(ref shuttingDown) != 0)
            {
                return;
            }
            foreach (string corr in pendingRequests.Keys.ToArray())
            {
                if (pendingRequests.TryRemove(corr, out TaskCompletionSource<OutboundRpcMessage>? tcs))
                {
                    tcs.TrySetResult(UnavailableMessage(corr, reason));
                }
            }
            sessionEnded.TrySetException(new InvalidOperationException("STS2 fatal: " + reason));
        }

        private static OutboundRpcMessage UnavailableMessage(string corr, string reason) => new()
        {
            Kind = "rpc.out.error",
            Corr = corr,
            Type = "v2/unavailable",
            Body = ToElement(new JsonObject
            {
                ["code"] = Sts2JsonRpcCodes.For(Sts2ErrorCodes.Unavailable),
                ["message"] = "STS2 is unavailable: " + reason,
                ["data"] = new JsonObject
                {
                    ["code"] = Sts2ErrorCodes.Unavailable,
                    ["retryable"] = true,
                    ["corr"] = corr,
                },
            }),
        };

        private static LocalRpcException UnavailableException(string reason) =>
            new("STS2 is unavailable: " + reason)
            {
                ErrorCode = Sts2JsonRpcCodes.For(Sts2ErrorCodes.Unavailable),
                ErrorData = ToElement(new JsonObject
                {
                    ["code"] = Sts2ErrorCodes.Unavailable,
                    ["retryable"] = true,
                })!.Value,
            };

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
            // Fatal containment (R001): once STS2 is dead, answer new requests immediately with
            // Sts2.Unavailable rather than posting into a stopped pump (which would hang).
            if (Volatile.Read(ref fatalReason) is string reason)
            {
                throw UnavailableException(reason);
            }

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

            [JsonRpcMethod("v2/diagnostics.setCapture", UseSingleObjectParameterDeserialization = true)]
            public Task<JsonElement?> SetCaptureAsync(JsonElement? args = null) => session.InvokeAsync("v2/diagnostics.setCapture", args);
        }
    }
}
