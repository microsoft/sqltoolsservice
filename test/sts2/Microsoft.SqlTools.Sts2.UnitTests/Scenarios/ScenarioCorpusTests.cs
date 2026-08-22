//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Architecture;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Scenarios
{
    /// <summary>SPEC §16 M1: at least 50 scenario stubs covering the §14.2 mandatory list.</summary>
    public class ScenarioCorpusTests
    {
        private static string ScenarioDir => Path.Combine(RepoRoot.Path, "test", "sts2", "scenarios");

        private static IReadOnlyList<ScenarioInfo> Corpus => ScenarioCatalog.Load(ScenarioDir);

        [Fact]
        public void CorpusHasAtLeastFiftyScenarios()
        {
            Assert.True(Corpus.Count >= 50, $"corpus has {Corpus.Count} scenarios; SPEC §16 M1 requires >= 50");
        }

        [Fact]
        public void NamesAreUniqueAndMatchFileNames()
        {
            Assert.Equal(Corpus.Count, Corpus.Select(s => s.Name).Distinct(StringComparer.Ordinal).Count());
            Assert.All(Corpus, s => Assert.Equal(s.Name, Path.GetFileNameWithoutExtension(s.FilePath)));
        }

        [Fact]
        public void EveryScenarioHasValidHeaderFields()
        {
            Assert.All(Corpus, s =>
            {
                Assert.Matches("^[a-z0-9-]+$", s.Name);
                Assert.NotEmpty(s.Tags);
                Assert.Contains(s.Status, new[] { "stub", "active" });
                Assert.Matches("^M[0-9]$", s.Milestone);
                Assert.Contains(s.Adapter, new[] { "fake", "sqlite", "sqlserver", "multiplexer" });
            });
        }

        [Fact]
        public void MandatoryCoverageListIsPresent()
        {
            // SPEC §14.2 minimum corpus items, mapped to scenario names.
            string[] required =
            [
                "happy-open-two-resultsets-complete",
                "open-timeout", "open-auth-fail", "open-network-fail",
                "mid-resultset-server-error", "severed-connection-mid-page", "slow-consumer-backpressure",
                "zero-rows", "huge-pages", "multiple-resultsets", "type-edge-values",
                "error-connectionfailed-auth", "error-connectionfailed-network", "error-connectionfailed-timeout",
                "error-queryfailed-server", "error-queryfailed-transport", "error-canceled", "error-busy",
                "error-invalidrequest", "error-notfound", "error-unavailable-after-fatal", "error-internal",
                "window-exhaustion-resume",
                "ack-duplicate", "ack-late", "ack-unknown",
                "cancel-duplicate", "cancel-late", "cancel-unknown",
                "dispose-while-streaming", "close-while-query-active",
                "open-cancel-race", "cancel-vs-complete-race", "shutdown-mid-query",
                "sts2-fatal-containment", "malformed-frame-routed-to-legacy", "outbound-server-request-id-collision",
                "secret-canary-connection-open", "sql-digest-export-replay", "row-digest-replay",
                "config-change-during-query",
            ];
            string[] names = Corpus.Select(s => s.Name).ToArray();
            string[] missing = required.Except(names, StringComparer.Ordinal).ToArray();
            Assert.True(missing.Length == 0, "mandatory scenarios missing: " + string.Join(", ", missing));
        }

        [Fact]
        public void MultiplexerLayerScenariosAreTaggedHonestly()
        {
            // SPEC §14.2: mux-layer behaviors are realized as unit/E2E tests and appear
            // in the matrix with adapter=multiplexer so corpus accounting stays honest.
            string[] muxScenarios = ["sts2-fatal-containment", "malformed-frame-routed-to-legacy", "outbound-server-request-id-collision"];
            foreach (string name in muxScenarios)
            {
                ScenarioInfo scenario = Assert.Single(Corpus, s => s.Name == name);
                Assert.Equal("multiplexer", scenario.Adapter);
            }
        }
    }
}
