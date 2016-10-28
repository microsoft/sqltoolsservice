//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
        private static readonly Lazy<ConnectParams> azureTestServerConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("y6q8uzka1m.database.windows.net"));

        public static ConnectParams AzureTestServerConnection
        {
            get { return azureTestServerConnection.Value; }
        }

        private static readonly Lazy<ConnectParams> sqlDataToolsAzureConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("ssdtprod.database.windows.net"));

        public static ConnectParams SqlDataToolsAzureConnection
        {
            get { return sqlDataToolsAzureConnection.Value; }
        }

        private static readonly Lazy<ConnectParams> dataToolsTelemetryAzureConnection =
            new Lazy<ConnectParams>(() => GetConnectionFromVsCodeSettings("datatoolstelemetry.database.windows.net"));

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
            string settingsFilename;
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
            string settingsFileContents = File.ReadAllText(settingsFilename);

            JObject root = JObject.Parse(settingsFileContents);
            JArray connections = (JArray)root["mssql.connections"];

            var connectionObject = connections.Where(x => x["server"].ToString() == serverName).First();

            return CreateConnectParams( connectionObject["server"].ToString(),
                                        connectionObject["database"].ToString(),
                                        connectionObject["user"].ToString(),
                                        connectionObject["password"].ToString());
        }
    }
}
