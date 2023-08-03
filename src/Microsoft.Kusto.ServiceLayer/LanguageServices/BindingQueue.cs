//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;


namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{    
    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> : IDisposable where T : IBindingContext, new()
    {
        private CancellationTokenSource _processQueueCancelToken;

        private readonly ManualResetEvent _itemQueuedEvent;

        private readonly object _bindingQueueLock;

        private readonly LinkedList<QueueItem> _bindingQueue;

        private readonly object _bindingContextLock ;

        private Task _queueProcessorTask;

        public delegate void UnhandledExceptionDelegate(string connectionKey, Exception ex);

        public event UnhandledExceptionDelegate OnUnhandledException;

        /// <summary>
        /// Map from context keys to binding context instances
        /// Internal for testing purposes only
        /// </summary>
        internal Dictionary<string, IBindingContext> BindingContextMap { get; }

        private Dictionary<IBindingContext, Task> BindingContextTasks { get; }

        /// <summary>
        /// Constructor for a binding queue instance
        /// </summary>
        internal BindingQueue()
        {
            BindingContextMap = new Dictionary<string, IBindingContext>();
            _itemQueuedEvent = new ManualResetEvent(initialState: false);
            _bindingQueueLock = new object();
            _bindingQueue = new LinkedList<QueueItem>();
            _bindingContextLock = new object();
            BindingContextTasks = new Dictionary<IBindingContext, Task>();
            StartQueueProcessor();
        }

        private void StartQueueProcessor()
        {
            this._queueProcessorTask = StartQueueProcessorAsync();
        }

        /// <summary>
        /// Stops the binding queue by sending cancellation request
        /// </summary>
        /// <param name="timeout"></param>
        public bool StopQueueProcessor(int timeout)
        {
            this._processQueueCancelToken.Cancel();
            return this._queueProcessorTask.Wait(timeout);
        }

        /// <summary>
        /// Returns true if cancellation is requested
        /// </summary>
        /// <returns></returns>
        public bool IsCancelRequested
        {
            get
            {
                return this._processQueueCancelToken.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Queue a binding request item
        /// </summary>
        public QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object> bindOperation,
            Func<IBindingContext, object> timeoutOperation = null,
            Func<Exception, object> errorHandler = null,
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
                ErrorHandler = errorHandler,
                BindingTimeout = bindingTimeout,
                WaitForLockTimeout = waitForLockTimeout
            };

            lock (this._bindingQueueLock)
            {
                this._bindingQueue.AddLast(queueItem);
            }

            this._itemQueuedEvent.Set();

            return queueItem;
        }

        /// <summary>
        /// Checks if a particular binding context is connected or not
        /// </summary>
        /// <param name="key"></param>
        public bool IsBindingContextConnected(string key)
        {
            lock (this._bindingContextLock)
            {
                IBindingContext context;
                if (this.BindingContextMap.TryGetValue(key, out context))
                {
                    return context.IsConnected;
                }
                return false;
            } 
        }

        /// <summary>
        /// Gets or creates a binding context for the provided context key
        /// </summary>
        /// <param name="key"></param>
        internal IBindingContext GetOrCreateBindingContext(string key)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "disconnected_binding_context";
            }
                        
            lock (this._bindingContextLock)
            {
                if (!this.BindingContextMap.ContainsKey(key))
                {
                    var bindingContext = new T();
                    this.BindingContextMap.Add(key, bindingContext);
                    this.BindingContextTasks.Add(bindingContext, Task.Run(() => null));
                }

                return this.BindingContextMap[key];
            }      
        }

        protected IEnumerable<IBindingContext> GetBindingContexts(string keyPrefix)
        {
            // use a default binding context for disconnected requests
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                keyPrefix = "disconnected_binding_context";
            }

            lock (this._bindingContextLock)
            {
                return this.BindingContextMap.Where(x => x.Key.StartsWith(keyPrefix)).Select(v => v.Value);
            }
        }

        /// <summary>
        /// Checks if a binding context already exists for the provided context key
        /// </summary>
        protected bool BindingContextExists(string key)
        {
            lock (this._bindingContextLock)
            {
                return this.BindingContextMap.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove the binding queue entry
        /// </summary>
        protected void RemoveBindingContext(string key)
        {
            lock (this._bindingContextLock)
            {
                if (this.BindingContextMap.TryGetValue(key, out IBindingContext? bindingContext))
                {
                    // disconnect existing connection
                    // remove key from the map
                    this.BindingContextMap.Remove(key);
                    this.BindingContextTasks.Remove(bindingContext);
                }
            }
        }

        private bool HasPendingQueueItems
        {
            get
            {
                lock (this._bindingQueueLock)
                {
                    return this._bindingQueue.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the next pending queue item
        /// </summary>
        private QueueItem GetNextQueueItem()
        {
            lock (this._bindingQueueLock)
            {
                if (this._bindingQueue.Count == 0)
                {
                    return null;
                }

                QueueItem queueItem = this._bindingQueue.First.Value;
                this._bindingQueue.RemoveFirst();
                return queueItem;
            }
        }

        /// <summary>
        /// Starts the queue processing thread
        /// </summary>        
        private Task StartQueueProcessorAsync()
        {
            if (this._processQueueCancelToken != null)
            {
                this._processQueueCancelToken.Dispose();
            }
            this._processQueueCancelToken = new CancellationTokenSource();

            return Task.Factory.StartNew(
                ProcessQueue,
                this._processQueueCancelToken.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// The core queue processing method
        /// </summary>
        private void ProcessQueue()
        {
            CancellationToken token = this._processQueueCancelToken.Token;
            WaitHandle[] waitHandles = new WaitHandle[2]
            {
                this._itemQueuedEvent,
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

                        var bindingContextTask = this.BindingContextTasks[bindingContext];

                        // Run in the binding context task in case this task has to wait for a previous binding operation
                        this.BindingContextTasks[bindingContext] = bindingContextTask.ContinueWith((task) =>
                        {
                            bool lockTaken = false;
                            try
                            {                                                    
                                // prefer the queue item binding item, otherwise use the context default timeout
                                int bindTimeout = queueItem.BindingTimeout ?? bindingContext.BindingTimeout;

                                // handle the case a previous binding operation is still running
                                if (!bindingContext.BindingLock.WaitOne(queueItem.WaitForLockTimeout ?? 0))
                                {
                                    try
                                    {
                                        Logger.Warning("Binding queue operation timed out waiting for previous operation to finish");
                                        queueItem.Result = queueItem.TimeoutOperation != null
                                            ? queueItem.TimeoutOperation(bindingContext)
                                            : null;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Exception running binding queue lock timeout handler: " + ex.ToString());
                                    }
                                    finally
                                    {
                                        queueItem.ItemProcessed.Set();
                                    }

                                    return;
                                }

                                bindingContext.BindingLock.Reset();  

                                lockTaken = true;

                                // execute the binding operation
                                object result = null;
                                CancellationTokenSource cancelToken = new CancellationTokenSource();
                        
                                // run the operation in a separate thread
                                var bindTask = Task.Run(() =>
                                {
                                    try
                                    {
                                        result = queueItem.BindOperation(
                                            bindingContext,
                                            cancelToken.Token);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Unexpected exception on the binding queue: " + ex.ToString());
                                        if (queueItem.ErrorHandler != null)
                                        {
                                            try
                                            {
                                                result = queueItem.ErrorHandler(ex);
                                            }
                                            catch (Exception ex2)
                                            {
                                                Logger.Error("Unexpected exception in binding queue error handler: " + ex2.ToString());
                                            }
                                        }

                                        if (IsExceptionOfType(ex, typeof(SqlException)) || IsExceptionOfType(ex, typeof(SocketException)))
                                        {
                                            if (this.OnUnhandledException != null)
                                            {
                                                this.OnUnhandledException(queueItem.Key, ex);
                                            }

                                            RemoveBindingContext(queueItem.Key);
                                        }    
                                    }
                                });

                                Task.Run(() => 
                                {
                                    try
                                    {
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

                                            bindTask.ContinueWithOnFaulted(t => Logger.Error("Binding queue threw exception " + t.Exception.ToString()));

                                            // Give the task a chance to complete before moving on to the next operation
                                            bindTask.Wait();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Binding queue task completion threw exception " + ex.ToString());  
                                    }
                                    finally
                                    {
                                        // set item processed to avoid deadlocks 
                                        if (lockTaken)
                                        {
                                            bindingContext.BindingLock.Set();
                                        }
                                        queueItem.ItemProcessed.Set();
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                // catch and log any exceptions raised in the binding calls
                                // set item processed to avoid deadlocks 
                                Logger.Error("Binding queue threw exception " + ex.ToString());
                                // set item processed to avoid deadlocks 
                                if (lockTaken)
                                {
                                    bindingContext.BindingLock.Set();
                                }
                                queueItem.ItemProcessed.Set();                          
                            }
                        }, TaskContinuationOptions.None);

                        // if a queue processing cancellation was requested then exit the loop
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    } 
                }
                finally
                {
                    lock (this._bindingQueueLock)
                    {
                        // verify the binding queue is still empty
                        if (this._bindingQueue.Count == 0)
                        {
                            // reset the item queued event since we've processed all the pending items
                            this._itemQueuedEvent.Reset();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear queued items
        /// </summary>
        public void ClearQueuedItems()
        {
            lock (this._bindingQueueLock)
            {
                if (this._bindingQueue.Count > 0)
                {
                    this._bindingQueue.Clear();
                }
            }
        }

        public void Dispose()
        {
            if (this._processQueueCancelToken != null)
            {
                this._processQueueCancelToken.Dispose();
            }

            if (_itemQueuedEvent != null)
            {
                _itemQueuedEvent.Dispose();
            }
        }
        
        private bool IsExceptionOfType(Exception ex, Type t)
        {
            return ex.GetType() == t || (ex.InnerException != null && ex.InnerException.GetType() == t);
        }
    }
}
