//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Kusto;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.KustoModel
{
    /// <summary>
    /// Server node implementation 
    /// </summary>
    public class ServerNode : TreeNode
    {
        private ConnectionSummary connectionSummary;
        private ServerInfo serverInfo;
        private Lazy<KustoQueryContext> context;
        private KustoWrapper smoWrapper;
        private SqlServerType sqlServerType;
        private ServerConnection serverConnection;

        public ServerNode(ConnectionCompleteParams connInfo, IMultiServiceProvider serviceProvider, ServerConnection serverConnection)
            : base()
        {
            Validate.IsNotNull(nameof(connInfo), connInfo);
            Validate.IsNotNull("connInfo.ConnectionSummary", connInfo.ConnectionSummary);
            Validate.IsNotNull(nameof(serviceProvider), serviceProvider);

            this.connectionSummary = connInfo.ConnectionSummary;
            this.serverInfo = connInfo.ServerInfo;
            this.sqlServerType = ServerVersionHelper.CalculateServerType(this.serverInfo);

            this.context = new Lazy<KustoQueryContext>(() => CreateContext(serviceProvider));
            this.serverConnection = serverConnection;

            NodeValue = connectionSummary.ServerName;
            IsAlwaysLeaf = false;
            NodeType = NodeTypes.Server.ToString();
            NodeTypeId = NodeTypes.Server;
            Label = GetConnectionLabel();
        }

        internal KustoWrapper KustoWrapper
        {
            get
            {
                if (smoWrapper == null)
                {
                    smoWrapper = new KustoWrapper();
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

       

        private KustoQueryContext CreateContext(IMultiServiceProvider serviceProvider)
        {
            string exceptionMessage;
   
            try
            {
                Server server = KustoWrapper.CreateServer(this.serverConnection);
                if (server != null)
                {
                    return new KustoQueryContext(server, serviceProvider, KustoWrapper)
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
