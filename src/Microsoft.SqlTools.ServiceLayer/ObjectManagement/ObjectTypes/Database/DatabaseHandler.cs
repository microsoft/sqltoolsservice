//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Login object type handler
    /// </summary>
    public class DatabaseHandler : ObjectTypeHandler<DatabaseInfo, DatabaseViewContext>
    {
        public DatabaseHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Database;
        }

        public async override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams requestParams)
        {
            // open a connection for running the user dialog and associated task
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(requestParams.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", requestParams.ConnectionUri);
            }
            string originalDatabaseName = originalConnInfo.ConnectionDetails.DatabaseName;
            try
            {
                originalConnInfo.ConnectionDetails.DatabaseName = requestParams.Database;
                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = requestParams.ContextId,
                    Connection = originalConnInfo.ConnectionDetails,
                    Type = Connection.ConnectionType.Default
                };
                await this.ConnectionService.Connect(connectParams);
            }
            finally
            {
                originalConnInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(requestParams.ContextId, out connInfo);

            // create a default user data context and database object
            CDataContainer dataContainer = CreateDatabaseDataContainer(connInfo, null, ConfigAction.Create, requestParams.Database);
            var parentServer = dataContainer.Server;

            var databaseViewInfo = new DatabaseViewInfo()
            {
                ObjectInfo = new DatabaseInfo()
                {
                    IsAzure = parentServer.ServerType == DatabaseEngineType.SqlAzureDatabase
                }
            };

            var databases = new List<string>();
            foreach (Database database in parentServer.Databases)
            {
                databases.Add(database.Name);
            }
            databaseViewInfo.DatabaseNames = databases.ToArray();

            var logins = new List<string>();
            foreach (Login login in parentServer.Logins)
            {
                logins.Add(login.Name);
            }
            databaseViewInfo.LoginNames = logins.ToArray();

            databaseViewInfo.CollationNames = this.GetCollations(parentServer);
            databaseViewInfo.CompatibilityLevels = this.GetCompatibilityLevels();
            databaseViewInfo.ContainmentTypes = this.GetContainmentTypes();
            databaseViewInfo.RecoveryModels = this.GetRecoveryModels();

            var context = new DatabaseViewContext(requestParams);
            return new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context };
        }

        private string[] GetCollations(Server server)
        {
            return new string[] { "Test" };
        }

        private string[] GetCompatibilityLevels()
        {
            return new string[] { "Test" };
        }

        private string[] GetContainmentTypes()
        {
            return new string[] { "Test" };
        }

        private string[] GetRecoveryModels()
        {
            return new string[] { "Test" };
        }

        private CDataContainer CreateDatabaseDataContainer(ConnectionInfo connInfo, DatabaseInfo database, ConfigAction configAction, string databaseName)
        {
            var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");
            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;

            string urn = (configAction == ConfigAction.Update && database != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server");

            ActionContext context = new ActionContext(serverConnection, "Database", urn);
            DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);

            if (configAction == ConfigAction.Create)
            {
                containerXml.AddProperty("itemtype", "Database");
            }

            XmlDocument xmlDoc = containerXml.GenerateXmlDocument();
            return CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);
        }

        public override Task Save(DatabaseViewContext context, DatabaseInfo obj)
        {
            throw new NotImplementedException();
        }

        public override Task<string> Script(DatabaseViewContext context, DatabaseInfo obj)
        {
            throw new NotImplementedException();
        }
    }
}