//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Observability;

namespace Microsoft.SqlTools.Sts2.Runtime.Journaling
{
    /// <summary>
    /// Append-only write-ahead JSONL journal (SPEC §8.3). The coordinator appends an
    /// envelope BEFORE dispatching it; the journal order is the truth. Single-threaded
    /// by contract: only the coordinator pump writes. It is the privileged, first
    /// <see cref="IEnvelopeSink"/>: every other observer sees what the journal recorded.
    /// </summary>
    public sealed class JournalWriter : IEnvelopeSink, IAsyncDisposable
    {
        private static readonly JsonSerializerOptions ManifestJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private readonly string runId;
        private readonly JournalOptions options;
        private readonly JournalRunInfo runInfo;
        private readonly List<JournalSegment> closedSegments = [];

        private FileStream? segmentStream;
        private IncrementalHash? segmentHash;
        private int segmentIndex;
        private long segmentBytes;
        private long lastSeq;
        private DateTimeOffset lastFlush;

        /// <summary>Creates the journal directory, the first segment, and the manifest.</summary>
        public JournalWriter(string runId, JournalOptions options, JournalRunInfo runInfo)
        {
            ArgumentException.ThrowIfNullOrEmpty(runId);
            this.runId = runId;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.runInfo = runInfo ?? throw new ArgumentNullException(nameof(runInfo));

            Directory.CreateDirectory(options.Directory);
            lastFlush = options.TimeProvider.GetUtcNow();
            OpenNextSegment();
            WriteManifest();
        }

        /// <summary>Path of the manifest file.</summary>
        public string ManifestPath => Path.Combine(options.Directory, "journal-" + runId + ".manifest.json");

        /// <summary>Latest appended sequence number; 0 before the first append.</summary>
        public long LatestSeq => lastSeq;

        /// <summary>
        /// Appends one envelope. Call BEFORE dispatching it (write-ahead rule). Set
        /// <paramref name="flush"/> for terminal responses, query completion, fatal
        /// diagnostics, and lifecycle signals; otherwise flushing is bounded by
        /// <see cref="JournalOptions.FlushIntervalMs"/>.
        /// </summary>
        public async ValueTask AppendAsync(Sts2Envelope envelope, bool flush)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            ObjectDisposedException.ThrowIf(segmentStream is null, this);
            if (envelope.Seq <= lastSeq)
            {
                throw new InvalidOperationException(
                    $"Journal seq must be monotonic: got {envelope.Seq} after {lastSeq}.");
            }
            lastSeq = envelope.Seq;

            byte[] line = Encoding.UTF8.GetBytes(EnvelopeJsonCodec.SerializeLine(envelope) + "\n");
            await segmentStream!.WriteAsync(line).ConfigureAwait(false);
            segmentHash!.AppendData(line);
            segmentBytes += line.Length;

            DateTimeOffset now = options.TimeProvider.GetUtcNow();
            if (flush || (now - lastFlush).TotalMilliseconds >= options.FlushIntervalMs)
            {
                await segmentStream.FlushAsync().ConfigureAwait(false);
                lastFlush = now;
            }

            if (segmentBytes >= options.SegmentBytes)
            {
                await RotateAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// <see cref="IEnvelopeSink"/> entry point: identical to <see cref="AppendAsync"/>.
        /// The coordinator awaits this as the write-ahead primary before Core dispatch.
        /// </summary>
        ValueTask IEnvelopeSink.OnEnvelopeAsync(Sts2Envelope envelope, bool flush) => AppendAsync(envelope, flush);

        /// <summary>Flushes the active segment to disk.</summary>
        public async ValueTask FlushAsync()
        {
            if (segmentStream is not null)
            {
                await segmentStream.FlushAsync().ConfigureAwait(false);
                lastFlush = options.TimeProvider.GetUtcNow();
            }
        }

        /// <summary>Closes the active segment, finalizes hashes, and writes the manifest.</summary>
        public async ValueTask DisposeAsync()
        {
            if (segmentStream is null)
            {
                return;
            }
            await CloseCurrentSegmentAsync().ConfigureAwait(false);
            WriteManifest();
        }

        private async ValueTask RotateAsync()
        {
            await CloseCurrentSegmentAsync().ConfigureAwait(false);
            OpenNextSegment();
            WriteManifest();
        }

        private async ValueTask CloseCurrentSegmentAsync()
        {
            await segmentStream!.FlushAsync().ConfigureAwait(false);
            await segmentStream.DisposeAsync().ConfigureAwait(false);

            string sha256 = "sha256:" + Convert.ToHexStringLower(segmentHash!.GetHashAndReset());
            closedSegments.Add(new JournalSegment
            {
                FileName = SegmentFileName(segmentIndex),
                Bytes = segmentBytes,
                Sha256 = sha256,
                PreviousSegmentSha256 = closedSegments.Count > 0 ? closedSegments[^1].Sha256 : null,
            });
            segmentHash.Dispose();
            segmentStream = null;
            segmentHash = null;
        }

        private void OpenNextSegment()
        {
            segmentIndex++;
            segmentBytes = 0;
            segmentStream = new FileStream(
                Path.Combine(options.Directory, SegmentFileName(segmentIndex)),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            segmentHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        private string SegmentFileName(int index) =>
            string.Create(CultureInfo.InvariantCulture, $"journal-{runId}-{index:D4}.jsonl");

        private void WriteManifest()
        {
            var manifest = new JournalManifest
            {
                RunId = runId,
                ServiceVersion = runInfo.ServiceVersion,
                GitCommit = runInfo.GitCommit,
                ProcessId = Environment.ProcessId,
                Os = Environment.OSVersion.ToString(),
                RuntimeVersion = Environment.Version.ToString(),
                CommandLine = runInfo.CommandLine,
                Segments = closedSegments.ToArray(),
                WrittenAt = options.TimeProvider.GetUtcNow(),
            };
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, ManifestJsonOptions));
        }
    }
}
