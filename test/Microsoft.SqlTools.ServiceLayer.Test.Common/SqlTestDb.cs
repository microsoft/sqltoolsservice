//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;

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

        public string ConnectionString
        {
            get
            {
                ConnectParams connectParams = TestConnectionProfileService.Instance.GetConnectionParameters(this.ServerType, this.DatabaseName);
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = connectParams.Connection.ServerName,
                    InitialCatalog = connectParams.Connection.DatabaseName,
                };

                if (connectParams.Connection.AuthenticationType == "Integrated")
                {
                    builder.IntegratedSecurity = true;
                }
                else
                {
                    builder.UserID = connectParams.Connection.UserName;
                    builder.Password = connectParams.Connection.Password;
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        public static SqlTestDb CreateNew(
            TestServerType serverType,
            bool doNotCleanupDb = false,
            string databaseName = null,
            string query = null,
            string dbNamePrefix = null)
        {
            return CreateNewAsync(serverType, doNotCleanupDb, databaseName, query, dbNamePrefix).Result;
        }

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        public static async Task<SqlTestDb> CreateNewAsync(
            TestServerType serverType,
            bool doNotCleanupDb = false,
            string databaseName = null,
            string query = null,
            string dbNamePrefix = null)
        {
            SqlTestDb testDb = new SqlTestDb();

            databaseName = databaseName ?? GetUniqueDBName(dbNamePrefix);
            string createDatabaseQuery = Scripts.CreateDatabaseQuery.Replace("#DatabaseName#", databaseName);
            await TestServiceProvider.Instance.RunQueryAsync(serverType, MasterDatabaseName, createDatabaseQuery);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test database '{0}' is created", databaseName));
            if (!string.IsNullOrEmpty(query))
            {
                query = string.Format(CultureInfo.InvariantCulture, query, databaseName);
                await TestServiceProvider.Instance.RunQueryAsync(serverType, databaseName, query);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Test database '{0}' SQL types are created", databaseName));
            }
            testDb.DatabaseName = databaseName;
            testDb.ServerType = serverType;
            testDb.DoNotCleanupDb = doNotCleanupDb;

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
        /// Create the test db if not already exists
        /// </summary>
        public static SqlTestDb CreateNew(TestServerType serverType)
        {
            return CreateNew(serverType, false, null, null);
        }

        /// <summary>
        /// Represents a test Database that was created in a test
        /// </summary>
        public static SqlTestDb CreateFromExisting(
            string dbName,
            TestServerType serverType = TestServerType.OnPrem,
            bool doNotCleanupDb = false)
        {
            SqlTestDb testDb = new SqlTestDb();

            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentOutOfRangeException("dbName");
            }

            testDb.DatabaseName = dbName;
            testDb.ServerType = serverType;

            return testDb;
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
            CleanupAsync().Wait();
        }

        public async Task CleanupAsync()
        {
            try
            {
                if (!DoNotCleanupDb)
                {
                    await DropDatabase(DatabaseName, ServerType);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Failed to cleanup database: {0} error:{1}", DatabaseName, ex.Message));
            }
        }

        public static async Task DropDatabase(string databaseName, TestServerType serverType = TestServerType.OnPrem)
        {
            string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                       (serverType == TestServerType.Azure ? Scripts.DropDatabaseIfExistAzure : Scripts.DropDatabaseIfExist), databaseName);

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Cleaning up database {0}", databaseName));
            await TestServiceProvider.Instance.RunQueryAsync(serverType, MasterDatabaseName, dropDatabaseQuery);
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

        /// <summary>
        /// Runs the passed query against the test db.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="throwOnError">If true, throw an exception if the query encounters an error executing a batch statement.</param>
        public void RunQuery(string query, bool throwOnError = false)
        {
            TestServiceProvider.Instance.RunQuery(this.ServerType, this.DatabaseName, query, throwOnError);
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
