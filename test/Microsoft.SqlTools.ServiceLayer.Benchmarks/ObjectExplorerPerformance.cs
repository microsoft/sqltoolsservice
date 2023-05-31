//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Extensions;

namespace Microsoft.SqlTools.ServiceLayer.Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectExplorerPerformance
    {
        private ObjectExplorerService _service;
        private SqlConnectionStringBuilder _builder = new SqlConnectionStringBuilder();
        private int _tableCount = 10;

        /// <summary>
        /// Creates a set of test tables in the Azure SQL Database before each test scenario.
        /// This is to ensure that the database is in a known state before each test.
        /// </summary>
        /// <param name="tableCount">The number of test tables to create</param>
        [GlobalSetup]
        public void SetupTestTables()
        {
            try
            {
                Console.WriteLine("*** Creating Test Tables ***");

                using (var connection = new SqlConnection(_builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("*** Connected to Azure SQL Database ***");

                    for (var tableNum = 1; tableNum <= _tableCount; ++tableNum)
                    {
                        var sql = $"CREATE TABLE [dbo].[TestTable{tableNum}] ( [Id] INT NOT NULL PRIMARY KEY, [Name] NVARCHAR(50) NOT NULL )";
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine($"*** Created TestTable{tableNum} ***");
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Drops the test tables in the Azure SQL Database after each test scenario.
        /// This is to ensure that the database is in a known state before each test.
        /// </summary>
        /// <param name="tableCount">The number of test tables to drop</param>
        [GlobalCleanup]
        public void TearDownTestTables()
        {
            try
            {
                Console.WriteLine("*** Dropping Test Tables ***");

                using (var connection = new SqlConnection(_builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("*** Connected to Azure SQL Database ***");

                    for (var tableNum = 1; tableNum <= _tableCount; ++tableNum)
                    {
                        var sql = $"DROP TABLE TestTable{tableNum}";
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine($"*** Dropped TestTable{tableNum} ***");
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        [Benchmark]
        public async Task RunBenchmark()
        {
            var query = "";
            string databaseName = "tempdb";
            await RunTest(databaseName, query, "TepmDb", async (testDbName, session) =>
            {
                // var children = (await _service.ExpandNode(session, session.Root.GetNodePath())).Nodes;
            });
        }

        /// <summary>
        /// Provides the necessary parameters like the database to target along with creating the object explorer session the benchmark will need.
        /// </summary>
        private async Task RunTest(string databaseName, string query, string testDbPrefix, Func<string, ObjectExplorerSession, Task> test)
        {
            SqlTestDb testDb = null;
            string uri = string.Empty;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.Azure, false, null, query, testDbPrefix);
                if (databaseName == "#testDb#")
                {
                    databaseName = testDb.DatabaseName;
                }

                var session = await CreateSession(databaseName);
                uri = session.Uri;
                await test(testDb.DatabaseName, session);
            }
            catch (Exception ex)
            {
                string msg = ex.BuildRecursiveErrorMessage();
                throw new Exception($"Failed to run OE test. uri:{uri} error:{msg} {ex.StackTrace}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(uri))
                {
                    CloseSession(uri);
                }
                if (testDb != null)
                {
                    await testDb.CleanupAsync();
                }
            }
        }
    }
}
