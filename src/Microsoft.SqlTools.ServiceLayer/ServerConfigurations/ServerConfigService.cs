//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations
{
    public class ServerConfigService
    {
        private ConnectionService connectionService = null;
        private static readonly Lazy<ServerConfigService> instance = new Lazy<ServerConfigService>(() => new ServerConfigService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ServerConfigService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ServerConfigViewRequest.Type, this.HandleServerConfigViewRequest);
            serviceHost.SetRequestHandler(ServerConfigUpdateRequest.Type, this.HandleServerConfigUpdateRequest);
            serviceHost.SetRequestHandler(ServerConfigListRequest.Type, this.HandleServerConfigListRequest);
        }

        /// <summary>
        /// Handles config view request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleServerConfigViewRequest(ServerConfigViewRequestParams parameters, RequestContext<ServerConfigViewResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleServerConfigViewRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                    ServerConfigProperty serverConfig = GetConfig(serverConnection, parameters.ConfigNumber);
                    await requestContext.SendResult(new ServerConfigViewResponseParams
                    {
                        ConfigProperty = serverConfig
                    });
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles config update request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleServerConfigUpdateRequest(ServerConfigUpdateRequestParams parameters, RequestContext<ServerConfigUpdateResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleServerConfigUpdateRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ServerConfigUpdateResponseParams response = new ServerConfigUpdateResponseParams
                {
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                    UpdateConfig(serverConnection, parameters.ConfigNumber, parameters.ConfigValue);
                    response.ConfigProperty = GetConfig(serverConnection, parameters.ConfigNumber);
                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles config list request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleServerConfigListRequest(ServerConfigListRequestParams parameters, RequestContext<ServerConfigListResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleServerConfigListRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ServerConfigListResponseParams response = new ServerConfigListResponseParams
                {
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                    response.ConfigProperties = GetConfigs(serverConnection);
                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }


        /// <summary>
        /// Updates external script in a given server. Throws exception if server doesn't support external script 
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <param name="configValue"></param>
        public void UpdateConfig(ServerConnection serverConnection, int configNumber, int configValue)
        {
            Server server = new Server(serverConnection);
            ConfigProperty serverConfig = GetSmoConfig(server, configNumber);

            if (serverConfig != null)
            {
                try
                {
                    serverConfig.ConfigValue = configValue;
                    server.Configuration.Alter(true);
                }
                catch (FailedOperationException ex)
                {
                    throw new ServerConfigException($"Failed to update config. config number: ${configNumber}", ex);
                }
            }
            else
            {
                throw new ServerConfigException($"Server doesn't have config. config number: ${configNumber}");
            }
        }

        /// <summary>
        /// Returns current value of external script config
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <returns></returns>
        private ServerConfigProperty GetConfig(ServerConnection serverConnection, int configNumber)
        {
            Server server = new Server(serverConnection);
            ConfigProperty serverConfig = GetSmoConfig(server, configNumber);
            return serverConfig != null ? ServerConfigProperty.ToServerConfigProperty(serverConfig) : null;
        }

        private List<ServerConfigProperty> GetConfigs(ServerConnection serverConnection)
        {
            Server server = new Server(serverConnection);
            List<ServerConfigProperty> list = new List<ServerConfigProperty>();
            foreach (ConfigProperty serverConfig in server.Configuration.Properties)
            {
                list.Add(serverConfig != null ? ServerConfigProperty.ToServerConfigProperty(serverConfig) : null);
            }
            return list;
        }

        private ConfigProperty GetSmoConfig(Server server, int configNumber)
        {
            try
            {
                ConfigProperty serverConfig = null;
                foreach (ConfigProperty configProperty in server.Configuration.Properties)
                {
                    if (configProperty.Number == configNumber)
                    {
                        serverConfig = configProperty;
                        break;
                    }
                }

                return serverConfig;
            }
            catch (Exception ex)
            {
                throw new ServerConfigException($"Failed to get config. config number: ${configNumber}", ex);
            }
        }
    }
}
