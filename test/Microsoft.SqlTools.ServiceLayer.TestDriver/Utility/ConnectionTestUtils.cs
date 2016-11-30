//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Utility
{
    /// <summary>
    /// Contains useful utility methods for testing connections
    /// </summary>
    public class ConnectionTestUtils
    {
        public static IEnumerable<TestServerIdentity> TestServers = InitTestServerNames();
        public static Setting Setting = InitSetting();

        private static readonly Lazy<ConnectParams> azureTestServerConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("***REMOVED***"));

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
                return null;
            }
        }

        private static Setting InitSetting()
        {
            try
            {
                string settingsFileContents = GetSettingFileContent();
                Setting setting = Newtonsoft.Json.JsonConvert.DeserializeObject<Setting>(settingsFileContents);

                return setting;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the connection settings. error: " + ex.Message);
                return null;
            }
        }

        public static ConnectParams AzureTestServerConnection
        {
            get { return azureTestServerConnection.Value; }
        }

        public static ConnectParams LocalhostConnection
        {
            get
            {
                return new ConnectParams()
                {
                    Connection = new ConnectionDetails()
                    {
                        DatabaseName = "master",
                        ServerName = "localhost",
                        AuthenticationType = "Integrated"
                    }
                };
            }
        }

        public static ConnectParams InvalidConnection
        {
            get
            {
                return new ConnectParams()
                {
                    Connection = new ConnectionDetails()
                    {
                        DatabaseName = "master",
                        ServerName = "localhost",
                        AuthenticationType = "SqlLogin",
                        UserName = "invalid",
                        Password = ".."
                    }
                };
            }
        }

        private static readonly Lazy<ConnectParams> sqlDataToolsAzureConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("***REMOVED***"));

        public static ConnectParams SqlDataToolsAzureConnection
        {
            get { return sqlDataToolsAzureConnection.Value; }
        }

        private static readonly Lazy<ConnectParams> dataToolsTelemetryAzureConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("***REMOVED***"));

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

        public static ConnectParams DataToolsTelemetryAzureConnection
        {
            get { return dataToolsTelemetryAzureConnection.Value; }
        }

        /// <summary>
        /// Create a connection parameters object
        /// </summary>
        public static ConnectParams CreateConnectParams(string server, string database, string username, string password)
        {
            ConnectParams connectParams = new ConnectParams();
            connectParams.Connection = new ConnectionDetails();
            connectParams.Connection.ServerName = server;
            connectParams.Connection.DatabaseName = database;
            connectParams.Connection.UserName = username;
            connectParams.Connection.Password = password;
            connectParams.Connection.AuthenticationType = "SqlLogin";
            return connectParams;
        }

        /// <summary>
        /// Retrieve connection parameters from the vscode settings file
        /// </summary>
        public static ConnectParams GetConnectionFromVsCodeSettings(string serverName)
        {
            try
            {
                string settingsFileContents = GetSettingFileContent();

                JObject root = JObject.Parse(settingsFileContents);
                JArray connections = (JArray)root["mssql.connections"];

                var connectionObject = connections.Where(x => x["server"].ToString() == serverName).First();

                return CreateConnectParams( connectionObject["server"].ToString(),
                                            connectionObject["database"].ToString(),
                                            connectionObject["user"].ToString(),
                                            connectionObject["password"].ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to load connection " + serverName + " from the vscode settings.json. Ensure the file is formatted correctly.", ex);
            }
        }
    }
}
