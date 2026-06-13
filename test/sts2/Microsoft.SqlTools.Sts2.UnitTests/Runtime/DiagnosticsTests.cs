//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §12: diagnostics.health and diagnostics.state return redacted snapshots (I16).</summary>
    public sealed class DiagnosticsTests : IAsyncDisposable, IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-diag-test-" + Guid.NewGuid().ToString("N"));
        private readonly Sts2TestSession session;

        public DiagnosticsTests()
        {
            session = new Sts2TestSession(directory, "diag-test", rowCapture: "digest", sqlCapture: "digest");
        }

        public ValueTask DisposeAsync() => session.DisposeAsync();

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

        [Fact]
        public async Task HealthReportsCounters()
        {
            string connectionId = await session.OpenConnectionAsync();
            OutboundRpcMessage health = await session.RequestAsync("v2/diagnostics.health", "{}");

            Assert.Equal("rpc.out.result", health.Kind);
            Assert.Equal(1, health.Body!.Value.GetProperty("activeConnections").GetInt32());
            Assert.False(health.Body!.Value.GetProperty("fatal").GetBoolean());
            Assert.True(health.Body!.Value.GetProperty("latestJournalSeq").GetInt64() > 0);

            await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
        }

        [Fact]
        public async Task StateIsRedactedAndCanaryClean()
        {
            string connectionId = await session.OpenConnectionAsync(); // profile carries the canary password
            OutboundRpcMessage state = await session.RequestAsync("v2/diagnostics.state", "{}");

            Assert.Equal("rpc.out.result", state.Kind);
            string json = state.Body!.Value.GetRawText();
            // I16: no secrets, row cells, or SQL — just ids/phases.
            Assert.Empty(SecretCanaries.FindIn(json));
            Assert.Contains(connectionId, json);
            Assert.Equal("open", state.Body!.Value.GetProperty("connections").GetProperty(connectionId).GetProperty("phase").GetString());

            await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
        }
    }
}
