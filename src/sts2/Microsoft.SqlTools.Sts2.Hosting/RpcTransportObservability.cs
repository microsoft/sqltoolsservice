//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using StreamJsonRpc.Reflection;

namespace Microsoft.SqlTools.Sts2.Hosting
{
    /// <summary>
    /// Aggregate, content-free counters for the StreamJsonRpc outbound edge. The counters
    /// deliberately separate JSON serialization, the handler's framing/copy body, and the
    /// asynchronous pipe flush so large-page ownership is visible without logging cells.
    /// </summary>
    internal sealed class RpcTransportStats
    {
        private readonly Action<string>? snapshotListener;
        private long messages;
        private long bytes;
        private long maxMessageBytes;
        private long bufferRequests;
        private long maxBufferSizeHint;
        private long serializeTicks;
        private long serializeAllocatedBytes;
        private long serializationFailures;
        private long writeCalls;
        private long writeTicks;
        private long writeAllocatedBytes;
        private long framingCopyTicks;
        private long framingCopyAllocatedBytes;
        private long writeFailures;
        private long flushCalls;
        private long flushTicks;
        private long flushFailures;
        private long rowMessages;
        private long rowBytes;
        private long rowSerializeTicks;
        private long rowSerializeAllocatedBytes;
        private long rowWriteTicks;
        private long rowWriteAllocatedBytes;
        private long rowFramingCopyTicks;
        private long rowFramingCopyAllocatedBytes;
        private long rowFlushTicks;

        internal RpcTransportStats(Action<string>? snapshotListener = null)
        {
            this.snapshotListener = snapshotListener;
        }

        internal void RecordSerialization(RpcSerializationMeasurement measurement)
        {
            Interlocked.Increment(ref messages);
            Interlocked.Add(ref bytes, measurement.Bytes);
            Interlocked.Add(ref bufferRequests, measurement.BufferRequests);
            Interlocked.Add(ref serializeTicks, measurement.ElapsedTicks);
            Interlocked.Add(ref serializeAllocatedBytes, measurement.AllocatedBytes);
            UpdateMaximum(ref maxMessageBytes, measurement.Bytes);
            UpdateMaximum(ref maxBufferSizeHint, measurement.MaxBufferSizeHint);
            if (!measurement.Succeeded)
            {
                Interlocked.Increment(ref serializationFailures);
            }

            if (measurement.IsRows)
            {
                Interlocked.Increment(ref rowMessages);
                Interlocked.Add(ref rowBytes, measurement.Bytes);
                Interlocked.Add(ref rowSerializeTicks, measurement.ElapsedTicks);
                Interlocked.Add(ref rowSerializeAllocatedBytes, measurement.AllocatedBytes);
            }
        }

        internal void RecordWrite(
            bool isRows,
            long elapsedTicks,
            long allocatedBytes,
            RpcSerializationMeasurement? serialization,
            bool succeeded)
        {
            Interlocked.Increment(ref writeCalls);
            Interlocked.Add(ref writeTicks, elapsedTicks);
            Interlocked.Add(ref writeAllocatedBytes, allocatedBytes);
            long framingTicks = serialization is { } measured
                ? Math.Max(0, elapsedTicks - measured.ElapsedTicks)
                : elapsedTicks;
            long framingAllocated = serialization is { } allocationMeasured
                ? Math.Max(0, allocatedBytes - allocationMeasured.AllocatedBytes)
                : allocatedBytes;
            Interlocked.Add(ref framingCopyTicks, framingTicks);
            Interlocked.Add(ref framingCopyAllocatedBytes, framingAllocated);
            if (!succeeded)
            {
                Interlocked.Increment(ref writeFailures);
            }

            if (isRows)
            {
                Interlocked.Add(ref rowWriteTicks, elapsedTicks);
                Interlocked.Add(ref rowWriteAllocatedBytes, allocatedBytes);
                Interlocked.Add(ref rowFramingCopyTicks, framingTicks);
                Interlocked.Add(ref rowFramingCopyAllocatedBytes, framingAllocated);
            }
        }

