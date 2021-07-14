//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlTools.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Xunit;

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

        public const string DefaultSql2005InstanceKey = "defaultSql2005";
        public const string DefaultSql2008InstanceKey = "defaultSql2008";
        public const string DefaultSql2011InstanceKey = "defaultSql2011";
        public const string DefaultSql2012Pcu1InstanceKey = "defaultSql2012pcu1";
        public const string DefaultSql2014InstanceKey = "defaultSql2014";
        public const string DefaultSqlAzureInstanceKey = "defaultSqlAzure";
        public const string DefaultServerlessInstanceKey = "defaultServerless";
        public const string DefaultSqlPdwInstanceKey = "defaultSqlPdw";
        public const string DefaultSqlAzureV12InstanceKey = "defaultSqlAzureV12";
        public const string DefaultSql2016InstanceKey = "defaultSql2016";
        public const string DefaultSqlvNextInstanceKey = "defaultSqlvNext";

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

        public static InstanceInfo DefaultSql2012Pcu1
        {
            get { return GetInstance(DefaultSql2012Pcu1InstanceKey); }
        }

        public static InstanceInfo DefaultSql2014
        {
            get { return GetInstance(DefaultSql2014InstanceKey); }
        }

        public static InstanceInfo DefaultSqlAzure
        {
            get { return GetInstance(DefaultSqlAzureInstanceKey); }
        }

        public static InstanceInfo DefaultSqlAzureV12
        {
            get { return GetInstance(DefaultSqlAzureV12InstanceKey); }
        }

        public static InstanceInfo DefaultSql2016
        {
            get { return GetInstance(DefaultSql2016InstanceKey); }
        }

        public static InstanceInfo DefaultSqlvNext
        {
            get { return GetInstance(DefaultSqlvNextInstanceKey); }
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

        public ConnectParams GetConnectionParameters(string key = DefaultSql2016InstanceKey, string databaseName = null)
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
            return  GetConnectionParameters(key, databaseName);
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
                        if (instance.ServerType == TestServerType.None)
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
            catch(Exception ex)
            {
                Assert.True(false, "Fail to load the SQL connection instances. error: " + ex.Message);
            }
        }

        private static void AddInstance(InstanceInfo instance)
        {
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
                }
            }
        }

        private static string ConvertServerTypeToVersionKey(TestServerType serverType)
        {
            return serverType == TestServerType.OnPrem ? DefaultSql2016InstanceKey : DefaultSqlAzureV12InstanceKey;
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
            if (!string.IsNullOrEmpty(databaseName))
            {
                connectParams.Connection.DatabaseName = databaseName;
                connectParams.Connection.DatabaseDisplayName = databaseName;
            }
            if (key == DefaultSqlAzureInstanceKey || key == DefaultSqlAzureV12InstanceKey)
            {
                connectParams.Connection.ConnectTimeout = 30;
                connectParams.Connection.Encrypt = true;
                connectParams.Connection.TrustServerCertificate = false;
            }
            return connectParams;
        }
    }
}
