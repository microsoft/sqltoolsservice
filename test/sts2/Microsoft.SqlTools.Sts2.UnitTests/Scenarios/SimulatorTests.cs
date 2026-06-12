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
    /// P0 determinism bugs, never retries.
    /// </summary>
    public class SimulatorTests
    {
        [Fact]
        public async Task TwoHundredSeedsAreGreen()
        {
            string root = Path.Combine(Path.GetTempPath(), "sts2-sim-" + Guid.NewGuid().ToString("N"));
            var failures = new List<string>();
            try
            {
                for (int seed = 1; seed <= 200; seed++)
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
