//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
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
            return ConnectionService.OpenServerConnection(connInfo, featureName);
        }
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
        private SqlConnectionOpener connectionOpener;

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

        public void RemoveBindigContext(ConnectionInfo connInfo)
        {
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
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
            string connectionKey = GetConnectionContextKey(connInfo.ConnectionDetails);
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
