//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// A lock that guards a document's parse and binding state.
    /// </summary>
    /// <remarks>
    /// This replaces a Monitor so that a holder can await the binding queue while holding the
    /// lock. A Monitor is bound to the thread that entered it, which forced every holder to block
    /// a thread pool thread for the whole wait; enough of those blocked threads exhaust the pool
    /// that the binding queue itself needs to run, and the service deadlocks. Unlike a Monitor
    /// this lock is not reentrant: a holder must not try to enter it again.
    /// </remarks>
    public sealed class MetadataLock
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Tries to enter the lock, blocking the calling thread for at most
        /// <paramref name="millisecondsTimeout"/>. Prefer <see cref="TryEnterAsync"/> from async
        /// code so the wait does not occupy a thread.
        /// </summary>
        public bool TryEnter(int millisecondsTimeout = 0)
        {
            return this.semaphore.Wait(millisecondsTimeout);
        }

        /// <summary>
        /// Tries to enter the lock without occupying a thread while waiting.
        /// </summary>
        public Task<bool> TryEnterAsync(
            int millisecondsTimeout = 0,
            CancellationToken cancellationToken = default)
        {
            return this.semaphore.WaitAsync(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Releases the lock. May be called from any thread, not only the one that entered.
        /// </summary>
        public void Exit()
        {
            this.semaphore.Release();
        }
    }
}
