//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.Testing.Scenarios;
using Microsoft.SqlTools.Sts2.UnitTests.Architecture;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Scenarios
{
    /// <summary>
    /// Executes every <c>status: active</c> scenario through the runner (SPEC §14.2).
    /// Journals land under artifacts/test-journals/scenarios for the verify replay gate.
    /// </summary>
    public class ActiveScenarioTests
    {
        private static string ScenarioDir => Path.Combine(RepoRoot.Path, "test", "sts2", "scenarios");

        private static string JournalRoot => Path.Combine(RepoRoot.Path, "artifacts", "test-journals", "scenarios");

        public static TheoryData<string> ActiveScenarioNames()
        {
            var data = new TheoryData<string>();
            foreach (ScenarioInfo scenario in ScenarioCatalog.Load(ScenarioDir).Where(s => s.Status == "active"))
            {
                data.Add(scenario.Name);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(ActiveScenarioNames))]
        public async Task ActiveScenarioIsGreen(string name)
        {
            ScenarioDefinition scenario = ScenarioYamlParser.Parse(Path.Combine(ScenarioDir, name + ".yaml"));
            ScenarioRunResult result = await ScenarioRunner.RunAsync(scenario, JournalRoot);
            Assert.True(result.Failures.Count == 0,
                name + " failed:\n" + string.Join("\n", result.Failures)
                + "\nrepro: replay " + result.JournalDirectory);
        }

        [Fact]
        public void AtLeastTwelveScenariosAreActiveAtM2()
        {
            int active = ScenarioCatalog.Load(ScenarioDir).Count(s => s.Status == "active");
            Assert.True(active >= 12, $"expected >= 12 active scenarios at M2, found {active}");
        }
    }
}
