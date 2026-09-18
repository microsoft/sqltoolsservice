//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    public class AsyncLockTests
    {
        [Test]
        public async Task AsyncLockSynchronizesAccess()
        {
            AsyncLock asyncLock = new AsyncLock();

            Task<IDisposable> lockOne = asyncLock.LockAsync();
            Task<IDisposable> lockTwo = asyncLock.LockAsync();

            Assert.AreEqual(TaskStatus.RanToCompletion, lockOne.Status);
            Assert.AreEqual(TaskStatus.WaitingForActivation, lockTwo.Status);
            lockOne.Result.Dispose();

            await lockTwo;
            Assert.AreEqual(TaskStatus.RanToCompletion, lockTwo.Status);
        }

        [Test]
        public void AsyncLockCancelsWhenRequested()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            AsyncLock asyncLock = new AsyncLock();

            Task<IDisposable> lockOne = asyncLock.LockAsync();
            Task<IDisposable> lockTwo = asyncLock.LockAsync(cts.Token);

            // Cancel the second lock before the first is released
            cts.Cancel();
            lockOne.Result.Dispose();

            Assert.AreEqual(TaskStatus.RanToCompletion, lockOne.Status);
            Assert.That(async () => await lockTwo, Throws.InstanceOf<OperationCanceledException>());
            Assert.AreEqual(TaskStatus.Canceled, lockTwo.Status);
        }

        [Test]
        public void AsyncLockRejectsPreCanceledAcquisition()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var asyncLock = new AsyncLock();

            Task<IDisposable> canceledLock = asyncLock.LockAsync(cts.Token);

            Assert.That(async () => await canceledLock, Throws.InstanceOf<OperationCanceledException>());
            Assert.That(asyncLock.LockAsync().IsCompletedSuccessfully, Is.True,
                "a canceled acquisition must not consume or release the semaphore");
        }
    }
}
