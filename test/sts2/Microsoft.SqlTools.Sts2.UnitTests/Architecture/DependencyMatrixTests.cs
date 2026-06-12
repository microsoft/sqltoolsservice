//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Architecture
{
    /// <summary>
    /// Enforces the SPEC §4 dependency matrix: each STS2 project may reference only
    /// the projects and packages listed there. Anything else fails this test (I11).
    /// </summary>
    public class DependencyMatrixTests
    {
        // Project name -> (allowed project references, allowed package references).
        // Analyzer packages (PrivateAssets=all, injected via src/sts2/Directory.Build.props)
        // are allowed everywhere because they never become runtime dependencies.
        private static readonly Dictionary<string, (string[] Projects, string[] Packages)> AllowedReferences = new()
        {
            ["Microsoft.SqlTools.Sts2.Contracts"] = ([], []),
            ["Microsoft.SqlTools.Sts2.Core"] = (["Microsoft.SqlTools.Sts2.Contracts"], []),
            ["Microsoft.SqlTools.Sts2.Abstractions"] = (["Microsoft.SqlTools.Sts2.Contracts"], []),
            ["Microsoft.SqlTools.Sts2.Runtime"] =
                (["Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Contracts", "Microsoft.SqlTools.Sts2.Abstractions"], []),
            ["Microsoft.SqlTools.Sts2.Hosting"] =
                (["Microsoft.SqlTools.Sts2.Runtime", "Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Contracts", "Microsoft.SqlTools.Sts2.Abstractions"],
                 ["StreamJsonRpc"]),
            ["Microsoft.SqlTools.Sts2.Multiplexer"] = ([], []),
            ["Microsoft.SqlTools.Sts2.Bootstrap"] =
                (["Microsoft.SqlTools.Sts2.Hosting", "Microsoft.SqlTools.Sts2.Runtime", "Microsoft.SqlTools.Sts2.Multiplexer",
                  "Microsoft.SqlTools.Sts2.Drivers.SqlClient", "Microsoft.SqlTools.Sts2.Drivers.Sqlite", "Microsoft.SqlTools.Sts2.Contracts"], []),
            ["Microsoft.SqlTools.Sts2.Drivers.SqlClient"] =
                (["Microsoft.SqlTools.Sts2.Abstractions", "Microsoft.SqlTools.Sts2.Contracts"], ["Microsoft.Data.SqlClient"]),
            ["Microsoft.SqlTools.Sts2.Drivers.Sqlite"] =
                (["Microsoft.SqlTools.Sts2.Abstractions", "Microsoft.SqlTools.Sts2.Contracts"], ["Microsoft.Data.Sqlite"]),
            ["Microsoft.SqlTools.Sts2.Testing"] =
                (["Microsoft.SqlTools.Sts2.Runtime", "Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Contracts", "Microsoft.SqlTools.Sts2.Abstractions"], []),
        };

        private static readonly string[] AnalyzerPackages =
        [
            "Microsoft.CodeAnalysis.BannedApiAnalyzers",
            "Microsoft.CodeAnalysis.PublicApiAnalyzers",
        ];

        public static TheoryData<string> Sts2ProjectNames()
        {
            var data = new TheoryData<string>();
            foreach (string name in AllowedReferences.Keys)
            {
                data.Add(name);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(Sts2ProjectNames))]
        public void ProjectReferencesAreWithinAllowedMatrix(string projectName)
        {
            string csprojPath = Path.Combine(RepoRoot.Sts2SourceDir, projectName, projectName + ".csproj");
            Assert.True(File.Exists(csprojPath), $"Expected project file {csprojPath}");

            XDocument csproj = XDocument.Load(csprojPath);
            (string[] allowedProjects, string[] allowedPackages) = AllowedReferences[projectName];

            string[] actualProjects = csproj.Descendants("ProjectReference")
                .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
                .ToArray();
            string[] actualPackages = csproj.Descendants("PackageReference")
                .Select(r => (r.Attribute("Include") ?? r.Attribute("Update"))!.Value)
                .Where(p => !AnalyzerPackages.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            string[] illegalProjects = actualProjects.Except(allowedProjects, StringComparer.OrdinalIgnoreCase).ToArray();
            string[] illegalPackages = actualPackages.Except(allowedPackages, StringComparer.OrdinalIgnoreCase).ToArray();

            Assert.True(illegalProjects.Length == 0,
                $"{projectName} has project references outside the SPEC §4 matrix: {string.Join(", ", illegalProjects)}");
            Assert.True(illegalPackages.Length == 0,
                $"{projectName} has package references outside the SPEC §4 matrix: {string.Join(", ", illegalPackages)}");
        }

        [Fact]
        public void AllMatrixProjectsExistOnDisk()
        {
            foreach (string projectName in AllowedReferences.Keys)
            {
                string csprojPath = Path.Combine(RepoRoot.Sts2SourceDir, projectName, projectName + ".csproj");
                Assert.True(File.Exists(csprojPath), $"SPEC §4 project missing from disk: {projectName}");
            }
        }

        [Fact]
        public void Sts2SourcesNeverUseLegacyServiceLayerNamespaces()
        {
            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(RepoRoot.Sts2SourceDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                foreach ((string line, int number) in File.ReadLines(file).Select((l, i) => (l, i + 1)))
                {
                    if (line.Contains("Microsoft.SqlTools.ServiceLayer", StringComparison.Ordinal))
                    {
                        offenders.Add($"{file}:{number}");
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "STS2 sources must not use legacy ServiceLayer namespaces (SPEC §4.2):\n" + string.Join("\n", offenders));
        }

        [Fact]
        public void DeterministicProjectsHaveBannedApiAnalyzersWired()
        {
            string[] timeBanned = ["Microsoft.SqlTools.Sts2.Contracts", "Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Abstractions"];
            foreach (string projectName in timeBanned)
            {
                XDocument csproj = XDocument.Load(Path.Combine(RepoRoot.Sts2SourceDir, projectName, projectName + ".csproj"));
                bool hasTimeBan = csproj.Descendants("Sts2DeterministicTimeBan").Any(e => e.Value == "true");
                Assert.True(hasTimeBan, $"{projectName} must set Sts2DeterministicTimeBan=true (SPEC §9.3.1)");
            }

            XDocument core = XDocument.Load(Path.Combine(RepoRoot.Sts2SourceDir, "Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Core.csproj"));
            Assert.True(core.Descendants("Sts2DeterministicCoreBan").Any(e => e.Value == "true"),
                "Core must set Sts2DeterministicCoreBan=true (SPEC §9.3.2-3)");
        }

        [Fact]
        public void PublicApiTrackedProjectsHaveApiFiles()
        {
            string[] tracked =
            [
                "Microsoft.SqlTools.Sts2.Contracts", "Microsoft.SqlTools.Sts2.Core", "Microsoft.SqlTools.Sts2.Abstractions",
                "Microsoft.SqlTools.Sts2.Runtime", "Microsoft.SqlTools.Sts2.Hosting",
            ];
            foreach (string projectName in tracked)
            {
                string dir = Path.Combine(RepoRoot.Sts2SourceDir, projectName);
                XDocument csproj = XDocument.Load(Path.Combine(dir, projectName + ".csproj"));
                Assert.True(csproj.Descendants("Sts2PublicApiTracked").Any(e => e.Value == "true"),
                    $"{projectName} must set Sts2PublicApiTracked=true (SPEC §4.4)");
                Assert.True(File.Exists(Path.Combine(dir, "PublicAPI.Shipped.txt")), $"{projectName} missing PublicAPI.Shipped.txt");
                Assert.True(File.Exists(Path.Combine(dir, "PublicAPI.Unshipped.txt")), $"{projectName} missing PublicAPI.Unshipped.txt");
            }
        }

        [Fact]
        public void EveryProductProjectHasComponentFragment()
        {
            foreach (string projectName in AllowedReferences.Keys)
            {
                Assert.True(File.Exists(Path.Combine(RepoRoot.Sts2SourceDir, projectName, "COMPONENT.md")),
                    $"{projectName} missing COMPONENT.md (SPEC §4.6)");
            }
        }
    }

    /// <summary>Locates the repo root from the test assembly's output directory.</summary>
    internal static class RepoRoot
    {
        internal static string Path { get; } = Find();

        internal static string Sts2SourceDir => System.IO.Path.Combine(Path, "src", "sts2");

        private static string Find()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(System.IO.Path.Combine(dir, "sqltoolsservice.sln")))
            {
                dir = System.IO.Path.GetDirectoryName(dir);
            }
            return dir ?? throw new InvalidOperationException("Could not locate repo root (sqltoolsservice.sln) above " + AppContext.BaseDirectory);
        }
    }
}
