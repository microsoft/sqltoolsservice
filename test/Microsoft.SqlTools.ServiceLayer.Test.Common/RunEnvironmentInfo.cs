
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Contains environment information needed when running tests. 
    /// </summary>
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
            string testPath = Path.Combine("test", "Microsoft.SqlTools.ServiceLayer.Test.Common", "TestData");
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
                    // We are running tests locally, which means we expect to be running inside the bin\debug\netcoreapp directory
                    // Test Files should be found at the root of the project so go back the necessary number of directories for this 
                    // to be found. We are manually specifying the testFolderPath here for clarity on where to expect this

                    string assemblyDir = Path.GetDirectoryName(typeof(Scripts).GetTypeInfo().Assembly.Location);
                    string defaultPath = Path.Combine(assemblyDir, GoUpNDirectories(4));
                    testFolderPath = Path.Combine(defaultPath, "Microsoft.SqlTools.ServiceLayer.Test.Common", "TestData");

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

        private static string GoUpNDirectories(int n)
        {
            string up = ".." + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/");
            return string.Concat(Enumerable.Repeat(up, n));

        }
    }
}
