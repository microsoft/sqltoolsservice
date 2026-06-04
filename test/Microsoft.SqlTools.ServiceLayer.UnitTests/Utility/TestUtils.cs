//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public static class TestUtils
    {

        /// <summary>
        /// Wait for a condition to be true for a limited amount of time.
        /// </summary>
        /// <param name="condition">Function that returns a boolean on a condition</param>
        /// <param name="intervalMilliseconds">Number of milliseconds to wait between test intervals.</param>
        /// <param name="intervalCount">Number of test intervals to perform before giving up.</param>
        /// <returns>True if the condition was met before the test interval limit.</returns>
        public static bool WaitFor(Func<bool> condition, int intervalMilliseconds = 10, int intervalCount = 200)
        {
            int count = 0;
            while (count++ < intervalCount && !condition.Invoke())
            {
                Thread.Sleep(intervalMilliseconds);
            }

            return (count < intervalCount);
        }


        public static async Task RunAndVerify<T>(Func<Task<T>> test, Action<T> verify)
        {
            T result = await test();
            verify(result);
        }

        public static void VerifyResult<U>(U expected, U actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void VerifyResult<T>(Action<T> verify, T actual)
        {
            verify(actual);
        }

    }
}
