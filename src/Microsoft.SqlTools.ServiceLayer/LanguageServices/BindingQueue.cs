//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// The state of a binding request
    /// </summary>
    public interface IBindingContext
    {
        bool IsConnected { get; set; }

        ServerConnection ServerConnection { get; set; }

        MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        SmoMetadataProvider SmoMetadataProvider { get; set; }

        IBinder Binder { get; set; }

        ManualResetEvent BindingLocked { get; set; }

        int BindingTimeout { get; set; }
    }

    /// <summary>
    /// The state of a binding request
    /// </summary>
    public class ConnectedBindingContext : IBindingContext
    { 
        /// <summary>
        /// Connected binding context constructor
        /// </summary>
        public ConnectedBindingContext()
        {
            this.BindingLocked = new ManualResetEvent(initialState: true);
        }

        /// <summary>
        /// Gets or sets a flag indicating if the binder is connected
        /// </summary>
        public bool IsConnected { get; set; }

        public ServerConnection ServerConnection { get; set; }

        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        public IBinder Binder { get; set; }

        public ManualResetEvent BindingLocked { get; set; } 

        public int BindingTimeout { get; set; } 
    }

    /// <summary>
    /// Class that stores the state of a binding queue request item
    /// </summary>    
    public class QueueItem
    {
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

        public ManualResetEvent ItemProcessed { get; set; } 

        public Task<object> ResultsTask { get; set; }

        public T GetResultAsT<T>() where T : class
        {
            var task = this.ResultsTask;
            return (task != null && task.IsCompleted && task.Result != null)
                ? task.Result as T
                : null;
        }
    }

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

        private Dictionary<string, IBindingContext> BindingContextMap { get; set; }

        private Task queueProcessorTask;

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

        private Task StartQueueProcessor()
        {
            return Task.Factory.StartNew(
                ProcessQueue, 
                null,
                this.processQueueCancelToken.Token);
        }

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
                WaitHandle.WaitAny(waitHandles);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    while (this.HasPendingQueueItems)
                    {
                        QueueItem queueItem = GetNextQueueItem();
                        IBindingContext bindingContext = GetOrCreateBindingContext(queueItem.Key);
                        if (bindingContext == null)                        
                        {
                            queueItem.ItemProcessed.Set();
                            continue;
                        }
                    
                        if (!bindingContext.BindingLocked.WaitOne(1000))
                        {
                            queueItem.ResultsTask = Task.Run(() => 
                            {
                                var timeoutTask = queueItem.TimeoutOperation(bindingContext);
                                queueItem.ItemProcessed.Set();
                                return timeoutTask.Result;
                            });    

                            continue;
                        }

                        CancellationTokenSource cancelToken = new CancellationTokenSource();
                        queueItem.ResultsTask = queueItem.BindOperation(
                                bindingContext,
                                cancelToken.Token);

                        queueItem.ResultsTask.ContinueWith((obj) => 
                            {   
                                queueItem.ItemProcessed.Set();
                                bindingContext.BindingLocked.Set();
                            });

                        if (!queueItem.ResultsTask.Wait(bindingContext.BindingTimeout))
                        {
                            if (queueItem.TimeoutOperation != null)
                            {                            
                                cancelToken.Cancel();
                                queueItem.ResultsTask = queueItem.TimeoutOperation(bindingContext);
                                queueItem.ItemProcessed.Set();
                            }
                        }

                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                    } 
                }
                finally
                {
                    this.itemQueuedEvent.Reset();
                }
            }
        }
    }

    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>
    {
        private string GetConnectionContextKey(ConnectionInfo connInfo)
        {
            ConnectionDetails details = connInfo.ConnectionDetails;
            return string.Format("{0}_{1}_{2}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );
        }

        /// <summary>
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo"></param>
        public virtual string AddConnectionContext(ConnectionInfo connInfo)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            string connectionKey = GetConnectionContextKey(connInfo);
            IBindingContext bindingContext = this.GetOrCreateBindingContext(
                GetConnectionContextKey(connInfo));

            try
            {
                connInfo.ConnectionDetails.ConnectTimeout = 30;
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                SqlConnection sqlConn = new SqlConnection(connectionString);
                if (sqlConn != null)
                {
                    sqlConn.Open();

                    ServerConnection serverConn = new ServerConnection(sqlConn);                            
                    bindingContext.SmoMetadataProvider = SmoMetadataProvider.CreateConnectedProvider(serverConn);
                    bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
                    bindingContext.Binder = BinderProvider.CreateBinder(bindingContext.SmoMetadataProvider);                           
                    bindingContext.ServerConnection = serverConn;
                    bindingContext.BindingTimeout = 60000;
                    bindingContext.IsConnected = true;
                }
            }
            catch (Exception)
            {
                bindingContext.IsConnected = false;
            }
            finally
            {
                bindingContext.BindingLocked.Set();                
            }

            return connectionKey;
        }
    }
}
