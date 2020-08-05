//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Hosting.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.UtilityTests
{
    [TestFixture]
    /// <summary>
    /// Logger test cases
    /// </summary>
    public class LoggerTests
    {
       
        /// <summary>
        /// Test to verify that the logger initialization is generating a valid file
        /// </summary>
        [Test]
        public void LoggerDefaultFile()
        {
            // delete any existing log files from the current directory 
            Directory.GetFiles(Directory.GetCurrentDirectory())
                .Where(fileName 
                     => fileName.Contains("sqltools_") 
                     && fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(File.Delete);

            Logger.Initialize(
                logFilePath: Path.Combine(Directory.GetCurrentDirectory(), "sqltools"),
                tracingLevel: SourceLevels.Verbose);

            // Write a test message.
            string logMessage = $"Message from {nameof(LoggerDefaultFile)} test";
            Logger.Write(TraceEventType.Information, logMessage);

            // close the logger
            Logger.Close();

            // find the name of the new log file
            string logFileName = Logger.LogFileFullPath;
                
            // validate the log file was created with desired name 
            Assert.True(!string.IsNullOrWhiteSpace(logFileName));
            if (!string.IsNullOrWhiteSpace(logFileName))
            {
                Assert.True(logFileName.Length > "sqltools_.log".Length);
                Assert.True(File.Exists(logFileName), $"the log file: {logFileName} must exist");
                //Ensure that our log message exists in the log file
                Assert.True(File.ReadAllText(logFileName).Contains(logMessage, StringComparison.InvariantCultureIgnoreCase), $"the log message:'{logMessage}' must be present in the log file");
                // delete the test log file
                if (File.Exists(logFileName))
                {
                    File.Delete(logFileName);
                }
            }
        }
    }
}
