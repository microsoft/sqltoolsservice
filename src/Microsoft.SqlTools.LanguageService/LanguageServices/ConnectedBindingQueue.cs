//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.LanguageService.LanguageServices
{
    public interface IConnectedBindingQueue
    {
        Task CloseConnectionsAsync(string serverName, string databaseName, int millisecondsTimeout);
        Task OpenConnectionsAsync(string serverName, string databaseName, int millisecondsTimeout);
        Task<string> AddConnectionContextAsync(ConnectionInfoBase connInfo, string featureName = null, bool overwrite = false);
        void Dispose();
        Task<QueueItem> QueueBindingOperationAsync(
            string key,
            Func<IBindingContext, CancellationToken, Task<object>> bindOperationAsync,
            Func<IBindingContext, object> timeoutOperation = null,
            Func<Exception, object> errorHandler = null,
            int? bindingTimeout = null,
            int? waitForLockTimeout = null,
            int? hardTimeout = null);
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
        /// Default slow-operation threshold in milliseconds. Callers can provide a later hard
        /// timeout when the operation should be allowed to continue beyond this threshold.
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
        /// Gets the prefix shared by the context keys of every connection to a database
        /// </summary>
        private static string GetConnectionContextKeyPrefix(string serverName, string databaseName)
        {
            return string.Format("{0}_{1}_",
                serverName ?? "NULL",
                databaseName ?? "NULL");
        }

        /// <summary>
        /// Disconnects every context connected to a database, each once no operation is using it.
        /// A context that stays busy for the whole timeout is left connected.
        /// </summary>
        public Task CloseConnectionsAsync(string serverName, string databaseName, int millisecondsTimeout)
        {
            return Task.WhenAll(GetBindingContexts(GetConnectionContextKeyPrefix(serverName, databaseName))
                .Select(context => RunWhenIdleAsync(context, millisecondsTimeout, connection => connection.Disconnect())));
        }

        /// <summary>
        /// Reconnects every context connected to a database, each once no operation is using it.
        /// </summary>
        public Task OpenConnectionsAsync(string serverName, string databaseName, int millisecondsTimeout)
        {
            return Task.WhenAll(GetBindingContexts(GetConnectionContextKeyPrefix(serverName, databaseName))
                .Select(context => RunWhenIdleAsync(context, millisecondsTimeout, connection =>
                {
                    try
                    {
                        connection.Connect();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to reconnect a binding context: {ex.Message}");
                    }
                })));
        }

        private static async Task RunWhenIdleAsync(IBindingContext bindingContext, int millisecondsTimeout, Action<ServerConnection> action)
        {
            // A context whose connection failed to open has nothing to act on
            if (bindingContext.ServerConnection == null
                || !await bindingContext.BindingLock.WaitAsync(millisecondsTimeout).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                action(bindingContext.ServerConnection);
            }
            finally
            {
                bindingContext.BindingLock.Release();
            }
        }

        public void RemoveConnectionContext(ConnectionInfoBase connInfo)
        {
            RemoveBindingContext(connInfo.ConnectionContextKey);
        }

        /// <summary>
        /// Removes the offline binding context registered for a SQL project.
        /// Drops the binder and MetadataProvider reference so GC can collect them.
        /// </summary>
        public void RemoveProjectContext(string projectKey)
        {
            RemoveBindingContext(projectKey);
        }

        /// <summary>
        /// Creates an offline binding context for a SQL project (no server connection required),
        /// replacing any existing one. Operations already running keep the context they started with.
        /// </summary>
        public void AddProjectContext(string projectKey, IBinder binder, ParseOptions parseOptions, IMetadataProvider metadataProvider = null)
        {
            // The context is complete before it is published, so no operation can see it half built.
            ReplaceBindingContext(projectKey, new ConnectedBindingContext
            {
                Binder = binder,
                MetadataProvider = metadataProvider,
                BindingTimeout = DefaultBindingTimeout,
                IsProjectContext = true,
                // IsConnected intentionally left false: no live server connection.
                // ParseOptions are fixed at context creation; no server query needed.
                ProjectParseOptions = parseOptions
            });
        }

        /// <summary>
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo">Connection info used to create binding context</param>
        /// <param name="overwrite">Overwrite existing context</param>
        public virtual async Task<string> AddConnectionContextAsync(ConnectionInfoBase connInfo, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            string connectionKey = connInfo.ConnectionContextKey;
            string ReuseExistingContext()
            {
                // no need to populate the context again since the context already exists
                Logger.Information($"AddConnectionContext: reusing existing binding context for connection key '{connectionKey}' (feature: '{featureName ?? "unknown"}')");
                return connectionKey;
            }

            if (!overwrite && BindingContextExists(connectionKey))
            {
                return ReuseExistingContext();
            }

            // Publish the context while holding its lock, so operations queued for the key wait
            // for it to be populated instead of running against an empty context.
            var bindingContext = new ConnectedBindingContext();
            await bindingContext.BindingLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (overwrite)
                {
                    ReplaceBindingContext(connectionKey, bindingContext);
                }
                else if (!this.BindingContextMap.TryAdd(connectionKey, bindingContext))
                {
                    return ReuseExistingContext();
                }

                // Opening the connection and loading metadata are synchronous, so keep them off the caller's thread.
                await Task.Run(() => PopulateConnectionContext(bindingContext, connInfo, featureName, connectionKey, overwrite)).ConfigureAwait(false);
            }
            finally
            {
                bindingContext.BindingLock.Release();
            }

            return connectionKey;
        }

        private void PopulateConnectionContext(ConnectedBindingContext bindingContext, ConnectionInfoBase connInfo, string featureName, string connectionKey, bool overwrite)
        {
            try
            {
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
                Logger.Information($"AddConnectionContext: binding context ready for connection key '{connectionKey}' (feature: '{featureName ?? "unknown"}', metadata: {this.needsMetadata}, overwrite: {overwrite})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed creating binding context for intellisense. Feature: '{featureName ?? "unknown"}' ConnKey: '{connectionKey}'. Exception: {ex}");
                bindingContext.IsConnected = false;
            }
        }
    }
}
