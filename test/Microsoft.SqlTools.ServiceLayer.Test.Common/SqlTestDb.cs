//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Creates a new test database
    /// </summary>
    public class SqlTestDb : IDisposable
    {
        public const string MasterDatabaseName = "master";

        public string DatabaseName { get; set; }

        public TestServerType ServerType { get; set; }

        public bool DoNotCleanupDb { get; set; }

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        public static SqlTestDb CreateNew(TestServerType serverType, bool doNotCleanupDb = false, string databaseName = null, string query = null)
        {
            SqlTestDb testDb = new SqlTestDb();

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                databaseName = databaseName ?? GetUniqueDBName("");
                string createDatabaseQuery = Scripts.CreateDatabaseQuery.Replace("#DatabaseName#", databaseName);
                TestServiceProvider.Instance.RunQuery(serverType, MasterDatabaseName, createDatabaseQuery);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test database '{0}' is created", databaseName));
                if (!string.IsNullOrEmpty(query))
                {
                    TestServiceProvider.Instance.RunQuery(serverType, databaseName, query);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test database '{0}' SQL types are created", databaseName));
                }
                testDb.DatabaseName = databaseName;
                testDb.ServerType = serverType;
                testDb.DoNotCleanupDb = doNotCleanupDb;
            }

            return testDb;
        }

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        public static SqlTestDb CreateNew(TestServerType serverType, string query = null)
        {
            return CreateNew(serverType, false, null, query);
        }

        /// <summary>
        /// Returns a mangled name that unique based on Prefix + Machine + Process
        /// </summary>
        /// <param name="namePrefix"></param>
        /// <returns></returns>
        public static string GetUniqueDBName(string namePrefix)
        {
            string safeMachineName = Environment.MachineName.Replace('-', '_');
            return string.Format("{0}_{1}_{2}",
                namePrefix, safeMachineName, Guid.NewGuid().ToString().Replace("-", ""));
        }

        public void Cleanup()
        {
            if (!DoNotCleanupDb)
            {
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                    (ServerType == TestServerType.Azure ? Scripts.DropDatabaseIfExistAzure : Scripts.DropDatabaseIfExist), DatabaseName);
                    TestServiceProvider.Instance.RunQuery(ServerType, MasterDatabaseName, dropDatabaseQuery);
                }
            }
        }

        /// <summary>
        /// Returns connection info after making a connection to the database
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="databaseName"></param>
        /// <param name="scriptFilePath"></param>
        /// <returns></returns>
        public ConnectionInfo InitLiveConnectionInfo(TestServerType serverType, string databaseName, string scriptFilePath)
        {
            ConnectParams connectParams = TestConnectionProfileService.Instance.GetConnectionParameters(serverType, databaseName);

            string ownerUri = scriptFilePath;
            var connectionService = ConnectionService.Instance;
            var connectionResult = connectionService.Connect(new ConnectParams()
            {
                OwnerUri = ownerUri,
                Connection = connectParams.Connection
            });

            connectionResult.Wait();

            ConnectionInfo connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            Assert.NotNull(connInfo);
            return connInfo;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
