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
        public ConnectedBindingContext()
        {
            this.BindingLocked = new ManualResetEvent(initialState: false);
        }

        public bool IsConnected { get; set; }

        public ServerConnection ServerConnection { get; set; }

        public MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        public SmoMetadataProvider SmoMetadataProvider { get; set; }

        public IBinder Binder { get; set; }

        public ManualResetEvent BindingLocked { get; set; } 

        public int BindingTimeout { get; set; } 
    }

    /// <summary>
    /// Main class for the Binding Queue
    /// </summary>
    public class BindingQueue<T> where T : IBindingContext, new()
    {       
        internal class QueueItem
        {
            public string Key { get; set; }

            public Func<IBindingContext, Task> BindOperation { get; set; }
        }
        
        private CancellationTokenSource processQueueCancelToken = new CancellationTokenSource();

        private ManualResetEvent itemQueuedEvent = new ManualResetEvent(initialState: false);

        private object bindingQueueLock = new object();

        private LinkedList<QueueItem> bindingQueue = new LinkedList<QueueItem>();

        private object bindingContextLock = new object();

        private Dictionary<string, IBindingContext> BindingContextMap { get; set; }

        private Task queueProcessorTask;

        public BindingQueue()
        {
            this.BindingContextMap = new Dictionary<string, IBindingContext>();

            this.queueProcessorTask = StartQueueProcessor();
        }

        public bool StopQueueProcessor(int timeout)
        {
            this.processQueueCancelToken.Cancel();
            return this.queueProcessorTask.Wait(timeout);
        }

        public void QueueBindingOperation(string key, Func<IBindingContext, Task> bindOperation)
        {
            lock (this.bindingQueueLock)
            {
                this.bindingQueue.AddLast(new QueueItem()
                {
                    Key = key,
                    BindOperation = bindOperation
                });
            }

            this.itemQueuedEvent.Set();
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
                TaskCreationOptions.LongRunning,
                this.processQueueCancelToken.Token);
        }

        private void ProcessQueue(object canelToken)
        {
            CancellationToken token = (CancellationToken)canelToken;
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
                            continue;
                        }

                        Task bindingTask = queueItem.BindOperation(bindingContext);
                        bindingTask.Wait(bindingContext.BindingTimeout);
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

        public void AddConnectionContext(ConnectionInfo connInfo)
        {
            if (connInfo == null)
            {
                return;
            }

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
        }
    }
}
