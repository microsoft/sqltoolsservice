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
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the bind operation callback method
        /// </summary>
        public Func<IBindingContext, CancellationToken, object> BindOperation { get; set; }

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
        public Func<IBindingContext, object> TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets an event to signal when this queue item has been processed
        /// </summary>
        public virtual ManualResetEvent ItemProcessed { get; set; } 

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
        /// Converts the result of the execution to type T
        /// </summary>
        public T GetResultAsT<T>() where T : class
        {
            //var task = this.ResultsTask;
            return (this.Result != null)
                ? this.Result as T
                : null;
        }
    }
}
