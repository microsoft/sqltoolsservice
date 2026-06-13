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
    /// SPEC §14.4: seeded random op/fault schedules. Quick gate runs 200 seeds. A
    /// failure prints the seed; reproduce with that exact seed — flaky results here are
    /// P0 determinism bugs, never retries. Tagged Category=Simulator so it runs in its
    /// own dedicated verify gate, not concurrently with the rest of the unit suite
    /// (which would starve its background-task liveness budgets under load).
    /// </summary>
    [Trait("Category", "Simulator")]
    public class SimulatorTests
    {
        [Fact]
        public async Task SeedSweepIsGreen()
        {
            // Quick gate sweeps 200 seeds; CI/nightly sets STS2_SIMULATOR_SEEDS=10000
            // (SPEC §14.4). Journals are deterministic per seed regardless of count (I7).
            int seedCount = int.TryParse(
                Environment.GetEnvironmentVariable("STS2_SIMULATOR_SEEDS"), out int configured) && configured > 0
                ? configured
                : 200;

            string root = Path.Combine(Path.GetTempPath(), "sts2-sim-" + Guid.NewGuid().ToString("N"));
            var failures = new List<string>();
            try
            {
                for (int seed = 1; seed <= seedCount; seed++)
                {
                    SimulatorResult result = await ConnectionSimulator.RunSeedAsync(seed, Path.Combine(root, "seed-" + seed));
                    if (result.Violations.Count > 0)
                    {
                        failures.Add($"seed {seed} ({result.Operations} ops): " + string.Join("; ", result.Violations)
                            + $"\n  repro: ConnectionSimulator.RunSeedAsync({seed}, ...)");
                    }
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch (IOException)
                {
                }
            }
            Assert.True(failures.Count == 0, failures.Count + " seed(s) failed:\n" + string.Join("\n", failures));
        }
    }
}
