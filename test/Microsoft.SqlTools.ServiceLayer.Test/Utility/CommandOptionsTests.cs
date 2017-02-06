//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.Test.Utility
{
    /// <summary>
    /// Tests for the CommandOptions class
    /// </summary>
    public class CommandOptionsTests
    {
        [Fact]
        public void LoggingEnabledWhenFlagProvided()
        {
            var args = new string[] {"--enable-logging"};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.EnableLogging);
            Assert.False(options.ShouldExit);
            Assert.Null(options.Locale);
        }

        [Fact]
        public void LoggingDisabledWhenFlagNotProvided()
        {
            var args = new string[] {};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.False(options.EnableLogging);
            Assert.False(options.ShouldExit);
            Assert.Null(options.Locale);
        }

        [Fact]
        public void UsageIsShownWhenHelpFlagProvided()
        {
            var args = new string[] {"--help"};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.Null(options.Locale);
        }

        [Fact]
        public void UsageIsShownWhenBadArgumentsProvided()
        {
            var args = new string[] {"--unknown-argument", "/bad-argument"};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.ShouldExit);
            Assert.Null(options.Locale);
        }

        [Fact]
        public void DefaultValuesAreUsedWhenNoArgumentsAreProvided()
        {
            var args = new string[] {};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.False(options.EnableLogging);
            Assert.False(options.ShouldExit);
            Assert.Null(options.Locale);
        }
        
        [Fact]
        public void LocaleSetWhenProvided()
        {
            var args = new string[] {"--locale en"};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.True(options.EnableLogging);
            Assert.False(options.ShouldExit);
            Assert.Equal(options.Locale, "en");
        }

        [Fact]
        public void LocaleNotSetWhenNotProvided()
        {
            var args = new string[] {};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.False(options.EnableLogging);
            Assert.False(options.ShouldExit);
            Assert.Null(options.Locale);
        }
    }
}