        internal void RecordFlush(bool isRows, long elapsedTicks, bool succeeded)
        {
            Interlocked.Increment(ref flushCalls);
            Interlocked.Add(ref flushTicks, elapsedTicks);
            if (!succeeded)
            {
                Interlocked.Increment(ref flushFailures);
            }
            if (isRows)
            {
                Interlocked.Add(ref rowFlushTicks, elapsedTicks);
            }
        }

        internal void EmitSnapshot()
        {
            if (snapshotListener is null)
            {
                return;
            }

            try
            {
                snapshotListener(SerializeSnapshot());
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or UnauthorizedAccessException)
            {
                // Diagnostics are best effort and must never affect the RPC transport.
            }
        }

        internal string SerializeSnapshot() => JsonSerializer.Serialize(new
        {
            schema = "sts2.rpc.transport.stats/1",
            messages = Volatile.Read(ref messages),
            bytes = Volatile.Read(ref bytes),
            maxMessageBytes = Volatile.Read(ref maxMessageBytes),
            bufferRequests = Volatile.Read(ref bufferRequests),
            maxBufferSizeHint = Volatile.Read(ref maxBufferSizeHint),
            serializeMsTotal = ToMilliseconds(Volatile.Read(ref serializeTicks)),
            serializeAllocatedBytes = Volatile.Read(ref serializeAllocatedBytes),
            serializationFailures = Volatile.Read(ref serializationFailures),
            writeCalls = Volatile.Read(ref writeCalls),
            writeMsTotal = ToMilliseconds(Volatile.Read(ref writeTicks)),
            writeAllocatedBytes = Volatile.Read(ref writeAllocatedBytes),
            framingCopyMsTotal = ToMilliseconds(Volatile.Read(ref framingCopyTicks)),
            framingCopyAllocatedBytes = Volatile.Read(ref framingCopyAllocatedBytes),
            writeFailures = Volatile.Read(ref writeFailures),
            flushCalls = Volatile.Read(ref flushCalls),
            flushMsTotal = ToMilliseconds(Volatile.Read(ref flushTicks)),
            flushFailures = Volatile.Read(ref flushFailures),
            rowMessages = Volatile.Read(ref rowMessages),
            rowBytes = Volatile.Read(ref rowBytes),
            rowSerializeMsTotal = ToMilliseconds(Volatile.Read(ref rowSerializeTicks)),
            rowSerializeAllocatedBytes = Volatile.Read(ref rowSerializeAllocatedBytes),
            rowWriteMsTotal = ToMilliseconds(Volatile.Read(ref rowWriteTicks)),
            rowWriteAllocatedBytes = Volatile.Read(ref rowWriteAllocatedBytes),
            rowFramingCopyMsTotal = ToMilliseconds(Volatile.Read(ref rowFramingCopyTicks)),
            rowFramingCopyAllocatedBytes = Volatile.Read(ref rowFramingCopyAllocatedBytes),
            rowFlushMsTotal = ToMilliseconds(Volatile.Read(ref rowFlushTicks)),
        });

        private static double ToMilliseconds(long elapsedTicks) => Math.Round(
            elapsedTicks * 1000d / Stopwatch.Frequency,
            3,
            MidpointRounding.AwayFromZero);

        private static void UpdateMaximum(ref long target, long candidate)
        {
            long current = Volatile.Read(ref target);
            while (candidate > current)
            {
                long observed = Interlocked.CompareExchange(ref target, candidate, current);
                if (observed == current)
                {
                    return;
                }
                current = observed;
            }
        }
    }

    internal readonly record struct RpcSerializationMeasurement(
        long Revision,
        long Bytes,
        long BufferRequests,
        long MaxBufferSizeHint,
        long ElapsedTicks,
        long AllocatedBytes,
        bool IsRows,
        bool Succeeded);

