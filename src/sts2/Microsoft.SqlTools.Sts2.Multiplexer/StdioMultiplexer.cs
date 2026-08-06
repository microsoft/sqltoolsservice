//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    /// <summary>
    /// Owns the real stdio pair when STS2 is enabled (SPEC §6). Routes inbound frames to
    /// the legacy or STS2 virtual channel by top-level inspection only, rewrites outbound
    /// server-initiated request ids to globally unique values, mirrors <c>shutdown</c>/<c>exit</c>
    /// lifecycle to STS2, serializes all stdout writes through a single writer, and
    /// contains STS2 crashes so legacy traffic continues.
    /// </summary>
    public sealed class StdioMultiplexer : IAsyncDisposable
    {
        private const int JsonRpcInternalErrorCode = -32603;
        private const int InitialReusableFrameBufferBytes = 64 * 1024;
        private const int MaxReusableFrameBufferBytes = 1024 * 1024;

        private readonly Stream realInput;
        private readonly Stream realOutput;
        private readonly MultiplexerOptions options;
        private readonly OutboundRequestIdTable idTable;
        private readonly MultiplexerTransportStats transportStats = new();
        private readonly SemaphoreSlim stdoutLock = new(1, 1);
        private readonly CancellationTokenSource cts = new();

        private readonly Pipe legacyInbound = new();
        private readonly Pipe legacyOutbound = new();
        private readonly Pipe sts2Inbound = new();
        private readonly Pipe sts2Outbound = new();

        private ISts2LifecycleSink? lifecycleSink;
        private Task completion = Task.CompletedTask;
        private int started;
        private int sts2Dead;
        private long transportStatsRevision;
        private long lastEmittedTransportStatsRevision = -1;

        /// <summary>Creates a multiplexer over the real stdio streams. Call <see cref="Start"/> to begin pumping.</summary>
        public StdioMultiplexer(Stream realInput, Stream realOutput, MultiplexerOptions? options = null)
        {
            this.realInput = realInput ?? throw new ArgumentNullException(nameof(realInput));
            this.realOutput = realOutput ?? throw new ArgumentNullException(nameof(realOutput));
            this.options = options ?? new MultiplexerOptions();
            this.idTable = new OutboundRequestIdTable(this.options.TimeProvider, this.options.OutboundRequestIdTtl);

            LegacyInput = legacyInbound.Reader.AsStream();
            LegacyOutput = legacyOutbound.Writer.AsStream();
            Sts2Input = sts2Inbound.Reader.AsStream();
            Sts2Output = sts2Outbound.Writer.AsStream();
        }

        /// <summary>Stream the legacy service host reads its inbound messages from.</summary>
        public Stream LegacyInput { get; }

        /// <summary>Stream the legacy service host writes its outbound messages to.</summary>
        public Stream LegacyOutput { get; }

        /// <summary>Stream the STS2 host reads its inbound messages from.</summary>
        public Stream Sts2Input { get; }

        /// <summary>Stream the STS2 host writes its outbound messages to.</summary>
        public Stream Sts2Output { get; }

        /// <summary>
        /// Direct STS2 inbound endpoint for the in-process composition root. Keeping this
        /// internal avoids expanding the multiplexer API while bypassing a redundant
        /// PipeReader-to-Stream-to-PipeReader adapter in the RPC transport.
        /// </summary>
        internal PipeReader Sts2InputReader => sts2Inbound.Reader;

        /// <summary>
        /// Direct STS2 outbound endpoint for the in-process composition root. The
        /// multiplexer remains the reader/owner and therefore retains backpressure.
        /// </summary>
        internal PipeWriter Sts2OutputWriter => sts2Outbound.Writer;

        /// <summary>Completes when all pump loops have stopped (stdin EOF or disposal).</summary>
        public Task Completion => completion;

        /// <summary>Starts the inbound and outbound pump loops. May be called once.</summary>
        public void Start(ISts2LifecycleSink? lifecycleSink = null)
        {
            if (Interlocked.Exchange(ref started, 1) == 1)
            {
                throw new InvalidOperationException("The multiplexer is already started.");
            }
            this.lifecycleSink = lifecycleSink;

            CancellationToken ct = cts.Token;
            completion = Task.WhenAll(
                Task.Run(() => GuardPumpAsync("inbound", RunInboundPumpAsync(ct)), ct),
                Task.Run(() => GuardPumpAsync("legacy-outbound", RunOutboundPumpAsync(ChannelKind.Legacy, legacyOutbound.Reader, ct)), ct),
                Task.Run(() => GuardPumpAsync("sts2-outbound", RunOutboundPumpAsync(ChannelKind.Sts2, sts2Outbound.Reader, ct)), ct));
        }

        /// <summary>
        /// Marks the STS2 channel dead (SPEC §6.5): emits one <c>v2/fatal</c> notification,
        /// drops STS2 id-table entries, and causes future <c>v2/*</c> requests to receive a
        /// synthesized <c>Sts2.Unavailable</c> error. Legacy traffic continues. Idempotent.
        /// </summary>
        public void MarkSts2Dead(string reason, string? journalPath = null)
        {
            if (Interlocked.Exchange(ref sts2Dead, 1) == 1)
            {
                return;
            }

            Diagnostic(MultiplexerDiagnosticCodes.Sts2Dead, "STS2 channel marked dead: " + reason);
            idTable.DropChannel(ChannelKind.Sts2);
            sts2Inbound.Writer.Complete();

            var fatal = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "v2/fatal",
                ["params"] = new JsonObject
                {
                    ["summary"] = reason,
                    ["journalPath"] = journalPath,
                },
            };
            byte[] frame = JsonRpcFraming.BuildFrame(JsonSerializer.SerializeToUtf8Bytes(fatal));
            _ = Task.Run(async () =>
            {
                try
                {
                    await WriteStdoutAsync(ChannelKind.Sts2, frame, cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
                {
                    // Transport already gone; nothing to notify.
                }
            });
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await cts.CancelAsync().ConfigureAwait(false);
            legacyInbound.Writer.Complete();
            legacyOutbound.Reader.Complete();
            sts2Outbound.Reader.Complete();
            if (sts2Dead == 0)
            {
                sts2Inbound.Writer.Complete();
            }
            try
            {
                await completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal disposal path.
            }
            finally
            {
                EmitTransportStats();
                cts.Dispose();
            }
        }

        private async Task GuardPumpAsync(string pumpName, Task pump)
        {
            try
            {
                await pump.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Disposal.
            }
            catch (Exception ex)
            {
                Diagnostic(MultiplexerDiagnosticCodes.PumpFailure, pumpName + " pump failed: " + ex.Message);
                if (pumpName == "sts2-outbound")
                {
                    MarkSts2Dead("sts2 outbound pump failed: " + ex.Message);
                    return;
                }
                throw;
            }
        }

        // ---------------- inbound: real stdin -> virtual channels ----------------

        private async Task RunInboundPumpAsync(CancellationToken ct)
        {
            PipeReader reader = PipeReader.Create(realInput);
            long passthroughRemaining = 0;
            bool degraded = false;
            try
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync(ct).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (buffer.Length > 0)
                    {
                        if (degraded)
                        {
                            await WriteToChannelAsync(legacyInbound.Writer, buffer.ToArray(), ct).ConfigureAwait(false);
                            buffer = buffer.Slice(buffer.End);
                            break;
                        }

                        if (passthroughRemaining > 0)
                        {
                            long take = Math.Min(passthroughRemaining, buffer.Length);
                            await WriteToChannelAsync(legacyInbound.Writer, buffer.Slice(0, take).ToArray(), ct).ConfigureAwait(false);
                            buffer = buffer.Slice(take);
                            passthroughRemaining -= take;
                            continue;
                        }

                        FrameHeaderStatus status = JsonRpcFraming.TryParseHeader(buffer, options.MaxFrameBytes, out int headerLength, out long contentLength);
                        if (status == FrameHeaderStatus.NeedMoreData)
                        {
                            break;
                        }
                        if (status == FrameHeaderStatus.MalformedHeader)
                        {
                            Diagnostic(MultiplexerDiagnosticCodes.MalformedHeader, "Unparseable header block; degrading to legacy passthrough.");
                            degraded = true;
                            continue;
                        }
                        if (status == FrameHeaderStatus.OversizedFrame)
                        {
                            Diagnostic(MultiplexerDiagnosticCodes.OversizedFrame,
                                $"Frame of {contentLength} bytes exceeds maxFrameBytes={options.MaxFrameBytes}; forwarding raw to legacy.");
                            passthroughRemaining = headerLength + contentLength;
                            continue;
                        }

                        long frameLength = headerLength + contentLength;
                        if (buffer.Length < frameLength)
                        {
                            break;
                        }

                        byte[] frameBytes = buffer.Slice(0, frameLength).ToArray();
                        await ProcessInboundFrameAsync(frameBytes, headerLength, ct).ConfigureAwait(false);
                        buffer = buffer.Slice(frameLength);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                legacyInbound.Writer.Complete();
                if (Interlocked.CompareExchange(ref sts2Dead, 0, 0) == 0)
                {
                    sts2Inbound.Writer.Complete();
                }
            }
        }

        private async Task ProcessInboundFrameAsync(byte[] frameBytes, int headerLength, CancellationToken ct)
        {
            JsonRpcMessageInfo info = JsonRpcMessageInspector.Inspect(frameBytes.AsSpan(headerLength));

            if (info.ParseFailed)
            {
                Diagnostic(MultiplexerDiagnosticCodes.MalformedPayload, "Unparseable JSON payload forwarded raw to legacy.");
                await WriteToChannelAsync(legacyInbound.Writer, frameBytes, ct).ConfigureAwait(false);
                return;
            }

            if (info.Method is not null)
            {
                switch (info.Method)
                {
                    case "shutdown":
                        // Raw frame goes to legacy only; STS2 sees a mirrored signal and
                        // can never produce a duplicate JSON-RPC response (I14). The flush
                        // wait is bounded and happens BEFORE legacy can act: this repo's
                        // legacy host exits the process from its shutdown handler (RF-0011).
                        await WaitForLifecycleFlushAsync(static s => s.OnShutdownAsync(), "shutdown", ct).ConfigureAwait(false);
                        await WriteToChannelAsync(legacyInbound.Writer, frameBytes, ct).ConfigureAwait(false);
                        return;

                    case "exit":
                        // Flush STS2 journals (bounded) BEFORE legacy can act on exit and
                        // terminate the process; otherwise the journal tail is lost.
                        await WaitForLifecycleFlushAsync(static s => s.OnExitAsync(), "exit", ct).ConfigureAwait(false);
                        idTable.Clear();
                        await WriteToChannelAsync(legacyInbound.Writer, frameBytes, ct).ConfigureAwait(false);
                        return;

                    default:
                        if (info.Method.StartsWith("v2/", StringComparison.Ordinal))
                        {
                            await DeliverToSts2Async(frameBytes, headerLength, info, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            await WriteToChannelAsync(legacyInbound.Writer, frameBytes, ct).ConfigureAwait(false);
                        }
                        return;
                }
            }

            // No method: a response to a server-initiated request. Route by the rewrite table.
            if (info.HasId && !info.IdIsNull && info.IdRawJson is not null
                && TryGetPublicId(info.IdRawJson, out string publicId)
                && idTable.TryConsume(publicId, out ChannelKind channel, out string originalIdRawJson))
            {
                byte[] restored = ReplaceId(frameBytes.AsSpan(headerLength), originalIdRawJson, asRawJson: true);
                PipeWriter target = channel == ChannelKind.Sts2 ? sts2Inbound.Writer : legacyInbound.Writer;
                if (channel == ChannelKind.Sts2 && sts2Dead == 1)
                {
                    Diagnostic(MultiplexerDiagnosticCodes.Sts2Dead, "Dropped response to dead STS2 channel.");
                    return;
                }
                await WriteToChannelAsync(target, JsonRpcFraming.BuildFrame(restored), ct).ConfigureAwait(false);
                return;
            }

            Diagnostic(MultiplexerDiagnosticCodes.UnknownResponseId,
                "Response with unknown id " + (info.IdRawJson ?? "<none>") + " routed to legacy.");
            await WriteToChannelAsync(legacyInbound.Writer, frameBytes, ct).ConfigureAwait(false);
        }

        private async Task DeliverToSts2Async(byte[] frameBytes, int headerLength, JsonRpcMessageInfo info, CancellationToken ct)
        {
            if (sts2Dead == 1)
            {
                if (info.HasId && !info.IdIsNull && info.IdRawJson is not null)
                {
                    await SynthesizeUnavailableErrorAsync(info.IdRawJson, ct).ConfigureAwait(false);
                }
                else
                {
                    Diagnostic(MultiplexerDiagnosticCodes.Sts2Dead, "Dropped v2 notification " + info.Method + " for dead STS2 channel.");
                }
                return;
            }
            await WriteToChannelAsync(sts2Inbound.Writer, frameBytes, ct).ConfigureAwait(false);
        }

        private async Task WaitForLifecycleFlushAsync(Func<ISts2LifecycleSink, Task> flushSelector, string signal, CancellationToken ct)
        {
            if (lifecycleSink is null || sts2Dead == 1)
            {
                EmitTransportStats();
                return;
            }
            try
            {
                Task flush = flushSelector(lifecycleSink);
                Task timeout = Task.Delay(TimeSpan.FromMilliseconds(options.ExitFlushMilliseconds), ct);
                Task winner = await Task.WhenAny(flush, timeout).ConfigureAwait(false);
                if (winner == timeout)
                {
                    Diagnostic(MultiplexerDiagnosticCodes.LifecycleSinkError,
                        $"STS2 {signal} flush did not complete within {options.ExitFlushMilliseconds}ms; forwarding {signal} anyway.");
                }
                else
                {
                    await flush.ConfigureAwait(false); // propagate flush faults to the catch below
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Diagnostic(MultiplexerDiagnosticCodes.LifecycleSinkError, "Lifecycle sink threw on " + signal + ": " + ex.Message);
            }
            finally
            {
                // This repository's legacy shutdown path may terminate the process before
                // DisposeAsync runs. Persist the aggregate before that signal is forwarded.
                EmitTransportStats();
            }
        }

        // ---------------- outbound: virtual channels -> real stdout ----------------

        private async Task RunOutboundPumpAsync(ChannelKind channel, PipeReader reader, CancellationToken ct)
        {
            MultiplexerTransportStats.ChannelStats stats = transportStats.For(channel);
            byte[]? reusableFrameBuffer = null;
            while (true)
            {
                ReadResult result = await reader.ReadAsync(ct).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                stats.RecordRead(buffer.Length);

                while (buffer.Length > 0)
                {
                    long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    long parseStarted = Stopwatch.GetTimestamp();
                    FrameHeaderStatus status = JsonRpcFraming.TryParseHeader(buffer, options.MaxFrameBytes, out int headerLength, out long contentLength);
                    stats.RecordHeaderParse(
                        Stopwatch.GetTimestamp() - parseStarted,
                        GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
                        status is FrameHeaderStatus.NeedMoreData);
                    if (status is FrameHeaderStatus.NeedMoreData)
                    {
                        break;
                    }
                    if (status is FrameHeaderStatus.MalformedHeader or FrameHeaderStatus.OversizedFrame)
                    {
                        // Our own services must frame correctly; treat as fatal for the channel.
                        throw new InvalidDataException($"The {channel} service wrote an invalid {status} frame header.");
                    }

                    long frameLength = headerLength + contentLength;
                    if (status is FrameHeaderStatus.HeaderParsed && buffer.Length < frameLength)
                    {
                        break;
                    }

                    ReadOnlySequence<byte> frame = buffer.Slice(0, frameLength);
                    int segments = CountSegments(frame);
                    stats.RecordFrame(frameLength, segments);
                    Interlocked.Increment(ref transportStatsRevision);
                    int frameByteLength = checked((int)frameLength);
                    if (frame.IsSingleSegment)
                    {
                        stats.RecordDirect(frameLength);
                        await ProcessOutboundFrameAsync(channel, frame.First, headerLength, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                        long copyStarted = Stopwatch.GetTimestamp();
                        bool useReusableBuffer = frameByteLength <= MaxReusableFrameBufferBytes;
                        byte[] contiguousBuffer;
                        if (useReusableBuffer)
                        {
                            if (reusableFrameBuffer is null || reusableFrameBuffer.Length < frameByteLength)
                            {
                                reusableFrameBuffer = GC.AllocateUninitializedArray<byte>(
                                    GetReusableFrameBufferCapacity(frameByteLength));
                                stats.RecordReusableBufferAllocation(reusableFrameBuffer.Length);
                            }

                            contiguousBuffer = reusableFrameBuffer;
                            stats.RecordReusable(frameLength);
                        }
                        else
                        {
                            contiguousBuffer = ArrayPool<byte>.Shared.Rent(frameByteLength);
                            stats.RecordPooled(frameLength);
                        }

                        frame.CopyTo(contiguousBuffer.AsSpan(0, frameByteLength));
                        stats.RecordMaterialization(
                            frameLength,
                            Stopwatch.GetTimestamp() - copyStarted,
                            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
                        try
                        {
                            await ProcessOutboundFrameAsync(
                                channel,
                                contiguousBuffer.AsMemory(0, frameByteLength),
                                headerLength,
                                ct).ConfigureAwait(false);
                        }
                        finally
                        {
                            // Row pages can contain sensitive database values. Remove every
                            // used payload before retaining or pooling its transport buffer.
                            long clearStarted = Stopwatch.GetTimestamp();
                            CryptographicOperations.ZeroMemory(contiguousBuffer.AsSpan(0, frameByteLength));
                            long clearTicks = Stopwatch.GetTimestamp() - clearStarted;
                            stats.RecordBufferClear(frameLength, clearTicks);
                            if (!useReusableBuffer)
                            {
                                stats.RecordPooledClear(frameLength, clearTicks);
                                ArrayPool<byte>.Shared.Return(contiguousBuffer);
                            }
                        }
                    }
                    buffer = buffer.Slice(frameLength);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }

        private async Task ProcessOutboundFrameAsync(ChannelKind channel, ReadOnlyMemory<byte> frameBytes, int headerLength, CancellationToken ct)
        {
            MultiplexerTransportStats.ChannelStats stats = transportStats.For(channel);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long inspectStarted = Stopwatch.GetTimestamp();
            JsonRpcMessageInfo info = JsonRpcMessageInspector.InspectOutbound(frameBytes.Span[headerLength..]);
            stats.RecordInspection(
                Stopwatch.GetTimestamp() - inspectStarted,
                GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
                info.ParseFailed);

            // Server-initiated request: rewrite the id so legacy and STS2 ids can never
            // collide on the shared transport (SPEC §6.3, I13).
            if (!info.ParseFailed && info.Method is not null && info.HasId && !info.IdIsNull && info.IdRawJson is not null)
            {
                stats.RecordRewrite();
                string publicId = idTable.Register(channel, info.IdRawJson);
                byte[] rewritten = ReplaceId(frameBytes.Span[headerLength..], publicId, asRawJson: false);
                await WriteStdoutAsync(channel, JsonRpcFraming.BuildFrame(rewritten), ct).ConfigureAwait(false);
                return;
            }

            // Responses and notifications pass through unchanged (SPEC §6.3).
            await WriteStdoutAsync(channel, frameBytes, ct).ConfigureAwait(false);
            if (channel == ChannelKind.Sts2 && string.Equals(info.Method, "v2/query.complete", StringComparison.Ordinal))
            {
                // The host can be terminated with its parent before normal disposal. A
                // query terminal is the cheapest reliable checkpoint for live diagnostics.
                EmitTransportStats();
            }
        }

        // ---------------- helpers ----------------

        private static int GetReusableFrameBufferCapacity(int requiredBytes)
        {
            int capacity = InitialReusableFrameBufferBytes;
            while (capacity < requiredBytes && capacity < MaxReusableFrameBufferBytes)
            {
                capacity *= 2;
            }

            return capacity;
        }

        private static bool TryGetPublicId(string idRawJson, out string publicId)
        {
            publicId = string.Empty;
            // Public ids are always JSON strings of the form "sts2mux-N".
            if (idRawJson.Length >= 2 && idRawJson[0] == '"' && idRawJson[^1] == '"')
            {
                string value = idRawJson[1..^1];
                if (value.StartsWith("sts2mux-", StringComparison.Ordinal))
                {
                    publicId = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the payload with its top-level id replaced. Full deserialization is
        /// acceptable here: this runs only for server-initiated requests and their
        /// responses, never on the routing hot path.
        /// </summary>
        private static byte[] ReplaceId(ReadOnlySpan<byte> payload, string newId, bool asRawJson)
        {
            var readerForNode = new Utf8JsonReader(payload);
            JsonNode node = JsonNode.Parse(ref readerForNode)!;
            node["id"] = asRawJson ? JsonNode.Parse(newId) : JsonValue.Create(newId);
            return JsonSerializer.SerializeToUtf8Bytes(node);
        }

        private async Task SynthesizeUnavailableErrorAsync(string idRawJson, CancellationToken ct)
        {
            var error = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = JsonNode.Parse(idRawJson),
                ["error"] = new JsonObject
                {
                    ["code"] = JsonRpcInternalErrorCode,
                    ["message"] = "STS2 is unavailable.",
                    ["data"] = new JsonObject
                    {
                        ["code"] = "Sts2.Unavailable",
                        ["retryable"] = false,
                    },
                },
            };
            Diagnostic(MultiplexerDiagnosticCodes.Sts2Dead, "Synthesized Sts2.Unavailable error for request id " + idRawJson + ".");
            await WriteStdoutAsync(ChannelKind.Sts2, JsonRpcFraming.BuildFrame(JsonSerializer.SerializeToUtf8Bytes(error)), ct).ConfigureAwait(false);
        }

        private async Task WriteStdoutAsync(ChannelKind channel, ReadOnlyMemory<byte> frame, CancellationToken ct)
        {
            MultiplexerTransportStats.ChannelStats stats = transportStats.For(channel);
            // The single stdout writer (SPEC §6.4, I10): whole frames only, never interleaved.
            long waitStarted = Stopwatch.GetTimestamp();
            await stdoutLock.WaitAsync(ct).ConfigureAwait(false);
            stats.RecordStdoutLockWait(Stopwatch.GetTimestamp() - waitStarted);
            try
            {
                long writeStarted = Stopwatch.GetTimestamp();
                await realOutput.WriteAsync(frame, ct).ConfigureAwait(false);
                stats.RecordStdoutWrite(frame.Length, Stopwatch.GetTimestamp() - writeStarted);
                long flushStarted = Stopwatch.GetTimestamp();
                await realOutput.FlushAsync(ct).ConfigureAwait(false);
                stats.RecordStdoutFlush(Stopwatch.GetTimestamp() - flushStarted);
            }
            finally
            {
                stdoutLock.Release();
            }
        }

        private static int CountSegments(in ReadOnlySequence<byte> sequence)
        {
            if (sequence.IsSingleSegment)
            {
                return 1;
            }

            int count = 0;
            foreach (ReadOnlyMemory<byte> _ in sequence)
            {
                count++;
            }
            return count;
        }

        private void EmitTransportStats()
        {
            long revision = Volatile.Read(ref transportStatsRevision);
            while (true)
            {
                long emitted = Volatile.Read(ref lastEmittedTransportStatsRevision);
                if (emitted == revision)
                {
                    return;
                }
                if (Interlocked.CompareExchange(ref lastEmittedTransportStatsRevision, revision, emitted) == emitted)
                {
                    Diagnostic(MultiplexerDiagnosticCodes.TransportStats, transportStats.SerializeSnapshot());
                    return;
                }
            }
        }

        private async Task WriteToChannelAsync(PipeWriter writer, byte[] bytes, CancellationToken ct)
        {
            try
            {
                await writer.WriteAsync(bytes, ct).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // Channel writer already completed (dead/disposed); drop with a diagnostic.
                Diagnostic(MultiplexerDiagnosticCodes.Sts2Dead, "Dropped frame for completed channel.");
            }
        }

        private void Diagnostic(string code, string message)
        {
            try
            {
                options.DiagnosticListener?.Invoke(new MultiplexerDiagnostic(code, message));
            }
            catch
            {
                // Diagnostics must never take the transport down; deliberately swallowed.
            }
        }
    }
}
