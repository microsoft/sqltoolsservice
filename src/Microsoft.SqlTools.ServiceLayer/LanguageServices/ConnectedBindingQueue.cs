//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.Utility;
using System.Threading;
using System.Linq;

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

    public class SqlConnectionOpener
    {
        /// <summary>
        /// Virtual method used to support mocking and testing
        /// </summary>
        public virtual ServerConnection OpenServerConnection(ConnectionInfo connInfo, string featureName)
        {
            if (ShouldTryMasterFirst(connInfo))
            {
                try
                {
                    Logger.Information("Opening language service binding connection against master");
                    return ConnectionService.OpenServerConnection(CloneConnectionInfoWithDatabase(connInfo, "master"), featureName);
                }
                catch (Exception ex) when (IsDatabaseAccessException(ex))
                {
                    Logger.Information($"Language service binding connection could not use master; falling back to target database. Exception type: {ex.GetType().Name}");
                }
            }

            return ConnectionService.OpenServerConnection(connInfo, featureName);
        }

        private static bool ShouldTryMasterFirst(ConnectionInfo connInfo)
        {
            string databaseName = connInfo?.ConnectionDetails?.DatabaseName;
            if (connInfo == null ||
                string.IsNullOrWhiteSpace(databaseName) ||
                string.Equals(databaseName, "master", StringComparison.OrdinalIgnoreCase) ||
                IsAzureConnection(connInfo) ||
                IsReadOnlyIntent(connInfo.ConnectionDetails.ApplicationIntent))
            {
                return false;
            }

            return true;
        }

        private static bool IsAzureConnection(ConnectionInfo connInfo)
        {
            return connInfo.IsSqlDb ||
                   connInfo.IsSqlDW ||
                   connInfo.EngineEdition == DatabaseEngineEdition.SqlDatabase ||
                   connInfo.EngineEdition == DatabaseEngineEdition.SqlDataWarehouse ||
                   IsAzureServerName(connInfo.ConnectionDetails.ServerName);
        }

        private static bool IsAzureServerName(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName))
            {
                return false;
            }

            return serverName.EndsWith(".database.windows.net", StringComparison.OrdinalIgnoreCase) ||
                   serverName.EndsWith(".database.chinacloudapi.cn", StringComparison.OrdinalIgnoreCase) ||
                   serverName.EndsWith(".database.usgovcloudapi.net", StringComparison.OrdinalIgnoreCase) ||
                   serverName.EndsWith(".database.cloudapi.de", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReadOnlyIntent(string applicationIntent)
        {
            return string.Equals(applicationIntent, "ReadOnly", StringComparison.OrdinalIgnoreCase);
        }

        private static ConnectionInfo CloneConnectionInfoWithDatabase(ConnectionInfo connInfo, string databaseName)
        {
            ConnectionDetails details = connInfo.ConnectionDetails.Clone();
            details.DatabaseName = databaseName;
            return new ConnectionInfo(connInfo.Factory, connInfo.OwnerUri, details)
            {
                IsCloud = connInfo.IsCloud,
                IsSqlDb = connInfo.IsSqlDb,
                IsSqlDW = connInfo.IsSqlDW,
                IsAzureAuth = connInfo.IsAzureAuth,
                AzureTokenFetcher = connInfo.AzureTokenFetcher,
                EngineEdition = connInfo.EngineEdition,
                MajorVersion = connInfo.MajorVersion
            };
        }

        private static bool IsDatabaseAccessException(Exception ex)
        {
            SqlException sqlException = ex as SqlException ?? ex.InnerException as SqlException;
            if (sqlException == null)
            {
                return ex is ConnectionFailureException && ex.InnerException is SqlException inner &&
                       IsDatabaseAccessSqlError(inner.Number);
            }

            return IsDatabaseAccessSqlError(sqlException.Number);
        }

        private static bool IsDatabaseAccessSqlError(int errorNumber)
        {
            return errorNumber == 18456 || // login failed
                   errorNumber == 916 ||   // no database access
                   errorNumber == 4060;    // cannot open database
        }
    }

    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>, IConnectedBindingQueue
    {
        internal const int DefaultBindingTimeout = 500;

        internal const int DefaultMinimumConnectionTimeout = 30;

        private static readonly TimeSpan NoOpBinderLifetime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// flag determing if the connection queue requires online metadata objects
        /// it's much cheaper to not construct these objects if not needed
        /// </summary>
        private bool needsMetadata;
        private SqlConnectionOpener connectionOpener;
        private readonly ConcurrentDictionary<string, BindingContextRegistration> bindingContextRegistrations = new();

        private sealed class BindingContextRegistration
        {
            public ConnectionInfo ConnectionInfo { get; set; }
            public string FeatureName { get; set; }
        }

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
            this.connectionOpener = new SqlConnectionOpener();
        }

        // For testing purposes only
        internal void SetConnectionOpener(SqlConnectionOpener opener)
        {
            this.connectionOpener = opener;
        }

        protected override IBindingContext GetOrCreateBindingContext(string key)
        {
            if (this.BindingContextMap.TryGetValue(key, out IBindingContext existingContext) &&
                existingContext is ConnectedBindingContext connectedContext &&
                connectedContext.IsDead)
            {
                Logger.Information($"Recreating expired no-op binding context '{key}'");
                RemoveBindingContext(key, removeTaskChain: false, disconnectImmediately: true);
            }

            return base.GetOrCreateBindingContext(key);
        }

        protected override IBindingContext CreateBindingContext(string key)
        {
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            if (this.bindingContextRegistrations.TryGetValue(key, out BindingContextRegistration registration))
            {
                PopulateConnectedBindingContext(
                    bindingContext,
                    registration.ConnectionInfo,
                    registration.FeatureName,
                    key);
            }
            return bindingContext;
        }

        protected override bool ShouldEvictBindingContextOnTimeout(IBindingContext bindingContext, QueueItem queueItem)
        {
            return bindingContext is ConnectedBindingContext connectedContext &&
                   connectedContext.IsConnected &&
                   !connectedContext.IsProjectContext;
        }

        /// <summary>
        /// Generate a unique key based on the ConnectionDetails object
        /// </summary>
        /// <param name="connInfo"></param>
        internal static string GetConnectionContextKey(ConnectionDetails details)
        {            
            string key = string.Format("{0}_{1}_{2}_{3}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );

            if (!string.IsNullOrEmpty(details.Id))
            {
                key += "_" + details.Id;
            }

            if (!string.IsNullOrEmpty(details.DatabaseDisplayName))
            {
                key += "_" + details.DatabaseDisplayName;
            }

            if (!string.IsNullOrEmpty(details.GroupId))
            {
                key += "_" + details.GroupId;
            }

            if (!string.IsNullOrEmpty(details.ConnectionName))
            {
                key += "_" + details.ConnectionName;
            }

            // Additional properties that are used to distinguish the connection (besides password)
            // These are so that multiple connections can connect to the same target, with different settings.
            foreach (KeyValuePair<string, object> entry in details.Options.OrderBy(entry => entry.Key))
            {
                // Filter out properties we already have or don't want (password)
                if (
                    // Exclude properties that are already used above
                    entry.Key != "server" &&
                    entry.Key != "database" &&
                    entry.Key != "user" &&
                    entry.Key != "authenticationType" &&
                    entry.Key != "databaseDisplayName" &&
                    // Exclude strictly-organizational properties that have no bearing on the connection
                    entry.Key != "connectionName" &&
                    entry.Key != "groupId" &&
                    // Exclude secrets/credentials that should never be logged or stored in plaintext
                    entry.Key != "password" && 
                    entry.Key != "azureAccountToken")
                {
                    // Boolean values are explicitly labeled true or false instead of undefined.
                    if (entry.Value is bool v)
                    {
                        if (v)
                        {
                            key += "_" + entry.Key + ":true";
                        }
                        else
                        {
                            key += "_" + entry.Key + ":false";
                        }
                    }
                    else if (!string.IsNullOrEmpty(entry.Value as String))
                    {
                        key += "_" + entry.Key + ":" + entry.Value;
                    }
                }
            }

#pragma warning disable SYSLIB0013 // we don't want to escape the ":" characters in our key-value options pairs because it's more readable
            return Uri.EscapeUriString(key);
#pragma warning restore SYSLIB0013
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
                    bindingContext.ServerConnection?.Disconnect();
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
                        bindingContext.ServerConnection?.Connect();
                    }
                    catch
                    {
                        //TODO: remove the binding context? 
                    }
                }
            }
        }

        public void RemoveBindigContext(ConnectionInfo connInfo)
        {
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
            this.bindingContextRegistrations.TryRemove(connectionKey, out _);
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
            this.bindingContextRegistrations.TryRemove(projectKey, out _);
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
        public virtual string AddConnectionContext(ConnectionInfo connInfo, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding context
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
            this.bindingContextRegistrations[connectionKey] = new BindingContextRegistration
            {
                ConnectionInfo = connInfo,
                FeatureName = featureName
            };

            if (BindingContextExists(connectionKey))
            {
                if (overwrite)
                {
                    RemoveBindingContext(connectionKey);
                }
                else if (this.BindingContextMap.TryGetValue(connectionKey, out IBindingContext existingContext) &&
                         existingContext is ConnectedBindingContext connectedContext &&
                         connectedContext.IsDead)
                {
                    RemoveBindingContext(connectionKey);
                }
                else
                {
                    // no need to populate the context again since the context already exists
                    return connectionKey;
                }
            }

            this.GetOrCreateBindingContext(connectionKey);

            return connectionKey;
        }

        private void PopulateConnectedBindingContext(
            ConnectedBindingContext bindingContext,
            ConnectionInfo connInfo,
            string featureName,
            string connectionKey)
        {
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
                        bindingContext.MetadataDisplayInfoProvider.BuiltInCasing =
                            this.CurrentSettings.SqlTools.Format.KeywordCasing == Formatter.CasingOptions.Lowercase
                                ? CasingStyle.Lowercase : CasingStyle.Uppercase;
                        bindingContext.Binder = BinderProvider.CreateBinder(bindingContext.SmoMetadataProvider);
                    }

                    bindingContext.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
                    bindingContext.MarkConnected();

                    // Prime parse options while this context is exclusively owned so later completions do not
                    // repeat compatibility probes on the hot path.
                    _ = bindingContext.ParseOptions;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed creating binding context for intellisense. Feature: '{featureName ?? "unknown"}' ConnKey: '{connectionKey}'. Exception: {ex}");
                    bindingContext.UseNoOpBinder(NoOpBinderLifetime);
                }
                finally
                {
                    bindingContext.BindingLock.Set();
                }
            }
        }
    }
}
