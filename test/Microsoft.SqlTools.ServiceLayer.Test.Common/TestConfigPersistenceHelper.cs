//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Class to handle loading test configuration settings
    ///    
    /// Example contents of file at default location ~/sqlConnectionSettings.json
    /// 
    /// {
    ///     "mssql.connections": [
    ///         {
    ///             "server": "localhost",
    ///             "database": "master",
    ///             "authenticationType": "SqlLogin",
    ///             "user": "sa",
    ///             "password": "[putvaluehere]",
    ///             "serverType":"OnPrem", 
    ///             "VersionKey": "defaultSql2016"
    ///         }
    ///     ]
    /// }
    /// 
    /// </summary> 
    public sealed class TestConfigPersistenceHelper
    {
        private static string DefaultSettingFileName = Path.Combine(FileUtils.UserRootFolder, "sqlConnectionSettings.json");
        private static TestCredentialService credentialService = TestCredentialService.Instance;

        public static bool Write(IEnumerable<InstanceInfo> instances)
        {
            try
            {
                ConnectionSetting connectionSetting = new Common.ConnectionSetting()
                {
                    Connections = new List<InstanceInfo>(instances)
                };

                //Remove the passwords and store in credential store and then store the copy without passwords in the file
                foreach (var instance in connectionSetting.Connections)
                {
                    if (!string.IsNullOrEmpty(instance.Password))
                    {
                        
                        if (!credentialService.SaveCredential(instance))
                        {
                            Console.WriteLine("Failed to store the password for server: " + instance.ServerName);
                        }
                        
                        instance.Password = null; //Make sure the password is not stored in sqlConnectionSettings.json
                        instance.AuthenticationType = AuthenticationType.SqlLogin;
                    }
                    else
                    {
                        instance.AuthenticationType = AuthenticationType.Integrated;
                    }
                }
                
                Console.WriteLine("The SQL connection instances will be written to " + DefaultSettingFileName);
                string jsonContent = JsonConvert.SerializeObject(connectionSetting);

                if (File.Exists(DefaultSettingFileName))
                {
                    Console.WriteLine("The file " + DefaultSettingFileName + " already exists and it will be overwritten.");

                }
                File.WriteAllText(DefaultSettingFileName, jsonContent);
                Environment.SetEnvironmentVariable(Constants.SqlConectionSettingsEnvironmentVariable, DefaultSettingFileName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to store the instances.", ex);
                return false;
            }
        }

        internal static IEnumerable<TestServerIdentity> InitTestServerNames()
        {
            try
            {
                string testServerNamesFileContent = GetTestServerNamesFileContent();
                if (!string.IsNullOrEmpty(testServerNamesFileContent))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<IList<TestServerIdentity>>(testServerNamesFileContent);
                }
                else
                {
                    return Enumerable.Empty<TestServerIdentity>();
                }
            }
            catch (Exception)
            {
                return Enumerable.Empty<TestServerIdentity>();
            }
        }

        internal static ConnectionSetting InitSetting()
        {
            try
            {
                string settingsFileContents = GetSettingFileContent();
                ConnectionSetting setting = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionSetting>(settingsFileContents);
                Console.WriteLine("Connection Settings loaded successfully");
                return setting;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the connection settings. error: " + ex.Message);
                return new ConnectionSetting();
            }
        }

        /// <summary>
        /// Get the location of testServerNames.json. Returns the value of environment variable 'SettingsFileName' and if it's empty returns
        /// the location of vs code testServerNames.json
        /// </summary>
        /// <returns></returns>
        private static string GetTestServerNamesFileContent()
        {
            var testServerNameFilePath = Environment.GetEnvironmentVariable("TestServerNamesFile");

            if (string.IsNullOrEmpty(testServerNameFilePath))
            {
                testServerNameFilePath = FileUtils.TestServerNamesDefaultFileName;
            }
            string testServerNamesFileContent = string.IsNullOrEmpty(testServerNameFilePath) ? string.Empty : File.ReadAllText(testServerNameFilePath);

            return testServerNamesFileContent;
        }

        /// <summary>
        /// Get the location of setting.json. Returns the value of environment variable 'SettingsFileName' and if it's empty returns
        /// the location of vs code settings.json
        /// </summary>
        /// <returns></returns>
        private static string GetSettingFileContent()
        {
            var settingsFileName = Environment.GetEnvironmentVariable(Constants.SqlConectionSettingsEnvironmentVariable);

            if (string.IsNullOrEmpty(settingsFileName))
            {
                if (File.Exists(DefaultSettingFileName))
                {
                    settingsFileName = DefaultSettingFileName;
                    Console.WriteLine(DefaultSettingFileName + " SQL connection instances are not configured. Will try to get connections from VS code settings.json");
                }
                else
                {
                    //If the SQL connection settings is not set use the VS code one
                    settingsFileName = FileUtils.VsCodeSettingsFileName;
                }
            }

            if (string.IsNullOrEmpty(settingsFileName))
            {
                Console.WriteLine("SQL connection instances are not configured. Run dotnet run Microsoft.SqlTools.ServiceLayer.TestEnvConfig from the command line to configure");
            }
            else
            {
                Console.WriteLine("SQL Connection settings are loaded from: " + settingsFileName);
            }

            string settingsFileContents = string.IsNullOrEmpty(settingsFileName) ? string.Empty : File.ReadAllText(settingsFileName);

            return settingsFileContents;
        }
    }
}
