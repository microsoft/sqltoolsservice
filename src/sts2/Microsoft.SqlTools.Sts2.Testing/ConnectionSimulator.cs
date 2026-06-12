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
            int operationCount = random.Next(15, 40);

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
                int corrCounter = 0;
                var awaitedCorrs = new List<string>();

                for (int op = 0; op < operationCount; op++)
                {
                    string corr = "r-" + (++corrCounter).ToString(CultureInfo.InvariantCulture);
                    awaitedCorrs.Add(corr);
                    int kind = random.Next(100);

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

                // Drain: every request must terminate (I1).
                foreach (string corr in awaitedCorrs)
                {
                    await AwaitTerminalAsync(pending, corr);
                }

                // Close everything still open so I8 applies.
                foreach (string connectionId in coordinator.CurrentState.Connections.Keys.ToArray())
                {
                    string corr = "r-close-" + connectionId;
                    awaitedCorrs.Add(corr);
                    await coordinator.PostRpcRequestAsync("v2/connection.close", corr,
                        JsonDocument.Parse("{\"connectionId\":\"" + connectionId + "\"}").RootElement);
                    await AwaitTerminalAsync(pending, corr);
                }
            }
            finally
            {
                await coordinator.DisposeAsync();
                leakedSessions = await effectRunner.DisposeLeakedSessionsAsync();
            }

            IReadOnlyList<string> violations = InvariantChecker.Check(
                ["I1", "I5", "I6", "I7", "I8", "I12"],
                journalDirectory,
                terminalsByCorr.ToDictionary(kv => kv.Key, kv => kv.Value),
                leakedSessions);

            return new SimulatorResult { Seed = seed, Violations = violations, Operations = operationCount };
        }

        private static async Task AwaitTerminalAsync(ConcurrentDictionary<string, TaskCompletionSource> pending, string corr)
        {
            TaskCompletionSource tcs = pending.GetOrAdd(corr, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }

        private static JsonElement Redact(string payloadJson, SecretSideTable secrets)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
            return JsonDocument.Parse(SecretRedactor.Redact(node, secrets)!.ToJsonString()).RootElement;
        }
    }
}
