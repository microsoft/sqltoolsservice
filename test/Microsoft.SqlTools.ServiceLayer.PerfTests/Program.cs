//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.Utility;


namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class Program
    {
        internal static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Microsoft.SqlTools.ServiceLayer.PerfTests.exe [tests]" + Environment.NewLine +
                                  "    [tests] is a space-separated list of tests to run." + Environment.NewLine +
                                  "            They are qualified within the Microsoft.SqlTools.ServiceLayer.TestDriver.PerfTests namespace" + Environment.NewLine +
                                  $"Be sure to set the environment variable {ServiceTestDriver.ServiceHostEnvironmentVariable} to the full path of the sqltoolsservice executable.");
                return 0;
            }

            Logger.Initialize("testdriver", LogLevel.Verbose);

            return TestRunner.RunTests(args, "Microsoft.SqlTools.ServiceLayer.PerfTests.").Result;
        }
    }
}

