//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    /// <summary>
    /// A binding queue request and, once the queue has finished with it, its outcome
    /// </summary>
    public class QueueItem
    {
        private static long nextId;

        /// <summary>
        /// QueueItem constructor
        /// </summary>
        public QueueItem()
        {
            this.Id = Interlocked.Increment(ref nextId);
            this.Lifetime = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets an identifier used to correlate this item in queue logs.
        /// </summary>
        internal long Id { get; }

        /// <summary>
        /// Gets elapsed time since the item was queued.
        /// </summary>
        internal Stopwatch Lifetime { get; }

        /// <summary>
        /// Gets or sets the queue item key
        /// </summary>
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets the bind operation callback method
        /// </summary>
        public Func<IBindingContext, CancellationToken, Task<object?>> BindOperation { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
        public Func<IBindingContext, object?>? TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets the operation to call if the bind operation encounters an unexpected exception.
        /// Supports returning an object in case of the exception occurring since in some cases we need to be
        /// tolerant of error cases and still return some value
        /// </summary>
        public Func<Exception, object?>? ErrorHandler { get; set; }

        /// <summary>
        /// Gets or sets the result of the queued task
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets whether the binding operation started. A false value after the item
        /// completes means the item was not executed.
        /// </summary>
        internal bool WasExecuted { get; set; }

        /// <summary>
        /// Gets or sets whether the item completed through a lock or operation timeout, or was
        /// abandoned before it ran.
        /// </summary>
        internal bool TimedOut { get; set; }

        /// <summary>
        /// Gets or sets the slow-operation threshold in milliseconds. When
        /// <see cref="HardTimeout"/> is not set, this is also the hard timeout.
        /// </summary>
        public int? BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum time the caller waits for the binding operation.
        /// </summary>
        public int? HardTimeout { get; set; }

        /// <summary>
        /// Gets or sets the timeout for how long to wait for the binding lock
        /// </summary>
        public int? WaitForLockTimeout { get; set; }

        /// <summary>
        /// Converts the result of the execution to type T
        /// </summary>
        public T? GetResultAsT<T>() where T : class
        {
            return this.Result as T;
        }
    }
}
