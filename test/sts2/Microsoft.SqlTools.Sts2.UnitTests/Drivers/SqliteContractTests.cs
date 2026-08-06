//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.Sqlite;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// SPEC §16 M4: the `dialect:neutral` contract suite on Sqlite. Drives a real session
    /// (the full coordinator/Core/effect-runner stack) through the Sqlite adapter and
    /// asserts the same wire contract and invariants the FakeDriver scenarios assert —
    /// proving the driver port is honest against real I/O (§10.3), not Fake-shaped.
    /// </summary>
    public sealed class SqliteContractTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-sqlite-contract-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private SqliteSessionHarness NewSession(string runId) => new(Path.Combine(directory, runId), runId);

        [Fact]
        public async Task NeutralQueryStreamsRealRowsThroughTheFullStack()
        {
            await using var session = NewSession("neutral-query");
            string connectionId = await session.OpenSqliteAsync(":memory:");

            await session.QueryToCompletionAsync(connectionId, "create table t(id integer, name text)");
            await session.QueryToCompletionAsync(connectionId, "insert into t values (1,'alpha'),(2,'beta'),(3,'gamma')");

            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select id, name from t order by id"}""");
            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("succeeded", completes[0].Body!.Value.GetProperty("status").GetString());

            // Same wire shape as the FakeDriver happy path: resultSet then rows.
            List<OutboundRpcMessage> resultSets = await session.WaitForNotificationsAsync("v2/query.resultSet", 1);
            Assert.Equal(2, resultSets[0].Body!.Value.GetProperty("columns").GetArrayLength());
            List<OutboundRpcMessage> rows = session.Notifications("v2/query.rows");
            int rowCount = rows.Sum(r => r.Body!.Value.GetProperty("rows").GetArrayLength());
            Assert.Equal(3, rowCount);
            Assert.Equal("alpha", rows[0].Body!.Value.GetProperty("rows")[0][1].GetString());

            await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
        }

        [Fact]
        public async Task NeutralSyntaxErrorSurfacesAsQueryComplete()
        {
            await using var session = NewSession("neutral-error");
            string connectionId = await session.OpenSqliteAsync(":memory:");

            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"selct bad"}""");
            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("error", completes[0].Body!.Value.GetProperty("status").GetString());
            Assert.Equal("Sts2.QueryFailed.Server", completes[0].Body!.Value.GetProperty("error").GetProperty("code").GetString());

            await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
        }

        [Fact]
        public async Task SqliteSessionReplaysIdentically()
        {
            await using (var session = NewSession("sqlite-replay"))
            {
                string connectionId = await session.OpenSqliteAsync(":memory:");
                await session.QueryToCompletionAsync(connectionId, "create table t(n integer)");
                await session.QueryToCompletionAsync(connectionId, "insert into t values (1),(2),(3)");
                await session.QueryToCompletionAsync(connectionId, "select n from t");
                await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            }

            ReplayResult replay = JournalReplayer.Replay(JournalReader.ReadAll(Path.Combine(directory, "sqlite-replay")));
            Assert.True(replay.Identical,
                "divergence: " + replay.Divergence?.Recorded + " vs " + replay.Divergence?.Replayed);
        }

        /// <summary>Harness wiring the real stack with both Fake and Sqlite drivers registered.</summary>
        private sealed class SqliteSessionHarness : IAsyncDisposable
        {
            private readonly Coordinator coordinator;
            private readonly DriverEffectRunner effectRunner;
            private readonly SecretSideTable secrets = new();
            private readonly System.Collections.Concurrent.ConcurrentQueue<OutboundRpcMessage> emitted = new();
            private int corrCounter;

            public SqliteSessionHarness(string journalDirectory, string runId)
            {
                effectRunner = new DriverEffectRunner(
                    new Dictionary<string, IDbDriver> { ["fake"] = new FakeDriver(), ["sqlite"] = new SqliteDriver() }, secrets);
                coordinator = new Coordinator(
                    new JournalWriter(runId, new JournalOptions { Directory = journalDirectory }, new JournalRunInfo { ServiceVersion = "m4" }),
                    new CoordinatorOptions { RunId = runId },
                    effectRunner,
                    emitted.Enqueue);
                coordinator.PostControlAsync("session.start", JsonDocument.Parse("""
                    {"serviceVersion":"m4","drivers":[{"name":"sqlite","dialects":["sqlite","neutral"],"production":false}]}
                    """).RootElement).AsTask().GetAwaiter().GetResult();
            }

            public List<OutboundRpcMessage> Notifications(string method) =>
                emitted.Where(m => m.Kind == "rpc.out.notify" && m.Type == method).ToList();

            public async Task<string> OpenSqliteAsync(string dataSource)
            {
                string payload = "{\"openId\":\"o-1\",\"profile\":{\"server\":\"" + dataSource + "\",\"driver\":\"sqlite\",\"auth\":{\"kind\":\"integrated\"}}}";
                OutboundRpcMessage open = await RequestAsync("v2/connection.open", payload);
                Assert.Equal("rpc.out.result", open.Kind);
                return open.Body!.Value.GetProperty("connectionId").GetString()!;
            }

            public async Task QueryToCompletionAsync(string connectionId, string sql)
            {
                int before = Notifications("v2/query.complete").Count;
                await RequestAsync("v2/query.execute",
                    "{\"connectionId\":\"" + connectionId + "\",\"sql\":" + JsonSerializer.Serialize(sql) + "}");
                await WaitForNotificationsAsync("v2/query.complete", before + 1);
            }

            public async Task<OutboundRpcMessage> RequestAsync(string method, string payloadJson)
            {
                string corr = "r-" + System.Threading.Interlocked.Increment(ref corrCounter);
                var node = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
                JsonElement payload = JsonDocument.Parse(SecretRedactor.Redact(node, secrets)!.ToJsonString()).RootElement;
                await coordinator.PostRpcRequestAsync(method, corr, payload);
                for (int spins = 0; spins < 1500; spins++)
                {
                    if (emitted.FirstOrDefault(m => m.Corr == corr) is { } match)
                    {
                        return match;
                    }
                    await Task.Delay(10);
                }
                throw new TimeoutException("No terminal for " + method);
            }

            public async Task<List<OutboundRpcMessage>> WaitForNotificationsAsync(string method, int count)
            {
                for (int spins = 0; spins < 1500; spins++)
                {
                    List<OutboundRpcMessage> matches = Notifications(method);
                    if (matches.Count >= count)
                    {
                        return matches;
                    }
                    await Task.Delay(10);
                }
                throw new TimeoutException($"Expected {count} {method}; have {Notifications(method).Count}");
            }

            public async ValueTask DisposeAsync()
            {
                await coordinator.DisposeAsync();
                await effectRunner.DisposeLeakedSessionsAsync();
            }
        }
    }
}
