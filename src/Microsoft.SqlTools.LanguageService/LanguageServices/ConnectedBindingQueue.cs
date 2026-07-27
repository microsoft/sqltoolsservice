//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Utility;
using System.Threading;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    public interface IConnectedBindingQueue
    {
        void CloseConnections(string serverName, string databaseName, int millisecondsTimeout);
        void OpenConnections(string serverName, string databaseName, int millisecondsTimeout);
        string AddConnectionContext(ConnectionInfoBase connInfo, string featureName = null, bool overwrite = false);
        void Dispose();
        QueueItem QueueBindingOperation(
            string key,
            Func<IBindingContext, CancellationToken, object> bindOperation,
            Func<IBindingContext, object> timeoutOperation = null,
            Func<Exception, object> errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null);
    }

    public class SqlConnectionOpener
    {
        /// <summary>
        /// Factory used to create a server connection for a given connection info. Wired up by the
        /// hosting service layer so the language service library does not depend on the connection service.
        /// </summary>
        public static Func<ConnectionInfoBase, string, ServerConnection> ServerConnectionFactory { get; set; }

        /// <summary>
        /// Virtual method used to support mocking and testing
        /// </summary>
        public virtual ServerConnection OpenServerConnection(ConnectionInfoBase connInfo, string featureName)
        {
            return ServerConnectionFactory(connInfo, featureName);
        }
    }

    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>, IConnectedBindingQueue
    {
        /// <summary>
        /// Default binding operation timeout in milliseconds.
        /// </summary>
        public const int BindingTimeout = 500;

        internal const int DefaultBindingTimeout = 500;

        internal const int DefaultMinimumConnectionTimeout = 30;

        /// <summary>
        /// Provider used to resolve the built-in keyword casing to apply to metadata display info.
        /// Wired up by the hosting service layer to read the current formatting settings; defaults to uppercase.
        /// </summary>
        internal static Func<bool> UseLowercaseKeywordCasingProvider { get; set; } = () => false;

        /// <summary>
        /// flag determing if the connection queue requires online metadata objects
        /// it's much cheaper to not construct these objects if not needed
        /// </summary>
        private bool needsMetadata;
        private SqlConnectionOpener connectionOpener;

        public ConnectedBindingQueue()
            : this(true)
        {            
        }

        public ConnectedBindingQueue(bool needsMetadata)
        {
            this.needsMetadata = needsMetadata;
            this.connectionOpener = new SqlConnectionOpener();
        }

        // For testing purposes only
        internal void SetConnectionOpener(SqlConnectionOpener opener)
        {
            this.connectionOpener = opener;
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
                    try
                    {
                        bindingContext.ServerConnection.Connect();
                    }
                    catch
                    {
                        //TODO: remove the binding context? 
                    }
                }
            }
        }

        public void RemoveBindigContext(ConnectionInfoBase connInfo)
        {
            string connectionKey = connInfo.ConnectionContextKey;
            if (BindingContextExists(connectionKey))
            {
                RemoveBindingContext(connectionKey);
            }
        }

        /// <summary>
        /// Removes the offline binding context registered for a SQL project.
        /// Drops the binder and MetadataProvider reference so GC can collect them.
        /// </summary>
        public void RemoveProjectContext(string projectKey)
        {
            if (BindingContextExists(projectKey))
            {
                RemoveBindingContext(projectKey);
            }
        }

        /// <summary>
        /// Creates an offline binding context for a SQL project (no server connection required).
        /// </summary>
        public void AddProjectContext(string projectKey, IBinder binder, ParseOptions parseOptions, IMetadataProvider metadataProvider = null)
        {
            if (BindingContextExists(projectKey))
            {
                RemoveBindingContext(projectKey);
            }

            ConnectedBindingContext bindingContext = (ConnectedBindingContext)this.GetOrCreateBindingContext(projectKey);
            if (bindingContext.BindingLock.WaitOne())
            {
                try
                {
                    bindingContext.BindingLock.Reset();
                    bindingContext.Binder = binder;
                    bindingContext.MetadataProvider = metadataProvider;
                    bindingContext.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
                    bindingContext.IsProjectContext = true;
                    // IsConnected intentionally left false: no live server connection.
                    // ParseOptions are fixed at context creation; no server query needed.
                    bindingContext.ProjectParseOptions = parseOptions;
                }
                finally
                {
                    bindingContext.BindingLock.Set();
                }
            }
        }

        /// <summary>
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo">Connection info used to create binding context</param>   
        /// <param name="overwrite">Overwrite existing context</param>      
        public virtual string AddConnectionContext(ConnectionInfoBase connInfo, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding context
            string connectionKey = connInfo.ConnectionContextKey;
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
                   
                    // populate the binding context to work with the SMO metadata provider
                    bindingContext.ServerConnection = connectionOpener.OpenServerConnection(connInfo, featureName);

                    if (this.needsMetadata)
                    {
                        bindingContext.SmoMetadataProvider = SmoMetadataProvider.CreateConnectedProvider(bindingContext.ServerConnection);
                        bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
                        bindingContext.MetadataDisplayInfoProvider.BuiltInCasing = UseLowercaseKeywordCasingProvider() ? CasingStyle.Lowercase : CasingStyle.Uppercase;
                            bindingContext.Binder = BinderProvider.CreateBinder(bindingContext.SmoMetadataProvider);
                        }
            
                    bindingContext.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
                    bindingContext.IsConnected = true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed creating binding context for intellisense. Feature: '{featureName ?? "unknown"}' ConnKey: '{connectionKey}'. Exception: {ex}");
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
