//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        public Func<IBindingContext, CancellationToken, Task<object>> BindOperation { get; set; }

        /// <summary>
        /// Gets or sets the timeout operation to call if the bind operation doesn't finish within timeout period
        /// </summary>
        public Func<IBindingContext, Task<object>> TimeoutOperation { get; set; }

        /// <summary>
        /// Gets or sets an event to signal when this queue item has been processed
        /// </summary>
        public ManualResetEvent ItemProcessed { get; set; } 

        /// <summary>
        /// Gets or sets the task that was used to execute this queue item.
        /// This allows the queuer to retrieve the execution result.
        /// </summary>
        public Task<object> ResultsTask { get; set; }

        /// <summary>
        /// Converts the result of the execution task to type T
        /// </summary>
        public T GetResultAsT<T>() where T : class
        {
            var task = this.ResultsTask;
            return (task != null && task.IsCompleted && task.Result != null)
                ? task.Result as T
                : null;
        }
    }
}
