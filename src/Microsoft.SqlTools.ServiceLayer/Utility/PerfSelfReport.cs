//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Performance-harness process self-report (perf harness design §18.7).
    /// Active ONLY when the process was launched by the perf orchestrator with
    /// PERF_MODE=1 and a PERF_MARKER_URL (a 127.0.0.1 sink). Sends a single
    /// fire-and-forget "sts.process.ready" marker so the harness can attribute
    /// this PID to the sts role. Outside perf mode this is a no-op; failures
    /// are swallowed — instrumentation must never affect the service.
    /// </summary>
    internal static class PerfSelfReport
    {
        public static void TrySendProcessReady()
        {
            try
            {
                if (Environment.GetEnvironmentVariable("PERF_MODE") != "1")
                {
                    return;
                }
                string? markerUrl = Environment.GetEnvironmentVariable("PERF_MARKER_URL");
                string? token = Environment.GetEnvironmentVariable("PERF_CONTROL_TOKEN");
                if (string.IsNullOrEmpty(markerUrl) || string.IsNullOrEmpty(token))
                {
                    return;
                }

                long unixNs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L;
                long monotonicNs = (long)(Stopwatch.GetTimestamp() * (1_000_000_000.0 / Stopwatch.Frequency));
                var marker = new
                {
                    schemaVersion = 1,
                    runId = Environment.GetEnvironmentVariable("PERF_RUN_ID") ?? "unknown-run",
                    repId = int.TryParse(Environment.GetEnvironmentVariable("PERF_REP_ID"), out int repId) ? repId : 0,
                    scenarioId = Environment.GetEnvironmentVariable("PERF_SCENARIO_ID") ?? "unknown-scenario",
                    name = "sts.process.ready",
                    phase = "instant",
                    timestampUnixNs = unixNs.ToString(),
                    monotonicNs = monotonicNs.ToString(),
                    process = new
                    {
                        role = "sts",
                        pid = Environment.ProcessId,
                        name = "MicrosoftSqlToolsServiceLayer"
                    },
                    attrs = new
                    {
                        version = typeof(PerfSelfReport).Assembly.GetName().Version?.ToString() ?? "unknown",
                        dotnetRuntime = RuntimeInformation.FrameworkDescription,
                        architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                        sts2Enabled = Environment.GetEnvironmentVariable("STS_ENABLE_STS2") == "1"
                    }
                };

                string body = JsonSerializer.Serialize(marker);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                        using var request = new HttpRequestMessage(HttpMethod.Post, markerUrl)
                        {
                            Content = new StringContent(body, Encoding.UTF8, "application/x-ndjson")
                        };
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                        using HttpResponseMessage _ = await client.SendAsync(request).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Best-effort only; the harness tolerates a missing self-report.
                    }
                });
            }
            catch
            {
                // Perf instrumentation must never surface into the service.
            }
        }
    }
}
