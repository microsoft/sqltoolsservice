//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class RunEnvironmentInfo
    {
        private static string cachedTestFolderPath;
        private static string cachedTraceFolderPath;

        public static bool IsLabMode()
        {
            string bvtLabRoot = Environment.GetEnvironmentVariable(Constants.BVTLocalRoot);
            if (string.IsNullOrEmpty(bvtLabRoot))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Location of all test data (baselines, etc).
        /// </summary>
        /// <returns>The full path to the test data directory</returns>
        public static string GetTestDataLocation()
        {
            string testFolderPath;
            string testPath = @"test\Microsoft.SqlTools.ServiceLayer.Test.Common\TestData";
            string projectPath = Environment.GetEnvironmentVariable(Constants.ProjectPath);

            if (projectPath != null)
            {
                testFolderPath = Path.Combine(projectPath, testPath);
            }
            else
            {
                if (cachedTestFolderPath != null)
                {
                    testFolderPath = cachedTestFolderPath;
                }
                else
                {
                    string defaultPath = Path.Combine(typeof(Scripts).GetTypeInfo().Assembly.Location, @"..\..\..\..\..");
                    testFolderPath = Path.Combine(defaultPath, @"Microsoft.SqlTools.ServiceLayer.Test.Common\TestData");
                    cachedTestFolderPath = testFolderPath;
                }
            }
            return testFolderPath;
        }

        public static string GetTraceOutputLocation()
        {
            string traceFolderPath;
            string testPath = @"test\Microsoft.SqlTools.ServiceLayer.Test.Common\Trace";
            string projectPath = Environment.GetEnvironmentVariable(Constants.ProjectPath);

            if (projectPath != null)
            {
                traceFolderPath = Path.Combine(projectPath, testPath);
            }
            else
            {
                if (cachedTraceFolderPath != null)
                {
                    traceFolderPath = cachedTraceFolderPath;
                }
                else
                {
                    string defaultPath = Path.Combine(typeof(Scripts).GetTypeInfo().Assembly.Location, @"..\..\..\..\..");
                    traceFolderPath = Path.Combine(defaultPath, @"Microsoft.SqlTools.ServiceLayer.Test.Common\Trace");
                    cachedTraceFolderPath = traceFolderPath;
                }
            }
            return traceFolderPath;
        }
    }
}
