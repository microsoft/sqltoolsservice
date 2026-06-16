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
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Observability;
using Microsoft.SqlTools.Sts2.UnitTests.Runtime;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Observability
{
    /// <summary>
    /// SPEC §12.1/§12.3: the health snapshot carries the full Runtime overlay, the metrics
    /// channel tallies the stream, and metric envelopes journal a snapshot on cadence.
    /// </summary>
    public sealed class MetricsAndHealthTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-metrics-test-" + Guid.NewGuid().ToString("N"));

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
        public async Task HealthCarriesTheFullRuntimeOverlay()
        {
            await using var session = new Sts2TestSession(directory);
            await session.OpenConnectionAsync();
            // An invalid request populates the error histogram.
            await session.RequestAsync("v2/connection.close", """{}""");

            OutboundRpcMessage health = await session.RequestAsync("v2/diagnostics.health", "{}");
            JsonElement body = health.Body!.Value;

            // Pure-Core facts.
            Assert.Equal(1, body.GetProperty("activeConnections").GetInt32());
            Assert.Equal(0, body.GetProperty("activeQueries").GetInt32());
            Assert.Equal(0, body.GetProperty("unackedPages").GetInt32());
            Assert.False(body.GetProperty("shuttingDown").GetBoolean());

            // Runtime overlay (SPEC §12.1) — all eleven dimensions are present and real.
            Assert.Equal(1, body.GetProperty("configVersion").GetInt32());
            Assert.True(body.GetProperty("queueDepth").GetInt32() >= 0);
            Assert.False(body.GetProperty("fatal").GetBoolean());
            Assert.Equal(1, body.GetProperty("openLeases").GetInt32());
            Assert.Equal(0, body.GetProperty("opensInFlight").GetInt32());
            Assert.Equal(0, body.GetProperty("activeQueryPumps").GetInt32());
            Assert.True(body.GetProperty("envelopesObserved").GetInt64() > 0);
            JsonElement dropped = body.GetProperty("droppedDiagnostics");
            Assert.Equal(0, dropped.GetProperty("emit").GetInt64());
            Assert.Equal(0, dropped.GetProperty("sink").GetInt64());
            // The malformed close produced an InvalidRequest error in the histogram.
            Assert.Equal(1, body.GetProperty("recentErrors").GetProperty("Sts2.InvalidRequest").GetInt32());
        }

        [Fact]
        public async Task MetricsSinkTalliesStreamByKindAndErrorCode()
        {
            await using var session = new Sts2TestSession(directory);
            await session.RequestAsync("v2/diagnostics.ping", """{"echo":"x"}""");
            await session.RequestAsync("v2/nope.unknown", null); // one error

            MetricsEnvelopeSink metrics = session.Coordinator.Metrics;
            Assert.True(metrics.Total > 0);
            IReadOnlyDictionary<string, long> byKind = metrics.EnvelopesByKind();
            Assert.True(byKind[EnvelopeKinds.RpcInRequest] >= 2);
            Assert.True(byKind[EnvelopeKinds.RpcOutResult] >= 1);
            Assert.Equal(1, metrics.Errors);
            Assert.Equal(1, metrics.ErrorsByCode()["Sts2.InvalidRequest"]);
        }

        [Fact]
        public async Task MetricEnvelopesAreJournaledOnCadenceAndReplaySkipsThem()
        {
            await using (var session = new Sts2TestSession(directory, metricSampleEvery: 3))
            {
                for (int i = 0; i < 6; i++)
                {
                    await session.RequestAsync("v2/diagnostics.ping", $$"""{"echo":"{{i}}"}""");
                }
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            List<Sts2Envelope> metricEnvelopes = journal.Where(e => e.Kind == EnvelopeKinds.Metric).ToList();
            Assert.NotEmpty(metricEnvelopes); // cadence fired
            Sts2Envelope sample = metricEnvelopes[0];
            Assert.Equal("sts2.snapshot", sample.Type);
            Assert.Null(sample.Cause); // journaled-only, no causal parent
            Assert.True(sample.Payload!.Value.GetProperty("envelopes").GetInt64() > 0);
            Assert.True(sample.Payload!.Value.TryGetProperty("byKind", out _));

            // I5 gapless seq holds with metric envelopes interleaved.
            Assert.Equal(Enumerable.Range(1, journal.Count).Select(i => (long)i), journal.Select(e => e.Seq));
            // I7: replay ignores metric envelopes and still matches exactly.
            Microsoft.SqlTools.Sts2.Runtime.Replay.ReplayResult replay =
                Microsoft.SqlTools.Sts2.Runtime.Replay.JournalReplayer.Replay(journal);
            Assert.True(replay.Identical, replay.Divergence?.Replayed);
        }

        [Fact]
        public async Task FatalStatusIsFalseOnAHealthyPump()
        {
            await using var session = new Sts2TestSession(directory);
            await session.RequestAsync("v2/diagnostics.ping", """{"echo":"ok"}""");
            Assert.Null(session.Coordinator.FatalReason);
        }
    }
}
