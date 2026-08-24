//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    /// <summary>
    /// SPEC §16 M6 / I13: torture the multiplexer with many concurrent legacy and STS2
    /// server-initiated requests whose numeric ids deliberately collide, under random
    /// chunking, and prove every id is rewritten distinctly and every response is restored
    /// to its exact original id and channel.
    /// </summary>
    public class MultiplexerInterleavingTortureTests
    {
        [Fact]
        public async Task CollidingServerRequestIdsStayDistinctAndRestoreExactly()
        {
            const int RequestsPerChannel = 150;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var h = new MuxHarness();

            // Both channels emit server-initiated requests with the SAME numeric ids 1..N.
            Task legacyProducer = Task.Run(async () =>
            {
                for (int i = 1; i <= RequestsPerChannel; i++)
                {
                    await h.LegacySendsAsync($$"""{"jsonrpc":"2.0","id":{{i}},"method":"legacy/req{{i}}"}""", cts.Token);
                }
            }, cts.Token);
            Task sts2Producer = Task.Run(async () =>
            {
                for (int i = 1; i <= RequestsPerChannel; i++)
                {
                    await h.Sts2SendsAsync($$"""{"jsonrpc":"2.0","id":{{i}},"method":"v2/req{{i}}"}""", cts.Token);
                }
            }, cts.Token);

            // Collect all rewritten public ids from stdout, mapping publicId -> (channel, originalId, method).
            var publicToOrigin = new Dictionary<string, (string Channel, int OriginalId, string Method)>(StringComparer.Ordinal);
            for (int i = 0; i < RequestsPerChannel * 2; i++)
            {
                JsonElement frame = JsonDocument.Parse(await h.StdoutFrameAsync(cts.Token)).RootElement;
                string publicId = frame.GetProperty("id").GetString()!; // public ids are strings
                string method = frame.GetProperty("method").GetString()!;
                string channel = method.StartsWith("v2/", StringComparison.Ordinal) ? "sts2" : "legacy";
                int originalId = int.Parse(method.AsSpan(channel == "sts2" ? "v2/req".Length : "legacy/req".Length), CultureInfo.InvariantCulture);
                Assert.False(publicToOrigin.ContainsKey(publicId), "public id collision: " + publicId);
                publicToOrigin[publicId] = (channel, originalId, method);
            }
            await Task.WhenAll(legacyProducer, sts2Producer);

            // Every public id is distinct across BOTH channels (300 total).
            Assert.Equal(RequestsPerChannel * 2, publicToOrigin.Count);

            // The client answers every request (in dictionary order, i.e. arbitrary); each
            // response must be restored to the exact original numeric id on the right channel.
            foreach ((string publicId, _) in publicToOrigin)
            {
                await h.ClientSendsAsync(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"" + publicId + "\",\"result\":{\"ok\":true}}", cts.Token);
            }

            // Each channel receives exactly RequestsPerChannel responses, each restored to
            // a distinct original numeric id on the correct channel.
            var seenLegacy = new HashSet<int>();
            var seenSts2 = new HashSet<int>();
            for (int i = 0; i < RequestsPerChannel; i++)
            {
                JsonElement legacy = JsonDocument.Parse(await h.LegacyReceivesAsync(cts.Token)).RootElement;
                Assert.Equal(JsonValueKind.Number, legacy.GetProperty("id").ValueKind); // restored to number
                Assert.True(seenLegacy.Add(legacy.GetProperty("id").GetInt32()));

                JsonElement sts2 = JsonDocument.Parse(await h.Sts2ReceivesAsync(cts.Token)).RootElement;
                Assert.Equal(JsonValueKind.Number, sts2.GetProperty("id").ValueKind);
                Assert.True(seenSts2.Add(sts2.GetProperty("id").GetInt32()));
            }

            // Each channel saw every original id 1..N restored exactly once.
            for (int i = 1; i <= RequestsPerChannel; i++)
            {
                Assert.Contains(i, seenLegacy);
                Assert.Contains(i, seenSts2);
            }
        }
    }
}
