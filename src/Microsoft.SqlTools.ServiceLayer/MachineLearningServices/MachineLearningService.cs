//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.MachineLearningServices.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.MachineLearningServices
{
    public class MachineLearningService
    {
        private ConnectionService connectionService = null;
        private static readonly Lazy<MachineLearningService> instance = new Lazy<MachineLearningService>(() => new MachineLearningService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static MachineLearningService Instance
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
            serviceHost.SetRequestHandler(ExternalScriptConfigStatusRequest.Type, this.HandleExternalScriptConfigStatusRequest);
            serviceHost.SetRequestHandler(ExternalScriptConfigUpdateRequest.Type, this.HandleExternalScriptConfigUpdateRequest);
            serviceHost.SetRequestHandler(ExternalLanguageStatusRequest.Type, this.HandleExternalLanguageStatusRequest);
        }

        /// <summary>
        /// Handles external script config status request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleExternalScriptConfigStatusRequest(ExternalScriptConfigStatusRequestParams parameters, RequestContext<ExternalScriptConfigStatusResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalScriptConfigStatusRequest");
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
                    Server server = new Server(serverConnection);
                    ConfigProperty serverConfig = GetExternalScriptConfig(server);
                    await requestContext.SendResult(new ExternalScriptConfigStatusResponseParams
                    {
                        Status = serverConfig != null && serverConfig.ConfigValue == 1
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
        /// Handles external script update request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleExternalScriptConfigUpdateRequest(ExternalScriptConfigUpdateRequestParams parameters, RequestContext<ExternalScriptConfigUpdateResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalScriptConfigUpdateRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ExternalScriptConfigUpdateResponseParams response = new ExternalScriptConfigUpdateResponseParams
                {
                    Result = true,
                    Message = string.Empty
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                    Server server = new Server(serverConnection);
                    ConfigProperty serverConfig = GetExternalScriptConfig(server);

                    if (serverConfig != null)
                    {
                        try
                        {
                            serverConfig.ConfigValue = parameters.Status ? 1 : 0;
                            server.Configuration.Alter(true);
                            response.Result = true;
                        }
                        catch(FailedOperationException ex)
                        {
                            response.Result = false;
                            response.Message = ex.Message;
                        }
                    }
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
        /// Handles external language status request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleExternalLanguageStatusRequest(ExternalLanguageStatusRequestParams parameters, RequestContext<ExternalLanguageStatusResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalLanguageStatusRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ExternalLanguageStatusResponseParams response = new ExternalLanguageStatusResponseParams
                {
                    Status = false,
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    response.Status = GetLanguageStatus(connInfo, parameters.LanguageName);
                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        private bool GetLanguageStatus(ConnectionInfo connectionInfo, string languageName)
        {
            IDbConnection connection = null;
            bool status = false;
            try
            {
                connection = ConnectionService.OpenSqlConnection(connectionInfo);
                using (IDbCommand command = connection.CreateCommand())
                {
                    
                    command.CommandText = 
                        @"SELECT is_installed
FROM sys.dm_db_external_language_stats s, sys.external_languages l
WHERE s.external_language_id = l.external_language_id AND language = @LanguageName";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@LanguageName";
                    parameter.Value = languageName;
                    command.Parameters.Add(parameter);
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            status = (reader[0].ToString() == "True");
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                connection.Close();
            }

            return status;
        }

        private ConfigProperty GetExternalScriptConfig(Server server)
        {
            ConfigProperty externalScriptConfig = null;
            foreach (ConfigProperty configProperty in server.Configuration.Properties)
            {
                if (configProperty.Number == 1586)
                {
                    externalScriptConfig = configProperty;
                    break;
                }
            }

            return externalScriptConfig;
        }
    }
}
