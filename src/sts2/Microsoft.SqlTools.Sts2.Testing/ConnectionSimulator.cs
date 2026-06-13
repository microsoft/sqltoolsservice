//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>Outcome of one simulator seed.</summary>
    public sealed record SimulatorResult
    {
        /// <summary>The seed; failures print the repro line with it.</summary>
        public required int Seed { get; init; }

        /// <summary>Invariant violations; empty means the seed is green.</summary>
        public required IReadOnlyList<string> Violations { get; init; }

        /// <summary>Operations executed.</summary>
        public required int Operations { get; init; }
    }

    /// <summary>
    /// Deterministic random connection-op simulator (SPEC §14.4, M2 scope: opens with
    /// random outcomes, cancels, closes, initialize, ping). All randomness derives from
    /// the seed; a failure reproduces with the same seed. Flaky failures here are P0
    /// determinism bugs, never tests to retry.
    /// </summary>
    public static class ConnectionSimulator
    {
        /// <summary>Runs one seed; the journal is written under <paramref name="journalDirectory"/>.</summary>
        public static async Task<SimulatorResult> RunSeedAsync(int seed, string journalDirectory)
        {
            var random = new Random(seed);
            int operationCount = random.Next(12, 28);

            var fakeDriver = new FakeDriver();
            var secrets = new SecretSideTable();
            var effectRunner = new DriverEffectRunner(
                new Dictionary<string, IDbDriver> { ["fake"] = fakeDriver }, secrets);
            var terminalsByCorr = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            var pending = new ConcurrentDictionary<string, TaskCompletionSource>(StringComparer.Ordinal);

            string runId = "sim-" + seed.ToString(CultureInfo.InvariantCulture);
            var coordinator = new Coordinator(
                new JournalWriter(runId, new JournalOptions { Directory = journalDirectory }, new JournalRunInfo { ServiceVersion = "sim" }),
                new CoordinatorOptions { RunId = runId },
                effectRunner,
                message =>
                {
                    if (message.Corr is null)
                    {
                        return;
                    }
                    terminalsByCorr.AddOrUpdate(message.Corr, 1, (_, n) => n + 1);
                    pending.GetOrAdd(message.Corr, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
                });

            int leakedSessions;
            try
            {
                await coordinator.PostControlAsync("session.start", JsonDocument.Parse(
                    """{"serviceVersion":"sim","drivers":[{"name":"fake","dialects":["neutral"],"production":false}],"limits":{"maxConnections":8}}""").RootElement);

                var knownOpenIds = new List<string>();
                var knownConnectionIds = new List<string>();
                var knownQueryIds = new List<string>();
                int corrCounter = 0;
                var awaitedCorrs = new List<string>();

                for (int op = 0; op < operationCount; op++)
                {
                    string corr = "r-" + (++corrCounter).ToString(CultureInfo.InvariantCulture);
                    awaitedCorrs.Add(corr);
                    int kind = random.Next(140);

                    if (kind >= 100) // query ops (M3)
                    {
                        if (kind < 118 && knownConnectionIds.Count > 0) // execute with a random script
                        {
                            var steps = new List<FakeQueryStep> { new() { Type = "resultSet", ResultSetId = 0, Columns = 1 + random.Next(3) } };
                            int pages = random.Next(0, 6); // sometimes > window to exercise backpressure (I9)
                            for (int p = 0; p < pages; p++)
                            {
                                steps.Add(new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1 + random.Next(5) });
                            }
                            int finale = random.Next(10);
                            steps.Add(finale switch
                            {
                                0 => new FakeQueryStep { Type = "error", ErrorCode = "Sts2.QueryFailed.Server", Text = "sim error", Number = 50000, Severity = 16 },
                                1 => new FakeQueryStep { Type = "sever" },
                                2 => new FakeQueryStep { Type = "crash" },
                                _ => new FakeQueryStep { Type = "completed", RowsAffected = pages },
                            });
                            fakeDriver.EnqueueQuery(new FakeQueryScript { Steps = steps });
                            string targetConnection = knownConnectionIds[random.Next(knownConnectionIds.Count)];
                            await coordinator.PostRpcRequestAsync("v2/query.execute", corr,
                                JsonDocument.Parse("{\"connectionId\":\"" + targetConnection + "\",\"sql\":\"select sim\"}").RootElement);
                        }
                        else if (kind < 128) // ack a random known or unknown query
                        {
                            awaitedCorrs.Remove(corr); // notifications have no terminal
                            string queryId = knownQueryIds.Count > 0 && random.Next(3) != 0
                                ? knownQueryIds[random.Next(knownQueryIds.Count)]
                                : "q-unknown";
                            string ack = random.Next(2) == 0
                                ? "{\"queryId\":\"" + queryId + "\",\"pageSeq\":" + random.Next(6).ToString(CultureInfo.InvariantCulture) + "}"
                                : "{\"queryId\":\"" + queryId + "\",\"throughPageSeq\":" + random.Next(8).ToString(CultureInfo.InvariantCulture) + "}";
                            await coordinator.PostRpcNotificationAsync("v2/query.ack", JsonDocument.Parse(ack).RootElement);
                        }
                        else if (kind < 134) // cancel a random known or unknown query
                        {
                            string queryId = knownQueryIds.Count > 0 && random.Next(2) == 0
                                ? knownQueryIds[random.Next(knownQueryIds.Count)]
                                : "q-unknown";
                            await coordinator.PostRpcRequestAsync("v2/query.cancel", corr,
                                JsonDocument.Parse("{\"queryId\":\"" + queryId + "\"}").RootElement);
                        }
                        else // dispose a random known or unknown query
                        {
                            string queryId = knownQueryIds.Count > 0 && random.Next(2) == 0
                                ? knownQueryIds[random.Next(knownQueryIds.Count)]
                                : "q-unknown";
                            await coordinator.PostRpcRequestAsync("v2/query.dispose", corr,
                                JsonDocument.Parse("{\"queryId\":\"" + queryId + "\"}").RootElement);
                        }

                        if (random.Next(3) != 0 && awaitedCorrs.Contains(corr))
                        {
                            await AwaitTerminalAsync(pending, corr);
                        }
                        knownConnectionIds.Clear();
                        knownConnectionIds.AddRange(coordinator.CurrentState.Connections.Keys);
                        knownQueryIds.Clear();
                        knownQueryIds.AddRange(coordinator.CurrentState.Queries.Keys);
                        continue;
                    }

                    if (kind < 45) // open with a random outcome
                    {
                        string outcome = random.Next(5) switch
                        {
                            0 => "authFail",
                            1 => "networkFail",
                            2 => "timeout",
                            _ => "ok",
                        };
                        fakeDriver.EnqueueOpen(new FakeOpenBehavior { Outcome = outcome, DelayMs = random.Next(0, 3) });
                        string openId = "open-" + op.ToString(CultureInfo.InvariantCulture);
                        knownOpenIds.Add(openId);
                        string payload = """
                            {"openId":"OPENID","profile":{"server":"fake://sim","driver":"fake","auth":{"kind":"sqlLogin","user":"sim","password":"CANARY"},"options":{"connectTimeoutMs":5000}}}
                            """.Replace("OPENID", openId).Replace("CANARY", SecretCanaries.Password);
                        JsonElement redacted = Redact(payload, secrets);
                        await coordinator.PostRpcRequestAsync("v2/connection.open", corr, redacted);
                    }
                    else if (kind < 60) // cancel a random known or unknown openId
                    {
                        string openId = knownOpenIds.Count > 0 && random.Next(2) == 0
                            ? knownOpenIds[random.Next(knownOpenIds.Count)]
                            : "open-unknown-" + op.ToString(CultureInfo.InvariantCulture);
                        await coordinator.PostRpcRequestAsync("v2/connection.cancel", corr,
                            JsonDocument.Parse("{\"openId\":\"" + openId + "\"}").RootElement);
                    }
                    else if (kind < 80) // close a random known or unknown connection
                    {
                        string connectionId = knownConnectionIds.Count > 0 && random.Next(2) == 0
                            ? knownConnectionIds[random.Next(knownConnectionIds.Count)]
                            : "c-unknown";
                        await coordinator.PostRpcRequestAsync("v2/connection.close", corr,
                            JsonDocument.Parse("{\"connectionId\":\"" + connectionId + "\"}").RootElement);
                    }
                    else if (kind < 90)
                    {
                        await coordinator.PostRpcRequestAsync("v2/initialize", corr,
                            JsonDocument.Parse("{\"clientName\":\"sim\"}").RootElement);
                    }
                    else
                    {
                        await coordinator.PostRpcRequestAsync("v2/diagnostics.ping", corr,
                            JsonDocument.Parse("{\"echo\":\"sim\"}").RootElement);
                    }

                    // Occasionally race ahead without waiting; otherwise await the terminal
                    // so the op stream stays mostly causal (like a real client).
                    if (random.Next(4) != 0)
                    {
                        await AwaitTerminalAsync(pending, corr);
                    }

                    // Track connection ids from any open results observed so far.
                    knownConnectionIds.Clear();
                    knownConnectionIds.AddRange(coordinator.CurrentState.Connections.Keys);
                }

                // Close everything still open so I8 applies. An Opening connection can
                // still resolve to Open after a snapshot (a cancel that lost the open
                // race), so re-loop until no connections remain — never silently leak.
                var alreadyClosed = new HashSet<string>(StringComparer.Ordinal);
                int closeDeadlineMs = 10_000;
                int closeElapsedMs = 0;
                int closeCounter = 0;
                while (closeElapsedMs < closeDeadlineMs)
                {
                    var connections = coordinator.CurrentState.Connections;
                    if (connections.Count == 0)
                    {
                        break;
                    }
                    bool postedAny = false;
                    foreach (string connectionId in connections.Keys)
                    {
                        // Close each connection once; a connection already Closing is on
                        // its way out. Re-loop only picks up connections that newly appear
                        // (an Opening connection resolving to Open after the cancel race).
                        if (alreadyClosed.Add(connectionId))
                        {
                            string corr = "r-drainclose-" + (++closeCounter).ToString(CultureInfo.InvariantCulture);
                            awaitedCorrs.Add(corr);
                            await coordinator.PostRpcRequestAsync("v2/connection.close", corr,
                                JsonDocument.Parse("{\"connectionId\":\"" + connectionId + "\"}").RootElement);
                            postedAny = true;
                        }
                    }
                    if (!postedAny)
                    {
                        await Task.Delay(15); // waiting for in-flight closes to drain the map
                        closeElapsedMs += 15;
                    }
                }

                // Drain to quiescence: poll until every request has a terminal. A
                // genuinely missing terminal is left to the I1 invariant check (precise
                // diagnostics) rather than a blunt per-corr timeout.
                await WaitForQuiescenceAsync(terminalsByCorr, awaitedCorrs, TimeSpan.FromSeconds(10));
            }
            finally
            {
                await coordinator.DisposeAsync();
                leakedSessions = await effectRunner.DisposeLeakedSessionsAsync();
            }

            IReadOnlyList<string> violations = InvariantChecker.Check(
                ["I1", "I2", "I3", "I5", "I6", "I7", "I8", "I9", "I12"],
                journalDirectory,
                terminalsByCorr.ToDictionary(kv => kv.Key, kv => kv.Value),
                leakedSessions);

            return new SimulatorResult { Seed = seed, Violations = violations, Operations = operationCount };
        }

        private static async Task AwaitTerminalAsync(ConcurrentDictionary<string, TaskCompletionSource> pending, string corr)
        {
            TaskCompletionSource tcs = pending.GetOrAdd(corr, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                // The terminal may still arrive; the I1 invariant check is the source of
                // truth for terminality. Do not fail the seed on a scheduling delay.
            }
        }

        private static async Task WaitForQuiescenceAsync(
            ConcurrentDictionary<string, int> terminalsByCorr,
            IReadOnlyCollection<string> awaitedCorrs,
            TimeSpan budget)
        {
            var deadline = budget;
            int elapsedMs = 0;
            while (elapsedMs < deadline.TotalMilliseconds)
            {
                bool allTerminal = true;
                foreach (string corr in awaitedCorrs)
                {
                    if (!terminalsByCorr.ContainsKey(corr))
                    {
                        allTerminal = false;
                        break;
                    }
                }
                if (allTerminal)
                {
                    return;
                }
                await Task.Delay(15);
                elapsedMs += 15;
            }
            // Budget exhausted: leave it to the I1 invariant check to report the gap.
        }

        private static JsonElement Redact(string payloadJson, SecretSideTable secrets)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
            return JsonDocument.Parse(SecretRedactor.Redact(node, secrets)!.ToJsonString()).RootElement;
        }
    }
}
