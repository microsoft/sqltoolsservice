using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    public class DatabaseNode : TreeNode
    {
        private readonly ConnectionSummary _connectionSummary;
        private readonly ServerInfo _serverInfo;
        private readonly Lazy<QueryContext> _context;

        public DatabaseNode(ConnectionCompleteParams connInfo, IMultiServiceProvider serviceProvider, IDataSource dataSource,
            DataSourceObjectMetadata objectMetadata) : base(dataSource, objectMetadata)
        {
            Validate.IsNotNull(nameof(connInfo), connInfo);
            Validate.IsNotNull("connInfo.ConnectionSummary", connInfo.ConnectionSummary);
            Validate.IsNotNull(nameof(serviceProvider), serviceProvider);

            _connectionSummary = connInfo.ConnectionSummary;
            _serverInfo = connInfo.ServerInfo;
            _context = new Lazy<QueryContext>(() => CreateContext(serviceProvider));

            NodeValue = _connectionSummary.ServerName;
            IsAlwaysLeaf = false;
            NodeType = NodeTypes.Database.ToString();
            NodeTypeId = NodeTypes.Database;
            Label = GetConnectionLabel();
        }

        /// <summary>
        /// Returns the label to display to the user.
        /// </summary>
        private string GetConnectionLabel()
        {
            string userName = _connectionSummary.UserName;

            // TODO Domain and username is not yet supported on .Net Core. 
            // Consider passing as an input from the extension where this can be queried
            //if (string.IsNullOrWhiteSpace(userName))
            //{
            //    userName = Environment.UserDomainName + @"\" + Environment.UserName;
            //}

            // TODO Consider adding IsAuthenticatingDatabaseMaster check in the code and
            // referencing result here
            if (!DatabaseUtils.IsSystemDatabaseConnection(_connectionSummary.DatabaseName))
            {
                // We either have an azure with a database specified or a Denali database using a contained user
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = _connectionSummary.DatabaseName;
                }
                else
                {
                    userName += ", " + _connectionSummary.DatabaseName;
                }
            }

            string label;
            if (string.IsNullOrWhiteSpace(userName))
            {
                label = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1} {2})",
                    _connectionSummary.ServerName,
                    "SQL Server",
                    _serverInfo.ServerVersion);
            }
            else
            {
                label = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1} {2} - {3})",
                    _connectionSummary.ServerName,
                    "SQL Server",
                    _serverInfo.ServerVersion,
                    userName);
            }

            return label;
        }

        private QueryContext CreateContext(IMultiServiceProvider serviceProvider)
        {
            string exceptionMessage;

            try
            {
                return new QueryContext(DataSource, serviceProvider)
                {
                    ParentObjectMetadata = this.ObjectMetadata
                };
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
            return _context.Value;
        }
    }
}