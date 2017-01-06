//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Common;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestDatabaseService
    {
        public const string MasterDatabaseName = "master";

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        internal static async Task CreateTestDatabase(TestServerType serverType, string databaseName = null, string query = null)
        {
            using (TestServiceDriverProvier testService = new TestServiceDriverProvier())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                databaseName = databaseName ?? GetUniqueDBName("");
                string createDatabaseQuery = Scripts.CreateDatabaseQuery.Replace("#DatabaseName#", databaseName);
                await testService.RunQuery(serverType, MasterDatabaseName, createDatabaseQuery);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' is created", databaseName));
                if (!string.IsNullOrEmpty(query))
                {
                    await testService.RunQuery(serverType, databaseName, query);// Scripts.CreateDatabaseObjectsQuery);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' SQL types are created", databaseName));
                }
            }
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
    }
}
