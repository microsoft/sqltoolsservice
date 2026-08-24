//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Architecture;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Docs
{
    /// <summary>
    /// SPEC §12.3: the committed review docs must equal their generators' output —
    /// verify fails on diff. Run scripts/update-sts2-docs.ps1 (sets STS2_UPDATE_DOCS=1)
    /// to regenerate after an intentional change.
    /// </summary>
    public class GeneratedDocsTests
    {
        private static string DocsDir => Path.Combine(RepoRoot.Path, "docs", "sts2");

        [Fact]
        public void CommittedDocsMatchGenerators()
        {
            IReadOnlyDictionary<string, string> generated = GeneratedDocs.All(RepoRoot.Path);
            bool update = Environment.GetEnvironmentVariable("STS2_UPDATE_DOCS") == "1";

            foreach ((string fileName, string content) in generated)
            {
                string path = Path.Combine(DocsDir, fileName);
                if (update)
                {
                    File.WriteAllText(path, content);
                    continue;
                }

                Assert.True(File.Exists(path), $"{fileName} is missing; run scripts/update-sts2-docs.ps1");
                string committed = File.ReadAllText(path).ReplaceLineEndings("\n");
                Assert.True(committed == content.ReplaceLineEndings("\n"),
                    $"{fileName} is stale relative to its generator; run scripts/update-sts2-docs.ps1 and review the diff");
            }
        }

        [Fact]
        public void GeneratorsAreDeterministic()
        {
            IReadOnlyDictionary<string, string> first = GeneratedDocs.All(RepoRoot.Path);
            IReadOnlyDictionary<string, string> second = GeneratedDocs.All(RepoRoot.Path);
            Assert.Equal(first, second);
        }

        [Fact]
        public void GeneratedDocsContainNoSecretCanaries()
        {
            foreach ((string fileName, string content) in GeneratedDocs.All(RepoRoot.Path))
            {
                Assert.True(SecretCanaries.FindIn(content).Count == 0, fileName + " contains a secret canary (I6)");
            }
        }
    }
}
