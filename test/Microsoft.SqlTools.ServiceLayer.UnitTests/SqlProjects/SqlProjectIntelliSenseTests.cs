//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects
{
    /// <summary>
    /// Tests for SQL Project IntelliSense core functionality
    /// </summary>
    public class SqlProjectIntelliSenseTests
    {
        [Test]
        public void TestCreateMetadataProviderFromSqlProject()
        {
            // Arrange: Create a test SQL project with some tables and stored procedures
            string projectPath = ProjectUtils.CreateTestProject("TestMetadataProject");
            var project = SqlProject.Open(projectPath);

            // Add a table script
            string tableScript = @"
CREATE TABLE dbo.Customers (
    CustomerId INT PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL
);
";
            string tablePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Tables", "Customers.sql");
            Directory.CreateDirectory(Path.GetDirectoryName(tablePath)!);
            File.WriteAllText(tablePath, tableScript);
            project.SqlObjectScripts.Add(new SqlObjectScript(tablePath));

            // Add a stored procedure script
            string spScript = @"
CREATE PROCEDURE dbo.GetCustomer
    @CustomerId INT
AS
BEGIN
    SELECT * FROM dbo.Customers WHERE CustomerId = @CustomerId;
END
";
            string spPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "StoredProcedures", "GetCustomer.sql");
            Directory.CreateDirectory(Path.GetDirectoryName(spPath)!);
            File.WriteAllText(spPath, spScript);
            project.SqlObjectScripts.Add(new SqlObjectScript(spPath));

            // Act: Build TSqlModel and create MetadataProvider
            var model = TSqlModelBuilder.LoadModel(project);
            var metadataProvider = new LazySchemaModelMetadataProvider(model, "TestDatabase");

            // Assert: Verify that the MetadataProvider contains our objects
            Assert.IsNotNull(metadataProvider, "MetadataProvider should not be null");
            
            // Get the server and database from the provider
            var server = metadataProvider.Server;
            Assert.IsNotNull(server, "Server should not be null");
            
            var database = server.Databases.FirstOrDefault();
            Assert.IsNotNull(database, "Database should not be null");
            Assert.AreEqual("TestDatabase", database.Name, "Database name should match");

            // Get the dbo schema
            var dboSchema = database.Schemas.FirstOrDefault(s => s.Name == "dbo");
            Assert.IsNotNull(dboSchema, "dbo schema should exist");

            // Verify table exists (lazy loaded)
            var tables = dboSchema.Tables;
            Assert.IsNotNull(tables, "Tables collection should not be null");
            var customersTable = tables.FirstOrDefault(t => t.Name == "Customers");
            Assert.IsNotNull(customersTable, "Customers table should exist in metadata");

            // Verify stored procedure exists (lazy loaded)
            var procedures = dboSchema.StoredProcedures;
            Assert.IsNotNull(procedures, "Stored procedures collection should not be null");
            var getCustomerProc = procedures.FirstOrDefault(p => p.Name == "GetCustomer");
            Assert.IsNotNull(getCustomerProc, "GetCustomer procedure should exist in metadata");

            // Cleanup
            model.Dispose();
            ProjectUtils.DeleteTestProject(projectPath);
        }
    }
}
