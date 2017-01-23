//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Threading.Tasks;

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
        public static async Task<SqlTestDb> CreateNew(TestServerType serverType, bool doNotCleanupDb = false, string databaseName = null, string query = null)
        {
            SqlTestDb testDb = new SqlTestDb();
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                databaseName = databaseName ?? GetUniqueDBName("");
                string createDatabaseQuery = Scripts.CreateDatabaseQuery.Replace("#DatabaseName#", databaseName);
                await testService.RunQuery(serverType, MasterDatabaseName, createDatabaseQuery);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' is created", databaseName));
                if (!string.IsNullOrEmpty(query))
                {
                    await testService.RunQuery(serverType, databaseName, query);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' SQL types are created", databaseName));
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
            return CreateNew(serverType, false, null, query).Result;
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

        public void CleanUp()
        {
            CleanUpAsync().Wait();
        }

        public async Task CleanUpAsync()
        {
            if (!DoNotCleanupDb)
            {
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                        (ServerType == TestServerType.Azure ? Scripts.DropDatabaseIfExistAzure : Scripts.DropDatabaseIfExist), DatabaseName);
                    await testService.RunQuery(ServerType, MasterDatabaseName, dropDatabaseQuery);
                }
            }
        }

        public void Dispose()
        {
            CleanUp();
        }
    }
}
