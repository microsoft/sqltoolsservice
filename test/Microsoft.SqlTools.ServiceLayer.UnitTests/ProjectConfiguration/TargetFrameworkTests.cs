//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ProjectConfiguration
{
    /// <summary>
    /// Tests to verify that source projects target the expected frameworks,
    /// including net472 for .NET Framework 4.7.2 support.
    /// </summary>
    [TestFixture]
    public class TargetFrameworkTests
    {
        private static readonly string SrcRoot = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "src"));

        [TestCase("Microsoft.SqlTools.Hosting", "net472")]
        [TestCase("Microsoft.SqlTools.Hosting", "netstandard2.0")]
        [TestCase("Microsoft.SqlTools.Hosting", "net8.0")]
        [TestCase("Microsoft.SqlTools.Connectors.VSCode", "net472")]
        [TestCase("Microsoft.SqlTools.Connectors.VSCode", "net8.0")]
        [TestCase("Microsoft.SqlTools.SqlCore", "net472")]
        [TestCase("Microsoft.SqlTools.SqlCore", "net6.0")]
        public void ProjectShouldTargetExpectedFramework(string projectName, string expectedFramework)
        {
            string csprojPath = Path.Combine(SrcRoot, projectName, $"{projectName}.csproj");
            Assert.That(File.Exists(csprojPath), Is.True, $"Project file not found: {csprojPath}");

            XDocument doc = XDocument.Load(csprojPath);
            XElement targetFrameworks = doc.Descendants("TargetFrameworks").FirstOrDefault()
                ?? doc.Descendants("TargetFramework").FirstOrDefault();

            Assert.That(targetFrameworks, Is.Not.Null,
                $"No TargetFramework(s) element found in {projectName}.csproj");

            string[] frameworks = targetFrameworks.Value.Split(';');
            Assert.That(frameworks, Does.Contain(expectedFramework),
                $"{projectName}.csproj should target '{expectedFramework}'. Found: {targetFrameworks.Value}");
        }
    }
}
