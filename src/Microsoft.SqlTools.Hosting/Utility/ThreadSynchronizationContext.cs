//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.SqlTools.Utility
{
    /// <summary>
    /// Provides a SynchronizationContext implementation that can be used
    /// in console applications or any thread which doesn't have its
    /// own SynchronizationContext.
    /// </summary>
    public class ThreadSynchronizationContext : SynchronizationContext
    {
        #region Private Fields

        private BlockingCollection<Tuple<SendOrPostCallback, object>> requestQueue =
            new BlockingCollection<Tuple<SendOrPostCallback, object>>();

        #endregion

        #region Constructors

        /// <summary>
        /// Posts a request for execution to the SynchronizationContext.
        /// This will be executed on the SynchronizationContext's thread.
        /// </summary>
        /// <param name="callback">
        /// The callback to be invoked on the SynchronizationContext's thread.
        /// </param>
        /// <param name="state">
        /// A state object to pass along to the callback when executed through
        /// the SynchronizationContext.
        /// </param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            // If the loop has already been shut down, silently drop the post rather than
            // crashing the caller. This can happen when an async event (e.g. a task-added
            // notification) fires on a thread-pool thread after EndLoop() has been called
            // during teardown. The check + Add is not atomic, so we also catch the race.
            if (this.requestQueue.IsAddingCompleted)
            {
                return;
            }

            try
            {
                this.requestQueue.Add(
                    new Tuple<SendOrPostCallback, object>(
                        callback, state));
            }
            catch (InvalidOperationException)
            {
                // CompleteAdding() was called between the IsAddingCompleted check and Add —
                // the loop is shutting down, so there is nothing to dispatch to.
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the SynchronizationContext message loop on the current thread.
        /// </summary>
        public void RunLoopOnCurrentThread()
        {
            Tuple<SendOrPostCallback, object> request;

            while (this.requestQueue.TryTake(out request, Timeout.Infinite))
            {
                // Invoke the request's callback
                request.Item1(request.Item2);
            }
        }

        /// <summary>
        /// Ends the SynchronizationContext message loop.
        /// </summary>
        public void EndLoop()
        {
            // Tell the blocking queue that we're done
            this.requestQueue.CompleteAdding();
        }

        #endregion
    }
}
