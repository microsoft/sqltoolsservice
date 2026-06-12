//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §8.3: append-only JSONL journal with manifest and hash chain.</summary>
    public sealed class JournalTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-journal-test-" + Guid.NewGuid().ToString("N"));

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

        private static Sts2Envelope Envelope(long seq, string type = "toy.echo", string kind = "cmd") => new()
        {
            RunId = "run-test",
            Seq = seq,
            Ts = new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(seq),
            Kind = kind,
            Type = type,
            ConfigVersion = 1,
            Digest = CanonicalJson.DigestOf(JsonDocument.Parse($"{{\"n\":{seq}}}").RootElement),
            Payload = JsonDocument.Parse($"{{\"n\":{seq}}}").RootElement.Clone(),
        };

        private JournalWriter CreateWriter(long segmentBytes = 64 * 1024 * 1024) =>
            new("run-test", new JournalOptions { Directory = directory, SegmentBytes = segmentBytes },
                new JournalRunInfo { ServiceVersion = "99.0.0", CommandLine = ["--enable-sts2"] });

        [Fact]
        public async Task AppendedEnvelopesRoundTripInOrder()
        {
            await using (JournalWriter writer = CreateWriter())
            {
                for (int i = 1; i <= 5; i++)
                {
                    await writer.AppendAsync(Envelope(i), flush: false);
                }
            }

            List<Sts2Envelope> back = JournalReader.ReadAll(directory).ToList();
            Assert.Equal([1L, 2, 3, 4, 5], back.Select(e => e.Seq));
            Assert.All(back, e => Assert.Equal("run-test", e.RunId));
        }

        [Fact]
        public async Task FlushHintMakesLinesVisibleBeforeDispose()
        {
            await using JournalWriter writer = CreateWriter();
            await writer.AppendAsync(Envelope(1), flush: true);

            string segment = Directory.EnumerateFiles(directory, "journal-run-test-*.jsonl").Single();
            using var reader = new StreamReader(new FileStream(segment, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            string content = await reader.ReadToEndAsync();
            Assert.Contains("\"seq\":1", content);
        }

        [Fact]
        public async Task SegmentsRotateAtConfiguredSize()
        {
            await using (JournalWriter writer = CreateWriter(segmentBytes: 512))
            {
                for (int i = 1; i <= 20; i++)
                {
                    await writer.AppendAsync(Envelope(i), flush: false);
                }
            }

            string[] segments = Directory.EnumerateFiles(directory, "journal-run-test-*.jsonl")
                .Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray()!;
            Assert.True(segments.Length > 1, "expected rotation to produce multiple segments");
            Assert.Equal("journal-run-test-0001.jsonl", segments[0]);
            Assert.Equal("journal-run-test-0002.jsonl", segments[1]);

            // Order survives rotation.
            Assert.Equal(Enumerable.Range(1, 20).Select(i => (long)i), JournalReader.ReadAll(directory).Select(e => e.Seq));
        }

        [Fact]
        public async Task ManifestRecordsSegmentsWithHashChain()
        {
            await using (JournalWriter writer = CreateWriter(segmentBytes: 512))
            {
                for (int i = 1; i <= 20; i++)
                {
                    await writer.AppendAsync(Envelope(i), flush: false);
                }
            }

            JournalManifest manifest = JournalReader.ReadManifest(Path.Combine(directory, "journal-run-test.manifest.json"));
            Assert.Equal("sts2.journal.manifest/1", manifest.Schema);
            Assert.Equal("run-test", manifest.RunId);
            Assert.Equal("99.0.0", manifest.ServiceVersion);
            Assert.True(manifest.Segments.Count > 1);

            string? previous = null;
            foreach (JournalSegment segment in manifest.Segments)
            {
                string path = Path.Combine(directory, segment.FileName);
                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal(bytes.Length, segment.Bytes);
                Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)), segment.Sha256);
                Assert.Equal(previous, segment.PreviousSegmentSha256);
                previous = segment.Sha256;
            }
        }

        [Fact]
        public async Task JournalLinesAreAppendOnlyJsonl()
        {
            await using (JournalWriter writer = CreateWriter())
            {
                await writer.AppendAsync(Envelope(1), flush: false);
                await writer.AppendAsync(Envelope(2), flush: false);
            }

            string segment = Directory.EnumerateFiles(directory, "journal-run-test-*.jsonl").Single();
            string[] lines = File.ReadAllLines(segment);
            Assert.Equal(2, lines.Length);
            Assert.All(lines, line => JsonDocument.Parse(line)); // each line is standalone JSON
        }

        [Fact]
        public async Task RejectsNonMonotonicSeq()
        {
            await using JournalWriter writer = CreateWriter();
            await writer.AppendAsync(Envelope(1), flush: false);
            await writer.AppendAsync(Envelope(2), flush: false);
            await Assert.ThrowsAsync<InvalidOperationException>(() => writer.AppendAsync(Envelope(2), flush: false).AsTask());
        }
    }
}
