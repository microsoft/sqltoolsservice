//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using BenchmarkDotNet.Attributes;

namespace Microsoft.SqlTools.ServiceLayer.Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectExplorerPerformance
    {
        private SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

        /// <summary>
        /// Creates a set of test tables in the Azure SQL Database before each test scenario.
        /// This is to ensure that the database is in a known state before each test.
        /// </summary>
        /// <param name="tableCount">The number of test tables to create</param>
        [GlobalSetup]
        public void SetupTestTables(int tableCount)
        {
            try
            {
                Console.WriteLine("*** Creating Test Tables ***");

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("*** Connected to Azure SQL Database ***");

                    for (var tableNum = 1; tableNum <= tableCount; ++tableNum)
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
        public void TearDownTestTables(int tableCount)
        {
            try
            {
                Console.WriteLine("*** Dropping Test Tables ***");

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("*** Connected to Azure SQL Database ***");

                    for (var tableNum = 1; tableNum <= tableCount; ++tableNum)
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

        // [Benchmark]
        // public void RunBenchmark()
        // {

        // }
    }
}
