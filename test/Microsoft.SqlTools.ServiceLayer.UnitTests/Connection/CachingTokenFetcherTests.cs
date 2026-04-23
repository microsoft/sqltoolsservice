//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.SqlCore.Connection;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    public class CachingTokenFetcherTests
    {
        private static readonly TimeSpan EarlyRefresh = CallbackAzureAccessToken.EarlyRefreshWindow;

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static DateTimeOffset FarFuture => DateTimeOffset.UtcNow.AddHours(1);
        private static DateTimeOffset JustExpired => DateTimeOffset.UtcNow - EarlyRefresh - TimeSpan.FromSeconds(1);
        private static DateTimeOffset NearExpiry => DateTimeOffset.UtcNow + EarlyRefresh - TimeSpan.FromSeconds(1);

        private static CachingTokenFetcher MakeFetcher(
            IEnumerable<(string token, DateTimeOffset expiresOn)> responses)
        {
            using var enumerator = responses.GetEnumerator();
            return new CachingTokenFetcher(() =>
            {
                enumerator.MoveNext();
                return Task.FromResult(enumerator.Current);
            });
        }

        // ---------------------------------------------------------------
        // IsCacheValid
        // ---------------------------------------------------------------

        [Test]
        public void IsCacheValidReturnsFalseBeforeFirstFetch()
        {
            var fetcher = new CachingTokenFetcher(() => Task.FromResult(("tok", FarFuture)));
            Assert.That(fetcher.IsCacheValid(), Is.False);
        }

        [Test]
        public async Task IsCacheValidReturnsTrueAfterFetchWithFarFutureExpiry()
        {
            var fetcher = new CachingTokenFetcher(() => Task.FromResult(("tok", FarFuture)));
            await fetcher.GetTokenAsync();
            Assert.That(fetcher.IsCacheValid(), Is.True);
        }

        [Test]
        public async Task IsCacheValidReturnsFalseWhenTokenIsWithinEarlyRefreshWindow()
        {
            var fetcher = new CachingTokenFetcher(() => Task.FromResult(("tok", NearExpiry)));
            await fetcher.GetTokenAsync();
            // Token expires within the early-refresh window — cache should be considered stale.
            Assert.That(fetcher.IsCacheValid(), Is.False);
        }

        // ---------------------------------------------------------------
        // GetTokenAsync — caching behaviour
        // ---------------------------------------------------------------

        [Test]
        public async Task GetTokenAsyncCallsInnerOnFirstCall()
        {
            int callCount = 0;
            var fetcher = new CachingTokenFetcher(() =>
            {
                callCount++;
                return Task.FromResult(("tok", FarFuture));
            });

            await fetcher.GetTokenAsync();

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetTokenAsyncReturnsCachedTokenOnSubsequentCalls()
        {
            int callCount = 0;
            var fetcher = new CachingTokenFetcher(() =>
            {
                callCount++;
                return Task.FromResult(("tok", FarFuture));
            });

            var (first, _) = await fetcher.GetTokenAsync();
            var (second, _) = await fetcher.GetTokenAsync();
            var (third, _) = await fetcher.GetTokenAsync();

            Assert.That(callCount, Is.EqualTo(1), "inner should only be called once while the token is valid");
            Assert.That(second, Is.EqualTo(first));
            Assert.That(third, Is.EqualTo(first));
        }

        [Test]
        public async Task GetTokenAsyncRefreshesWhenCachedTokenIsNearExpiry()
        {
            int callCount = 0;
            var fetcher = new CachingTokenFetcher(() =>
            {
                callCount++;
                // First call returns a token that is already inside the refresh window.
                // Second call returns a long-lived token.
                return callCount == 1
                    ? Task.FromResult(("near-expiry-tok", NearExpiry))
                    : Task.FromResult(("fresh-tok", FarFuture));
            });

            var (first, _) = await fetcher.GetTokenAsync();   // fetches (near-expiry)
            var (second, _) = await fetcher.GetTokenAsync();  // near expiry → fetches again

            Assert.That(callCount, Is.EqualTo(2), "inner should be called again when cached token is near expiry");
            Assert.That(first, Is.EqualTo("near-expiry-tok"));
            Assert.That(second, Is.EqualTo("fresh-tok"));
        }

        [Test]
        public async Task GetTokenAsyncRefetchesAfterTokenExpires()
        {
            // Simulate a token that was already past the refresh window from the start.
            int callCount = 0;
            var fetcher = new CachingTokenFetcher(() =>
            {
                callCount++;
                return Task.FromResult(($"tok-{callCount}", JustExpired));
            });

            // Both calls should trigger a fetch because the returned token is always stale.
            await fetcher.GetTokenAsync();
            await fetcher.GetTokenAsync();

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public async Task GetTokenAsyncReturnsCorrectValues()
        {
            var expected = ("my-token", FarFuture);
            var fetcher = new CachingTokenFetcher(() => Task.FromResult(expected));

            var (token, expiresOn) = await fetcher.GetTokenAsync();

            Assert.That(token, Is.EqualTo(expected.Item1));
            Assert.That(expiresOn, Is.EqualTo(expected.Item2));
        }

        // ---------------------------------------------------------------
        // Thundering-herd prevention
        // ---------------------------------------------------------------

        [Test]
        public async Task GetTokenAsyncCallsInnerOnlyOnceUnderConcurrentRequests()
        {
            int callCount = 0;
            // Use a TaskCompletionSource to make the inner delegate take a measurable amount of
            // time, so multiple concurrent callers are queued on the semaphore.
            var gate = new TaskCompletionSource<bool>();

            var fetcher = new CachingTokenFetcher(async () =>
            {
                await gate.Task;
                callCount++;
                return ("tok", FarFuture);
            });

            // Launch 10 concurrent callers before the inner delegate can return.
            var tasks = new Task<(string, DateTimeOffset)>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = fetcher.GetTokenAsync();
            }

            // Unblock the inner delegate.
            gate.SetResult(true);
            await Task.WhenAll(tasks);

            Assert.That(callCount, Is.EqualTo(1),
                "inner should only be called once even when many callers race simultaneously");

            foreach (var t in tasks)
            {
                Assert.That(t.Result.Item1, Is.EqualTo("tok"));
            }
        }

        // ---------------------------------------------------------------
        // Constructor guard
        // ---------------------------------------------------------------

        [Test]
        public void ConstructorThrowsWhenInnerIsNull()
        {
            Assert.That(
                () => new CachingTokenFetcher(null),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
