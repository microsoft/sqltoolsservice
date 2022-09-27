//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Service to get connection profiles from the configured settings
    /// credential service will be used to get the credentials
    /// </summary>
    public class TestConnectionProfileService
    {
        private static Dictionary<string, InstanceInfo> connectionProfilesCache = new Dictionary<string, InstanceInfo>();
        private static TestConnectionProfileService instance = new TestConnectionProfileService();

        public const string SqlAzureInstanceKey = "sqlAzure";
        public const string SqlOnPremInstanceKey = "sqlOnPrem";

        private TestConnectionProfileService()
        {
            LoadInstanceSettings();
        }

        public static TestConnectionProfileService Instance
        {
            get
            {
                return instance;
            }
        }

        public static InstanceInfo SqlAzure
        {
            get { return GetInstance(SqlAzureInstanceKey); }
        }

        public static InstanceInfo SqlOnPrem
        {
            get { return GetInstance(SqlOnPremInstanceKey); }
        }

        /// <summary>
        /// Returns the SQL connection info for given version key
        /// </summary>
        public static InstanceInfo GetInstance(string key)
        {
            InstanceInfo instanceInfo;
            connectionProfilesCache.TryGetValue(key, out instanceInfo);
            Assert.True(instanceInfo != null, string.Format(CultureInfo.InvariantCulture, "Cannot find any instance for version key: {0}", key));
            return instanceInfo;
        }

        public ConnectParams GetConnectionParameters(string key = SqlOnPremInstanceKey, string databaseName = null)
        {
            InstanceInfo instanceInfo = GetInstance(key);
            if (instanceInfo != null)
            {
                ConnectParams connectParam = CreateConnectParams(instanceInfo, key, databaseName);

                return connectParam;
            }
            return null;
        }

        /// <summary>
        /// Returns database connection parameters for given server type
        /// </summary>
        public ConnectParams GetConnectionParameters(TestServerType serverType = TestServerType.OnPrem, string databaseName = null)
        {
            string key = ConvertServerTypeToVersionKey(serverType);
            return GetConnectionParameters(key, databaseName);
        }

        /// <summary>
        /// Forces the InstanceManager to load/reload it's instance list
        /// </summary>
        internal void LoadInstanceSettings()
        {
            try
            {
                connectionProfilesCache = new Dictionary<string, InstanceInfo>();
                IEnumerable<TestServerIdentity> testServers = TestConfigPersistenceHelper.InitTestServerNames();
                ConnectionSetting settings = TestConfigPersistenceHelper.InitSetting();
                if (settings == null)
                {
                    Console.WriteLine("DBTestInstance not configured. Run 'dotnet run Microsoft.SqlTools.ServiceLayer.TestEnvConfig' from the command line to configure");
                }

                if (testServers != null && settings != null)
                {
                    foreach (var serverIdentity in testServers)
                    {
                        var instance = settings != null ? settings.GetConnectionProfile(serverIdentity.ProfileName, serverIdentity.ServerName) : null;
                        if (instance?.ServerType == TestServerType.None)
                        {
                            instance.ServerType = serverIdentity.ServerType;
                            AddInstance(instance);
                        }
                    }
                }
                if (settings != null)
                {
                    foreach (var instance in settings.Connections)
                    {
                        AddInstance(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.True(false, "Fail to load the SQL connection instances. error: " + ex.Message);
            }
        }

        private static void AddInstance(InstanceInfo instance)
        {
            Console.WriteLine($"Checking whether instance should be added to connections cache, server type: {instance.ServerType.ToString()}, version key: {instance.VersionKey}");
            if (instance != null && (instance.ServerType != TestServerType.None || !string.IsNullOrEmpty(instance.VersionKey)))
            {
                TestServerType serverType = instance.ServerType == TestServerType.None ? TestServerType.OnPrem : instance.ServerType; //Default to onPrem
                string versionKey = string.IsNullOrEmpty(instance.VersionKey) ? ConvertServerTypeToVersionKey(serverType) : instance.VersionKey;
                if (!connectionProfilesCache.ContainsKey(versionKey))
                {
                    //If the password is empty, get the credential using the service
                    if (instance.AuthenticationType == AuthenticationType.SqlLogin && string.IsNullOrEmpty(instance.Password))
                    {
                        Credential credential = TestCredentialService.Instance.ReadCredential(instance);
                        instance.Password = credential.Password;
                    }
                    connectionProfilesCache.Add(versionKey, instance);
                    Console.WriteLine("Instance added.");
                }
                else
                {
                    Console.WriteLine("Instance already in the cache.");
                }
            }
            else
            {
                Console.WriteLine("Instance skipped.");
            }
        }

        private static string ConvertServerTypeToVersionKey(TestServerType serverType)
        {
            return serverType == TestServerType.OnPrem ? SqlOnPremInstanceKey : SqlAzureInstanceKey;
        }

        /// <summary>
        /// Create a connection parameters object
        /// </summary>
        private ConnectParams CreateConnectParams(InstanceInfo connectionProfile, string key, string databaseName)
        {
            ConnectParams connectParams = new ConnectParams();
            connectParams.Connection = new ConnectionDetails();
            connectParams.Connection.ServerName = connectionProfile.ServerName;
            connectParams.Connection.DatabaseName = connectionProfile.Database;
            connectParams.Connection.DatabaseDisplayName = connectionProfile.Database;
            connectParams.Connection.UserName = connectionProfile.User;
            connectParams.Connection.Password = connectionProfile.Password;
            connectParams.Connection.MaxPoolSize = 200;
            connectParams.Connection.AuthenticationType = connectionProfile.AuthenticationType.ToString();

            if (connectParams.Connection.Encrypt == Data.SqlClient.SqlConnectionEncryptOption.Strict)
            {
                connectParams.Connection.HostNameInCertificate = connectionProfile.HostNameInCertificate;
            }
            else
            {
                connectParams.Connection.TrustServerCertificate = true;
            }

            if (!string.IsNullOrEmpty(databaseName))
            {
                connectParams.Connection.DatabaseName = databaseName;
                connectParams.Connection.DatabaseDisplayName = databaseName;
            }
            if (key == SqlAzureInstanceKey || key == SqlAzureInstanceKey)
            {
                connectParams.Connection.ConnectTimeout = 30;
                connectParams.Connection.Encrypt = true;
                connectParams.Connection.TrustServerCertificate = false;
                connectParams.Connection.HostNameInCertificate = connectionProfile.HostNameInCertificate;
            }

            return connectParams;
        }
    }
}
