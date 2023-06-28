//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
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
                this.server = new Server(context.Connection);
                if (this.server != null)
                {
                    this.serverViewInfo.ObjectInfo = new ServerInfo()
                    {
                        Name = server.Name,
                        HardwareGeneration = server.HardwareGeneration,
                        Language = server.Language,
                        MemoryInMB = server.PhysicalMemory,
                        OperatingSystem = server.HostDistribution,
                        Platform = server.HostPlatform,
                        Processors = server.Processors,
                        IsClustered = server.IsClustered,
                        IsHadrEnabled = server.IsHadrEnabled,
                        IsPolyBaseInstalled = server.IsPolyBaseInstalled,
                        IsXTPSupported = server.IsXTPSupported,
                        Product = server.Product,
                        ReservedStorageSizeMB = server.ReservedStorageSizeMB,
                        RootDirectory = server.RootDirectory,
                        ServerCollation = server.Collation,
                        ServiceTier = server.ServiceTier,
                        StorageSpaceUsageInGB = (int)ByteConverter.ConvertMbtoGb(server.UsedStorageSizeMB),
                        Version = server.Version.ToString(),
                        MinServerMemory = GetServerMinMemory(),
                        MaxServerMemory = GetServerMaxMemory()
                    };
                }

                return Task.FromResult(new InitializeViewResult { ViewInfo = this.serverViewInfo, Context = context });
            }
        }

        public override Task Save(ServerViewContext context, ServerInfo obj)
        {
            UpdateServerProperties(context.Parameters, obj);
            return Task.CompletedTask;
        }

        public override Task<string> Script(ServerViewContext context, ServerInfo obj)
        {
            throw new NotSupportedException("ServerHandler does not support Script method");
        }

        private int GetServerMaxMemory()
        {
            return configService.GetServerSmoConfig(server, configService.MaxServerMemoryPropertyNumber).ConfigValue;
        }

        private int GetServerMinMemory()
        {
            return configService.GetServerSmoConfig(server, configService.MinServerMemoryPropertyNumber).ConfigValue;
        }

        private void UpdateServerProperties(InitializeViewRequestParams viewParams, ServerInfo serverObj)
        {
            if (viewParams != null)
            {
                ConnectionInfo connInfo = this.GetConnectionInfo(viewParams.ConnectionUri);
                ServerConnection serverConnection = ConnectionService.OpenServerConnection(connInfo, ObjectManagementService.ApplicationName);
                this.server = new Server(serverConnection);
                if (GetServerMaxMemory() != serverObj.MaxServerMemory)
                {
                    configService.UpdateConfig(serverConnection, configService.MaxServerMemoryPropertyNumber, serverObj.MaxServerMemory);
                }
                else if (GetServerMinMemory() != serverObj.MinServerMemory)
                {
                    configService.UpdateConfig(serverConnection, configService.MinServerMemoryPropertyNumber, serverObj.MinServerMemory);
                }
            }
        }
    }
}