//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.MachineLearningServices
{
    public class MachineLearningServcieOperations
    {
        private const int ExternalScriptConfigNumber = 1586;
        private const string LanguageStatusScript = @"SELECT is_installed
FROM sys.dm_db_external_language_stats s, sys.external_languages l
WHERE s.external_language_id = l.external_language_id AND language = @LanguageName";

        /// <summary>
        /// Updates external script in a given server. Throws exception if server doesn't support external script 
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <param name="configValue"></param>
        public void UpdateExternalScriptConfig(ServerConnection serverConnection, bool configValue)
        {
            Server server = new Server(serverConnection);
            ConfigProperty serverConfig = GetExternalScriptConfig(server);

            if (serverConfig != null)
            {
                try
                {
                    serverConfig.ConfigValue = configValue ? 1 : 0;
                    server.Configuration.Alter(true);
                }
                catch (FailedOperationException ex)
                {
                    throw new MachineLearningServicesException("Failed to update external script config", ex);
                }
            }
            else
            {
                throw new MachineLearningServicesException("Server doesn't have external script config");
            }
        }

        /// <summary>
        /// Returns current value of external script config
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <returns></returns>
        public ConfigProperty GetExternalScriptConfig(ServerConnection serverConnection)
        {
            Server server = new Server(serverConnection);
            return GetExternalScriptConfig(server);
        }

        private ConfigProperty GetExternalScriptConfig(Server server)
        {
            try
            {
                ConfigProperty externalScriptConfig = null;
                foreach (ConfigProperty configProperty in server.Configuration.Properties)
                {
                    if (configProperty.Number == ExternalScriptConfigNumber)
                    {
                        externalScriptConfig = configProperty;
                        break;
                    }
                }

                return externalScriptConfig;
            }
            catch (Exception ex)
            {
                throw new MachineLearningServicesException("Failed to get external script config", ex);
            }
        }

        /// <summary>
        /// Returns the status of external languages in a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public bool GetLanguageStatus(IDbConnection connection, string languageName)
        {
            bool status = false;
            try
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = LanguageStatusScript;
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
                status = false;
            }

            return status;
        }
    }
}
