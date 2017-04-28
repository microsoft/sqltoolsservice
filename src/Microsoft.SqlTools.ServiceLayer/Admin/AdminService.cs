//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
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

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal AdminService()
        {
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
        }


        private static XmlDocument CreateDataContainerDocument()
        {
            string xml =
@"<?xml version=""1.0""?>
<formdescription><params>
<servername>sqltools100</servername>
<connectionmoniker>sqltools100 (SQLServer, user = sa)</connectionmoniker>
<servertype>sql</servertype>
<urn>Server[@Name='SQLTOOLS100']</urn>
<itemtype>Database</itemtype>
<assemblyname>SqlManagerUi.dll</assemblyname>
<formtype>Microsoft.SqlServer.Management.SqlManagerUI.CreateDatabase</formtype>
<object-name-9524b5c1-e996-4119-a433-b5b947985566>SQLTOOLS100</object-name-9524b5c1-e996-4119-a433-b5b947985566>
</params></formdescription>
";
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

            XmlDocument xmlDoc = CreateDataContainerDocument();

            char[] passwordArray = "Katmai900".ToCharArray();
            unsafe
            {
                fixed (char* passwordPtr = passwordArray)
                {

                    var dataContainer = new CDataContainer(
                        CDataContainer.ServerType.SQL,
                        "sqltools100",
                        false,
                        "sa",
                        new System.Security.SecureString(passwordPtr, passwordArray.Length),
                        xmlDoc.InnerXml);

                    var createDb = new DatabaseTaskHelper();
                    createDb.CreateDatabase(dataContainer);
                }
            }

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
