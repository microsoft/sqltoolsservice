//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Utility
{
    /// <summary>
    /// Provides a simple wrapper over a SemaphoreSlim to allow
    /// synchronization locking inside of async calls.  Cannot be
    /// used recursively.
    /// </summary>
    public class AsyncLock
    {
        #region Fields

        private Task<IDisposable> lockReleaseTask;
        private SemaphoreSlim lockSemaphore = new SemaphoreSlim(1, 1);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AsyncLock class.
        /// </summary>
        public AsyncLock()
        {
            this.lockReleaseTask =
                Task.FromResult(
                    (IDisposable)new LockReleaser(this));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Locks
        /// </summary>
        /// <returns>A task which has an IDisposable</returns>
        public Task<IDisposable> LockAsync()
        {
            return this.LockAsync(CancellationToken.None);
        }

        /// <summary>
        /// Obtains or waits for a lock which can be used to synchronize
        /// access to a resource.  The wait may be cancelled with the
        /// given CancellationToken.
        /// </summary>
        /// <param name="cancellationToken">
        /// A CancellationToken which can be used to cancel the lock.
        /// </param>
        /// <returns></returns>
        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            await lockSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                // Cancellation can race with a release. If the semaphore granted this waiter at
                // the same moment the token was cancelled, return the permit before propagating
                // cancellation so the lock cannot be lost.
                lockSemaphore.Release();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return await this.lockReleaseTask;
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// Provides an IDisposable wrapper around an AsyncLock so
        /// that it can easily be used inside of a 'using' block.
        /// </summary>
        private class LockReleaser : IDisposable
        {
            private AsyncLock lockToRelease;

            internal LockReleaser(AsyncLock lockToRelease)
            {
                this.lockToRelease = lockToRelease;
            }

            public void Dispose()
            {
                this.lockToRelease.lockSemaphore.Release();
            }
        }

        #endregion
    }
}

