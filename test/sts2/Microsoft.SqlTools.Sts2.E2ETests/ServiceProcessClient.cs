//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Sts2.E2ETests
{
    /// <summary>
    /// Spawns the real MicrosoftSqlToolsServiceLayer executable and speaks
    /// Content-Length framed JSON-RPC over its stdio, exactly like a client.
    /// </summary>
    internal sealed class ServiceProcessClient : IAsyncDisposable
    {
        private readonly Process process;
        private readonly Stream stdin;
        private readonly Stream stdout;
        private readonly CancellationTokenSource readLoopCts = new();
        private readonly Task readLoop;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pendingRequests = new(StringComparer.Ordinal);
        private int nextId;

        private ServiceProcessClient(Process process)
        {
            this.process = process;
            stdin = process.StandardInput.BaseStream;
            stdout = process.StandardOutput.BaseStream;
            readLoop = Task.Run(() => ReadLoopAsync(readLoopCts.Token));
        }

        /// <summary>Queue of notifications received from the service, by method name.</summary>
        public ConcurrentQueue<(string Method, JsonElement Params)> Notifications { get; } = new();

        public static string LocateServiceDll()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "sqltoolsservice.sln")))
            {
                dir = Path.GetDirectoryName(dir);
            }
            if (dir == null)
            {
                throw new InvalidOperationException("Could not locate repo root above " + AppContext.BaseDirectory);
            }

            foreach (string configuration in new[] { "Debug", "Release" })
            {
                string candidate = Path.Combine(
                    dir, "src", "Microsoft.SqlTools.ServiceLayer", "bin", configuration, "net10.0", "MicrosoftSqlToolsServiceLayer.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new InvalidOperationException(
                "MicrosoftSqlToolsServiceLayer.dll not found. Build src/Microsoft.SqlTools.ServiceLayer first (verify.sh does this).");
        }

        public static ServiceProcessClient Start(bool enableSts2, string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);
            string args = "\"" + LocateServiceDll() + "\" --log-file \"" + Path.Combine(logDirectory, "sqltools.log") + "\"";
            if (enableSts2)
            {
                args += " --enable-sts2";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.Environment.Remove("STS_ENABLE_STS2"); // only the flag controls activation in these tests

            Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the service process.");
            return new ServiceProcessClient(process);
        }

        public async Task<JsonElement> RequestAsync(string method, object? parameters, CancellationToken ct)
        {
            string id = "e2e-" + Interlocked.Increment(ref nextId).ToString(CultureInfo.InvariantCulture);
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            pendingRequests[id] = tcs;

            string paramsJson = parameters is null ? "null" : JsonSerializer.Serialize(parameters);
            string json = "{\"jsonrpc\":\"2.0\",\"id\":\"" + id + "\",\"method\":\"" + method + "\",\"params\":" + paramsJson + "}";
            await WriteFrameAsync(json, ct);

            await using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                return await tcs.Task;
            }
        }

        /// <summary>
        /// Sends a request without awaiting any response. Needed for legacy
        /// <c>shutdown</c>, which terminates the process instead of responding.
        /// </summary>
        public Task SendRequestFireAndForgetAsync(string method, CancellationToken ct)
        {
            string id = "e2e-" + Interlocked.Increment(ref nextId).ToString(CultureInfo.InvariantCulture);
            return WriteFrameAsync("{\"jsonrpc\":\"2.0\",\"id\":\"" + id + "\",\"method\":\"" + method + "\"}", ct);
        }

        public Task NotifyAsync(string method, object? parameters, CancellationToken ct)
        {
            string paramsJson = parameters is null ? "null" : JsonSerializer.Serialize(parameters);
            return WriteFrameAsync("{\"jsonrpc\":\"2.0\",\"method\":\"" + method + "\",\"params\":" + paramsJson + "}", ct);
        }

        public async Task<bool> WaitForExitAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task WriteFrameAsync(string json, CancellationToken ct)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] header = Encoding.ASCII.GetBytes(
                "Content-Length: " + payload.Length.ToString(CultureInfo.InvariantCulture) + "\r\n\r\n");
            await stdin.WriteAsync(header, ct);
            await stdin.WriteAsync(payload, ct);
            await stdin.FlushAsync(ct);
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    string payload = await ReadFrameAsync(ct);
                    JsonElement root = JsonDocument.Parse(payload).RootElement;
                    if (root.TryGetProperty("method", out JsonElement methodElement))
                    {
                        root.TryGetProperty("params", out JsonElement paramsElement);
                        Notifications.Enqueue((methodElement.GetString()!, paramsElement));
                        continue;
                    }
                    if (root.TryGetProperty("id", out JsonElement idElement)
                        && idElement.ValueKind == JsonValueKind.String
                        && pendingRequests.TryRemove(idElement.GetString()!, out TaskCompletionSource<JsonElement>? tcs))
                    {
                        tcs.TrySetResult(root);
                    }
                }
            }
            catch (Exception ex) when (ex is EndOfStreamException or IOException or OperationCanceledException or ObjectDisposedException)
            {
                foreach (TaskCompletionSource<JsonElement> tcs in pendingRequests.Values)
                {
                    tcs.TrySetException(new EndOfStreamException("Service process stdout closed.", ex));
                }
            }
        }

        private async Task<string> ReadFrameAsync(CancellationToken ct)
        {
            var headerBytes = new List<byte>(64);
            byte[] one = new byte[1];
            while (true)
            {
                int n = await stdout.ReadAsync(one, ct);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }
                headerBytes.Add(one[0]);
                int c = headerBytes.Count;
                if (c >= 4 && headerBytes[c - 4] == (byte)'\r' && headerBytes[c - 3] == (byte)'\n'
                           && headerBytes[c - 2] == (byte)'\r' && headerBytes[c - 1] == (byte)'\n')
                {
                    break;
                }
            }

            int contentLength = -1;
            foreach (string line in Encoding.ASCII.GetString(headerBytes.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon > 0 && line[..colon].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    contentLength = int.Parse(line[(colon + 1)..].Trim(), CultureInfo.InvariantCulture);
                }
            }
            if (contentLength < 0)
            {
                throw new InvalidDataException("Service emitted a frame without Content-Length: unframed stdout text?");
            }

            byte[] payload = new byte[contentLength];
            int read = 0;
            while (read < contentLength)
            {
                int n = await stdout.ReadAsync(payload.AsMemory(read), ct);
                if (n == 0)
                {
                    throw new EndOfStreamException();
                }
                read += n;
            }
            return Encoding.UTF8.GetString(payload);
        }

        public async ValueTask DisposeAsync()
        {
            readLoopCts.Cancel();
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Already exited.
            }
            try
            {
                await readLoop;
            }
            catch (OperationCanceledException)
            {
            }
            process.Dispose();
            readLoopCts.Dispose();
        }
    }
}
