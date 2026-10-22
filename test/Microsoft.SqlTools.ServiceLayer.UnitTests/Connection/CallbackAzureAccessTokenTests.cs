//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.SqlCore.Connection;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    public class CallbackAzureAccessTokenTests
    {
        private static DateTimeOffset UnexpiredTimestamp => DateTimeOffset.UtcNow.AddHours(1);

        [Test]
        public void GetAccessTokenReturnsFetchedToken()
        {
            int callCount = 0;
            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                callCount++;
                return Task.FromResult(("tok1", UnexpiredTimestamp));
            });

            string result = callbackToken.GetAccessToken();

            Assert.That(result, Is.EqualTo("tok1"));
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void GetAccessTokenFetchesOnEveryCallEvenWhenTokenIsUnexpired()
        {
            int callCount = 0;
            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                callCount++;
                return Task.FromResult(($"tok{callCount}", UnexpiredTimestamp));
            });

            string first = callbackToken.GetAccessToken();
            string second = callbackToken.GetAccessToken();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo("tok1"));
                Assert.That(second, Is.EqualTo("tok2"),
                    "a token the client refreshed (e.g. after its token cache was cleared) should be used right away");
                Assert.That(callCount, Is.EqualTo(2), "tokens should not be cached; the client owns the token cache");
            });
        }

        [Test]
        public void GetAccessTokenPropagatesFetchFailure()
        {
            var callbackToken = new CallbackAzureAccessToken(
                () => Task.FromException<(string, DateTimeOffset)>(new InvalidOperationException("fetch failed")));

            Assert.Throws<InvalidOperationException>(() => callbackToken.GetAccessToken());
        }
    }
}
