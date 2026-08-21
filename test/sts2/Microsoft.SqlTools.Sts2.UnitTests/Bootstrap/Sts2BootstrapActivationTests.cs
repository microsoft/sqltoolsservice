//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Sts2.Hosting;
using Xunit;
using Sts2BootstrapClass = Microsoft.SqlTools.Sts2.Bootstrap.Sts2Bootstrap;

namespace Microsoft.SqlTools.Sts2.UnitTests.Bootstrap
{
    /// <summary>SPEC §5.2: STS2 activates only via --enable-sts2 or STS_ENABLE_STS2=1.</summary>
    [Collection("environment")] // env-var mutation must not run concurrently with itself
    public class Sts2BootstrapActivationTests : IDisposable
    {
        private readonly string? originalEnableEnvironmentValue;

        public Sts2BootstrapActivationTests()
        {
            originalEnableEnvironmentValue = Environment.GetEnvironmentVariable(
                Sts2BootstrapClass.EnableEnvironmentVariable);
            Environment.SetEnvironmentVariable(Sts2BootstrapClass.EnableEnvironmentVariable, null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(
                Sts2BootstrapClass.EnableEnvironmentVariable,
                originalEnableEnvironmentValue);
        }

        [Fact]
        public void DisabledByDefault()
        {
            Assert.False(Sts2BootstrapClass.IsEnabled(["--log-file", "x.log", "--enable-logging"]));
            Assert.False(Sts2BootstrapClass.IsEnabled([]));
        }

        [Theory]
        [InlineData("--enable-sts2")]
        [InlineData("--ENABLE-STS2")]
        public void FlagEnables(string flag)
        {
            Assert.True(Sts2BootstrapClass.IsEnabled(["--log-file", "x.log", flag]));
        }

        [Fact]
        public void EnvironmentVariableOneEnables()
        {
            Environment.SetEnvironmentVariable(Sts2BootstrapClass.EnableEnvironmentVariable, "1");
            Assert.True(Sts2BootstrapClass.IsEnabled([]));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("true")]
        [InlineData("")]
        public void OtherEnvironmentValuesDoNotEnable(string value)
        {
            Environment.SetEnvironmentVariable(Sts2BootstrapClass.EnableEnvironmentVariable, value);
            Assert.False(Sts2BootstrapClass.IsEnabled([]));
        }

        [Fact]
        public void DisabledHandleExposesNullStreams()
        {
            var handle = Sts2BootstrapClass.TryStart(["--log-file", "x.log"], logFilePath: null);
            Assert.False(handle.IsEnabled);
            Assert.Null(handle.LegacyInputStream);
            Assert.Null(handle.LegacyOutputStream);
        }

        [Fact]
        public void FatalReasonExposesTypeButNotExceptionMessage()
        {
            const string canary = "secret-connection-string-and-sql";

            string reason = Sts2Session.RedactedFailureReason(
                "coordinator",
                new InvalidOperationException(canary));

            Assert.Contains(nameof(InvalidOperationException), reason, StringComparison.Ordinal);
            Assert.DoesNotContain(canary, reason, StringComparison.Ordinal);
        }
    }
}
