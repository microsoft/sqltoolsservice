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
    /// SPEC §12.2: live <c>diagnostics.state</c> and replay <c>DumpState</c> share one Core
    /// format, so a viewer diffing live-vs-replay sees no spurious differences. The live
    /// response additionally carries a Runtime overlay the journal never records.
    /// </summary>
    public sealed class StateDumpUnificationTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-statedump-test-" + Guid.NewGuid().ToString("N"));

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
        public async Task JournaledStateResultEqualsReplayDumpState()
        {
            OutboundRpcMessage wireState;
            await using (var session = new Sts2TestSession(directory))
            {
                await session.OpenConnectionAsync();
                wireState = await session.RequestAsync("v2/diagnostics.state", "{}");
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            // The journaled state result is the PURE Core dump (no runtime overlay).
            Sts2Envelope stateRequest = journal.Single(e => e.Kind == EnvelopeKinds.RpcInRequest && e.Type == "v2/diagnostics.state");
            Sts2Envelope journaledResult = journal.Single(e => e.Kind == EnvelopeKinds.RpcOutResult && e.Cause == stateRequest.Seq);

            // Replaying up to that seq and dumping the state reproduces the journaled result byte-for-byte.
            ReplayResult replay = JournalReplayer.Replay(journal, untilSeq: stateRequest.Seq);
            string replayDump = JournalReplayer.DumpState(replay.FinalState, stateRequest.Seq);
            Assert.Equal(journaledResult.Payload!.Value.GetRawText(), replayDump);

            // The journaled (pure) result has no runtime section; the wire response does.
            Assert.False(journaledResult.Payload!.Value.TryGetProperty("runtime", out _));
            Assert.True(wireState.Body!.Value.TryGetProperty("runtime", out JsonElement runtime));
            Assert.Equal(1, runtime.GetProperty("openLeases").GetInt32());
            Assert.Equal(1, runtime.GetProperty("configVersion").GetInt32());
        }

        [Fact]
        public async Task StateDumpExposesMachineFlagsThatExplainParkedConnections()
        {
            await using var session = new Sts2TestSession(directory);
            string connectionId = await session.OpenConnectionAsync();
            OutboundRpcMessage state = await session.RequestAsync("v2/diagnostics.state", "{}");

            JsonElement connection = state.Body!.Value.GetProperty("connections").GetProperty(connectionId);
            // The enriched dump explains why a connection is where it is (audit finding 5).
            Assert.True(connection.GetProperty("hasHandle").GetBoolean());
            Assert.False(connection.GetProperty("cancelRequested").GetBoolean());
            Assert.False(connection.GetProperty("closeAfterQuery").GetBoolean());
            Assert.False(connection.GetProperty("closePending").GetBoolean());
            // Top-level session facts.
            Assert.Equal("9.9.9", state.Body!.Value.GetProperty("serviceVersion").GetString());
            Assert.True(state.Body!.Value.GetProperty("maxConnections").GetInt32() > 0);
        }
    }
}
