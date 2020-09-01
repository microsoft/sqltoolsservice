//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Workspace;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    [Export(typeof(IConnectedBindingQueue))]
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>, IConnectedBindingQueue
    {
        internal const int DefaultBindingTimeout = 500;
        private readonly ISqlConnectionOpener _connectionOpener;
        private readonly IDataSourceFactory _dataSourceFactory;

        /// <summary>
        /// Gets the current settings
        /// </summary>
        private SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
        }

        [ImportingConstructor]
        public ConnectedBindingQueue(ISqlConnectionOpener sqlConnectionOpener, IDataSourceFactory dataSourceFactory)
        {
            _connectionOpener = sqlConnectionOpener;
            _dataSourceFactory = dataSourceFactory;
        }

        /// <summary>
        /// Generate a unique key based on the ConnectionInfo object
        /// </summary>
        /// <param name="details"></param>
        internal static string GetConnectionContextKey(ConnectionDetails details)
        {            
            string key = string.Format("{0}_{1}_{2}_{3}",
                details.ServerName ?? "NULL",
                details.DatabaseName ?? "NULL",
                details.UserName ?? "NULL",
                details.AuthenticationType ?? "NULL"
            );

            if (!string.IsNullOrEmpty(details.DatabaseDisplayName))
            {
                key += "_" + details.DatabaseDisplayName;
            }

            if (!string.IsNullOrEmpty(details.GroupId))
            {
                key += "_" + details.GroupId;
            }

            return Uri.EscapeUriString(key);
        }

        /// <summary>
        /// Generate a unique key based on the ConnectionInfo object
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="databaseName"></param>
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

        public void RemoveBindingContext(ConnectionInfo connInfo)
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
        /// <param name="needMetadata"></param>
        /// <param name="featureName"></param>
        /// <param name="overwrite">Overwrite existing context</param>
        public string AddConnectionContext(ConnectionInfo connInfo, bool needMetadata, string featureName = null, bool overwrite = false)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding contextna
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
            IBindingContext bindingContext = GetOrCreateBindingContext(connectionKey);

            if (bindingContext.BindingLock.WaitOne())
            {
                try
                {
                    bindingContext.BindingLock.Reset();
                   
                    // populate the binding context to work with the SMO metadata provider
                    bindingContext.ServerConnection = _connectionOpener.OpenServerConnection(connInfo, featureName);

                    string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                    bindingContext.DataSource = _dataSourceFactory.Create(DataSourceType.Kusto, connectionString, connInfo.ConnectionDetails.AzureAccountToken);

                    if (needMetadata)
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
                catch (Exception ex)
                {
                    throw;
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
