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

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// ConnectedBindingQueue class for processing online binding requests
    /// </summary>
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>
    {
        internal const int DefaultBindingTimeout = 60000;

        internal const int DefaultMinimumConnectionTimeout = 30;

        /// <summary>
        /// Gets the current settings
        /// </summary>
        internal SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
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
        /// Use a ConnectionInfo item to create a connected binding context
        /// </summary>
        /// <param name="connInfo"></param>
        public virtual string AddConnectionContext(ConnectionInfo connInfo)
        {
            if (connInfo == null)
            {
                return string.Empty;
            }

            // lookup the current binding context
            string connectionKey = GetConnectionContextKey(connInfo);
            IBindingContext bindingContext = this.GetOrCreateBindingContext(connectionKey);

            lock (bindingContext.BindingLock)
            {
                try
                {
                    // increase the connection timeout to at least 30 seconds and and build connection string
                    // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                    int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                    bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                    connInfo.ConnectionDetails.ConnectTimeout = Math.Max(DefaultMinimumConnectionTimeout, originalTimeout ?? 0);
                    connInfo.ConnectionDetails.PersistSecurityInfo = true;
                    string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                    connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                    connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                    // open a dedicated binding server connection
                    SqlConnection sqlConn = new SqlConnection(connectionString);
                    if (sqlConn != null)
                    {
                        sqlConn.Open();

                        // populate the binding context to work with the SMO metadata provider
                        ServerConnection serverConn = new ServerConnection(sqlConn);                            
                        bindingContext.SmoMetadataProvider = SmoMetadataProvider.CreateConnectedProvider(serverConn);
                        bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
                        bindingContext.MetadataDisplayInfoProvider.BuiltInCasing =
                            this.CurrentSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value
                                ? CasingStyle.Lowercase : CasingStyle.Uppercase;
                        bindingContext.Binder = BinderProvider.CreateBinder(bindingContext.SmoMetadataProvider);                           
                        bindingContext.ServerConnection = serverConn;
                        bindingContext.BindingTimeout = ConnectedBindingQueue.DefaultBindingTimeout;
                        bindingContext.IsConnected = true;
                    }
                }
                catch (Exception)
                {
                    bindingContext.IsConnected = false;
                }                
            }

            return connectionKey;
        }
    }
}
