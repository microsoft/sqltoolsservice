//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// Tests for the CommandOptions class
    /// </summary>
    public class CommandOptionsTests
    {

        [Fact]
        public void UsageIsShownWhenHelpFlagProvided()
        {
            var args = new string[] {"--help"};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.Equal(options.Locale, string.Empty);
        }

        [Fact]
        public void UsageIsShownWhenBadArgumentsProvided()
        {
            var args = new string[] {"--unknown-argument", "/bad-argument"};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.Equal(options.Locale, string.Empty);
        }

        [Fact]
        public void DefaultValuesAreUsedWhenNoArgumentsAreProvided()
        {
            var args = new string[] { };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);
            Assert.NotNull(options);

            Assert.Equal(string.Empty, options.ErrorMessage);
            Assert.Null(options.TracingLevel);
            Assert.Null(options.LogFilePath);
            Assert.False(options.ShouldExit);
            Assert.False(string.IsNullOrWhiteSpace(options.LoggingDirectory));
            Assert.Equal(Path.Combine(options.DefaultLogRoot, options.ServiceName), options.LoggingDirectory);
            Assert.Equal(options.Locale, string.Empty);
        }
        
        [Theory]
        [InlineData("en")]
        [InlineData("es")]
        public void LocaleSetWhenProvided(string locale)
        {
            var args = new string[] {"--locale", locale};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args); ;

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.Locale, locale);
        }

        [Fact]
        public void ShouldExitNotSetWhenInvalidLocale()
        {
            string locale = "invalid";
            var args = new string[] { "--locale", locale };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
        }

        [Fact]
        public void LocaleNotSetWhenNotProvided()
        {
            var args = new string[] {};
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.Locale, string.Empty);
        }

        [Fact]
        public void LoggingDirectorySet()
        {
            string logDir = Directory.GetCurrentDirectory();
            var args = new string[] { "--log-dir", logDir };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.LoggingDirectory, logDir);
        }


        [Fact]
        public void TracingLevelSet()
        {
            string expectedLevel = "Critical";
            var args = new string[] { "--tracing-level", expectedLevel };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.TracingLevel, expectedLevel);
        }


        [Fact]
        public void LogFilePathSet()
        {
            string expectedFilePath = Path.GetRandomFileName();
            var args = new string[] { "--log-file", expectedFilePath };
            ServiceLayerCommandOptions options = new ServiceLayerCommandOptions(args);

            // Asserting all options were properly set 
            Assert.NotNull(options);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.LogFilePath, expectedFilePath);
        }
    }
}
