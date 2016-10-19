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
        }

        [Fact]
        public void LoggingDisabledWhenFlagNotProvided()
        {
            var args = new string[] {};
            CommandOptions options = new CommandOptions(args);
            Assert.NotNull(options);

            Assert.False(options.EnableLogging);
        }
    }
}
