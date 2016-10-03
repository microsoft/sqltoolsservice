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
    public class ConnectedBindingQueue : BindingQueue<ConnectedBindingContext>
    {

        /// <summary>
        /// Gets the current settings
        /// </summary>
        internal SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
        }

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
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                SqlConnection sqlConn = new SqlConnection(connectionString);
                if (sqlConn != null)
                {
                    sqlConn.Open();

                    ServerConnection serverConn = new ServerConnection(sqlConn);                            
                    bindingContext.SmoMetadataProvider = SmoMetadataProvider.CreateConnectedProvider(serverConn);
                    bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
                    bindingContext.MetadataDisplayInfoProvider.BuiltInCasing =
                    this.CurrentSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value
                        ? CasingStyle.Lowercase
                        : CasingStyle.Uppercase;
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
