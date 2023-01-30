//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility
{
    public static class TestContextHelpers
    {
        private static string TestName => TestContext.CurrentContext.Test.Name;

        public static string GetTestWorkingFolder(this TestContext context) => Path.Join(TestBase.TestRunFolder, TestName);

        public static string GetTestProjectPath(this TestContext context, string? projectName = null) => Path.Join(context.GetTestWorkingFolder(), $"{projectName ?? TestName}.sqlproj");
    }
}
