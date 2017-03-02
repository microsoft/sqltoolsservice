//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

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
        public virtual QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object> bindOperation,
            Func<IBindingContext, object> timeoutOperation = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null)
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
                TimeoutOperation = timeoutOperation,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout
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
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "disconnected_binding_context";
            }
                        
            lock (this.bindingContextLock)
            {
                if (!this.BindingContextMap.ContainsKey(key))
                {
                    this.BindingContextMap.Add(key, new T());
                }

                return this.BindingContextMap[key];
            }      
        }

        /// <summary>
        /// Checks if a binding context already exists for the provided context key
        /// </summary>
        protected bool BindingContextExists(string key)
        {
            lock (this.bindingContextLock)
            {
                return this.BindingContextMap.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove the binding queue entry
        /// </summary>
        protected void RemoveBindingContext(string key)
        {
            lock (this.bindingContextLock)
            {
                if (this.BindingContextMap.ContainsKey(key))
                {
                    // disconnect existing connection
                    var bindingContext = this.BindingContextMap[key];
                    if (bindingContext.ServerConnection != null && bindingContext.ServerConnection.IsOpen)
                    {
                        bindingContext.ServerConnection.Disconnect();
                    }

                    // remove key from the map
                    this.BindingContextMap.Remove(key);
                }
            }
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
                this.processQueueCancelToken.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// The core queue processing method
        /// </summary>
        /// <param name="state"></param>
        private void ProcessQueue()
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
                        if (queueItem == null)
                        {
                            continue;
                        }

                        IBindingContext bindingContext = GetOrCreateBindingContext(queueItem.Key);
                        if (bindingContext == null)                        
                        {
                            queueItem.ItemProcessed.Set();
                            continue;
                        }

                        bool lockTaken = false;
                        try
                        {                                                    
                            // prefer the queue item binding item, otherwise use the context default timeout
                            int bindTimeout = queueItem.BindingTimeout ?? bindingContext.BindingTimeout;

                            // handle the case a previous binding operation is still running                                                 
                            if (!bindingContext.BindingLock.WaitOne(queueItem.WaitForLockTimeout ?? 0))
                            {
                                queueItem.Result = queueItem.TimeoutOperation != null
                                    ? queueItem.TimeoutOperation(bindingContext)
                                    : null;

                                continue;
                            }

                            bindingContext.BindingLock.Reset();

                            lockTaken = true;

                            // execute the binding operation
                            object result = null;
                            CancellationTokenSource cancelToken = new CancellationTokenSource();
                            var bindTask = Task.Run(() =>
                            {
                                result = queueItem.BindOperation(
                                     bindingContext,
                                     cancelToken.Token);  
                            });

                            // check if the binding tasks completed within the binding timeout
                            if (bindTask.Wait(bindTimeout))
                            {
                                queueItem.Result = result;
                            }
                            else
                            {       
                                cancelToken.Cancel();

                                // if the task didn't complete then call the timeout callback
                                if (queueItem.TimeoutOperation != null)
                                {                                    
                                    queueItem.Result = queueItem.TimeoutOperation(bindingContext);                              
                                }
                                
                                lockTaken = false;

                                bindTask.ContinueWith((a) => bindingContext.BindingLock.Set());
                            }                            
                        }                        
                        catch (Exception ex)
                        {
                            // catch and log any exceptions raised in the binding calls
                            // set item processed to avoid deadlocks 
                            Logger.Write(LogLevel.Error, "Binding queue threw exception " + ex.ToString());                            
                        }
                        finally
                        {
                            if (lockTaken)
                            {
                                bindingContext.BindingLock.Set();
                            }

                            queueItem.ItemProcessed.Set();
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
                    lock (this.bindingQueueLock)
                    {
                        // verify the binding queue is still empty
                        if (this.bindingQueue.Count == 0)
                        {
                            // reset the item queued event since we've processed all the pending items
                            this.itemQueuedEvent.Reset();
                        }
                    }
                }
            }
        }
    }
}
