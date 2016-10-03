//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{    
    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> where T : IBindingContext, new()
    {             
        private CancellationTokenSource processQueueCancelToken = new CancellationTokenSource();

        private ManualResetEvent itemQueuedEvent = new ManualResetEvent(initialState: false);

        private object bindingQueueLock = new object();

        private LinkedList<QueueItem> bindingQueue = new LinkedList<QueueItem>();

        private object bindingContextLock = new object();

        private Task queueProcessorTask;

        /// <summary>
        /// Map from context keys to binding context instances
        /// Internal for testing purposes only
        /// </summary>
        internal Dictionary<string, IBindingContext> BindingContextMap { get; set; }

        /// <summary>
        /// Constructor for a binding queue instance
        /// </summary>
        public BindingQueue()
        {
            this.BindingContextMap = new Dictionary<string, IBindingContext>();

            this.queueProcessorTask = StartQueueProcessor();
        }

        /// <summary>
        /// Stops the binding queue by sending cancellation request
        /// </summary>
        /// <param name="timeout"></param>
        public bool StopQueueProcessor(int timeout)
        {
            this.processQueueCancelToken.Cancel();
            return this.queueProcessorTask.Wait(timeout);
        }

        /// <summary>
        /// Queue a binding request item
        /// </summary>
        public QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, Task<object>> bindOperation,
            Func<IBindingContext, Task<object>> timeoutOperation = null)
        {
            // don't add null operations to the binding queue
            if (bindOperation == null)
            {
                return null;
            }

            QueueItem queueItem = new QueueItem()
            {
                Key = key,
                BindOperation = bindOperation,
                TimeoutOperation = timeoutOperation
            };

            lock (this.bindingQueueLock)
            {
                this.bindingQueue.AddLast(queueItem);
            }

            this.itemQueuedEvent.Set();

            return queueItem;
        }

        /// <summary>
        /// Gets or creates a binding context for the provided context key
        /// </summary>
        /// <param name="key"></param>
        protected IBindingContext GetOrCreateBindingContext(string key)
        {
            if (!this.BindingContextMap.ContainsKey(key))
            {
                lock (this.bindingContextLock)
                {
                    if (!this.BindingContextMap.ContainsKey(key))
                    {
                        this.BindingContextMap.Add(key, new T());
                    }
                }
            }

            return this.BindingContextMap[key];
        }

        private bool HasPendingQueueItems
        {
            get
            {
                lock (this.bindingQueueLock)
                {
                    return this.bindingQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the next pending queue item
        /// </summary>
        private QueueItem GetNextQueueItem()
        {
            lock (this.bindingQueueLock)
            {
                if (this.bindingQueue.Count == 0)
                {
                    return null;
                }

                QueueItem queueItem = this.bindingQueue.First.Value;
                this.bindingQueue.RemoveFirst();
                return queueItem;
            }
        }

        /// <summary>
        /// Starts the queue processing thread
        /// </summary>        
        private Task StartQueueProcessor()
        {
            return Task.Factory.StartNew(
                ProcessQueue, 
                null,
                this.processQueueCancelToken.Token);
        }

        /// <summary>
        /// The core queue processing method
        /// </summary>
        /// <param name="state"></param>
        private void ProcessQueue(object state)
        {
            CancellationToken token = this.processQueueCancelToken.Token;
            WaitHandle[] waitHandles = new WaitHandle[2]
            {
                this.itemQueuedEvent,
                token.WaitHandle
            };
    
            while (true)
            {
                // wait for with an item to be queued or the a cancellation request
                WaitHandle.WaitAny(waitHandles);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // dispatch all pending queue items
                    while (this.HasPendingQueueItems)
                    {                    
                        QueueItem queueItem = GetNextQueueItem();
                        IBindingContext bindingContext = GetOrCreateBindingContext(queueItem.Key);
                        if (bindingContext == null)                        
                        {
                            queueItem.ItemProcessed.Set();
                            continue;
                        }
                    
                        // handle the case a previous binding operation is still running
                        if (!bindingContext.BindingLocked.WaitOne(bindingContext.BindingTimeout))
                        {
                            queueItem.ResultsTask = Task.Run(() => 
                            {
                                var timeoutTask = queueItem.TimeoutOperation(bindingContext);
                                queueItem.ItemProcessed.Set();
                                return timeoutTask.Result;
                            });    

                            continue;
                        }

                        // execute the binding operation
                        CancellationTokenSource cancelToken = new CancellationTokenSource();
                        queueItem.ResultsTask = queueItem.BindOperation(
                                bindingContext,
                                cancelToken.Token);

                        // set notification events once the binding operation task completes
                        queueItem.ResultsTask.ContinueWith((obj) => 
                            {   
                                queueItem.ItemProcessed.Set();
                                bindingContext.BindingLocked.Set();
                            });

                        // check if the binding tasks completed within the binding timeout
                        if (!queueItem.ResultsTask.Wait(bindingContext.BindingTimeout))
                        {
                            // if the task didn't complete then call the timeout callback
                            if (queueItem.TimeoutOperation != null)
                            {                            
                                cancelToken.Cancel();
                                queueItem.ResultsTask = queueItem.TimeoutOperation(bindingContext);
                                queueItem.ItemProcessed.Set();
                            }
                        }

                        // if a queue processing cancellation was requested then exit the loop
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    } 
                }
                finally
                {
                    // reset the item queued event since we've processed all the pending items
                    this.itemQueuedEvent.Reset();
                }
            }
        }
    }
}
