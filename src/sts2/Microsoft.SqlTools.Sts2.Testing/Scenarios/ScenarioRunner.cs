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
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;

namespace Microsoft.SqlTools.Sts2.Testing.Scenarios
{
    /// <summary>Outcome of one scenario run.</summary>
    public sealed record ScenarioRunResult
    {
        /// <summary>The scenario that ran.</summary>
        public required string Name { get; init; }

        /// <summary>Empty when green; assertion and invariant failures otherwise.</summary>
        public required IReadOnlyList<string> Failures { get; init; }

        /// <summary>Where the journal was written (kept for the verify replay gate).</summary>
        public required string JournalDirectory { get; init; }
    }

    /// <summary>
    /// Executes scenario YAML against the coordinator + Core + FakeDriver (SPEC §14.2):
    /// no multiplexer, no RPC transport. Each run journals to its own directory and is
    /// immediately invariant-checked and replayed.
    /// </summary>
    public sealed class ScenarioRunner
    {
        private static readonly string FakeBasicProfile = """
            {"server":"fake://local","database":"main","driver":"fake","auth":{"kind":"sqlLogin","user":"sa","password":"PASSWORD"},"options":{"connectTimeoutMs":5000}}
            """.Replace("PASSWORD", SecretCanaries.Password);

        /// <summary>Runs one parsed scenario; the journal lands under <paramref name="journalRoot"/>/&lt;name&gt;.</summary>
        public static async Task<ScenarioRunResult> RunAsync(ScenarioDefinition scenario, string journalRoot)
        {
            ArgumentNullException.ThrowIfNull(scenario);
            string journalDirectory = System.IO.Path.Combine(journalRoot, scenario.Info.Name);
            if (System.IO.Directory.Exists(journalDirectory))
            {
                System.IO.Directory.Delete(journalDirectory, recursive: true);
            }

            var failures = new List<string>();
            var fakeDriver = new FakeDriver();
            foreach (FakeOpenBehavior behavior in scenario.OpenBehaviors)
            {
                fakeDriver.EnqueueOpen(behavior);
            }

            var secrets = new SecretSideTable();
            var effectRunner = new DriverEffectRunner(
                new Dictionary<string, IDbDriver> { ["fake"] = fakeDriver }, secrets);
            var emitted = new ConcurrentDictionary<string, TaskCompletionSource<OutboundRpcMessage>>(StringComparer.Ordinal);
            var terminalsByCorr = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

            var coordinator = new Coordinator(
                new JournalWriter(scenario.Info.Name,
                    new JournalOptions { Directory = journalDirectory },
                    new JournalRunInfo { ServiceVersion = "scenario" }),
                new CoordinatorOptions { RunId = scenario.Info.Name },
                effectRunner,
                message =>
                {
                    if (message.Corr is null)
                    {
                        return; // notifications: matched via expect.outbound from M3
                    }
                    terminalsByCorr.AddOrUpdate(message.Corr, 1, (_, n) => n + 1);
                    emitted.GetOrAdd(message.Corr, _ => new TaskCompletionSource<OutboundRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously))
                        .TrySetResult(message);
                });

            int leakedSessions;
            try
            {
                var sessionStart = JsonNode.Parse("""
                    {"serviceVersion":"scenario","drivers":[{"name":"fake","dialects":["neutral","tsql"],"production":false}]}
                    """)!.AsObject();
                if (scenario.ConfigLimits is not null)
                {
                    sessionStart["limits"] = scenario.ConfigLimits.DeepClone();
                }
                await coordinator.PostControlAsync("session.start", JsonDocument.Parse(sessionStart.ToJsonString()).RootElement);

                var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
                var corrByLabel = new Dictionary<string, string>(StringComparer.Ordinal);
                int corrCounter = 0;

                foreach (ScenarioStep step in scenario.Steps)
                {
                    switch (step.Kind)
                    {
                        case "request":
                        {
                            string corr = "r-" + (++corrCounter).ToString(CultureInfo.InvariantCulture);
                            if (step.Label is not null)
                            {
                                corrByLabel[step.Label] = corr;
                            }
                            JsonElement? payload = null;
                            if (step.Params is not null)
                            {
                                JsonNode substituted = Substitute(step.Params.DeepClone(), bindings)!;
                                JsonNode redacted = SecretRedactor.Redact(substituted, secrets)!;
                                payload = JsonDocument.Parse(redacted.ToJsonString()).RootElement;
                            }
                            await coordinator.PostRpcRequestAsync(step.Method!, corr, payload);
                            if (step.Await)
                            {
                                OutboundRpcMessage terminal = await AwaitTerminalAsync(emitted, corr);
                                AssertStep(scenario.Info.Name, step, terminal, bindings, failures);
                            }
                            break;
                        }

                        case "awaitTerminal":
                        {
                            if (!corrByLabel.TryGetValue(step.Label!, out string? corr))
                            {
                                failures.Add($"awaitTerminal: unknown label '{step.Label}'");
                                break;
                            }
                            OutboundRpcMessage terminal = await AwaitTerminalAsync(emitted, corr);
                            AssertStep(scenario.Info.Name, step, terminal, bindings, failures);
                            break;
                        }

                        default:
                            failures.Add("unknown step kind: " + step.Kind);
                            break;
                    }
                }
            }
            finally
            {
                await coordinator.DisposeAsync();
                leakedSessions = await effectRunner.DisposeLeakedSessionsAsync();
            }

