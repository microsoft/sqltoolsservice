//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.Utility;
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

            string fileA = Path.Combine("Tables", "FooA.sql");
            string fileB = Path.Combine("Tables", "FooB.sql");
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
                string sourceNameB = Path.Combine(project.DirectoryPath, fileB);
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

        /// <summary>
        /// When a single .sql file defines the same object twice,
        /// IsDuplicate should still report the object as duplicated.
        /// </summary>
        [Test]
        public void IsDuplicate_ReturnsTrueForObjectDefinedTwiceInSameFile()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            string fileA = Path.Combine("Tables", "Foo.sql");
            const string duplicateInSingleFileScript = @"
CREATE TABLE dbo.Foo (Id INT PRIMARY KEY);
GO
CREATE TABLE dbo.Foo (Id INT PRIMARY KEY);
";

            project.SqlObjectScripts.Add(new SqlObjectScript(fileA), duplicateInSingleFileScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");

                Assert.IsTrue(provider.IsDuplicate("dbo.Foo"),
                    "dbo.Foo is defined twice in one file and should be reported as duplicate");

                Assert.IsTrue(provider.IsDuplicate("Foo"),
                    "Bare name 'Foo' should resolve to dbo.Foo and be reported as duplicate");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        /// <summary>
        /// When a base table gains a new column, all dependent views (SELECT *) must reflect
        /// that column after <see cref="TSqlModelMetadataProvider.UpdateForFileChange"/> is called.
        /// Covers both direct dependents (View1 → FileTable1) and chained dependents
        /// (View2 → View1 → FileTable1) to verify the BFS traversal in Step 2b.
        /// </summary>
        [Test]
        public void UpdateForFileChange_ResetsTransitiveDependents_AfterTableColumnAdded()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            string tablesFile = Path.Combine("Tables", "FileTable1.sql");
            string viewsFile  = Path.Combine("Views",  "View1.sql");
            string views2File = Path.Combine("Views",  "View2.sql");

            // Initial table: 2 columns (Id, Name).
            const string tableScriptV1 = @"
CREATE TABLE [sss].[FileTable1] (
    [Id]   INT           NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL
);";

            // Both views use SELECT * so column count tracks the underlying table.
            const string viewScript1 = @"
CREATE VIEW [sss].[View1] AS
SELECT * FROM [sss].[FileTable1];";

            const string viewScript2 = @"
CREATE VIEW [sss].[View2] AS
SELECT * FROM [sss].[View1];";

            project.SqlObjectScripts.Add(new SqlObjectScript(tablesFile), tableScriptV1);
            project.SqlObjectScripts.Add(new SqlObjectScript(viewsFile),  viewScript1);
            project.SqlObjectScripts.Add(new SqlObjectScript(views2File), viewScript2);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");

                var sssSchema = provider.Server.Databases.First()
                                        .Schemas.FirstOrDefault(s => s.Name == "sss");
                Assert.IsNotNull(sssSchema, "Schema 'sss' should exist");

                // Before update: each view should expose exactly 2 columns (Id, Name).
                Assert.AreEqual(2, sssSchema!.Views.FirstOrDefault(v => v.Name == "View1")?.Columns.Count,
                    "View1 should have 2 columns before table update");
                Assert.AreEqual(2, sssSchema.Views.FirstOrDefault(v => v.Name == "View2")?.Columns.Count,
                    "View2 should have 2 columns before table update");

                // Add the Email column to FileTable1 and push the update into the model.
                const string tableScriptV2 = @"
CREATE TABLE [sss].[FileTable1] (
    [Id]    INT           NOT NULL PRIMARY KEY,
    [Name]  NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL
);";
                string tablesSourceName = Path.Combine(project.DirectoryPath, tablesFile);

                model.AddOrUpdateObjects(tableScriptV2, tablesSourceName, new TSqlObjectOptions());
                provider.UpdateForFileChange(tablesSourceName, deleted: false);

                // After update: both views should now expose 3 columns including Email.
                // The lazy wrappers were reset by the BFS in UpdateForFileChange, so the
                // next access triggers FetchFromModel and re-reads from the updated model.
                Assert.AreEqual(3, sssSchema.Views.FirstOrDefault(v => v.Name == "View1")?.Columns.Count,
                    "View1 (direct dependent) should expose 3 columns after table update");
                Assert.AreEqual(3, sssSchema.Views.FirstOrDefault(v => v.Name == "View2")?.Columns.Count,
                    "View2 (chained dependent via View1) should expose 3 columns after table update");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        /// <summary>
        /// A cross-database SqlProjectReference configured with a DatabaseSqlCmdVariable should
        /// resolve the referenced project's objects under its bracketed $(Name) alias.
        /// </summary>
        [Test]
        public void CrossProjectReference_ResolvesReferencedDatabase_BySqlCmdVariable()
        {
            string projectBPath = ProjectUtils.CreateTestProject("ProjectB_" + System.Guid.NewGuid().ToString("N"));
            string projectAPath = ProjectUtils.CreateTestProject("ProjectA_" + System.Guid.NewGuid().ToString("N"));

            var projectB = SqlProject.OpenProject(projectBPath);
            projectB.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "SomeTable.sql")),
                "CREATE TABLE dbo.SomeTable (Id INT PRIMARY KEY);");

            var projectA = SqlProject.OpenProject(projectAPath);
            projectA.SqlCmdVariables.Add(new SqlCmdVariable("ProjectB", "ProjectB"));
            var databaseVariable = projectA.SqlCmdVariables.Get("ProjectB");
            var reference = new SqlProjectReference(
                projectBPath, System.Guid.NewGuid().ToString("B"), suppressMissingDependencies: false,
                databaseSqlCmdVariable: databaseVariable, serverSqlCmdVariable: null);
            projectA.DatabaseReferences.Add(reference);

            TSqlModel? modelA = null;
            TSqlModel? modelB = null;
            try
            {
                modelA = TSqlModelBuilder.LoadModel(projectA);
                modelB = TSqlModelBuilder.LoadModel(projectB);

                var providerA = new TSqlModelMetadataProvider(modelA, "ProjectA");

                Assert.AreEqual(1, providerA.Server.Databases.ToList().Count,
                    "Only the project's own database should be present before registering references");

                var aliases = SqlProjectsService.GetReferenceDatabaseAliases(reference).ToList();
                CollectionAssert.AreEqual(new[] { "$(ProjectB)" }, aliases,
                    "A SqlCmdVariable reference should yield only the bracketed form");
                foreach (string alias in aliases)
                    providerA.AddReferencedDatabase(modelB, alias);

                var databases = providerA.Server.Databases.ToList();
                Assert.AreEqual(2, databases.Count, "Should expose ProjectA's database plus the $(ProjectB) alias");

                foreach (string aliasName in aliases)
                {
                    var referencedDb = databases.FirstOrDefault(d => d.Name == aliasName);
                    Assert.IsNotNull(referencedDb, $"Database alias '{aliasName}' should be registered");

                    var dboSchema = referencedDb!.Schemas.FirstOrDefault(s => s.Name == "dbo");
                    Assert.IsNotNull(dboSchema, $"dbo schema should be visible under alias '{aliasName}'");

                    Assert.IsNotNull(dboSchema!.Tables.FirstOrDefault(t => t.Name == "SomeTable"),
                        $"SomeTable should resolve under alias '{aliasName}'");
                }
            }
            finally
            {
                modelA?.Dispose();
                modelB?.Dispose();
                ProjectUtils.DeleteTestProject(projectAPath);
                ProjectUtils.DeleteTestProject(projectBPath);
            }
        }

        /// <summary>
        /// A DatabaseSqlCmdVariable reference yields only the bracketed "$(Name)" alias, never a
        /// resolved literal, even when "Value" is set to an unevaluated MSBuild property
        /// placeholder (the shape SDK-style projects generate for it).
        /// </summary>
        [Test]
        public void GetReferenceDatabaseAliases_VariableReference_YieldsOnlyBracketedForm()
        {
            var databaseVariable = new SqlCmdVariable("ProjectB", defaultValue: "ProjectB", value: "$(SqlCmdVar__1)");
            var reference = new SqlProjectReference(
                "..\\ProjectB\\ProjectB.sqlproj", System.Guid.NewGuid().ToString("B"),
                suppressMissingDependencies: false,
                databaseSqlCmdVariable: databaseVariable, serverSqlCmdVariable: null);

            var aliases = SqlProjectsService.GetReferenceDatabaseAliases(reference).ToList();

            CollectionAssert.AreEqual(new[] { "$(ProjectB)" }, aliases);
        }

        /// <summary>
        /// A literal database name reference yields that name as-is.
        /// </summary>
        [Test]
        public void GetReferenceDatabaseAliases_LiteralReference_YieldsLiteralName()
        {
            var reference = new SqlProjectReference(
                "..\\ProjectB\\ProjectB.sqlproj", System.Guid.NewGuid().ToString("B"),
                suppressMissingDependencies: false, databaseVariableLiteralName: "ProjectB");

            var aliases = SqlProjectsService.GetReferenceDatabaseAliases(reference).ToList();

            CollectionAssert.AreEqual(new[] { "ProjectB" }, aliases);
        }

        /// <summary>
        /// An alias that collides with the primary database's own name should not be registered.
        /// Registering the same alias twice should replace the earlier entry, not duplicate it.
        /// </summary>
        [Test]
        public void AddReferencedDatabase_AvoidsPrimaryCollisionAndDuplicateAliases()
        {
            string projectAPath = ProjectUtils.CreateTestProject("DedupProjectA_" + System.Guid.NewGuid().ToString("N"));
            string projectBPath = ProjectUtils.CreateTestProject("DedupProjectB_" + System.Guid.NewGuid().ToString("N"));
            string projectCPath = ProjectUtils.CreateTestProject("DedupProjectC_" + System.Guid.NewGuid().ToString("N"));

            var projectA = SqlProject.OpenProject(projectAPath);
            var projectB = SqlProject.OpenProject(projectBPath);
            projectB.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "TableFromB.sql")),
                "CREATE TABLE dbo.TableFromB (Id INT PRIMARY KEY);");
            var projectC = SqlProject.OpenProject(projectCPath);
            projectC.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "TableFromC.sql")),
                "CREATE TABLE dbo.TableFromC (Id INT PRIMARY KEY);");

            TSqlModel? modelA = null;
            TSqlModel? modelB = null;
            TSqlModel? modelC = null;
            try
            {
                modelA = TSqlModelBuilder.LoadModel(projectA);
                modelB = TSqlModelBuilder.LoadModel(projectB);
                modelC = TSqlModelBuilder.LoadModel(projectC);

                var provider = new TSqlModelMetadataProvider(modelA, "ProjectA");

                provider.AddReferencedDatabase(modelB, "ProjectA");
                Assert.AreEqual(1, provider.Server.Databases.ToList().Count,
                    "A reference alias matching the primary database's name should not be registered");

                provider.AddReferencedDatabase(modelB, "Shared");
                provider.AddReferencedDatabase(modelC, "Shared");

                var databases = provider.Server.Databases.ToList();
                Assert.AreEqual(2, databases.Count, "A repeated alias should replace, not duplicate");

                var sharedDb = databases.FirstOrDefault(d => d.Name == "Shared");
                Assert.IsNotNull(sharedDb, "The 'Shared' alias should be registered");

                var sharedSchema = sharedDb!.Schemas.FirstOrDefault(s => s.Name == "dbo");
                Assert.IsNotNull(sharedSchema!.Tables.FirstOrDefault(t => t.Name == "TableFromC"),
                    "The later registration should win over the earlier one under the same alias");
                Assert.IsNull(sharedSchema.Tables.FirstOrDefault(t => t.Name == "TableFromB"),
                    "The earlier registration should no longer be reachable under the shared alias");

                Assert.IsTrue(provider.TryGetReferencedSourceInformation("Shared", "dbo.TableFromC", out _),
                    "Source-location lookup should also reflect the later registration");
                Assert.IsFalse(provider.TryGetReferencedSourceInformation("Shared", "dbo.TableFromB", out _),
                    "Source-location lookup should not still point at the replaced registration");
            }
            finally
            {
                modelA?.Dispose();
                modelB?.Dispose();
                modelC?.Dispose();
                ProjectUtils.DeleteTestProject(projectAPath);
                ProjectUtils.DeleteTestProject(projectBPath);
                ProjectUtils.DeleteTestProject(projectCPath);
            }
        }

        /// <summary>
        /// Adding a project reference to an already-open project should refresh its live
        /// IntelliSense so the reference resolves immediately, without closing and reopening.
        /// </summary>
        [Test]
        public async Task AddSqlProjectReference_ToOpenProject_RefreshesLiveIntelliSense()
        {
            string projectBPath = ProjectUtils.CreateTestProject("RefreshProjectB_" + Guid.NewGuid().ToString("N"));
            var projectB = SqlProject.OpenProject(projectBPath);
            projectB.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "SomeTable.sql")),
                "CREATE TABLE dbo.SomeTable (Id INT PRIMARY KEY);");

            var service = new SqlProjectsService();
            string projectAPath = ProjectUtils.CreateTestProject("RefreshProjectA_" + Guid.NewGuid().ToString("N"));
            // SqlProjectsService's "ProjectUri" is a plain filesystem path (GetProject passes it
            // straight to SqlProject.OpenProject), unlike TSqlLanguageService's real file:// URIs.
            string projectUri = projectAPath;

            try
            {
                var openRequest = new MockRequest<ResultStatus>();
                await service.HandleOpenSqlProjectRequest(new SqlProjectParams { ProjectUri = projectUri }, openRequest.Object);
                await WaitUntilAsync(() => service.TryGetProvider(projectUri, out _));

                service.Projects[projectUri].SqlCmdVariables.Add(new SqlCmdVariable("ProjectB", "ProjectB"));

                service.TryGetProvider(projectUri, out var beforeProvider);
                Assert.AreEqual(1, beforeProvider!.Server.Databases.ToList().Count,
                    "Only the project's own database should be present before adding the reference");

                var addRequest = new MockRequest<ResultStatus>();
                await service.HandleAddSqlProjectReferenceRequest(new AddSqlProjectReferenceParams
                {
                    ProjectUri = projectUri,
                    ProjectPath = projectBPath,
                    SuppressMissingDependencies = false,
                    DatabaseVariable = "ProjectB"
                }, addRequest.Object);
                addRequest.AssertSuccess(nameof(service.HandleAddSqlProjectReferenceRequest));

                await WaitUntilAsync(() =>
                    service.TryGetProvider(projectUri, out var p) && p!.Server.Databases.ToList().Count > 1);

                service.TryGetProvider(projectUri, out var afterProvider);
                var referencedDb = afterProvider!.Server.Databases.ToList().FirstOrDefault(d => d.Name == "$(ProjectB)");
                Assert.IsNotNull(referencedDb, "The newly added reference should be resolvable without reopening the project");

                var someTable = referencedDb!.Schemas.FirstOrDefault(s => s.Name == "dbo")?.Tables.FirstOrDefault(t => t.Name == "SomeTable");
                Assert.IsNotNull(someTable, "SomeTable should resolve through the newly added reference");
            }
            finally
            {
                var closeRequest = new MockRequest<ResultStatus>();
                await service.HandleCloseSqlProjectRequest(new SqlProjectParams { ProjectUri = projectUri }, closeRequest.Object);
                closeRequest.AssertSuccess(nameof(service.HandleCloseSqlProjectRequest));
                ProjectUtils.DeleteTestProject(projectAPath);
                ProjectUtils.DeleteTestProject(projectBPath);
            }
        }

        /// <summary>
        /// Adding a reference while a project's initial IntelliSense build is still in flight must
        /// still trigger a rebuild that reflects it.
        /// </summary>
        [Test]
        public async Task AddSqlProjectReference_WhileInitialBuildInFlight_StillRefreshesLiveIntelliSense()
        {
            string projectBPath = ProjectUtils.CreateTestProject("RaceProjectB_" + Guid.NewGuid().ToString("N"));
            var projectB = SqlProject.OpenProject(projectBPath);
            projectB.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "SomeTable.sql")),
                "CREATE TABLE dbo.SomeTable (Id INT PRIMARY KEY);");

            var service = new SqlProjectsService();
            string projectAPath = ProjectUtils.CreateTestProject("RaceProjectA_" + Guid.NewGuid().ToString("N"));
            string projectUri = projectAPath;

            try
            {
                var openRequest = new MockRequest<ResultStatus>();
                await service.HandleOpenSqlProjectRequest(new SqlProjectParams { ProjectUri = projectUri }, openRequest.Object);

                // Add the reference before the initial build finishes, to hit the race.
                service.Projects[projectUri].SqlCmdVariables.Add(new SqlCmdVariable("ProjectB", "ProjectB"));

                var addRequest = new MockRequest<ResultStatus>();
                await service.HandleAddSqlProjectReferenceRequest(new AddSqlProjectReferenceParams
                {
                    ProjectUri = projectUri,
                    ProjectPath = projectBPath,
                    SuppressMissingDependencies = false,
                    DatabaseVariable = "ProjectB"
                }, addRequest.Object);
                addRequest.AssertSuccess(nameof(service.HandleAddSqlProjectReferenceRequest));

                await WaitUntilAsync(() =>
                    service.TryGetProvider(projectUri, out var p) && p!.Server.Databases.ToList().Count > 1);

                service.TryGetProvider(projectUri, out var provider);
                var referencedDb = provider!.Server.Databases.ToList().FirstOrDefault(d => d.Name == "$(ProjectB)");
                Assert.IsNotNull(referencedDb, "The reference added during the initial build should still resolve");

                var someTable = referencedDb!.Schemas.FirstOrDefault(s => s.Name == "dbo")?.Tables.FirstOrDefault(t => t.Name == "SomeTable");
                Assert.IsNotNull(someTable, "SomeTable should resolve through the reference added during the race");
            }
            finally
            {
                var closeRequest = new MockRequest<ResultStatus>();
                await service.HandleCloseSqlProjectRequest(new SqlProjectParams { ProjectUri = projectUri }, closeRequest.Object);
                closeRequest.AssertSuccess(nameof(service.HandleCloseSqlProjectRequest));
                ProjectUtils.DeleteTestProject(projectAPath);
                ProjectUtils.DeleteTestProject(projectBPath);
            }
        }

        /// <summary>
        /// Polls <paramref name="condition"/> until it's true or a 5-second timeout elapses, for
        /// asserting on the result of a fire-and-forget background IntelliSense build.
        /// </summary>
        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    Assert.Fail("Timed out waiting for the background IntelliSense build to complete");
                await Task.Delay(20);
            }
        }

        /// <summary>
        /// Verifies that TSqlModelTable.Indexes is populated with IRelationalIndex entries for
        /// PRIMARY KEY and UNIQUE constraints so the SqlParser binder can validate FOREIGN KEY
        /// references without firing a false "no primary or candidate keys" error.
        ///
        /// Covers SqlForeignKeyConstraint.FindPrimaryKey (checks IndexKey.Type == PrimaryKey)
        /// and FindReferencedKey (checks IsUnique + IndexedColumns by name).
        /// </summary>
        [Test]
        public void TableIndexes_ExposePrimaryKeyAndUniqueConstraints_ForFkBinderValidation()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            // Orders table: PK on OrderId, UNIQUE on OrderNumber
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "Orders.sql")), @"
CREATE TABLE dbo.Orders (
    OrderId     INT          NOT NULL,
    OrderNumber NVARCHAR(20) NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY (OrderId),
    CONSTRAINT UQ_Orders_Number UNIQUE (OrderNumber)
);");

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");

                var dbo = provider.Server.Databases.First().Schemas.First(s => s.Name == "dbo");
                var ordersTable = dbo.Tables.First(t => t.Name == "Orders") as ITable;
                Assert.IsNotNull(ordersTable, "Orders table should exist");

                var indexes = ordersTable!.Indexes.ToList();
                Assert.AreEqual(2, indexes.Count, "Should expose 2 indexes (PK + UNIQUE)");

                // FindPrimaryKey: needs IndexKey.Type == PrimaryKey
                var pkIndex = indexes.OfType<IRelationalIndex>()
                                     .FirstOrDefault(i => i.IndexKey?.Type == ConstraintType.PrimaryKey);
                Assert.IsNotNull(pkIndex, "Should have a PrimaryKey index entry for the binder's FindPrimaryKey");
                Assert.IsTrue(pkIndex!.IsUnique, "PK index must be unique");
                var pkCols = pkIndex.IndexedColumns.ToList();
                Assert.AreEqual(1, pkCols.Count, "PK index should have 1 key column");
                Assert.AreEqual("OrderId", pkCols[0].Name, "PK indexed column name should be OrderId");
                Assert.IsFalse(pkCols[0].IsIncluded, "PK column must not be an INCLUDE column");

                // FindReferencedKey: needs IsUnique + IndexedColumns matching by name
                var uqIndex = indexes.OfType<IRelationalIndex>()
                                     .FirstOrDefault(i => i.IndexKey?.Type == ConstraintType.Unique);
                Assert.IsNotNull(uqIndex, "Should have a Unique index entry for the binder's FindReferencedKey");
                Assert.IsTrue(uqIndex!.IsUnique, "UNIQUE index must be unique");
                var uqCols = uqIndex.IndexedColumns.ToList();
                Assert.AreEqual(1, uqCols.Count, "UNIQUE index should have 1 key column");
                Assert.AreEqual("OrderNumber", uqCols[0].Name, "UNIQUE indexed column name should be OrderNumber");
                Assert.IsFalse(uqCols[0].IsIncluded, "UNIQUE column must not be an INCLUDE column");

                // Name-based lookup — used by FindReferencedKey to match FK columns
                Assert.IsNotNull(pkIndex.IndexedColumns["OrderId"], "PK IndexedColumns must support name lookup");
                Assert.IsNotNull(uqIndex.IndexedColumns["OrderNumber"], "UNIQUE IndexedColumns must support name lookup");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }
    }
}
