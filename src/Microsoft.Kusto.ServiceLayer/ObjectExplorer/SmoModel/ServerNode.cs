//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Server node implementation 
    /// </summary>
    public class ServerNode : TreeNode
    {
        private ConnectionSummary connectionSummary;
        private ServerInfo serverInfo;
        private Lazy<QueryContext> context;
        private SmoWrapper smoWrapper;
        private SqlServerType sqlServerType;
        private ServerConnection serverConnection;
        private IDataSource kustoUtils;

        public ServerNode(ConnectionCompleteParams connInfo, IMultiServiceProvider serviceProvider, ServerConnection serverConnection, IDataSource kustoUtils)
            : base()
        {
            Validate.IsNotNull(nameof(connInfo), connInfo);
            Validate.IsNotNull("connInfo.ConnectionSummary", connInfo.ConnectionSummary);
            Validate.IsNotNull(nameof(serviceProvider), serviceProvider);

            this.kustoUtils = kustoUtils;
            this.connectionSummary = connInfo.ConnectionSummary;
            this.serverInfo = connInfo.ServerInfo;
            this.sqlServerType = ServerVersionHelper.CalculateServerType(this.serverInfo);

            this.context = new Lazy<QueryContext>(() => CreateContext(serviceProvider));
            this.serverConnection = serverConnection;

            NodeValue = connectionSummary.ServerName;
            IsAlwaysLeaf = false;
            NodeType = NodeTypes.Server.ToString();
            NodeTypeId = NodeTypes.Server;
            Label = GetConnectionLabel();
        }

        internal SmoWrapper SmoWrapper
        {
            get
            {
                if (smoWrapper == null)
                {
                    smoWrapper = new SmoWrapper();
                }
                return smoWrapper;
            }
            set
            {
                this.smoWrapper = value;
            }
        }

        /// <summary>
        /// Returns the label to display to the user.
        /// </summary>
        internal string GetConnectionLabel()
        {
            string userName = connectionSummary.UserName;

            // TODO Domain and username is not yet supported on .Net Core. 
            // Consider passing as an input from the extension where this can be queried
            //if (string.IsNullOrWhiteSpace(userName))
            //{
            //    userName = Environment.UserDomainName + @"\" + Environment.UserName;
            //}

            // TODO Consider adding IsAuthenticatingDatabaseMaster check in the code and
            // referencing result here
            if (!DatabaseUtils.IsSystemDatabaseConnection(connectionSummary.DatabaseName))
            {
                // We either have an azure with a database specified or a Denali database using a contained user
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = connectionSummary.DatabaseName;
                }
                else
                {
                    userName += ", " + connectionSummary.DatabaseName;
                }
            }

            string label;
            if (string.IsNullOrWhiteSpace(userName))
            {
                label = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1} {2})",
                connectionSummary.ServerName,
                "SQL Server",
                serverInfo.ServerVersion);
            }
            else
            {
                label = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1} {2} - {3})",
                connectionSummary.ServerName,
                "SQL Server",
                serverInfo.ServerVersion,
                userName);
            }

            return label;
        }

        private QueryContext CreateContext(IMultiServiceProvider serviceProvider)
        {
            string exceptionMessage;
   
            try
            {
                Server server = SmoWrapper.CreateServer(this.serverConnection);
                if (server != null)
                {
                    return new QueryContext(server, this.kustoUtils, serviceProvider, SmoWrapper)
                    {
                        Parent = server,
                        SqlServerType = this.sqlServerType
                    };
                }
                else
                {
                    return null;
                }
            }
            catch (ConnectionFailureException cfe)
            {
                exceptionMessage = cfe.Message;
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            Logger.Write(TraceEventType.Error, "Exception at ServerNode.CreateContext() : " + exceptionMessage);
            this.ErrorStateMessage = string.Format(SR.TreeNodeError, exceptionMessage);
            return null;
        }

        public override object GetContext()
        {
            return context.Value;
        }
    }
}
