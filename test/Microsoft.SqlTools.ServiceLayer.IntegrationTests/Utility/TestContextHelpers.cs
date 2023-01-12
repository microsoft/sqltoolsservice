//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility
{
    internal static class TestContextHelpers
    {
        private static string TestName => TestContext.CurrentContext.Test.Name;

        public static string GetTestWorkingFolder() => Path.Join(TestContext.CurrentContext.WorkDirectory, "TestRuns", TestName + DateTime.Now.ToString("yyyyMMdd-HHmmssffff"));

        public static string GetTestProjectPath(string? projectName = null) => Path.Join(GetTestWorkingFolder(), $"{projectName ?? TestName}.sqlproj");
    }
}
