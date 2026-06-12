//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    /// <summary>
    /// Harness owning a <see cref="StdioMultiplexer"/> wired to in-memory pipes so tests
    /// can play the client (writing to stdin, reading stdout) and both services
    /// (reading/writing the virtual channel streams).
    /// </summary>
    internal sealed class MuxHarness : IAsyncDisposable
    {
        private readonly Pipe stdinPipe = new();
        private readonly Pipe stdoutPipe = new();

        public MuxHarness(MultiplexerOptions? options = null, ISts2LifecycleSink? lifecycleSink = null)
        {
            Diagnostics = new ConcurrentQueue<MultiplexerDiagnostic>();
            var opts = options ?? new MultiplexerOptions();
            opts = opts with { DiagnosticListener = d => Diagnostics.Enqueue(d) };
            Mux = new StdioMultiplexer(stdinPipe.Reader.AsStream(), stdoutPipe.Writer.AsStream(), opts);
            Mux.Start(lifecycleSink);
            StdoutReader = stdoutPipe.Reader.AsStream();
        }

        public StdioMultiplexer Mux { get; }

        public ConcurrentQueue<MultiplexerDiagnostic> Diagnostics { get; }

        /// <summary>Stream the test reads multiplexed stdout frames from.</summary>
        public Stream StdoutReader { get; }

        /// <summary>Writes one framed JSON-RPC message to the multiplexer's stdin.</summary>
        public Task ClientSendsAsync(string json, CancellationToken ct = default) =>
            Frames.WriteFrameAsync(stdinPipe.Writer.AsStream(), json, ct);

        /// <summary>Writes raw bytes to stdin (for chunking and malformed-frame tests).</summary>
        public async Task ClientSendsRawAsync(byte[] bytes, CancellationToken ct = default)
        {
            await stdinPipe.Writer.WriteAsync(bytes, ct);
            await stdinPipe.Writer.FlushAsync(ct);
        }

        public void ClientClosesStdin() => stdinPipe.Writer.Complete();

        public Task<string> LegacyReceivesAsync(CancellationToken ct = default) =>
            Frames.ReadFrameAsync(Mux.LegacyInput, ct);

        public Task<string> Sts2ReceivesAsync(CancellationToken ct = default) =>
            Frames.ReadFrameAsync(Mux.Sts2Input, ct);

        public Task LegacySendsAsync(string json, CancellationToken ct = default) =>
            Frames.WriteFrameAsync(Mux.LegacyOutput, json, ct);

        public Task Sts2SendsAsync(string json, CancellationToken ct = default) =>
            Frames.WriteFrameAsync(Mux.Sts2Output, json, ct);

        public Task<string> StdoutFrameAsync(CancellationToken ct = default) =>
            Frames.ReadFrameAsync(StdoutReader, ct);

        public async ValueTask DisposeAsync()
        {
            await Mux.DisposeAsync();
        }
    }

    internal static class Frames
    {
        public static byte[] Frame(string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] header = Encoding.ASCII.GetBytes(
                "Content-Length: " + payload.Length.ToString(CultureInfo.InvariantCulture) + "\r\n\r\n");
            byte[] frame = new byte[header.Length + payload.Length];
            header.CopyTo(frame, 0);
            payload.CopyTo(frame, header.Length);
            return frame;
        }

        public static async Task WriteFrameAsync(Stream stream, string json, CancellationToken ct = default)
        {
            byte[] frame = Frame(json);
            await stream.WriteAsync(frame, ct);
            await stream.FlushAsync(ct);
        }

        /// <summary>Reads one Content-Length framed message and returns its JSON payload text.</summary>
        public static async Task<string> ReadFrameAsync(Stream stream, CancellationToken ct = default)
        {
            // Read the header byte-by-byte until \r\n\r\n; ok for tests.
            var headerBytes = new List<byte>(64);
            byte[] one = new byte[1];
            while (true)
            {
                int n = await stream.ReadAsync(one, ct);
                if (n == 0)
                {
                    throw new EndOfStreamException("Stream closed while reading frame header.");
                }
                headerBytes.Add(one[0]);
                int c = headerBytes.Count;
                if (c >= 4 && headerBytes[c - 4] == (byte)'\r' && headerBytes[c - 3] == (byte)'\n'
                           && headerBytes[c - 2] == (byte)'\r' && headerBytes[c - 1] == (byte)'\n')
                {
                    break;
                }
            }

            string header = Encoding.ASCII.GetString(headerBytes.ToArray());
            int contentLength = -1;
            foreach (string line in header.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon > 0 && line[..colon].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    contentLength = int.Parse(line[(colon + 1)..].Trim(), CultureInfo.InvariantCulture);
                }
            }
            if (contentLength < 0)
            {
                throw new InvalidDataException("No Content-Length header in: " + header);
            }

            byte[] payload = new byte[contentLength];
            int read = 0;
            while (read < contentLength)
            {
                int n = await stream.ReadAsync(payload.AsMemory(read), ct);
                if (n == 0)
                {
                    throw new EndOfStreamException("Stream closed mid-payload.");
                }
                read += n;
            }
            return Encoding.UTF8.GetString(payload);
        }

        /// <summary>Reads exactly <paramref name="count"/> raw bytes.</summary>
        public static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken ct = default)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer.AsMemory(read), ct);
                if (n == 0)
                {
                    throw new EndOfStreamException($"Stream closed after {read}/{count} bytes.");
                }
                read += n;
            }
            return buffer;
        }
    }

    internal sealed class TestLifecycleSink : ISts2LifecycleSink
    {
        private readonly TaskCompletionSource exitFlushed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ShutdownCalls;
        public int ExitCalls;

        /// <summary>When false the sink hangs until <see cref="CompleteExitFlush"/> is called.</summary>
        public bool CompleteExitImmediately { get; init; } = true;

        public void OnShutdown() => Interlocked.Increment(ref ShutdownCalls);

        public Task OnExitAsync()
        {
            Interlocked.Increment(ref ExitCalls);
            return CompleteExitImmediately ? Task.CompletedTask : exitFlushed.Task;
        }

        public void CompleteExitFlush() => exitFlushed.TrySetResult();
    }

    /// <summary>Manually advanced clock for id-table TTL tests.</summary>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        private long nowTicks = new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero).UtcTicks;

        public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref nowTicks), TimeSpan.Zero);

        public void Advance(TimeSpan by) => Interlocked.Add(ref nowTicks, by.Ticks);
    }
}
