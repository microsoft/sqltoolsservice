﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.ObjectTypes.Server;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Server object type handler
    /// </summary>
    public class ServerHandler : ObjectTypeHandler<ServerInfo, ServerViewContext>
    {
        private ServerViewInfo serverViewInfo = new ServerViewInfo();
        private ServerConfigService configService = new ServerConfigService();
        private Server server = null;

        public ServerHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Server;
        }

        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams requestParams)
        {
            ConnectionInfo connInfo = this.GetConnectionInfo(requestParams.ConnectionUri);
            ServerConnection serverConnection = ConnectionService.OpenServerConnection(connInfo, ObjectManagementService.ApplicationName);

            using (var context = new ServerViewContext(requestParams, serverConnection))
            {
                var serverSmo = CreateServerSmoObject(context).Result;
                if(serverSmo != null)
                {
                    this.serverViewInfo.ObjectInfo = new ServerInfo()
                    {
                        Name = serverSmo.Name,
                        HardwareGeneration = serverSmo.HardwareGeneration,
                        Language = serverSmo.Language,
                        MemoryInMB = serverSmo.PhysicalMemory,
                        OperatingSystem = serverSmo.HostDistribution,
                        Platform = serverSmo.HostPlatform,
                        Processors = serverSmo.Processors,
                        IsClustered = serverSmo.IsClustered,
                        IsHadrEnabled = serverSmo.IsHadrEnabled,
                        IsPolyBaseInstalled = serverSmo.IsPolyBaseInstalled,
                        IsXTPSupported = serverSmo.IsXTPSupported,
                        Product = serverSmo.Product,
                        ReservedStorageSizeMB = serverSmo.ReservedStorageSizeMB,
                        RootDirectory = serverSmo.RootDirectory,
                        ServerCollation = serverSmo.Collation,
                        ServiceTier = serverSmo.ServiceTier,
                        StorageSpaceUsageInGB = (int)ByteConverter.ConvertMbtoGb(serverSmo.UsedStorageSizeMB),
                        Version = serverSmo.Version.ToString(),
                        MinServerMemory = GetServerMinMemory(),
                        MaxServerMemory = GetServerMaxMemory()
                    };
                }

                return Task.FromResult(new InitializeViewResult { ViewInfo = this.serverViewInfo, Context = context });
            }
        }

        public override Task Save(ServerViewContext context, ServerInfo serverInfo)
        {
            var ex = new NotSupportedException();
            ex.Data["UserMessage"] += "ServerHandler does not support Save method";
            throw ex;
        }

        public override Task<string> Script(ServerViewContext context, ServerInfo obj)
        {
            var ex = new NotSupportedException();
            ex.Data["UserMessage"] += "ServerHandler does not support Script method";
            throw ex;
        }
        public Task<Server?> CreateServerSmoObject(ServerViewContext context)
        {
            using (context.Connection.SqlConnectionObject)
            {
                this.server = new Server(context.Connection);
                string objectUrn = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server");
                var serverSmo = server.GetSmoObject(new Urn(objectUrn)) as Server;
                return Task.FromResult(serverSmo);
            }
        }

        private int GetServerMaxMemory()
        {
            return configService.GetSmoConfig(server, 1544).ConfigValue;
        }

        private int GetServerMinMemory()
        {
            return configService.GetSmoConfig(server, 1543).ConfigValue;
        }
    }
}