//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Guards a document's parse and binding state. Unlike a Monitor it can be held across an
    /// await and released from any thread, and it is not reentrant.
    /// </summary>
    public sealed class MetadataLock
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public bool TryEnter(int millisecondsTimeout = 0)
        {
            return this.semaphore.Wait(millisecondsTimeout);
        }

        public Task<bool> TryEnterAsync(int millisecondsTimeout = 0, CancellationToken cancellationToken = default)
        {
            return this.semaphore.WaitAsync(millisecondsTimeout, cancellationToken);
        }

        public void Exit()
        {
            this.semaphore.Release();
        }
    }
}
