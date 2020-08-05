//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// Tests for the CommandOptions class
    /// </summary>
    public class CommandOptionsTests
    {

        [Test]
        public void UsageIsShownWhenHelpFlagProvided()
        {
            var args = new string[] {"--help"};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.AreEqual(options.Locale, string.Empty);
        }

        [Test]
        public void UsageIsShownWhenBadArgumentsProvided()
        {
            var args = new string[] {"--unknown-argument", "/bad-argument"};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.AreEqual(options.Locale, string.Empty);
        }

        [Test]
        public void DefaultValuesAreUsedWhenNoArgumentsAreProvided()
        {
            int? testNo = 1;
            // Test 1: All defaults, no options specified
            {
                var args = new string[] { };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++);
            }
            // Test 2: All defaults, -autoflush-log as  null 
            {
                var args = new string[] { "--autoflush-log", null };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, autoFlushLog: true);
            }
            // Test 3: All defaults, -autoflush-log as  empty string 
            {
                var args = new string[] { "--autoflush-log", string.Empty };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, autoFlushLog: true);
            }
            // Test 4: All defaults, -tracing-level as  empty string 
            {
                var args = new string[] { "--tracing-level", string.Empty };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, tracingLevel: string.Empty);
            }
            // Test 5: All defaults, -tracing-level as  null 
            {
                var args = new string[] { "--tracing-level", null };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, tracingLevel: null);
            }
            // Test 6: All defaults, -log-file as  empty string 
            {
                var args = new string[] { "--log-file", string.Empty };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, logFilePath: string.Empty);
            }
            // Test 6: All defaults, -log-file as null 
            {
                var args = new string[] { "--log-file", null };
                ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
                VerifyCommandOptions(options, testNo++, logFilePath: null);
            }
        }

        private static void VerifyCommandOptions(ServiceLayerCommandOptions options, int? testNo = null, string errorMessage = "", string tracingLevel = null, string logFilePath = null, bool shouldExit = false, string locale = "", bool autoFlushLog = false)
        {
            Assert.NotNull(options);
            string MsgPrefix = testNo != null ? $"TestNo:{testNo} ::" : string.Empty;
            Assert.True(errorMessage == options.ErrorMessage, $"{MsgPrefix} options:{nameof(errorMessage)} should be '{errorMessage}'");
            Assert.True(tracingLevel == options.TracingLevel, $"{MsgPrefix} options:{nameof(tracingLevel)} should be '{tracingLevel}'");
            Assert.True(logFilePath == options.LogFilePath, $"{MsgPrefix} options:{nameof(logFilePath)} should be '{logFilePath}'");
            Assert.True(shouldExit == options.ShouldExit, $"{MsgPrefix} options:{nameof(shouldExit)} should be '{shouldExit}'");
            Assert.True(autoFlushLog == options.AutoFlushLog, $"{MsgPrefix} options:{nameof(autoFlushLog)} should be '{autoFlushLog}'");
            Assert.True(options.Locale == locale, $"{MsgPrefix} options:{nameof(locale)} should be '{locale}'");
        }

        [Test]
        public void LocaleSetWhenProvided([Values("en", "es")] string locale)
        {
            var args = new string[] {"--locale", locale};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args); ;

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.AreEqual(options.Locale, locale);
        }

        [Test]
        public void ShouldExitNotSetWhenInvalidLocale()
        {
            string locale = "invalid";
            var args = new string[] { "--locale", locale };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
        }

        [Test]
        public void LocaleNotSetWhenNotProvided()
        {
            var args = new string[] {};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.AreEqual(options.Locale, string.Empty);
        }


        [Test]
        public void TracingLevelSet()
        {
            string expectedLevel = "Information";
            var args = new string[] { "--tracing-level", expectedLevel };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.AreEqual(options.TracingLevel, expectedLevel);
        }

        [Test]
        public void AutoFlushLogSet()
        {
            bool expectedAutoFlush = true;
            var args = new string[] { "--autoflush-log"};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.AreEqual(options.AutoFlushLog, expectedAutoFlush);
        }

        [Test]
        public void LogFilePathSet()
        {
            string expectedFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var args = new string[] { "--log-file", expectedFilePath };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.AreEqual(options.LogFilePath, expectedFilePath);
        }
    }
}
