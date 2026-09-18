//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// Awaitable waits on wait handles that do not occupy a thread while waiting.
    /// </summary>
    internal static class WaitHandleAsyncExtensions
    {
        /// <summary>
        /// Waits for <paramref name="handle"/> to be signalled without blocking a thread. The
        /// wait is parked on the runtime's wait thread and the continuation runs once the handle
        /// is signalled or <paramref name="millisecondsTimeout"/> elapses.
        /// </summary>
        /// <returns>True if the handle was signalled, false if the wait timed out.</returns>
        public static async Task<bool> WaitOneAsync(
            this WaitHandle handle,
            int millisecondsTimeout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // A zero-timeout probe does not block; it is the fast path for an already-signalled handle.
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
                static (state, timedOut) =>
                {
                    if (state is TaskCompletionSource<bool> source)
                    {
                        source.TrySetResult(!timedOut);
                    }
                },
                completion,
                millisecondsTimeout,
                executeOnlyOnce: true);
            using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    if (state is TaskCompletionSource<bool> source)
                    {
                        source.TrySetCanceled();
                    }
                },
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