    /// <summary>
    /// Behavior-preserving decorator for <see cref="SystemTextJsonFormatter"/>. All optional
    /// StreamJsonRpc interfaces are forwarded so message factories, tracing, and the owner
    /// JsonRpc instance continue to reach the underlying formatter.
    /// </summary>
    internal sealed class MeasuredSystemTextJsonFormatter :
        IJsonRpcMessageTextFormatter,
        IJsonRpcInstanceContainer,
        IJsonRpcMessageFactory,
        IJsonRpcFormatterTracingCallbacks
    {
        private readonly SystemTextJsonFormatter inner;
        private readonly RpcTransportStats stats;
        private readonly CountingBufferWriter countingWriter = new();
        private long revision;

        internal MeasuredSystemTextJsonFormatter(SystemTextJsonFormatter inner, RpcTransportStats stats)
        {
            this.inner = inner;
            this.stats = stats;
        }

        internal long SerializationRevision => revision;

        internal RpcSerializationMeasurement LastSerialization { get; private set; }

        public Encoding Encoding
        {
            get => inner.Encoding;
            set => inner.Encoding = value;
        }

        public JsonRpcMessage Deserialize(ReadOnlySequence<byte> contentBuffer) => inner.Deserialize(contentBuffer);

        public JsonRpcMessage Deserialize(ReadOnlySequence<byte> contentBuffer, Encoding encoding) =>
            inner.Deserialize(contentBuffer, encoding);

        public void Serialize(IBufferWriter<byte> bufferWriter, JsonRpcMessage message)
        {
            countingWriter.Reset(bufferWriter);
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            bool succeeded = false;
            try
            {
                inner.Serialize(countingWriter, message);
                succeeded = true;
            }
            finally
            {
                var measurement = new RpcSerializationMeasurement(
                    ++revision,
                    countingWriter.BytesAdvanced,
                    countingWriter.BufferRequests,
                    countingWriter.MaxSizeHint,
                    Stopwatch.GetTimestamp() - started,
                    Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocationStart),
                    IsRowsMessage(message),
                    succeeded);
                LastSerialization = measurement;
                stats.RecordSerialization(measurement);
                countingWriter.Release();
            }
        }

#pragma warning disable CS0618 // Required by IJsonRpcMessageFormatter for compatibility.
        public object GetJsonText(JsonRpcMessage message) => inner.GetJsonText(message);
#pragma warning restore CS0618

        JsonRpc IJsonRpcInstanceContainer.Rpc
        {
            set => ((IJsonRpcInstanceContainer)inner).Rpc = value;
        }

        JsonRpcRequest IJsonRpcMessageFactory.CreateRequestMessage() =>
            ((IJsonRpcMessageFactory)inner).CreateRequestMessage();

        JsonRpcError IJsonRpcMessageFactory.CreateErrorMessage() =>
            ((IJsonRpcMessageFactory)inner).CreateErrorMessage();

        JsonRpcResult IJsonRpcMessageFactory.CreateResultMessage() =>
            ((IJsonRpcMessageFactory)inner).CreateResultMessage();

        void IJsonRpcFormatterTracingCallbacks.OnSerializationComplete(
            JsonRpcMessage message,
            ReadOnlySequence<byte> encodedMessage) =>
            ((IJsonRpcFormatterTracingCallbacks)inner).OnSerializationComplete(message, encodedMessage);

        internal static bool IsRowsMessage(JsonRpcMessage message) =>
            message is JsonRpcRequest request
            && string.Equals(request.Method, "v2/query.rows", StringComparison.Ordinal);

        private sealed class CountingBufferWriter : IBufferWriter<byte>
        {
            private IBufferWriter<byte>? innerWriter;

            internal long BytesAdvanced { get; private set; }

            internal long BufferRequests { get; private set; }

            internal int MaxSizeHint { get; private set; }

            public void Advance(int count)
            {
                IBufferWriter<byte> writer = innerWriter ?? throw new InvalidOperationException("The counter is not active.");
                writer.Advance(count);
                BytesAdvanced += count;
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                IBufferWriter<byte> writer = innerWriter ?? throw new InvalidOperationException("The counter is not active.");
                RecordRequest(sizeHint);
                return writer.GetMemory(sizeHint);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                IBufferWriter<byte> writer = innerWriter ?? throw new InvalidOperationException("The counter is not active.");
                RecordRequest(sizeHint);
                return writer.GetSpan(sizeHint);
            }

            internal void Reset(IBufferWriter<byte> writer)
            {
                if (innerWriter is not null)
                {
                    throw new InvalidOperationException("Concurrent formatter serialization is not supported by the RPC handler.");
                }
                innerWriter = writer;
                BytesAdvanced = 0;
                BufferRequests = 0;
                MaxSizeHint = 0;
            }

