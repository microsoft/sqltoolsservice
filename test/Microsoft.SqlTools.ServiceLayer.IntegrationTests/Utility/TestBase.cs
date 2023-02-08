//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility
{
    [TestFixture]
    public abstract class TestBase
    {
        static TestBase()
        {
            RunTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssffff");
        }

        public static string RunTimestamp
        {
            get;
            private set;
        }

        public static string TestRunFolder => Path.Join(TestContext.CurrentContext.WorkDirectory, "SqlToolsServiceTestRuns", $"Run{RunTimestamp}");


        [OneTimeSetUp]
        public void SetUp()
        {
            if (!Directory.Exists(TestRunFolder))
            {
                Directory.CreateDirectory(TestRunFolder);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (Directory.Exists(TestRunFolder))
            {
                Directory.Delete(TestRunFolder, recursive: true);
            }
        }
    }
}
