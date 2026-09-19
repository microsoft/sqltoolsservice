//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    internal static class WaitHandleAsyncExtensions
    {
        /// <summary>
        /// Waits for a handle to be signalled without holding a thread.
        /// </summary>
        /// <returns>True if the handle was signalled, false if the wait timed out.</returns>
        public static async Task<bool> WaitOneAsync(
            this WaitHandle handle,
            int millisecondsTimeout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (handle.WaitOne(0))
            {
                return true;
            }

            if (millisecondsTimeout == 0)
            {
                return false;
            }

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisteredWaitHandle registration = ThreadPool.RegisterWaitForSingleObject(
                handle,
                static (state, timedOut) => (state as TaskCompletionSource<bool>)?.TrySetResult(!timedOut),
                completion,
                millisecondsTimeout,
                executeOnlyOnce: true);
            using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
                static state => (state as TaskCompletionSource<bool>)?.TrySetCanceled(),
                completion);
            try
            {
                return await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                registration.Unregister(null);
            }
        }
    }
}
