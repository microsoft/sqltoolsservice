//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions;
using System.Linq;

namespace Microsoft.SqlTools.SqlCore.IntegrationTests.ObjectExplorer
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
            OperationTimeoutSeconds = 10000,
        };

        [Test]
        [TestCase("", "dbo")]
        [TestCase("testserver/{0}/dbo", "Tables")]
        [TestCase("testserver/{0}/dbo/Tables", "dbo.t1")]
        [TestCase("testserver/{0}/dbo/Tables", "dbo.t2")]
        public async Task ExpandingPathShouldReturnCorrectNodes(string oePath, string childLabel)
        {
            var query = @"Create table t1 (c1 int)
                            GO
                            Create table t2 (c1 int)
                            GO";
            await RunTest(databaseName, query, "testdb", async (testdbName, connectionString) =>
            {
                serverInfo.DatabaseName = testdbName;
                var pathWithDb = string.Format(oePath, testdbName);
                var nodes = StatelessObjectExplorer.Expand(connectionString, null, pathWithDb, serverInfo, options);

                Assert.True(nodes.Any(node => node.Label == childLabel), $"Expansion result for {pathWithDb} does not contain node {childLabel}");
            });
        }

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