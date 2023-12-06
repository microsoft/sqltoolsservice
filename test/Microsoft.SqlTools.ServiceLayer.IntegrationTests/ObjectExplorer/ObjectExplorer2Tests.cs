//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Castle.Core.Internal;
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
                            create view v1 as select * from t1
                            GO
                            -- Create a stored procedure with input and output parameters
                            CREATE PROCEDURE sp1
                                @p1 INT,               -- Input parameter
                                @p2 NVARCHAR(100) OUTPUT, -- Output parameter
                                @p3 DECIMAL(10, 2) OUTPUT        -- Output parameter
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                SELECT @p2 = 'test', @p3 = 'test'
                                FROM sys.all_objects
                            END
                            GO
                            CREATE FUNCTION dbo.f1
                            (
                                @Input1 INT,
                                @Input2 INT
                            )
                            RETURNS INT
                            AS
                            BEGIN
                                DECLARE @Result INT;
                                SET @Result = 2
                                RETURN @Result;
                            END;
                            GO
                            CREATE FUNCTION dbo.f2
                            (
                                @p1 INT = 2
                            )
                            RETURNS TABLE
                            AS
                            RETURN
                            (
                                select * from sys.all_columns
                            );
                            GO";
            await RunTest(databaseName, query, "testdb", async (testdbName, connection) =>
            {
                SqlCore.ObjectExplorer2.ObjectExplorer objectExplorer = new SqlCore.ObjectExplorer2.ObjectExplorer();

                // Getting schema nodes
                var nodes = objectExplorer.getNodeByPath("/", connection);
                Assert.AreEqual(1, nodes.Length, "Root node should have 1 schema node");
                Assert.IsNotNull(nodes.Find(node => node.Name == "dbo"), "Root node should have dbo schema node");

                // Expand dbo schema node
                nodes = objectExplorer.getNodeByPath("/dbo/", connection);
                Assert.AreEqual(4, nodes.Length, "dbo schema node should have 4 folders");

                // Expand Tables folder
                nodes = objectExplorer.getNodeByPath("/dbo/Tables/", connection);
                Assert.AreEqual(2, nodes.Length, "Tables folder should have 2 tables");
                Assert.IsNotNull(nodes.Find(node => node.Name == "t1"), "Tables folder should have t1 table");
                Assert.IsNotNull(nodes.Find(node => node.Name == "t2"), "Tables folder should have t2 table");

                // Expand t1 table
                nodes = objectExplorer.getNodeByPath("/dbo/Tables/t1/", connection);
                Assert.AreEqual(1, nodes.Length, "t1 table should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "TableColumns"), "t1 table should have Columns folder");

                // Expand Columns folder
                nodes = objectExplorer.getNodeByPath("/dbo/Tables/t1/TableColumns/", connection);
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Views folder
                nodes = objectExplorer.getNodeByPath("/dbo/Tables/t2/", connection);
                Assert.AreEqual(1, nodes.Length, "t2 table should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "TableColumns"), "t2 table should have Columns folder");

                // Expand Columns folder
                nodes = objectExplorer.getNodeByPath("/dbo/Tables/t2/TableColumns/", connection);
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Views folder
                nodes = objectExplorer.getNodeByPath("/dbo/Views/", connection);
                Assert.AreEqual(1, nodes.Length, "Views folder should have 1 view");
                Assert.IsNotNull(nodes.Find(node => node.Name == "v1"), "Views folder should have v1 view");

                // Expand v1 view
                nodes = objectExplorer.getNodeByPath("/dbo/Views/v1/", connection);
                Assert.AreEqual(1, nodes.Length, "v1 view should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "ViewColumns"), "v1 view should have Columns folder");

                // Expand Columns folder
                nodes = objectExplorer.getNodeByPath("/dbo/Views/v1/ViewColumns/", connection);
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Stored Procedures folder
                nodes = objectExplorer.getNodeByPath("/dbo/StoredProcedures/", connection);
                Assert.AreEqual(1, nodes.Length, "Stored Procedures folder should have 1 stored procedure");
                Assert.IsNotNull(nodes.Find(node => node.Name == "sp1"), "Stored Procedures folder should have sp1 stored procedure");

                // Expand sp1 stored procedure
                nodes = objectExplorer.getNodeByPath("/dbo/StoredProcedures/sp1/", connection);
                Assert.AreEqual(1, nodes.Length, "sp1 stored procedure should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "StoredProcedureParameters"), "sp1 stored procedure should have Parameters folder");

                // Expand Parameters folder
                nodes = objectExplorer.getNodeByPath("/dbo/StoredProcedures/sp1/StoredProcedureParameters/", connection);
                Assert.AreEqual(3, nodes.Length, "Parameters folder should have 3 parameters");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p1" && node.SubType == "InputParameter"), "Parameters folder should have @p1 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p2" && node.SubType == "OutputParameter"), "Parameters folder should have @p2 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p3" && node.SubType == "OutputParameter"), "Parameters folder should have @p3 parameter");

                // Expand Functions folder
                nodes = objectExplorer.getNodeByPath("/dbo/Functions/", connection);
                Assert.AreEqual(2, nodes.Length, "Functions folder should have 2 folders");

                nodes = objectExplorer.getNodeByPath("/dbo/Functions/ScalarFunctions/", connection);
                Assert.AreEqual(1, nodes.Length, "ScalarFunctions folder should have 1 function");
                Assert.IsNotNull(nodes.Find(node => node.Name == "f1"), "ScalarFunctions folder should have f1 function");

                // Expand f1 function
                nodes = objectExplorer.getNodeByPath("/dbo/Functions/ScalarFunctions/f1/", connection);
                Assert.AreEqual(1, nodes.Length, "f1 function should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "ScalarFunctionParameters"), "f1 function should have Parameters folder");

                // Expand Parameters folder
                nodes = objectExplorer.getNodeByPath("/dbo/Functions/ScalarFunctions/f1/ScalarFunctionParameters/", connection);
                Assert.AreEqual(2, nodes.Length, "Parameters folder should have 2 parameters");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@Input1" && node.SubType == "InputParameter"), "Parameters folder should have @Input1 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@Input2" && node.SubType == "InputParameter"), "Parameters folder should have @Input2 parameter");

                nodes = objectExplorer.getNodeByPath("/dbo/Functions/TableValuedFunctions/", connection);
                Assert.AreEqual(1, nodes.Length, "TableValuedFunctions folder should have 1 function");
                Assert.IsNotNull(nodes.Find(node => node.Name == "f2"), "TableValuedFunctions folder should have f2 function");

                // Expand f2 function
                nodes = objectExplorer.getNodeByPath("/dbo/Functions/TableValuedFunctions/f2/", connection);
                Assert.AreEqual(1, nodes.Length, "f2 function should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "TableValuedFunctionParameters"), "f2 function should have Parameters folder");
                
                // Expand Parameters folder
                nodes = objectExplorer.getNodeByPath("/dbo/Functions/TableValuedFunctions/f2/TableValuedFunctionParameters/", connection);
                Assert.AreEqual(1, nodes.Length, "Parameters folder should have 1 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p1" && node.SubType == "InputParameter" ), "Parameters folder should have @p1 parameter");
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