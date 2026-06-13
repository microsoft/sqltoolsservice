//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Architecture
{
    /// <summary>
    /// SPEC §16 M4 / §4.3: Core and Contracts must contain zero references to ADO.NET
    /// driver assemblies. Verified two ways — referenced-assembly metadata and a source
    /// scan — so a stray using is caught even before it becomes a hard reference.
    /// </summary>
    public class DriverIsolationTests
    {
        private static readonly string[] BannedAssemblySubstrings =
        [
            "Microsoft.Data.", "System.Data.SqlClient", "System.Data.Common",
        ];

        private static readonly string[] BannedNamespaceSubstrings =
        [
            "Microsoft.Data.", "System.Data.SqlClient", "StreamJsonRpc",
        ];

        [Theory]
        [InlineData("Microsoft.SqlTools.Sts2.Core")]
        [InlineData("Microsoft.SqlTools.Sts2.Contracts")]
        public void DeterministicAssembliesReferenceNoDriverAssemblies(string assemblyName)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName)
                ?? Assembly.Load(assemblyName);

            string[] illegal = assembly.GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .Where(name => BannedAssemblySubstrings.Any(b => name.Contains(b, StringComparison.Ordinal)))
                .ToArray();

            Assert.True(illegal.Length == 0,
                $"{assemblyName} references banned driver assemblies: {string.Join(", ", illegal)}");
        }

        [Theory]
        [InlineData("Microsoft.SqlTools.Sts2.Core")]
        [InlineData("Microsoft.SqlTools.Sts2.Contracts")]
        [InlineData("Microsoft.SqlTools.Sts2.Abstractions")]
        public void DeterministicSourcesUseNoDriverNamespaces(string projectName)
        {
            string projectDir = Path.Combine(RepoRoot.Sts2SourceDir, projectName);
            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }
                foreach ((string line, int number) in File.ReadLines(file).Select((l, i) => (l, i + 1)))
                {
                    if (BannedNamespaceSubstrings.Any(b => line.Contains(b, StringComparison.Ordinal)))
                    {
                        offenders.Add($"{file}:{number}  {line.Trim()}");
                    }
                }
            }
            Assert.True(offenders.Count == 0,
                $"{projectName} sources reference banned driver/transport namespaces:\n" + string.Join("\n", offenders));
        }
    }
}
