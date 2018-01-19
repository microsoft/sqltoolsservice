//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Dmp.Hosting.Utility;
using Xunit;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.UtilityTests
{
    /// <summary>
    /// Logger test cases
    /// </summary>
    public class LoggerTests
    {
       
        /// <summary>
        /// Test to verify that the logger initialization is generating a valid file
        /// </summary>
        [Fact]
        public void LoggerDefaultFile()
        {
            // delete any existing log files from the current directory 
            Directory.GetFiles(Directory.GetCurrentDirectory())
                .Where(fileName 
                     => fileName.Contains("sqltools_") 
                     && fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(File.Delete);

            // initialize the logger
            Logger.Initialize(
                logFilePath: Path.Combine(Directory.GetCurrentDirectory(), "sqltools"),
                minimumLogLevel: LogLevel.Verbose);

            // close the logger
            Logger.Close();

            // find the name of the new log file
            string logFileName = Directory.GetFiles(Directory.GetCurrentDirectory())
                .SingleOrDefault(fileName =>
                    fileName.Contains("sqltools_")
                    && fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase));
                
            // validate the log file was created with desired name 
            Assert.True(!string.IsNullOrWhiteSpace(logFileName));
            if (!string.IsNullOrWhiteSpace(logFileName))
            {
                Assert.True(logFileName.Length > "sqltools_.log".Length);
                Assert.True(File.Exists(logFileName));

                // delete the test log file
                if (File.Exists(logFileName))
                {
                    File.Delete(logFileName);
                }
            }
        }
    }
}
