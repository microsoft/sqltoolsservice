//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
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

        private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

        public void Dispose()
        {
            try
            {
                Directory.Delete(logDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; temp cleanup.
            }
        }

        [Fact]
        public async Task DisabledMode_V1VersionWorks_AndNoSts2ArtifactsAreCreated()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: false, logDirectory);

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
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory);

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
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory);

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
        public async Task EnabledMode_SqliteQueryStreamsOverRealStdio()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory);
            await client.RequestAsync("v2/initialize", new { clientName = "e2e" }, TestTimeout);

            JsonElement open = await client.RequestAsync("v2/connection.open",
                new { openId = "o-1", profile = new { server = ":memory:", driver = "sqlite", auth = new { kind = "integrated" } } }, TestTimeout);
            Assert.True(open.TryGetProperty("result", out JsonElement openResult), "open failed: " + open.GetRawText());
            string connectionId = openResult.GetProperty("connectionId").GetString()!;

            // One active query per connection: each must complete (async notification)
            // before the next executes.
            await ExecuteToCompletionAsync(client, connectionId, "create table t(n integer)", TestTimeout);
            await ExecuteToCompletionAsync(client, connectionId, "insert into t values (10),(20)", TestTimeout);
            JsonElement completeParams = await ExecuteToCompletionAsync(client, connectionId, "select n from t order by n", TestTimeout);
            Assert.Equal("succeeded", completeParams.GetProperty("status").GetString());
        }

        private static async Task<JsonElement> ExecuteToCompletionAsync(
            ServiceProcessClient client, string connectionId, string sql, System.Threading.CancellationToken ct)
        {
            JsonElement execute = await client.RequestAsync("v2/query.execute", new { connectionId, sql }, ct);
            Assert.True(execute.TryGetProperty("result", out JsonElement result), "execute rejected: " + execute.GetRawText());
            string queryId = result.GetProperty("queryId").GetString()!;
            while (!ct.IsCancellationRequested)
            {
                (string _, JsonElement completeParams) = await WaitForNotificationAsync(client, "v2/query.complete", ct);
                if (completeParams.GetProperty("queryId").GetString() == queryId)
                {
                    return completeParams;
                }
            }
            throw new TimeoutException("query " + queryId + " did not complete");
        }

        private static async Task<(string Method, JsonElement Params)> WaitForNotificationAsync(
            ServiceProcessClient client, string method, System.Threading.CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                while (client.Notifications.TryDequeue(out (string Method, JsonElement Params) notification))
                {
                    if (notification.Method == method)
                    {
                        return notification;
                    }
                }
                await Task.Delay(20, ct);
            }
            throw new TimeoutException("notification " + method + " not received");
        }

        [Fact]
        public async Task EnabledMode_ShutdownTerminatesProcess()
        {
            await using var client = ServiceProcessClient.Start(enableSts2: true, logDirectory);

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
