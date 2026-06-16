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
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.UnitTests.Runtime;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Observability
{
    /// <summary>
    /// SPEC §8.4/§11.1: v2/diagnostics.setCapture changes capture mode at runtime, journals
    /// a config.changed envelope, bumps configVersion, and is replay-visible (I15).
    /// </summary>
    public sealed class SetCaptureTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-setcapture-test-" + Guid.NewGuid().ToString("N"));

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
        public async Task SetCaptureJournalsConfigChangedBumpsVersionAndTakesEffect()
        {
            await using (var session = new Sts2TestSession(directory)) // starts full/text
            {
                Assert.Equal(1, session.Coordinator.ConfigVersion);
                string connectionId = await session.OpenConnectionAsync();

                OutboundRpcMessage set = await session.RequestAsync("v2/diagnostics.setCapture",
                    """{"rowCapture":"digest","sqlCapture":"digest"}""");
                Assert.Equal("rpc.out.result", set.Kind);
                Assert.Equal(2, set.Body!.Value.GetProperty("configVersion").GetInt32());
                Assert.Equal("digest", set.Body!.Value.GetProperty("sqlCapture").GetString());
                Assert.Equal(2, session.Coordinator.ConfigVersion);

                // The new mode applies to the next envelope: this query's SQL is elided.
                await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"SELECT 1"}""");
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();

            // A config.changed envelope was journaled and is a flush point.
            Sts2Envelope configChanged = journal.Single(e => e.Kind == EnvelopeKinds.ConfigChanged);
            Assert.Equal("capture", configChanged.Type);
            Assert.Equal("digest", configChanged.Payload!.Value.GetProperty("rowCapture").GetString());
            Assert.Equal(2, configChanged.Payload!.Value.GetProperty("configVersion").GetInt32());

            // Envelopes after the change carry the new config version.
            Assert.Equal(2, configChanged.ConfigVersion);

            // The post-change query.execute journaled its SQL as a $redacted digest wrapper.
            Sts2Envelope queryExecute = journal.First(e => e.Kind == EnvelopeKinds.RpcInRequest && e.Type == "v2/query.execute");
            Assert.Equal(JsonValueKind.Object, queryExecute.Payload!.Value.GetProperty("sql").ValueKind);
        }

        [Fact]
        public async Task SetCaptureIsIdempotentWhenUnchanged()
        {
            await using var session = new Sts2TestSession(directory, rowCapture: "digest", sqlCapture: "digest");
            OutboundRpcMessage set = await session.RequestAsync("v2/diagnostics.setCapture",
                """{"rowCapture":"digest","sqlCapture":"digest"}""");

            Assert.Equal("rpc.out.result", set.Kind);
            Assert.Equal(1, set.Body!.Value.GetProperty("configVersion").GetInt32()); // unchanged: no bump
            Assert.Equal(1, session.Coordinator.ConfigVersion);
            Assert.DoesNotContain(JournalReader.ReadAll(directory), e => e.Kind == EnvelopeKinds.ConfigChanged);
        }

        [Fact]
        public async Task SetCaptureRejectsInvalidMode()
        {
            await using var session = new Sts2TestSession(directory);
            OutboundRpcMessage error = await session.RequestAsync("v2/diagnostics.setCapture", """{"rowCapture":"bogus"}""");

            Assert.Equal("rpc.out.error", error.Kind);
            Assert.Equal("Sts2.InvalidRequest", error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
            Assert.Equal(1, session.Coordinator.ConfigVersion); // unchanged
        }

        [Fact]
        public async Task ConfigChangeIsReplayVisibleAndDeterministic()
        {
            await using (var session = new Sts2TestSession(directory))
            {
                await session.RequestAsync("v2/diagnostics.setCapture", """{"rowCapture":"digest","sqlCapture":"digest"}""");
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            ReplayResult replay = JournalReplayer.Replay(journal);
            Assert.True(replay.Identical, replay.Divergence?.Replayed); // I7 with config.changed in the stream
            // I15: the change is visible in replayed state.
            Assert.Equal(2, replay.FinalState.ConfigVersion);
            Assert.Equal("digest", replay.FinalState.RowCapture);
            Assert.Equal("digest", replay.FinalState.SqlCapture);
        }
    }
}
