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
using OE = Microsoft.SqlTools.SqlCore.SimpleObjectExplorer.ObjectExplorer;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectExplorer
{
    public class ObjectExplorer2Tests
    {
        string databaseName = "tempdb";

        [Test]
        public async Task ExpandingPathShouldReturnCorrectNodes()
        {
            var query = @"Create table t1 (c1 int PRIMARY KEY)
                            GO
                            Create table t2 (c1 int)
                            GO
                            CREATE INDEX t2_idx ON t2 (c1)
                            GO
                            create view v1 WITH SCHEMABINDING as select c1 from dbo.t2
                            GO
                            CREATE UNIQUE CLUSTERED INDEX v2_idx ON dbo.v1(c1)
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
                var oeRoot = await OE.GetObjectExplorerModel(connection);

                // Getting schema nodes
                var nodes = OE.GetNodeChildrenFromPath(oeRoot, "/");
                Assert.AreEqual(1, nodes.Length, "Root node should have 1 schema node");
                Assert.IsNotNull(nodes.Find(node => node.Name == "dbo"), "Root node should have dbo schema node");

                // Expand dbo schema node
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/");
                Assert.AreEqual(4, nodes.Length, "dbo schema node should have 4 folders");

                // Expand Tables folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/");
                Assert.AreEqual(2, nodes.Length, "Tables folder should have 2 tables");
                Assert.IsNotNull(nodes.Find(node => node.Name == "t1"), "Tables folder should have t1 table");
                Assert.IsNotNull(nodes.Find(node => node.Name == "t2"), "Tables folder should have t2 table");

                // Expand t1 table
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t1/");
                Assert.AreEqual(2, nodes.Length, "t1 table should have two folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Columns"), "t1 table should have Columns folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Indexes"), "t1 table should have Indexes folder");

                // Expand Columns folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t1/Columns/");
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Indexes folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t1/Indexes/");
                Assert.AreEqual(1, nodes.Length, "Indexes folder should have 1 index");

                // Expand t2 table
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t2/");
                Assert.AreEqual(2, nodes.Length, "t2 table should have two folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Columns"), "t2 table should have Columns folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Indexes"), "t2 table should have Indexes folder");

                // Expand Columns folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t2/Columns/");
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Indexes folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Tables/t2/Indexes/");
                Assert.AreEqual(1, nodes.Length, "Indexes folder should have 1 index");
                Assert.IsNotNull(nodes.Find(node => node.Name == "t2_idx"), "Indexes folder should have t2_idx index");

                // Expand Views folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Views/");
                Assert.AreEqual(1, nodes.Length, "Views folder should have 1 view");
                Assert.IsNotNull(nodes.Find(node => node.Name == "v1"), "Views folder should have v1 view");

                // Expand v1 view
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Views/v1/");
                Assert.AreEqual(2, nodes.Length, "v1 view should two folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Columns"), "v1 view should have Columns folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Indexes"), "v1 view should have Indexes folder");

                // Expand Columns folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Views/v1/Columns/");
                Assert.AreEqual(1, nodes.Length, "Columns folder should have 1 column");
                Assert.IsNotNull(nodes.Find(node => node.Name == "c1"), "Columns folder should have c1 column");

                // Expand Indexes folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Views/v1/Indexes/");
                Assert.AreEqual(1, nodes.Length, "Indexes folder should have 1 index");

                // Expand Stored Procedures folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/StoredProcedures/");
                Assert.AreEqual(1, nodes.Length, "Stored Procedures folder should have 1 stored procedure");
                Assert.IsNotNull(nodes.Find(node => node.Name == "sp1"), "Stored Procedures folder should have sp1 stored procedure");

                // Expand sp1 stored procedure
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/StoredProcedures/sp1/");
                Assert.AreEqual(1, nodes.Length, "sp1 stored procedure should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Parameters"), "sp1 stored procedure should have Parameters folder");

                // Expand Parameters folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/StoredProcedures/sp1/Parameters/");
                Assert.AreEqual(3, nodes.Length, "Parameters folder should have 3 parameters");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p1" && node.SubType == "InputParameter"), "Parameters folder should have @p1 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p2" && node.SubType == "OutputParameter"), "Parameters folder should have @p2 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@p3" && node.SubType == "OutputParameter"), "Parameters folder should have @p3 parameter");

                // Expand Functions folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/");
                Assert.AreEqual(2, nodes.Length, "Functions folder should have 2 folders");

                // Expand ScalarFunctions folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/ScalarFunctions/");
                Assert.AreEqual(1, nodes.Length, "ScalarFunctions folder should have 1 function");
                Assert.IsNotNull(nodes.Find(node => node.Name == "f1"), "ScalarFunctions folder should have f1 function");

                // Expand f1 function
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/ScalarFunctions/f1/");
                Assert.AreEqual(1, nodes.Length, "f1 function should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Parameters"), "f1 function should have Parameters folder");

                // Expand Parameters folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/ScalarFunctions/f1/Parameters/");
                Assert.AreEqual(2, nodes.Length, "Parameters folder should have 2 parameters");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@Input1" && node.SubType == "InputParameter"), "Parameters folder should have @Input1 parameter");
                Assert.IsNotNull(nodes.Find(node => node.Name == "@Input2" && node.SubType == "InputParameter"), "Parameters folder should have @Input2 parameter");

                // Expand TableValuedFunctions folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/TableValuedFunctions/");
                Assert.AreEqual(1, nodes.Length, "TableValuedFunctions folder should have 1 function");
                Assert.IsNotNull(nodes.Find(node => node.Name == "f2"), "TableValuedFunctions folder should have f2 function");

                // Expand f2 function
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/TableValuedFunctions/f2/");
                Assert.AreEqual(1, nodes.Length, "f2 function should one folder");
                Assert.IsNotNull(nodes.Find(node => node.Name == "Parameters"), "f2 function should have Parameters folder");
                
                // Expand Parameters folder
                nodes = OE.GetNodeChildrenFromPath(oeRoot, "/dbo/Functions/TableValuedFunctions/f2/Parameters/");
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