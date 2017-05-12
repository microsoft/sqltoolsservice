//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Datasource admin task service class
    /// </summary>
    public class AdminService
    {
        private static readonly Lazy<AdminService> instance = new Lazy<AdminService>(() => new AdminService());

        private static ConnectionService connectionService = null;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal AdminService()
        {
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (AdminService.connectionService == null)
                {
                    AdminService.connectionService = ConnectionService.Instance;
                }
                return AdminService.connectionService;
            }

            set
            {
                AdminService.connectionService = value;
            }
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AdminService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(CreateDatabaseRequest.Type, HandleCreateDatabaseRequest);
            serviceHost.SetRequestHandler(CreateLoginRequest.Type, HandleCreateLoginRequest);
            serviceHost.SetRequestHandler(DefaultDatabaseInfoRequest.Type, HandleDefaultDatabaseInfoRequest);
        }

        private static DatabaseTaskHelper CreateDatabaseTaskHelper(ConnectionInfo connInfo)
        {
            XmlDocument xmlDoc = CreateDataContainerDocument(connInfo);
            char[] passwordArray = connInfo.ConnectionDetails.Password.ToCharArray();
            CDataContainer dataContainer;

            unsafe
            {
                fixed (char* passwordPtr = passwordArray)
                {
                    dataContainer = new CDataContainer(
                        CDataContainer.ServerType.SQL,
                        connInfo.ConnectionDetails.ServerName,
                        false,
                        connInfo.ConnectionDetails.UserName,
                        new System.Security.SecureString(passwordPtr, passwordArray.Length),
                        xmlDoc.InnerXml);
                }
            }

            var taskHelper = new DatabaseTaskHelper(dataContainer);
            return taskHelper;
        }

        /// <summary>
        /// Handle a request for the default database prototype info
        /// </summary>
        public static async Task HandleDefaultDatabaseInfoRequest(
            DefaultDatabaseInfoParams optionsParams,
            RequestContext<DefaultDatabaseInfoResponse> requestContext)
        {
            var response = new DefaultDatabaseInfoResponse();
            ConnectionInfo connInfo;
            AdminService.ConnectionServiceInstance.TryFindConnection(
                optionsParams.OwnerUri,
                out connInfo);

            DatabaseTaskHelper taskHelper = CreateDatabaseTaskHelper(connInfo);

            response.DefaultDatabaseInfo = DatabaseTaskHelper.DatabasePrototypeToDatabaseInfo(taskHelper.Prototype);
            await requestContext.SendResult(response);
        }

        private static XmlDocument CreateDataContainerDocument(ConnectionInfo connInfo)
        {
            string xml =
               string.Format(@"<?xml version=""1.0""?>
                <formdescription><params>
                <servername>{0}</servername>
                <connectionmoniker>{0} (SQLServer, user = {1})</connectionmoniker>
                <servertype>sql</servertype>
                <urn>Server[@Name='{0}']</urn>
                <itemtype>Database</itemtype>                
                </params></formdescription> ",
                connInfo.ConnectionDetails.ServerName.ToUpper(),
                connInfo.ConnectionDetails.UserName);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            return xmlDoc;
        }

        /// <summary>
        /// Handles a create database request
        /// </summary>
        internal static async Task HandleCreateDatabaseRequest(
            CreateDatabaseParams databaseParams,
            RequestContext<CreateDatabaseResponse> requestContext)
        {
            var response = new DefaultDatabaseInfoResponse();
            ConnectionInfo connInfo;
            AdminService.ConnectionServiceInstance.TryFindConnection(
                databaseParams.OwnerUri,
                out connInfo);

            DatabaseTaskHelper taskHelper = CreateDatabaseTaskHelper(connInfo);
            DatabaseTaskHelper.ApplyToPrototype(databaseParams.DatabaseInfo, taskHelper.Prototype);

            response.DefaultDatabaseInfo = DatabaseTaskHelper.DatabasePrototypeToDatabaseInfo(taskHelper.Prototype);

            await requestContext.SendResult(new CreateDatabaseResponse());
        }

        /// <summary>
        /// Handles a create login request
        /// </summary>
        internal static async Task HandleCreateLoginRequest(
            CreateLoginParams loginParams,
            RequestContext<CreateLoginResponse> requestContext)
        {
            await requestContext.SendResult(new CreateLoginResponse());
        }
    }
}
