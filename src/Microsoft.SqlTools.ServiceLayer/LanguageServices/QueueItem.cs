//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Class that stores the state of a binding queue request item
    /// </summary>    
    public class QueueItem
    {
        /// <summary>
        /// QueueItem constructor
        /// </summary>
        public QueueItem()
        {
            this.ItemProcessed = new ManualResetEvent(initialState: false);
        }

        /// <summary>
        /// Gets or sets the queue item key
        /// </summary>
#pragma warning disable IDE0370 // Suppression is unnecessary — null! is required here to satisfy CS8618 for properties set by callers before use
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets the bind operation callback method
        /// </summary>
        public Func<IBindingContext, CancellationToken, object?> BindOperation { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
#pragma warning restore IDE0370
        public Func<IBindingContext, object>? TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets the operation to call if the bind operation encounters an unexpected exception.
        /// Supports returning an object in case of the exception occurring since in some cases we need to be
        /// tolerant of error cases and still return some value
        /// </summary>
#pragma warning disable IDE0370
        public Func<Exception, object> ErrorHandler { get; set; } = null!;
#pragma warning restore IDE0370

        /// <summary>
        /// Gets or sets an event to signal when this queue item has been processed
        /// </summary>
        public virtual ManualResetEvent ItemProcessed { get; set; }

        /// <summary>
        /// Gets or sets the result of the queued task
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Gets or sets the binding operation timeout in milliseconds
        /// </summary>
        public int? BindingTimeout { get; set; }

        /// <summary>
        /// Gets or sets the timeout for how long to wait for the binding lock
        /// </summary>
        public int? WaitForLockTimeout { get; set; }

        /// <summary>
        /// Converts the result of the execution to type T
        /// </summary>
        public T? GetResultAsT<T>() where T : class
        {
            //var task = this.ResultsTask;
            return (this.Result != null)
                ? this.Result as T
                : null;
        }
    }
}
