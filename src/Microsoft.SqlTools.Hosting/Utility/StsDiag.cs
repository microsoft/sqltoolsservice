//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Hosting.Utility
{
    /// <summary>
    /// Lightweight service-side diagnostics emitter for the MSSQL Debug
    /// Console. Active ONLY when the host process (the VS Code extension)
    /// launched this service with STS_DIAG_URL/STS_DIAG_TOKEN pointing at its
    /// 127.0.0.1 listener; otherwise every call is a single static bool check.
    ///
    /// Hard rules: never SQL text, never row values, never connection strings
    /// or secrets — only protocol metadata (method names, durations, counts,
    /// object types). Emission is bounded, batched, fire-and-forget and can
    /// never block or throw into service code paths.
    /// </summary>
    public static class StsDiag
    {
        private const int MaxQueue = 2000;
        private const int FlushIntervalMs = 250;

        public static readonly bool Enabled;
        private static readonly string? url;
        private static readonly string? token;
        private static readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private static readonly HttpClient? httpClient;
#if NETSTANDARD2_0
        private static readonly int processId = Process.GetCurrentProcess().Id;
#else
        private static readonly int processId = Environment.ProcessId;
#endif
        private static int queuedCount;
        private static int flushScheduled;

        static StsDiag()
        {
            try
            {
                url = Environment.GetEnvironmentVariable("STS_DIAG_URL");
                token = Environment.GetEnvironmentVariable("STS_DIAG_TOKEN");
                Enabled = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token)
                    && url.StartsWith("http://127.0.0.1", StringComparison.Ordinal);
                if (Enabled)
                {
                    httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }
            }
            catch
            {
                Enabled = false;
            }
        }

        /// <summary>
        /// Emit a completed span (single event carrying its own duration —
        /// halves event volume vs begin/end pairs). No-op unless enabled.
        /// </summary>
        public static void EmitSpan(
            string type,
            string feature,
            double durationMs,
            long startUnixMs,
            string status = "ok",
            IReadOnlyDictionary<string, object?>? fields = null)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["type"] = type,
                    ["feature"] = feature,
                    ["kind"] = "span",
                    ["status"] = status,
                    ["epochMs"] = startUnixMs + (long)durationMs,
                    ["startEpochMs"] = startUnixMs,
                    ["durationMs"] = Math.Round(durationMs, 2),
                    ["pid"] = processId,
                };
                if (fields != null)
                {
                    payload["fields"] = fields;
                }
                Enqueue(JsonSerializer.Serialize(payload));
            }
            catch
            {
                // diagnostics must never affect the service
            }
        }

        /// <summary>Emit an instantaneous event. No-op unless enabled.</summary>
        public static void EmitEvent(
            string type,
            string feature,
            string status = "ok",
            IReadOnlyDictionary<string, object?>? fields = null)
        {
            if (!Enabled)
            {
                return;
            }
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["type"] = type,
                    ["feature"] = feature,
                    ["kind"] = "event",
                    ["status"] = status,
                    ["epochMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ["pid"] = processId,
                };
                if (fields != null)
                {
                    payload["fields"] = fields;
                }
                Enqueue(JsonSerializer.Serialize(payload));
            }
            catch
            {
                // diagnostics must never affect the service
            }
        }

        /// <summary>Start a stopwatch-backed span scope; dispose to emit.</summary>
        public static SpanScope StartSpan(string type, string feature)
        {
            return new SpanScope(type, feature);
        }

        public readonly struct SpanScope : IDisposable
        {
            private readonly string type;
            private readonly string feature;
            private readonly long startUnixMs;
            private readonly long startTimestamp;

            internal SpanScope(string type, string feature)
            {
                this.type = type;
                this.feature = feature;
                this.startUnixMs = Enabled ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : 0;
                this.startTimestamp = Enabled ? Stopwatch.GetTimestamp() : 0;
            }

            public void Complete(string status = "ok", IReadOnlyDictionary<string, object?>? fields = null)
            {
                if (!Enabled)
                {
                    return;
                }
                double durationMs = (Stopwatch.GetTimestamp() - this.startTimestamp) * 1000.0 / Stopwatch.Frequency;
                EmitSpan(this.type, this.feature, durationMs, this.startUnixMs, status, fields);
            }

            public void Dispose()
            {
                // Default disposal reports ok with no fields; call Complete()
                // explicitly for status/fields.
            }
        }

        private static void Enqueue(string line)
        {
            if (Volatile.Read(ref queuedCount) >= MaxQueue)
            {
                return; // drop newest under pressure; never block
            }
            queue.Enqueue(line);
            Interlocked.Increment(ref queuedCount);
            if (Interlocked.CompareExchange(ref flushScheduled, 1, 0) == 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(FlushIntervalMs).ConfigureAwait(false);
                        await FlushAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // swallow — next enqueue reschedules
                    }
                    finally
                    {
                        Volatile.Write(ref flushScheduled, 0);
                    }
                });
            }
        }

        private static async Task FlushAsync()
        {
            if (httpClient == null || url == null)
            {
                return;
            }
            var batch = new StringBuilder();
            int taken = 0;
            while (taken < MaxQueue && queue.TryDequeue(out string? line))
            {
                batch.AppendLine(line);
                taken++;
            }
            if (taken == 0)
            {
                return;
            }
            Interlocked.Add(ref queuedCount, -taken);
            try
            {
                using var content = new StringContent(batch.ToString(), Encoding.UTF8, "application/x-ndjson");
                using var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                // Response ignored; the listener discards when no sink is active.
            }
            catch
            {
                // Listener not up (console closed / extension gone): drop batch.
            }
        }
    }
}
