//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Scenarios
{
    /// <summary>
    /// Pinned simulator seeds that once exposed real bugs. Unlike the seed sweep, each
    /// seed here is run REPEATEDLY: the rpc stream is seed-deterministic, but the
    /// arrival order of effect responses relative to later rpc inputs is scheduler
    /// timing, so a seed can be green in one interleaving and wedged in another.
    /// Repetition is what makes the race reproducible (87/100 iterations failed
    /// pre-fix); a lone pass proves nothing.
    /// </summary>
    [Trait("Category", "Simulator")]
    public class SimulatorSeedRegressionTests
    {
        /// <summary>
        /// Seed 7496 (found by the M7 10k sweep): v2/query.dispose immediately followed
        /// by v2/query.cancel for the same query. When the cancel landed while the
        /// driver.queryDispose ack was still in flight, DecideQueryCancel stomped the
        /// phase Disposing -> CancelRequested, the ack handler (which requires
        /// Disposing) dropped the terminal, and the run wedged: the query never emitted
        /// query.complete (I2), the connection never released its ActiveQueryId, the
        /// drain close parked forever, and the driver session leaked (I8). Fixed by
        /// making cancel idempotent for Disposing queries (dispose supersedes cancel).
        /// </summary>
        [Fact]
        public async Task Seed7496DisposeCancelRaceStaysGreen()
        {
            string root = Path.Combine(Path.GetTempPath(), "sts2-sim-regression-" + Guid.NewGuid().ToString("N"));
            const int iterations = 20;
            var failures = new List<string>();
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    Task<SimulatorResult> run = ConnectionSimulator.RunSeedAsync(7496, Path.Combine(root, "iter-" + i));
                    // A wedged run stalls for its full 90s drain/settle budget before
                    // reporting violations; a 60s guard fails fast and names the hang
                    // instead of eating the whole gate.
                    Task winner = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(60)));
                    if (winner != run)
                    {
                        failures.Add($"iter {i}: wedged (>60s; journals under {root})");
                        continue;
                    }
                    SimulatorResult result = await run;
                    if (result.Violations.Count > 0)
                    {
                        failures.Add($"iter {i}: " + string.Join("; ", result.Violations));
                    }
                }
            }
            finally
            {
                if (failures.Count == 0)
                {
                    try
                    {
                        Directory.Delete(root, recursive: true);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            Assert.True(failures.Count == 0,
                $"seed 7496 x{iterations}: {failures.Count} failure(s) (journals under {root}):\n" + string.Join("\n", failures));
        }
    }
}
