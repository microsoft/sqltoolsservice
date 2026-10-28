//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Authentication
{
    /// <summary>
    /// Tests for the native MSAL runtime that Microsoft.Data.SqlClient loads for the WAM broker.
    /// </summary>
    [TestFixture]
    public class MsalRuntimeTests
    {
        [Test]
        [TestCase("win-x64", "msalruntime.dll")]
        [TestCase("win-x86", "msalruntime_x86.dll")]
        [TestCase("win-arm64", "msalruntime_arm64.dll")]
        public void WindowsMsalRuntime_IsShipped(string runtime, string fileName)
        {
            Assert.IsTrue(File.Exists(GetRuntimePath(runtime, fileName)),
                $"SqlClient uses the WAM broker on Windows, so {fileName} should be copied next to the service.");
        }

        [Test]
        [TestCase("linux-x64", "libmsalruntime.so")]
        [TestCase("osx-x64", "msalruntime.dylib")]
        [TestCase("osx-arm64", "msalruntime_arm64.dylib")]
        public void NonWindowsMsalRuntime_IsLeftOut(string runtime, string fileName)
        {
            Assert.IsFalse(File.Exists(GetRuntimePath(runtime, fileName)),
                $"SqlClient uses the WAM broker only on Windows, so {fileName} should not be copied.");
        }

        private static string GetRuntimePath(string runtime, string fileName) =>
            Path.Combine(AppContext.BaseDirectory, "runtimes", runtime, "native", fileName);
    }
}
