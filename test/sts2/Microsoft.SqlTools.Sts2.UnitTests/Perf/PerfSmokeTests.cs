//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.SqlTools.Sts2.UnitTests.Perf
{
    /// <summary>
    /// SPEC §14.6 perf smoke: 1M rows x 10 columns through the full pipeline in digest
    /// mode, gate >= 50k rows/sec. Runs in verify.sh --full (Category=Perf is excluded
    /// from the quick unit gate); the M3 baseline lives in the verification report.
    /// </summary>
    [Trait("Category", "Perf")]
    public sealed class PerfSmokeTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-perf-" + Guid.NewGuid().ToString("N"));
        private readonly ITestOutputHelper output;

        public PerfSmokeTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

        [Fact]
        public async Task OneMillionRowsTimesTenColumnsInDigestMode()
        {
            const int TotalRows = 1_000_000;
            const int RowsPerPage = 1000;
            const int Pages = TotalRows / RowsPerPage;

            await using var session = new Sts2TestSession(directory, "perf-smoke", rowCapture: "digest", sqlCapture: "digest");
            string connectionId = await session.OpenConnectionAsync();

            var steps = new List<FakeQueryStep>(Pages + 2) { new() { Type = "resultSet", ResultSetId = 0, Columns = 10 } };
            steps.AddRange(Enumerable.Range(0, Pages).Select(_ => new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = RowsPerPage, Columns = 10 }));
            steps.Add(new FakeQueryStep { Type = "completed", RowsAffected = TotalRows });
            session.Driver.EnqueueQuery(new FakeQueryScript { Steps = steps });

            var stopwatch = Stopwatch.StartNew();
            var execute = await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select perf"}""");
            string queryId = execute.Body!.Value.GetProperty("queryId").GetString()!;

            // Consumer loop: high-water ack every window so backpressure stays realistic.
            int acked = 0;
            while (session.Emitted.All(m => m.Type != "v2/query.complete"))
            {
                int seen = session.Emitted.Count(m => m.Type == "v2/query.rows");
                if (seen > acked)
                {
                    acked = seen;
                    await session.NotifyAsync("v2/query.ack",
                        $$"""{"queryId":"{{queryId}}","throughPageSeq":{{(acked - 1).ToString(CultureInfo.InvariantCulture)}}}""");
                }
                else
                {
                    await Task.Delay(1);
                }
            }
            stopwatch.Stop();

            int pagesDelivered = session.Emitted.Count(m => m.Type == "v2/query.rows");
            double rowsPerSecond = TotalRows / stopwatch.Elapsed.TotalSeconds;
            long journalBytes = Directory.EnumerateFiles(directory, "*.jsonl").Sum(f => new FileInfo(f).Length);
            output.WriteLine($"perf: {TotalRows} rows x 10 cols in {stopwatch.Elapsed.TotalSeconds:F2}s = {rowsPerSecond:F0} rows/s; pages={pagesDelivered}; journal={journalBytes / 1024.0 / 1024.0:F1} MiB (digest mode)");

            Assert.Equal(Pages, pagesDelivered);
            Assert.True(rowsPerSecond >= 50_000, $"perf gate: {rowsPerSecond:F0} rows/s is below the 50k rows/s floor (SPEC §14.6)");

            // Memory bound proxy: digest mode keeps the journal tiny (no row cells).
            Assert.True(journalBytes < 50 * 1024 * 1024, $"digest-mode journal unexpectedly large: {journalBytes} bytes");
        }
    }
}
