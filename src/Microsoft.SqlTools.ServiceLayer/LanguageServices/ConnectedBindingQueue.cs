//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public interface IConnectedBindingQueue
    {
        void CloseConnections(string serverName, string databaseName, int millisecondsTimeout);
        void OpenConnections(string serverName, string databaseName, int millisecondsTimeout);
        string AddConnectionContext(ConnectionInfo connInfo, string featureName = null, bool overwrite = false);
        void Dispose();
        QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object> bindOperation,
            Func<IBindingContext, object> timeoutOperation = null,
            Func<Exception, object> errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null);
    }

    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>, IConnectedBindingQueue
    {
        internal const int DefaultBindingTimeout = 500;

        internal const int DefaultMinimumConnectionTimeout = 30;

        /// <summary>
        /// flag determing if the connection queue requires online metadata objects
        /// it's much cheaper to not construct these objects if not needed
        /// </summary>
        private bool needsMetadata;

        /// <summary>
        /// Gets the current settings
        /// </summary>
        internal SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
        }

        public ConnectedBindingQueue()
            : this(true)
        {            
        }

        public ConnectedBindingQueue(bool needsMetadata)
        {
            this.needsMetadata = needsMetadata;
        }

        /// <summary>
        /// Generate a unique key based on the ConnectionInfo object
        /// </summary>
        /// <param name="connInfo"></param>
        private string GetConnectionContextKey(ConnectionInfo connInfo)
        {
            ConnectionDetails details = connInfo.ConnectionDetails;
            return string.Format("{0}_{1}_{2}_{3}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );
        }

        /// <summary>
        /// Generate a unique key based on the ConnectionInfo object
        /// </summary>
        /// <param name="connInfo"></param>
        private string GetConnectionContextKey(string serverName, string databaseName)
        {
            return string.Format("{0}_{1}",
                serverName ?? "NULL",
                databaseName ?? "NULL");
            
        }

        public void CloseConnections(string serverName, string databaseName, int millisecondsTimeout)
        {
            string connectionKey = GetConnectionContextKey(serverName, databaseName);
            var contexts = GetBindingContexts(connectionKey);
            foreach (var bindingContext in contexts)
            {
                if (bindingContext.BindingLock.WaitOne(millisecondsTimeout))
                {
                    bindingContext.ServerConnection.Disconnect();
                }
            }
        }

        public void OpenConnections(string serverName, string databaseName, int millisecondsTimeout)
        {
            string connectionKey = GetConnectionContextKey(serverName, databaseName);
            var contexts = GetBindingContexts(connectionKey);
            foreach (var bindingContext in contexts)
            {
                if (bindingContext.BindingLock.WaitOne(millisecondsTimeout))
                {
                    bindingContext.ServerConnection.Connect();
                }
            }
        }

        public void RemoveBindigContext(ConnectionInfo connInfo)
        {
            string connectionKey = GetConnectionContextKey(connInfo);
            if (BindingContextExists(connectionKey))
            {
                RemoveBindingContext(connectionKey);
            }
        }

        /// <summary>
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo">Connection info used to create binding context</param>   
        /// <param name="overwrite">Overwrite existing context</param>      
        public virtual string AddConnectionContext(ConnectionInfo connInfo, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding context
            string connectionKey = GetConnectionContextKey(connInfo);
            if (BindingContextExists(connectionKey))
            {
                if (overwrite)
                {
                    RemoveBindingContext(connectionKey);
                }
                else
                {
                    // no need to populate the context again since the context already exists
                    return connectionKey;
                }
            }
            IBindingContext bindingContext = this.GetOrCreateBindingContext(connectionKey);

            if (bindingContext.BindingLock.WaitOne())
            {
                try
                {
                    bindingContext.BindingLock.Reset();
                    SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, featureName);
                   
                    // populate the binding context to work with the SMO metadata provider
                    bindingContext.ServerConnection = new ServerConnection(sqlConn);

                    if (this.needsMetadata)
                    {
                        bindingContext.SmoMetadataProvider = SmoMetadataProvider.CreateConnectedProvider(bindingContext.ServerConnection);
                        bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
                        bindingContext.MetadataDisplayInfoProvider.BuiltInCasing =
                            this.CurrentSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value
                                ? CasingStyle.Lowercase : CasingStyle.Uppercase;
                        bindingContext.Binder = BinderProvider.CreateBinder(bindingContext.SmoMetadataProvider);
                    }         
            
                    bindingContext.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
                    bindingContext.IsConnected = true;
                }
                catch (Exception)
                {
                    bindingContext.IsConnected = false;
                }       
                finally
                {
                    bindingContext.BindingLock.Set();
                }         
            }

            return connectionKey;
        }
    }
}
