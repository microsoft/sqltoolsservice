//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.SqlCore.Connection;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    public class CallbackAzureAccessTokenTests
    {
        private static DateTimeOffset UnexpiredTimestamp => DateTimeOffset.UtcNow.AddHours(1);

        private static Func<Task<(string token, DateTimeOffset expiresOn)>> SingleTokenFetcher(string token = "test-token")
        {
            return () => Task.FromResult((token, UnexpiredTimestamp));
        }

        [Test]
        public void GetAccessTokenFetchesOnFirstCall()
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
        public void GetAccessTokenReturnsCachedTokenBeforeNearExpiry()
        {
            int callCount = 0;
            // Token expires well in the future — cache should remain valid
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(10);
            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                callCount++;
                return Task.FromResult(("cached-tok", expiresOn));
            });

            callbackToken.GetAccessToken(); // primes cache
            string second = callbackToken.GetAccessToken();

            Assert.That(second, Is.EqualTo("cached-tok"));
            Assert.That(callCount, Is.EqualTo(1), "fetcher should only be called once when cache is still valid");
        }

        [Test]
        public void GetAccessTokenRefetchesTokenWhenWithinTwoMinutesOfExpiry()
        {
            int callCount = 0;
            // First call returns a token that is 90 seconds from expiry (inside the 2-minute threshold)
            var nearExpiry = DateTimeOffset.UtcNow.AddSeconds(90);
            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                callCount++;
                return Task.FromResult(("near-expiry-tok", nearExpiry));
            });

            callbackToken.GetAccessToken(); // first call — fetches and caches
            callbackToken.GetAccessToken(); // second call — cache is near-expiry so should re-fetch

            Assert.That(callCount, Is.EqualTo(2), "fetcher should be called again when cache is within 2 minutes of expiry");
        }

        [Test]
        public void GetAccessTokenRefetchesTokenWhenExpired()
        {
            int callCount = 0;
            // Token expired in the past
            var pastExpiry = DateTimeOffset.UtcNow.AddSeconds(-30);
            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                callCount++;
                return Task.FromResult(("expired-tok", pastExpiry));
            });

            callbackToken.GetAccessToken(); // first call — fetches and caches (with past expiry)
            callbackToken.GetAccessToken(); // second call — cache is expired so should re-fetch

            Assert.That(callCount, Is.EqualTo(2), "fetcher should be called again when cached token is expired");
        }

        [Test]
        public void GetAccessTokenThreadSafeDoesNotDoubleFetch()
        {
            int callCount = 0;
            var gate = new ManualResetEventSlim(false);

            var callbackToken = new CallbackAzureAccessToken(() =>
            {
                Interlocked.Increment(ref callCount);
                // Briefly yield to give the second thread a chance to enter before the first finishes
                gate.Wait(TimeSpan.FromMilliseconds(100));
                return Task.FromResult(("thread-tok", UnexpiredTimestamp));
            });

            // Launch two threads simultaneously against a cold cache
            var t1 = Task.Run(() => callbackToken.GetAccessToken());
            var t2 = Task.Run(() => callbackToken.GetAccessToken());
            gate.Set(); // allow the fetcher to complete
            Task.WaitAll(t1, t2);

            Assert.That(callCount, Is.EqualTo(1), "the lock should ensure the fetcher is called exactly once");
            Assert.That(t1.Result, Is.EqualTo("thread-tok"));
            Assert.That(t2.Result, Is.EqualTo("thread-tok"));
        }

        [Test]
        public void ConstructorThrowsArgumentNullExceptionWhenFetcherIsNull()
        {
            Assert.That(() => new CallbackAzureAccessToken(null), Throws.TypeOf<ArgumentNullException>());
        }
    }
}
