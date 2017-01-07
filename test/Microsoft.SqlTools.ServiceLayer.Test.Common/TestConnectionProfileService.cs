//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Service to get connection profiles from the configured settings
    /// to get the credentials, test driver will be used if available otherwise the credential service will be called directly
    /// </summary>
    public class TestConnectionProfileService
    {
        public static IEnumerable<TestServerIdentity> TestServers = InitTestServerNames();
        public static ConnectionSetting Setting = InitSetting();

        public TestConnectionProfileService(ServiceTestDriver driver)
        {
            Driver = driver;
        }

        public TestConnectionProfileService()
        {
        }

        private ServiceTestDriver Driver { get; set; }

        private ConnectionProfile GetConnectionProfile(TestServerType serverType)
        {
            ConnectionProfile connectionProfile = null;

            //Get the server or profile name for given type to use for database connection
            TestServerIdentity serverIdentiry = TestServers != null ? TestServers.FirstOrDefault(x => x.ServerType == serverType) : null;

            //Search for the connection info in settings.json
            if (serverIdentiry == null)
            {
                //If not server name found, try to find the connection info for given type
                connectionProfile = Setting != null && Setting.Connections != null ? Setting.Connections.FirstOrDefault(x => x.ServerType == serverType) : null;
            }
            else
            {
                //Find the connection info for specific server name or profile name
                connectionProfile = Setting != null ? Setting.GetConnentProfile(serverIdentiry.ProfileName, serverIdentiry.ServerName) : null;
            }

            if(connectionProfile == null)
            {
                return new ConnectionProfile
                {
                    ServerName = "localhost",
                    Database = "master",
                    AuthenticationType = AuthenticationType.Integrated
                };  
            }
            return connectionProfile;
        }

        /// <summary>
        /// Returns database connection parameters for given server type
        /// </summary>
        public async Task<ConnectParams> GetConnectionParametersAsync(TestServerType serverType = TestServerType.OnPrem, string databaseName = null)
        {
            ConnectionProfile connectionProfile = GetConnectionProfile(serverType);

            if (connectionProfile != null)
            {
                //If the password is empty, get the credential using the service
                if (string.IsNullOrEmpty(connectionProfile.Password))
                {
                    Credential credential = await ReadCredentialAsync(connectionProfile.formatCredentialId());
                    connectionProfile.Password = credential.Password;
                }

                ConnectParams connenctParam = CreateConnectParams(connectionProfile, serverType, databaseName);
               
                return connenctParam;
            }
            return null;
        }

        /// <summary>
        /// Request a Read Credential for given credential id
        /// </summary>
        private async Task<Credential> ReadCredentialAsync(string credentialId)
        {
            var credentialParams = new Credential();
            credentialParams.CredentialId = credentialId;

            ServiceTestDriver driver = Driver;
            if (driver == null)
            {
                TestServiceProvider.InitializeTestServices();
                return await Task.Factory.StartNew(() => CredentialService.Instance.ReadCredential(credentialParams));
            }
            else
            {
                return await Driver.SendRequest(ReadCredentialRequest.Type, credentialParams);
            }
        }

        /// <summary>
        /// Create a connection parameters object
        /// </summary>
        private ConnectParams CreateConnectParams(ConnectionProfile connectionProfile, TestServerType serverType, string databaseName)
        {
            ConnectParams connectParams = new ConnectParams();
            connectParams.Connection = new ConnectionDetails();
            connectParams.Connection.ServerName = connectionProfile.ServerName;
            connectParams.Connection.DatabaseName = connectionProfile.Database;
            connectParams.Connection.UserName = connectionProfile.User;
            connectParams.Connection.Password = connectionProfile.Password;
            connectParams.Connection.AuthenticationType = connectionProfile.AuthenticationType.ToString();
            if (!string.IsNullOrEmpty(databaseName))
            {
                connectParams.Connection.DatabaseName = databaseName;
            }
            if (serverType == TestServerType.Azure)
            {
                connectParams.Connection.ConnectTimeout = 30;
                connectParams.Connection.Encrypt = true;
                connectParams.Connection.TrustServerCertificate = false;
            }
            return connectParams;
        }

        private static IEnumerable<TestServerIdentity> InitTestServerNames()
        {
            try
            {
                string testServerNamesFilePath = Environment.GetEnvironmentVariable("TestServerNamesFile");
                if (!string.IsNullOrEmpty(testServerNamesFilePath))
                {
                    string jsonFileContent = File.ReadAllText(testServerNamesFilePath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<IList<TestServerIdentity>>(jsonFileContent);
                }
                else
                {
                    return Enumerable.Empty<TestServerIdentity>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the database connection server name settings. error: " + ex.Message);
                return new List<TestServerIdentity>();
            }
        }

        private static ConnectionSetting InitSetting()
        {
            try
            {
                string settingsFileContents = GetSettingFileContent();
                ConnectionSetting setting = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionSetting>(settingsFileContents);

                return setting;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the connection settings. error: " + ex.Message);
                return new ConnectionSetting();
            }
        }

        /// <summary>
        /// Get the location of setting.json. Returns the value of environment variable 'SettingsFileName' and if it's empty returns
        /// the location of vs code settings.json
        /// </summary>
        /// <returns></returns>
        private static string GetSettingFileContent()
        {
            string settingsFilename;
            settingsFilename = Environment.GetEnvironmentVariable("SettingsFileName");

            if (string.IsNullOrEmpty(settingsFilename))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    settingsFilename = Environment.GetEnvironmentVariable("APPDATA") + @"\Code\User\settings.json";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    settingsFilename = Environment.GetEnvironmentVariable("HOME") + @"/Library/Application Support/Code/User/settings.json";
                }
                else
                {
                    settingsFilename = Environment.GetEnvironmentVariable("HOME") + @"/.config/Code/User/settings.json";
                }
            }
            string settingsFileContents = File.ReadAllText(settingsFilename);

            return settingsFileContents;
        }
    }
}
