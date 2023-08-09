//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.CoreSql.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectExplorer
{
    public class StatelessObjectExplorerServiceTests
    {

        string databaseName = "tempdb";

        ObjectExplorerServerInfo serverInfo = new ObjectExplorerServerInfo()
        {
            DatabaseName = "tempdb",
            ServerName = "testserver",
            UserName = "testuser",
            IsCloud = true,
            isDefaultOrSystemDatabase = false
        };

        ObjectExplorerOptions options = new ObjectExplorerOptions()
        {
            GroupBySchemaFlagGetter = () => true,
            OperationTimeout = 10000,
        };

        [Test]
        public async Task ExpandingServerNodeShouldReturnSchemas()
        {
            var query = @"Create table t1 (c1 int)
                            GO
                            Create table t2 (c1 int)
                            GO";
            await RunTest(databaseName, query, "testdb", async (testdbName, connectionString) =>
            {


                serverInfo.DatabaseName = testdbName;
                var nodes = StatelessObjectExplorer.Expand(connectionString, null, "", serverInfo, options);

                var dboSchemaPath = $"testserver/{testdbName}/dbo";
                nodes = StatelessObjectExplorer.Expand(connectionString, null, dboSchemaPath, serverInfo, options);

                // Expanding tables in dbo
                var tablesfolder = $"testserver/{testdbName}/dbo/Tables"; 
                nodes = StatelessObjectExplorer.Expand(connectionString, null, tablesfolder, serverInfo, options);

                if (nodes.Length == 0)
                {
                    throw new Exception(" no nodes returned");
                }

            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="query"></param>
        /// <param name="testDbPrefix"></param>
        /// <param name="test"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="query"></param>
        /// <param name="testDbPrefix"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        private async Task RunTest(string databaseName, string query, string testDbPrefix, Func<string, string, Task> test)
        {
            SqlTestDb? testDb = null;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, testDbPrefix);
                if (databaseName == "#testDb#")
                {
                    databaseName = testDb.DatabaseName;
                }
                await test(testDb.DatabaseName, testDb.ConnectionString);
            }
            catch (Exception ex)
            {
                string msg = ex.BuildRecursiveErrorMessage();
                throw new Exception($"Failed to run OE test. error:{msg} {ex.StackTrace}");
            }
            finally
            {
                if (testDb != null)
                {
                    await testDb.CleanupAsync();
                }
            }
        }
    }
}