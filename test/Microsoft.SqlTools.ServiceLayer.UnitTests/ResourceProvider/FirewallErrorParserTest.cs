//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.ResourceProvider.Core.FirewallRule;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
{
    /// <summary>
    /// Tests to verify FirewallErrorParser 
    /// </summary>
    public class FirewallErrorParserTest
    {
        private const int SqlAzureFirewallBlockedErrorNumber = 40615; 
        private const int SqlAzureLoginFailedErrorNumber = 18456;
        private string _errorMessage = "error Message with 1.2.3.4 as IP address";
        private FirewallErrorParser _firewallErrorParser = new FirewallErrorParser();


        [Fact]
        public void ParseExceptionShouldThrowExceptionGivenNullErrorMessage()
        {
            string errorMessage = null;
            int errorCode = SqlAzureFirewallBlockedErrorNumber;

            Assert.Throws<ArgumentNullException>("errorMessage", () =>
            {
                FirewallParserResponse response = _firewallErrorParser.ParseErrorMessage(errorMessage, errorCode);
                Assert.False(response.FirewallRuleErrorDetected);
            });
        }

        [Fact]
        public void ParseExceptionShouldReturnFireWallRuleNotDetectedGivenDifferentError()
        {
            int errorCode = 123;

            FirewallParserResponse response = _firewallErrorParser.ParseErrorMessage(_errorMessage, errorCode);
            Assert.False(response.FirewallRuleErrorDetected);
        }

        [Fact]
        public void ParseExceptionShouldReturnFireWallRuleNotDetectedGivenLoginFailedError()
        {
            int errorCode = SqlAzureLoginFailedErrorNumber;

            FirewallParserResponse response = _firewallErrorParser.ParseErrorMessage(_errorMessage, errorCode);
            Assert.False(response.FirewallRuleErrorDetected);
        }

        [Fact]
        public void ParseExceptionShouldReturnFireWallRuleNotDetectedGivenInvalidErrorMessage()
        {
            int errorCode = SqlAzureFirewallBlockedErrorNumber;
            string errorMessage = "error Message with no IP address";
            FirewallParserResponse response = _firewallErrorParser.ParseErrorMessage(errorMessage, errorCode);
            Assert.False(response.FirewallRuleErrorDetected);
        }

        [Fact]
        public void ParseExceptionShouldReturnFireWallRuleDetectedGivenValidErrorMessage()
        {
            int errorCode = SqlAzureFirewallBlockedErrorNumber;
            FirewallParserResponse response = _firewallErrorParser.ParseErrorMessage(_errorMessage, errorCode);
            Assert.True(response.FirewallRuleErrorDetected);
            Assert.Equal(response.BlockedIpAddress.ToString(), "1.2.3.4");
        }
    }
}
