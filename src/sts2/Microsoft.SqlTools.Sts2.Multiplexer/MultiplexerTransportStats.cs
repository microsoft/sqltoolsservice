//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    /// <summary>
    /// Process-local, content-free counters for the virtual-channel to stdout path.
    /// Recording is allocation-free; JSON snapshots are materialized only at query
    /// terminals and lifecycle checkpoints.
    /// </summary>
    internal sealed class MultiplexerTransportStats
    {
        private const long LargeObjectThresholdBytes = 85_000;

        private static readonly JsonSerializerOptions SnapshotSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ChannelStats legacy = new();
        private readonly ChannelStats sts2 = new();

        internal ChannelStats For(ChannelKind channel) => channel == ChannelKind.Sts2 ? sts2 : legacy;

        internal string SerializeSnapshot() => JsonSerializer.Serialize(
            new TransportSnapshot(
                Schema: "sts2.transport.stats/1",
                Legacy: legacy.Snapshot(),
                Sts2: sts2.Snapshot()),
            SnapshotSerializerOptions);

        internal sealed class ChannelStats
        {
            private long readCalls;
            private long maxBufferedBytes;
            private long headerParseCalls;
            private long headerParseTicks;
            private long headerParseAllocatedBytes;
            private long partialFrameWaits;
            private long outboundFrames;
            private long outboundFrameBytes;
            private long maxOutboundFrameBytes;
            private long largeObjectFrames;
            private long pipeSegments;
            private long singleSegmentFrames;
            private long multiSegmentFrames;
            private long directFrames;
            private long directBytes;
            private long materializedFrames;
            private long materializedBytes;
            private long materializeTicks;
            private long materializeAllocatedBytes;
            private long reusableFrames;
            private long reusableBytes;
            private long reusableBufferAllocations;
            private long reusableBufferCapacityBytes;
            private long pooledFrames;
            private long pooledBytes;
            private long pooledClearBytes;
            private long pooledClearTicks;
            private long bufferClearBytes;
            private long bufferClearTicks;
            private long inspectTicks;
            private long inspectAllocatedBytes;
            private long inspectParseFailures;
            private long rewrittenFrames;
            private long stdoutLockWaitTicks;
            private long stdoutWriteCalls;
            private long stdoutWriteBytes;
            private long stdoutWriteTicks;
            private long stdoutFlushCalls;
            private long stdoutFlushTicks;

            internal void RecordRead(long bufferedBytes)
            {
                Interlocked.Increment(ref readCalls);
                SetMax(ref maxBufferedBytes, bufferedBytes);
            }

            internal void RecordHeaderParse(long elapsedTicks, long allocatedBytes, bool needsMoreData)
            {
                Interlocked.Increment(ref headerParseCalls);
                Interlocked.Add(ref headerParseTicks, elapsedTicks);
                Interlocked.Add(ref headerParseAllocatedBytes, allocatedBytes);
                if (needsMoreData)
                {
                    Interlocked.Increment(ref partialFrameWaits);
                }
            }

            internal void RecordFrame(long frameBytes, int segments)
            {
                Interlocked.Increment(ref outboundFrames);
                Interlocked.Add(ref outboundFrameBytes, frameBytes);
                SetMax(ref maxOutboundFrameBytes, frameBytes);
                if (frameBytes >= LargeObjectThresholdBytes)
                {
                    Interlocked.Increment(ref largeObjectFrames);
                }

                Interlocked.Add(ref pipeSegments, segments);
                if (segments == 1)
                {
                    Interlocked.Increment(ref singleSegmentFrames);
                }
                else
                {
                    Interlocked.Increment(ref multiSegmentFrames);
                }
            }

            internal void RecordMaterialization(long bytes, long elapsedTicks, long allocatedBytes)
            {
                Interlocked.Increment(ref materializedFrames);
                Interlocked.Add(ref materializedBytes, bytes);
                Interlocked.Add(ref materializeTicks, elapsedTicks);
                Interlocked.Add(ref materializeAllocatedBytes, allocatedBytes);
            }

            internal void RecordReusable(long bytes)
            {
                Interlocked.Increment(ref reusableFrames);
                Interlocked.Add(ref reusableBytes, bytes);
            }

            internal void RecordReusableBufferAllocation(long capacityBytes)
            {
                Interlocked.Increment(ref reusableBufferAllocations);
                SetMax(ref reusableBufferCapacityBytes, capacityBytes);
            }

            internal void RecordPooled(long bytes)
            {
                Interlocked.Increment(ref pooledFrames);
                Interlocked.Add(ref pooledBytes, bytes);
            }

            internal void RecordDirect(long bytes)
            {
                Interlocked.Increment(ref directFrames);
                Interlocked.Add(ref directBytes, bytes);
            }

            internal void RecordPooledClear(long bytes, long elapsedTicks)
            {
                Interlocked.Add(ref pooledClearBytes, bytes);
                Interlocked.Add(ref pooledClearTicks, elapsedTicks);
            }

            internal void RecordBufferClear(long bytes, long elapsedTicks)
            {
                Interlocked.Add(ref bufferClearBytes, bytes);
                Interlocked.Add(ref bufferClearTicks, elapsedTicks);
            }

            internal void RecordInspection(long elapsedTicks, long allocatedBytes, bool parseFailed)
            {
                Interlocked.Add(ref inspectTicks, elapsedTicks);
                Interlocked.Add(ref inspectAllocatedBytes, allocatedBytes);
                if (parseFailed)
                {
                    Interlocked.Increment(ref inspectParseFailures);
                }
            }

            internal void RecordRewrite() => Interlocked.Increment(ref rewrittenFrames);

            internal void RecordStdoutLockWait(long elapsedTicks) =>
                Interlocked.Add(ref stdoutLockWaitTicks, elapsedTicks);

            internal void RecordStdoutWrite(long bytes, long elapsedTicks)
            {
                Interlocked.Increment(ref stdoutWriteCalls);
                Interlocked.Add(ref stdoutWriteBytes, bytes);
                Interlocked.Add(ref stdoutWriteTicks, elapsedTicks);
            }

            internal void RecordStdoutFlush(long elapsedTicks)
            {
                Interlocked.Increment(ref stdoutFlushCalls);
                Interlocked.Add(ref stdoutFlushTicks, elapsedTicks);
            }

            internal ChannelSnapshot Snapshot() => new(
                ReadCalls: Interlocked.Read(ref readCalls),
                MaxBufferedBytes: Interlocked.Read(ref maxBufferedBytes),
                HeaderParseCalls: Interlocked.Read(ref headerParseCalls),
                HeaderParseMsTotal: ToMilliseconds(Interlocked.Read(ref headerParseTicks)),
                HeaderParseAllocatedBytes: Interlocked.Read(ref headerParseAllocatedBytes),
                PartialFrameWaits: Interlocked.Read(ref partialFrameWaits),
                OutboundFrames: Interlocked.Read(ref outboundFrames),
                OutboundFrameBytes: Interlocked.Read(ref outboundFrameBytes),
                MaxOutboundFrameBytes: Interlocked.Read(ref maxOutboundFrameBytes),
                LargeObjectFrames: Interlocked.Read(ref largeObjectFrames),
                PipeSegments: Interlocked.Read(ref pipeSegments),
                SingleSegmentFrames: Interlocked.Read(ref singleSegmentFrames),
                MultiSegmentFrames: Interlocked.Read(ref multiSegmentFrames),
                DirectFrames: Interlocked.Read(ref directFrames),
                DirectBytes: Interlocked.Read(ref directBytes),
                MaterializedFrames: Interlocked.Read(ref materializedFrames),
                MaterializedBytes: Interlocked.Read(ref materializedBytes),
                MaterializeMsTotal: ToMilliseconds(Interlocked.Read(ref materializeTicks)),
                MaterializeAllocatedBytes: Interlocked.Read(ref materializeAllocatedBytes),
                ReusableFrames: Interlocked.Read(ref reusableFrames),
                ReusableBytes: Interlocked.Read(ref reusableBytes),
                ReusableBufferAllocations: Interlocked.Read(ref reusableBufferAllocations),
                ReusableBufferCapacityBytes: Interlocked.Read(ref reusableBufferCapacityBytes),
                PooledFrames: Interlocked.Read(ref pooledFrames),
                PooledBytes: Interlocked.Read(ref pooledBytes),
                PooledClearBytes: Interlocked.Read(ref pooledClearBytes),
                PooledClearMsTotal: ToMilliseconds(Interlocked.Read(ref pooledClearTicks)),
                BufferClearBytes: Interlocked.Read(ref bufferClearBytes),
                BufferClearMsTotal: ToMilliseconds(Interlocked.Read(ref bufferClearTicks)),
                InspectMsTotal: ToMilliseconds(Interlocked.Read(ref inspectTicks)),
                InspectAllocatedBytes: Interlocked.Read(ref inspectAllocatedBytes),
                InspectParseFailures: Interlocked.Read(ref inspectParseFailures),
                RewrittenFrames: Interlocked.Read(ref rewrittenFrames),
                StdoutLockWaitMsTotal: ToMilliseconds(Interlocked.Read(ref stdoutLockWaitTicks)),
                StdoutWriteCalls: Interlocked.Read(ref stdoutWriteCalls),
                StdoutWriteBytes: Interlocked.Read(ref stdoutWriteBytes),
                StdoutWriteMsTotal: ToMilliseconds(Interlocked.Read(ref stdoutWriteTicks)),
                StdoutFlushCalls: Interlocked.Read(ref stdoutFlushCalls),
                StdoutFlushMsTotal: ToMilliseconds(Interlocked.Read(ref stdoutFlushTicks)));

            private static double ToMilliseconds(long ticks) =>
                Math.Round(ticks * 1000d / Stopwatch.Frequency, 3, MidpointRounding.AwayFromZero);

            private static void SetMax(ref long target, long value)
            {
                long observed = Volatile.Read(ref target);
                while (value > observed)
                {
                    long prior = Interlocked.CompareExchange(ref target, value, observed);
                    if (prior == observed)
                    {
                        return;
                    }
                    observed = prior;
                }
            }
        }

        private sealed record TransportSnapshot(string Schema, ChannelSnapshot Legacy, ChannelSnapshot Sts2);

        internal sealed record ChannelSnapshot(
            long ReadCalls,
            long MaxBufferedBytes,
            long HeaderParseCalls,
            double HeaderParseMsTotal,
            long HeaderParseAllocatedBytes,
            long PartialFrameWaits,
            long OutboundFrames,
            long OutboundFrameBytes,
            long MaxOutboundFrameBytes,
            long LargeObjectFrames,
            long PipeSegments,
            long SingleSegmentFrames,
            long MultiSegmentFrames,
            long DirectFrames,
            long DirectBytes,
            long MaterializedFrames,
            long MaterializedBytes,
            double MaterializeMsTotal,
            long MaterializeAllocatedBytes,
            long ReusableFrames,
            long ReusableBytes,
            long ReusableBufferAllocations,
            long ReusableBufferCapacityBytes,
            long PooledFrames,
            long PooledBytes,
            long PooledClearBytes,
            double PooledClearMsTotal,
            long BufferClearBytes,
            double BufferClearMsTotal,
            double InspectMsTotal,
            long InspectAllocatedBytes,
            long InspectParseFailures,
            long RewrittenFrames,
            double StdoutLockWaitMsTotal,
            long StdoutWriteCalls,
            long StdoutWriteBytes,
            double StdoutWriteMsTotal,
            long StdoutFlushCalls,
            double StdoutFlushMsTotal);
    }
}
