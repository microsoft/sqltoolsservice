//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Server node implementation 
    /// </summary>
    public class ServerNode : TreeNode
    {
        private ConnectionSummary connectionSummary;
        private ServerInfo serverInfo;
        private string connectionUri;
        private Lazy<SmoQueryContext> context;
        private ConnectionService connectionService;
        private SmoServerCreator serverCreator;

        public ServerNode(ConnectionCompleteParams connInfo, IMultiServiceProvider serviceProvider)
            : base()
        {
            Validate.IsNotNull(nameof(connInfo), connInfo);
            Validate.IsNotNull("connInfo.ConnectionSummary", connInfo.ConnectionSummary);
            Validate.IsNotNull(nameof(serviceProvider), serviceProvider);

            this.connectionSummary = connInfo.ConnectionSummary;
            this.serverInfo = connInfo.ServerInfo;
            this.connectionUri = connInfo.OwnerUri;

            this.connectionService = serviceProvider.GetService<ConnectionService>();

            this.context = new Lazy<SmoQueryContext>(() => CreateContext(serviceProvider));

            NodeValue = connectionSummary.ServerName;
            IsAlwaysLeaf = false;
            NodeType = NodeTypes.Server.ToString();
            NodeTypeId = NodeTypes.Server;
            Label = GetConnectionLabel();
        }

        internal SmoServerCreator ServerCreator
        {
            get
            {
                if (serverCreator == null)
                {
                    ServerCreator = new SmoServerCreator();
                }
                return serverCreator;
            }
            set
            {
                this.serverCreator = value;
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
            if (!ObjectExplorerUtils.IsSystemDatabaseConnection(connectionSummary.DatabaseName))
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

        private SmoQueryContext CreateContext(IMultiServiceProvider serviceProvider)
        {
            string exceptionMessage;
            ConnectionInfo connectionInfo;
            SqlConnection connection = null;
            // Get server object from connection
            if (!connectionService.TryFindConnection(this.connectionUri, out connectionInfo) ||
                connectionInfo.AllConnections == null || connectionInfo.AllConnections.Count == 0)
            {
                ErrorStateMessage = string.Format(CultureInfo.CurrentCulture, 
                    SR.ServerNodeConnectionError, connectionSummary.ServerName);
                return null;
            }
            //TODO: figure out how to use existing connections
            DbConnection dbConnection = connectionInfo.AllConnections.First();
            ReliableSqlConnection reliableSqlConnection = dbConnection as ReliableSqlConnection;
            SqlConnection sqlConnection = dbConnection as SqlConnection;
            if (reliableSqlConnection != null)
            {
                connection = reliableSqlConnection.GetUnderlyingConnection();
            }
            else if (sqlConnection != null)
            {
                connection = sqlConnection;
            }
            else
            {
                ErrorStateMessage = string.Format(CultureInfo.CurrentCulture,
                   SR.ServerNodeConnectionError, connectionSummary.ServerName);
                return null;
            }

            try
            {
                Server server = ServerCreator.Create(connection);
                return new SmoQueryContext(server, serviceProvider)
                {
                    Parent = server
                };
            }
            catch (ConnectionFailureException cfe)
            {
                exceptionMessage = cfe.Message;
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            Logger.Write(LogLevel.Error, "Exception at ServerNode.CreateContext() : " + exceptionMessage);
            this.ErrorStateMessage = string.Format(SR.TreeNodeError, exceptionMessage);
            return null;
        }

        public override object GetContext()
        {
            return context.Value;
        }
    }

    /// <summary>
    /// Internal for testing purposes only
    /// </summary>
    internal class SmoServerCreator
    {
        public virtual Server Create(SqlConnection connection)
        {
            ServerConnection serverConn = new ServerConnection(connection);
            return new Server(serverConn);
        }
    }
}
