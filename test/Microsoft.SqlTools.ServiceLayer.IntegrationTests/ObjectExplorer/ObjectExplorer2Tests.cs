//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectExplorer
{
    public class ObjectExplorer2Tests
    {
        string databaseName = "tempdb";

        [Test]
        public async Task ExpandingPathShouldReturnCorrectNodes()
        {
            var query = @"Create table t1 (c1 int)
                            GO
                            Create table t2 (c1 int)
                            GO
                            CREATE PROCEDURE sp1
                                @CategoryID INT,
                                @MinPrice DECIMAL(10, 2)
                            AS
                            BEGIN
                                select * from sys.all_objects
                            END;
                            GO";
            await RunTest(databaseName, query, "testdb", async (testdbName, connection) =>
            {
                SqlCore.ObjectExplorer2.ObjectExplorer objectExplorer = new SqlCore.ObjectExplorer2.ObjectExplorer();
                var nodes = objectExplorer.getNodeByPath("/dbo/StoredProcedures/sp1/StoredProcedureParameters/", connection);
                Assert.IsNotNull(nodes);
            });
        }

        private async Task RunTest(string databaseName, string query, string testDbPrefix, Func<string, SqlConnection, Task> test)
        {
            SqlTestDb? testDb = null;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, testDbPrefix);
                if (databaseName == "#testDb#")
                {
                    databaseName = testDb.DatabaseName;
                }
                using (SqlConnection connection = new SqlConnection(testDb.ConnectionString))
                {
                    await connection.OpenAsync();
                    await test(testDb.DatabaseName, connection);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to run OE test. error:{ex.Message} {ex.StackTrace}");
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