            failures.AddRange(InvariantChecker.Check(
                scenario.Invariants, journalDirectory,
                terminalsByCorr.ToDictionary(kv => kv.Key, kv => kv.Value), leakedSessions));

            return new ScenarioRunResult
            {
                Name = scenario.Info.Name,
                Failures = failures,
                JournalDirectory = journalDirectory,
            };
        }

        private static async Task<OutboundRpcMessage> AwaitTerminalAsync(
            ConcurrentDictionary<string, TaskCompletionSource<OutboundRpcMessage>> emitted, string corr)
        {
            TaskCompletionSource<OutboundRpcMessage> tcs = emitted.GetOrAdd(corr,
                _ => new TaskCompletionSource<OutboundRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously));
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }

        private static void AssertStep(string scenarioName, ScenarioStep step, OutboundRpcMessage terminal, Dictionary<string, string> bindings, List<string> failures)
        {
            if (step.ExpectError is { } expectedError)
            {
                if (terminal.Kind != "rpc.out.error")
                {
                    failures.Add($"{scenarioName}: expected error {expectedError.DataCode} but got {terminal.Kind}");
                    return;
                }
                JsonElement body = terminal.Body!.Value;
                string? actualCode = body.GetProperty("data").GetProperty("code").GetString();
                if (actualCode != expectedError.DataCode)
                {
                    failures.Add($"{scenarioName}: expected data.code {expectedError.DataCode}, got {actualCode}");
                }
                if (expectedError.JsonRpcCode is int jsonRpcCode && body.GetProperty("code").GetInt32() != jsonRpcCode)
                {
                    failures.Add($"{scenarioName}: expected JSON-RPC code {jsonRpcCode}, got {body.GetProperty("code").GetInt32()}");
                }
                return;
            }

            if (terminal.Kind != "rpc.out.result")
            {
                failures.Add($"{scenarioName}: expected a result but got {terminal.Kind}: {terminal.Body?.GetRawText()}");
                return;
            }
            if (step.ExpectResult is { } expectedResult
                && !PartialMatch(expectedResult, terminal.Body!.Value, out string? mismatch))
            {
                failures.Add($"{scenarioName}: result mismatch — {mismatch}");
            }
            foreach ((string property, string variable) in step.Bind)
            {
                if (terminal.Body!.Value.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                {
                    bindings[variable] = value.GetString()!;
                }
                else
                {
                    failures.Add($"{scenarioName}: bind failed — result has no string property '{property}'");
                }
            }
        }

        /// <summary>Replaces <c>$profiles.fakeBasic</c> and bound <c>$var</c> string values.</summary>
        private static JsonNode? Substitute(JsonNode? node, Dictionary<string, string> bindings)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (string key in obj.Select(kv => kv.Key).ToArray())
                    {
                        obj[key] = Substitute(obj[key], bindings)?.DeepClone();
                    }
                    return obj;
                case JsonArray array:
                    for (int i = 0; i < array.Count; i++)
                    {
                        array[i] = Substitute(array[i], bindings)?.DeepClone();
                    }
                    return array;
                case JsonValue value when value.GetValueKind() == JsonValueKind.String:
                    string text = value.GetValue<string>();
                    if (text == "$profiles.fakeBasic")
                    {
                        return JsonNode.Parse(FakeBasicProfile);
                    }
                    if (text.StartsWith('$') && bindings.TryGetValue(text, out string? bound))
                    {
                        return JsonValue.Create(bound);
                    }
                    return value;
                default:
                    return node;
            }
        }

        /// <summary>Subset match: every expected property exists and matches; arrays match exactly.</summary>
        private static bool PartialMatch(JsonNode expected, JsonElement actual, out string? mismatch)
        {
            mismatch = null;
            switch (expected)
            {
                case JsonObject obj:
                    if (actual.ValueKind != JsonValueKind.Object)
                    {
                        mismatch = "expected object, got " + actual.ValueKind;
                        return false;
                    }
                    foreach ((string key, JsonNode? value) in obj)
                    {
                        if (!actual.TryGetProperty(key, out JsonElement actualValue))
                        {
                            mismatch = "missing property '" + key + "'";
                            return false;
                        }
                        if (value is not null && !PartialMatch(value, actualValue, out mismatch))
                        {
                            mismatch = key + "." + mismatch;
                            return false;
                        }
                    }
                    return true;

                case JsonArray array:
                    if (actual.ValueKind != JsonValueKind.Array || actual.GetArrayLength() != array.Count)
                    {
                        mismatch = "array shape mismatch";
                        return false;
                    }
                    int index = 0;
                    foreach (JsonElement item in actual.EnumerateArray())
                    {
                        if (array[index] is not null && !PartialMatch(array[index]!, item, out mismatch))
                        {
                            mismatch = $"[{index}].{mismatch}";
                            return false;
                        }
                        index++;
                    }
                    return true;

                default:
                    string expectedJson = expected.ToJsonString();
                    string actualJson = actual.GetRawText();
                    if (expectedJson != actualJson)
                    {
                        mismatch = $"expected {expectedJson}, got {actualJson}";
                        return false;
                    }
                    return true;
            }
        }
    }
}
