//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
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
        private ObjectExplorerServerInfo serverInfo;
        private Lazy<SmoQueryContext> context;
        private SmoWrapper smoWrapper;
        private SqlServerType sqlServerType;
        private ServerConnection serverConnection;

        public ServerNode(ObjectExplorerServerInfo serverInfo, ServerConnection serverConnection)
            : base()
        {
            Validate.IsNotNull(nameof(ObjectExplorerServerInfo), serverInfo);

            this.serverInfo = serverInfo;
            this.sqlServerType = ServerVersionHelper.CalculateServerType(this.serverInfo);

            this.context = new Lazy<SmoQueryContext>(CreateContext());
            this.serverConnection = serverConnection;

            NodeValue = serverInfo.ServerName;
            IsAlwaysLeaf = false;
            NodeType = NodeTypes.Server.ToString();
            NodeTypeId = NodeTypes.Server;
            Label = GetConnectionLabel();
        }

        internal SmoWrapper SmoWrapper
        {
            get
            {
                smoWrapper ??= new SmoWrapper();
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
            string userName = serverInfo.UserName;

            // TODO Domain and username is not yet supported on .Net Core. 
            // Consider passing as an input from the extension where this can be queried
            //if (string.IsNullOrWhiteSpace(userName))
            //{
            //    userName = Environment.UserDomainName + @"\" + Environment.UserName;
            //}

            // TODO Consider adding IsAuthenticatingDatabaseMaster check in the code and
            // referencing result here
            if (!DatabaseUtils.IsSystemDatabaseConnection(serverInfo.DatabaseName))
            {
                // We either have an azure with a database specified or a Denali database using a contained user
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = serverInfo.DatabaseName;
                }
                else
                {
                    userName += ", " + serverInfo.DatabaseName;
                }
            }

            string label;
            if (string.IsNullOrWhiteSpace(userName))
            {
                label = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1} {2})",
                serverInfo.ServerName,
                "SQL Server",
                serverInfo.ServerVersion);
            }
            else
            {
                label = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1} {2} - {3})",
                serverInfo.ServerName,
                "SQL Server",
                serverInfo.ServerVersion,
                userName);
            }

            return label;
        }



        private SmoQueryContext CreateContext()
        {
            string exceptionMessage;

            try
            {
                Server server = SmoWrapper.CreateServer(this.serverConnection);
                if (server != null)
                {
                    return new SmoQueryContext(server, SmoWrapper)
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