            internal void Release() => innerWriter = null;

            private void RecordRequest(int sizeHint)
            {
                BufferRequests++;
                MaxSizeHint = Math.Max(MaxSizeHint, sizeHint);
            }
        }
    }

    /// <summary>
    /// Measures the synchronous HeaderDelimited body separately from its asynchronous
    /// flush. StreamJsonRpc serializes and copies in <see cref="Write"/>, then flushes the
    /// pipe under its ordered send semaphore.
    /// </summary>
    internal sealed class MeasuredHeaderDelimitedMessageHandler : HeaderDelimitedMessageHandler
    {
        private readonly MeasuredSystemTextJsonFormatter measuredFormatter;
        private readonly RpcTransportStats stats;
        private bool pendingRowsFlush;
        private bool emitSnapshotAfterFlush;

        internal MeasuredHeaderDelimitedMessageHandler(
            Stream sendingStream,
            Stream receivingStream,
            MeasuredSystemTextJsonFormatter formatter,
            RpcTransportStats stats)
            : base(sendingStream, receivingStream, formatter)
        {
            measuredFormatter = formatter;
            this.stats = stats;
        }

        protected override void Write(JsonRpcMessage content, CancellationToken cancellationToken)
        {
            bool isRows = MeasuredSystemTextJsonFormatter.IsRowsMessage(content);
            long previousSerializationRevision = measuredFormatter.SerializationRevision;
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            bool succeeded = false;
            try
            {
                base.Write(content, cancellationToken);
                succeeded = true;
                pendingRowsFlush = isRows;
                emitSnapshotAfterFlush = IsQueryTerminal(content);
            }
            finally
            {
                RpcSerializationMeasurement latest = measuredFormatter.LastSerialization;
                RpcSerializationMeasurement? serialization = latest.Revision != previousSerializationRevision
                    ? latest
                    : null;
                stats.RecordWrite(
                    isRows,
                    Stopwatch.GetTimestamp() - started,
                    Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocationStart),
                    serialization,
                    succeeded);
            }
        }

        protected override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            bool isRows = pendingRowsFlush;
            bool emitSnapshot = emitSnapshotAfterFlush;
            pendingRowsFlush = false;
            emitSnapshotAfterFlush = false;
            long started = Stopwatch.GetTimestamp();
            bool succeeded = false;
            try
            {
                await base.FlushAsync(cancellationToken).ConfigureAwait(false);
                succeeded = true;
            }
            finally
            {
                stats.RecordFlush(isRows, Stopwatch.GetTimestamp() - started, succeeded);
                if (emitSnapshot)
                {
                    stats.EmitSnapshot();
                }
            }
        }

        private static bool IsQueryTerminal(JsonRpcMessage message) =>
            message is JsonRpcRequest request
            && string.Equals(request.Method, "v2/query.complete", StringComparison.Ordinal);
    }

    /// <summary>Best-effort private file for cumulative content-free RPC transport snapshots.</summary>
    internal sealed class RpcTransportDiagnostics : IDisposable
    {
        internal const string FileName = "sts2-rpc-transport.log";
        internal const string Marker = "[rpcTransportStats] ";

        private readonly Lock syncObject = new();
        private StreamWriter? writer;

        private RpcTransportDiagnostics(StreamWriter writer)
        {
            this.writer = writer;
        }

        internal static RpcTransportDiagnostics? TryCreate(string journalDirectory)
        {
            try
            {
                Directory.CreateDirectory(journalDirectory);
                return new RpcTransportDiagnostics(new StreamWriter(
                    Path.Combine(journalDirectory, FileName),
                    append: false,
                    Encoding.UTF8)
                {
                    AutoFlush = true,
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        internal void WriteSnapshot(string snapshot)
        {
            lock (syncObject)
            {
                writer?.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{DateTimeOffset.UtcNow:O} {Marker}{snapshot}"));
            }
        }

        public void Dispose()
        {
            lock (syncObject)
            {
                writer?.Dispose();
                writer = null;
            }
        }
    }
}
