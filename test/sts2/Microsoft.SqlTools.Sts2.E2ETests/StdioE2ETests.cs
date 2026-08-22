//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.Sts2.E2ETests
{
    /// <summary>
    /// SPEC §5.3 and §16 M0: spawned-exe tests over real stdio, enabled and disabled modes.
    /// </summary>
    public class StdioE2ETests : IDisposable
    {
        private readonly string logDirectory = Path.Combine(
            Path.GetTempPath(), "sts2-e2e-" + Guid.NewGuid().ToString("N"));
        private readonly CancellationTokenSource testTimeout = new(TimeSpan.FromSeconds(60));

        private CancellationToken TestTimeout => testTimeout.Token;

        private sealed record QueryTranscript(
            string QueryId,
            IReadOnlyList<JsonElement> ResultSets,
            IReadOnlyList<JsonElement> Pages,
            JsonElement Completion);

        public void Dispose()
        {
            testTimeout.Dispose();
            try
            {
                Directory.Delete(logDirectory, recursive: true);
            }
            catch (Exception)
            {
                // Best effort; temp cleanup.
            }
        }

        [Fact]
        public void SpawnedSubjectMatchesCurrentTestBuild()
        {
            string serviceDll = ServiceProcessClient.LocateServiceDll();
            var testOutput = new DirectoryInfo(AppContext.BaseDirectory);
            var serviceOutput = new DirectoryInfo(Path.GetDirectoryName(serviceDll)!);

            Assert.Equal(testOutput.Name, serviceOutput.Name);
            Assert.Equal(testOutput.Parent!.Name, serviceOutput.Parent!.Name);
            Assert.True(File.Exists(serviceDll));
        }

        [Fact]
        public async Task DisabledMode_V1VersionWorks_AndNoSts2ArtifactsAreCreated()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: false, logDirectory: logDirectory);

            JsonElement response = await client.RequestAsync("version", new { }, TestTimeout);
            Assert.True(response.TryGetProperty("result", out JsonElement result), "version request failed: " + response.GetRawText());
            Assert.Equal(JsonValueKind.String, result.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(result.GetString()));

            // SPEC §5.3: disabled mode creates no multiplexer diagnostic log and no journal dir.
            Assert.Empty(Directory.EnumerateFiles(logDirectory, "sts2-mux-*.log"));
            Assert.False(Directory.Exists(Path.Combine(logDirectory, "sts2")), "disabled mode must not create an sts2 journal directory");
        }

        [Fact]
        public async Task EnabledMode_PingAndV1VersionShareOneSession()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory: logDirectory);

            // v2 and v1 requests interleaved on the same stdio stream (SPEC §1.1).
            JsonElement ping = await client.RequestAsync("v2/diagnostics.ping", new { echo = "m0-e2e" }, TestTimeout);
            Assert.True(ping.TryGetProperty("result", out JsonElement pingResult), "ping failed: " + ping.GetRawText());
            Assert.Equal("2.0.0-preview.1", pingResult.GetProperty("specVersion").GetString());
            Assert.Equal("m0-e2e", pingResult.GetProperty("echo").GetString());
            Assert.Equal("ok", pingResult.GetProperty("health").GetString());

            JsonElement version = await client.RequestAsync("version", new { }, TestTimeout);
            Assert.True(version.TryGetProperty("result", out JsonElement versionResult), "version failed: " + version.GetRawText());
            Assert.Equal(JsonValueKind.String, versionResult.ValueKind);

            // And v2 again after v1, proving routing is stable across interleaving.
            JsonElement ping2 = await client.RequestAsync("v2/diagnostics.ping", new { echo = "again" }, TestTimeout);
            Assert.Equal("again", ping2.GetProperty("result").GetProperty("echo").GetString());
        }

        [Fact]
        public async Task EnabledMode_InitializeWorksAndJournalIsWritten()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory: logDirectory);

            JsonElement initialize = await client.RequestAsync("v2/initialize", new { clientName = "e2e" }, TestTimeout);
            Assert.True(initialize.TryGetProperty("result", out JsonElement result), "initialize failed: " + initialize.GetRawText());
            Assert.Equal("2.0.0-preview.1", result.GetProperty("specVersion").GetString());
            Assert.True(result.GetProperty("limits").GetProperty("pageRows").GetInt32() > 0);

            // The journal exists under <log-dir>/sts2/<runId>/ in enabled mode (SPEC §8.3,
            // one directory per run — R007).
            string journalDir = Path.Combine(logDirectory, "sts2");
            Assert.True(Directory.Exists(journalDir), "journal directory missing: " + journalDir);
            Assert.NotEmpty(Directory.EnumerateFiles(journalDir, "journal-*.jsonl", SearchOption.AllDirectories));

            // Unregistered v2 methods get JSON-RPC method-not-found from the gateway
            // (numeric code, I12-compatible); registered-but-invalid requests get
            // Sts2.* identities from Core (covered by unit scenarios).
            JsonElement unknown = await client.RequestAsync("v2/does.not.exist", new { }, TestTimeout);
            Assert.True(unknown.TryGetProperty("error", out JsonElement error), "expected error: " + unknown.GetRawText());
            Assert.Equal(-32601, error.GetProperty("code").GetInt32());
        }

        [Fact]
        public async Task EnabledMode_SqliteQueryExercisesPagedLifecycleOverRealStdio()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory: logDirectory);
            await client.RequestAsync("v2/initialize", new { clientName = "e2e" }, TestTimeout);

            JsonElement open = await client.RequestAsync("v2/connection.open",
                new { openId = "o-1", profile = new { server = ":memory:", driver = "sqlite", auth = new { kind = "integrated" } } }, TestTimeout);
            Assert.True(open.TryGetProperty("result", out JsonElement openResult), "open failed: " + open.GetRawText());
            string connectionId = openResult.GetProperty("connectionId").GetString()!;

            // One active query per connection: each must complete (async notification)
            // before the next executes.
            await ExecuteToCompletionAsync(client, connectionId, "create table t(n integer)", TestTimeout);
            await ExecuteToCompletionAsync(client, connectionId, "insert into t values (10),(20)", TestTimeout);
            QueryTranscript selected = await ExecuteToCompletionAsync(
                client,
                connectionId,
                "select n from t order by n",
                TestTimeout);
            Assert.Equal("succeeded", selected.Completion.GetProperty("status").GetString());
            Assert.Single(selected.ResultSets);
            Assert.Equal("n", selected.ResultSets[0].GetProperty("columns")[0].GetProperty("name").GetString());
            Assert.Equal(
                [10L, 20L],
                selected.Pages.SelectMany(page => page.GetProperty("rows").EnumerateArray())
                    .Select(row => row[0].GetInt64()));

            // More result sets than the four-page credit window. Wire pageSeq restarts at
            // zero for each set; the helper's cumulative per-query ack ordinal must keep the
            // stream moving through all six pages.
            QueryTranscript manySets = await ExecuteToCompletionAsync(
                client,
                connectionId,
                "select 0 as n; select 1 as n; select 2 as n; " +
                "select 3 as n; select 4 as n; select 5 as n;",
                TestTimeout);
            Assert.Equal("succeeded", manySets.Completion.GetProperty("status").GetString());
            Assert.Equal(6, manySets.ResultSets.Count);
            Assert.Equal(6, manySets.Pages.Count);
            Assert.All(manySets.Pages, page => Assert.Equal(0, page.GetProperty("pageSeq").GetInt32()));
            Assert.Equal(
                Enumerable.Range(0, 6),
                manySets.Pages.Select(page => page.GetProperty("rows")[0][0].GetInt32()));

            JsonElement afterDispose = (await client.RequestAsync(
                "v2/diagnostics.state",
                new { },
                TestTimeout)).GetProperty("result");
            Assert.Empty(afterDispose.GetProperty("queries").EnumerateObject());
            Assert.Equal(
                JsonValueKind.Null,
                afterDispose.GetProperty("connections").GetProperty(connectionId).GetProperty("activeQueryId").ValueKind);
            Assert.Equal(0, afterDispose.GetProperty("runtime").GetProperty("activeQueryPumps").GetInt32());

            JsonElement close = await client.RequestAsync(
                "v2/connection.close",
                new { connectionId },
                TestTimeout);
            Assert.True(close.TryGetProperty("result", out _), "close failed: " + close.GetRawText());

            JsonElement afterClose = (await client.RequestAsync(
                "v2/diagnostics.state",
                new { },
                TestTimeout)).GetProperty("result");
            Assert.Empty(afterClose.GetProperty("connections").EnumerateObject());
            Assert.Equal(0, afterClose.GetProperty("runtime").GetProperty("openLeases").GetInt32());
        }

        private static async Task<QueryTranscript> ExecuteToCompletionAsync(
            ServiceProcessClient client, string connectionId, string sql, System.Threading.CancellationToken ct)
        {
            JsonElement execute = await client.RequestAsync(
                "v2/query.execute",
                new { connectionId, sql, options = new { pageRows = 1 } },
                ct);
            Assert.True(execute.TryGetProperty("result", out JsonElement result), "execute rejected: " + execute.GetRawText());
            string queryId = result.GetProperty("queryId").GetString()!;
            var resultSets = new List<JsonElement>();
            var pages = new List<JsonElement>();
            int receivedPageOrdinal = -1;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    (string method, JsonElement parameters) =
                        await client.ReadQueryNotificationAsync(queryId, ct);
                    switch (method)
                    {
                        case "v2/query.resultSet":
                            resultSets.Add(parameters);
                            break;

                        case "v2/query.rows":
                            pages.Add(parameters);
                            receivedPageOrdinal++;
                            await client.NotifyAsync(
                                "v2/query.ack",
                                new { queryId, throughPageSeq = receivedPageOrdinal },
                                ct);
                            break;

                        case "v2/query.complete":
                            JsonElement dispose = await client.RequestAsync(
                                "v2/query.dispose",
                                new { queryId },
                                ct);
                            Assert.True(
                                dispose.TryGetProperty("result", out _),
                                "dispose failed: " + dispose.GetRawText());
                            return new QueryTranscript(queryId, resultSets, pages, parameters);
                    }
                }
                throw new TimeoutException("query " + queryId + " did not complete");
            }
            finally
            {
                client.ReleaseQueryNotifications(queryId);
            }
        }

        [Fact]
        public async Task EnabledMode_ShutdownTerminatesProcess()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory: logDirectory);

            // Prove the session is alive before shutting down.
            JsonElement ping = await client.RequestAsync("v2/diagnostics.ping", new { }, TestTimeout);
            Assert.True(ping.TryGetProperty("result", out _));

            // Legacy never responds to shutdown: its handler runs shutdown callbacks and
            // calls Environment.Exit(0) directly, and no exit handler exists (RF-0011).
            // The multiplexer's bounded flush wait happens before the frame reaches legacy.
            await client.SendRequestFireAndForgetAsync("shutdown", TestTimeout);
            Assert.True(await client.WaitForExitAsync(TimeSpan.FromSeconds(30)), "process did not exit after shutdown request");
        }
    }
}
