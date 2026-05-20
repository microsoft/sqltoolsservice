//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.IntelliSense
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
            // Use unique project name per test run to avoid cross-test interference
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            // Add a table script
            string tableScript = @"
CREATE TABLE dbo.Customers (
    CustomerId INT PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL
);
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "Customers.sql")), tableScript);

            // Add a stored procedure script
            string spScript = @"
CREATE PROCEDURE dbo.GetCustomer
    @CustomerId INT
AS
BEGIN
    SELECT * FROM dbo.Customers WHERE CustomerId = @CustomerId;
END
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("StoredProcedures", "GetCustomer.sql")), spScript);

            // Debug: Verify scripts were added
            Assert.AreEqual(2, project.SqlObjectScripts.Count, "Should have 2 scripts in project (table, sproc)");

            TSqlModel? model = null;
            try
            {
                // Act: Build TSqlModel and create MetadataProvider
                model = TSqlModelBuilder.LoadModel(project);
                
                // Debug: Verify model has objects
                var allObjects = model.GetObjects(DacQueryScopes.All).ToList();
                Assert.Greater(allObjects.Count, 0, $"Model should have objects. Project directory: {project.DirectoryPath}");
                
                var metadataProvider = new TSqlModelMetadataProvider(model, "TestDatabase");

                // Assert: Verify that the MetadataProvider contains our objects
                Assert.IsNotNull(metadataProvider, "MetadataProvider should not be null");
                
                // Get the server and database from the provider
                var server = metadataProvider.Server;
                Assert.IsNotNull(server, "Server should not be null");
                
                var database = server.Databases.FirstOrDefault();
                Assert.IsNotNull(database, "Database should not be null");
                Assert.AreEqual("TestDatabase", database!.Name, "Database name should match");

                // Debug: Check what schemas exist
                var allSchemas = database.Schemas.ToList();
                var schemaNames = string.Join(", ", allSchemas.Select(s => $"{s.Name} (System={s.IsSystemObject})"));
                Assert.Greater(allSchemas.Count, 0, $"Should have schemas. Found: {schemaNames}");

                // Get the dbo schema
                var dboSchema = database.Schemas.FirstOrDefault(s => s.Name == "dbo");
                Assert.IsNotNull(dboSchema, $"dbo schema should exist. Available schemas: {schemaNames}");

                // Verify table exists (lazy loaded)
                var tables = dboSchema!.Tables;
                Assert.IsNotNull(tables, "Tables collection should not be null");
                
                // Force lazy evaluation and check what tables exist
                var allTables = tables.ToList();
                var tableNames = string.Join(", ", allTables.Select(t => t.Name));
                Assert.Greater(allTables.Count, 0, $"Should have tables. Found: {tableNames}");
                
                var customersTable = tables.FirstOrDefault(t => t.Name == "Customers");
                Assert.IsNotNull(customersTable, $"Customers table should exist in metadata. Available tables: {tableNames}");

                // Verify stored procedure exists (lazy loaded)
                var procedures = dboSchema.StoredProcedures;
                Assert.IsNotNull(procedures, "Stored procedures collection should not be null");
                var getCustomerProc = procedures.FirstOrDefault(p => p.Name == "GetCustomer");
                Assert.IsNotNull(getCustomerProc, "GetCustomer procedure should exist in metadata");
            }
            finally
            {
                // Cleanup: Always dispose and delete temp project, even if assertions fail
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        /// <summary>
        /// When two .sql files in a project both define the same object (e.g. dbo.Foo),
        /// IsDuplicate should return true at construction time.
        /// When one of the files is updated to remove the definition, IsDuplicate should return false.
        /// </summary>
        [Test]
        public void IsDuplicate_ReturnsTrueForObjectDefinedInTwoFiles_AndFalseAfterOneRemoved()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string fileA = "Tables\\FooA.sql";
            const string fileB = "Tables\\FooB.sql";
            const string tableScript = "CREATE TABLE dbo.Foo (Id INT PRIMARY KEY);";
            const string unrelatedScript = "CREATE TABLE dbo.Bar (Id INT PRIMARY KEY);";

            project.SqlObjectScripts.Add(new SqlObjectScript(fileA), tableScript);
            project.SqlObjectScripts.Add(new SqlObjectScript(fileB), tableScript);   // same object, second file

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");

                // Both files define dbo.Foo → should be a duplicate.
                Assert.IsTrue(provider.IsDuplicate("dbo.Foo"),
                    "dbo.Foo is defined in two files and should be reported as duplicate");

                // Bare name fallback: binder may emit just 'Foo' when DDL has no schema qualifier.
                Assert.IsTrue(provider.IsDuplicate("Foo"),
                    "Bare name 'Foo' should resolve to dbo.Foo and still be reported as duplicate");

                // dbo.Bar is only in one file → not a duplicate.
                Assert.IsFalse(provider.IsDuplicate("dbo.Bar"),
                    "dbo.Bar is defined in only one file and should not be a duplicate");

                // Simulate saving fileB so it no longer defines dbo.Foo (replaced with unrelated content).
                string sourceNameB = Path.Combine(project.DirectoryPath, fileB.Replace('\\', Path.DirectorySeparatorChar));
                model.AddOrUpdateObjects(unrelatedScript, sourceNameB, new TSqlObjectOptions());
                provider.UpdateForFileChange(sourceNameB, deleted: false);

                // After the update, dbo.Foo is only in fileA → no longer a duplicate.
                Assert.IsFalse(provider.IsDuplicate("dbo.Foo"),
                    "After removing dbo.Foo from fileB, it is in only one file and must not be a duplicate");
                Assert.IsFalse(provider.IsDuplicate("Foo"),
                    "Bare name 'Foo' must also be non-duplicate after the update");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }
    }
}
