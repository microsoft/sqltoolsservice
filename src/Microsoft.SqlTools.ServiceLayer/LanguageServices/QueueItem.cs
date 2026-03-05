//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class that stores the state of a binding queue request item
    /// </summary>    
    public class QueueItem
    {
        private readonly TaskCompletionSource<object> _completionSource =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets or sets the queue item key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the bind operation callback method
        /// </summary>
        public Func<IBindingContext, CancellationToken, object?> BindOperation { get; set; }

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
        public Func<IBindingContext, object>? TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets the operation to call if the bind operation encounters an unexpected exception.
        /// Supports returning an object in case of the exception occurring since in some cases we need to be
        /// tolerant of error cases and still return some value
        /// </summary>
        public Func<Exception, object> ErrorHandler { get; set; }

        /// <summary>
        /// Gets the task that completes when this queue item has been processed.
        /// Use <see cref="SignalCompleted"/> to mark the item as done.
        /// </summary>
        public Task Completed => _completionSource.Task;

        /// <summary>
        /// Signals that processing of this queue item is complete.
        /// Safe to call multiple times — only the first call has any effect.
        /// </summary>
        public void SignalCompleted() => _completionSource.TrySetResult(null);

        /// <summary>
        /// Gets or sets the result of the queued task
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Gets or sets the binding operation timeout in milliseconds
        /// </summary>
        public int? BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the timeout for how long to wait for the binding lock
        /// </summary>
        public int? WaitForLockTimeout { get; set; }

        /// <summary>
        /// Optional cancellation token from the caller (e.g. superseded completion request).
        /// When signalled before dispatch, the queue processor can skip this item entirely.
        /// </summary>
        public CancellationToken CallerCancellation { get; set; } = CancellationToken.None;

        /// <summary>
        /// Converts the result of the execution to type T
        /// </summary>
        public T? GetResultAsT<T>() where T : class
        {
            return (this.Result != null)
                ? this.Result as T
                : null;
        }
    }
}
