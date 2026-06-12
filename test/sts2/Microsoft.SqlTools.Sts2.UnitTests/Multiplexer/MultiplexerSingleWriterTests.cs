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
    /// SPEC §6.4 / I10: concurrent producers, one stdout writer, zero interleaved frames.
    /// </summary>
    public class MultiplexerSingleWriterTests
    {
        [Fact]
        public async Task ConcurrentProducersProduceOnlyIntactFrames()
        {
            const int framesPerProducer = 400;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await using var h = new MuxHarness();

            // Deterministic seed so a failure is reproducible; sizes vary from tiny to multi-KiB.
            var rng = new Random(0x5753_3221);
            string Payload(string channel, int i)
            {
                string pad = new string('p', rng.Next(0, 4096));
                return $"{{\"jsonrpc\":\"2.0\",\"method\":\"{channel}/n{i.ToString(CultureInfo.InvariantCulture)}\",\"params\":{{\"pad\":\"{pad}\"}}}}";
            }

            // Pre-generate payloads on one thread (Random is not thread-safe).
            string[] legacyFrames = new string[framesPerProducer];
            string[] sts2Frames = new string[framesPerProducer];
            for (int i = 0; i < framesPerProducer; i++)
            {
                legacyFrames[i] = Payload("legacy", i);
                sts2Frames[i] = Payload("v2", i);
            }

            Task legacyProducer = Task.Run(async () =>
            {
                foreach (string f in legacyFrames)
                {
                    await h.LegacySendsAsync(f, cts.Token);
                }
            }, cts.Token);
            Task sts2Producer = Task.Run(async () =>
            {
                foreach (string f in sts2Frames)
                {
                    await h.Sts2SendsAsync(f, cts.Token);
                }
            }, cts.Token);

            var received = new List<string>(framesPerProducer * 2);
            for (int i = 0; i < framesPerProducer * 2; i++)
            {
                received.Add(await h.StdoutFrameAsync(cts.Token));
            }
            await Task.WhenAll(legacyProducer, sts2Producer);

            // Every frame parses as standalone JSON (intact, no interleaving)...
            int legacySeen = 0, sts2Seen = 0;
            foreach (string frame in received)
            {
                string method = JsonDocument.Parse(frame).RootElement.GetProperty("method").GetString()!;
                if (method.StartsWith("v2/", StringComparison.Ordinal))
                {
                    // ...and per-producer order is preserved.
                    Assert.Equal($"v2/n{sts2Seen}", method);
                    sts2Seen++;
                }
                else
                {
                    Assert.Equal($"legacy/n{legacySeen}", method);
                    legacySeen++;
                }
            }
            Assert.Equal(framesPerProducer, legacySeen);
            Assert.Equal(framesPerProducer, sts2Seen);
        }
    }
}
