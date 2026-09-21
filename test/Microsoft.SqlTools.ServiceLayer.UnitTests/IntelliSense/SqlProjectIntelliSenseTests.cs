//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;
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

        /// <summary>
        /// The SqlParser binder copies a table's indexes into a name-keyed SortedList when it
        /// duplicates the table (TableViewBase's constructor). Empty or colliding names make that
        /// copy throw, which aborts binding and silently disables IntelliSense for the whole file.
        /// </summary>
        [Test]
        public void TableWithPrimaryKeyAndUniqueConstraint_HasDistinctNonEmptyKeyNames()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string tableScript = @"
CREATE TABLE dbo.DimCustomer (
    DimKey INT IDENTITY (1, 1) NOT NULL,
    BusinessKey VARCHAR (64) NOT NULL,
    ValidFrom DATETIME2 (3) NOT NULL,
    CONSTRAINT PK_DimCustomer PRIMARY KEY CLUSTERED (DimKey ASC),
    CONSTRAINT UQ_DimCustomer_BK UNIQUE NONCLUSTERED (BusinessKey ASC, ValidFrom ASC)
);
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "DimCustomer.sql")), tableScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");
                var table = provider.Server.Databases.First()
                                    .Schemas.First(s => s.Name == "dbo")
                                    .Tables.First(t => t.Name == "DimCustomer");

                var indexNames = table.Indexes.Select(i => i.Name).ToList();
                Assert.AreEqual(2, indexNames.Count, "PK and UNIQUE constraints should each yield an index");
                CollectionAssert.DoesNotContain(indexNames, string.Empty, "Index names must never be empty");
                Assert.AreEqual(
                    indexNames.Count,
                    indexNames.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    $"Index names must be unique; got: {string.Join(", ", indexNames)}");
                CollectionAssert.AreEquivalent(
                    new[] { "PK_DimCustomer", "UQ_DimCustomer_BK" },
                    indexNames,
                    "Index names should come from the declared constraint names");

                var constraintNames = table.Constraints.Select(c => c.Name).ToList();
                CollectionAssert.AreEquivalent(
                    new[] { "PK_DimCustomer", "UQ_DimCustomer_BK" },
                    constraintNames,
                    "Constraint names should match the declared names, and the index names");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        /// <summary>
        /// DacFx leaves Name.Parts empty for constraints declared without a CONSTRAINT clause, so
        /// reading the trailing name part unguarded throws. Anonymous constraints must instead get a
        /// synthesized name that is still unique within the table.
        /// </summary>
        [Test]
        public void TableWithAnonymousConstraints_GetsSynthesizedUniqueNames()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string tableScript = @"
CREATE TABLE dbo.BooksAuthors (
    AuthorId INT NOT NULL,
    BookId INT NOT NULL,
    Isbn VARCHAR (32) NOT NULL,
    PRIMARY KEY CLUSTERED (AuthorId ASC, BookId ASC),
    UNIQUE NONCLUSTERED (Isbn ASC)
);
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "BooksAuthors.sql")), tableScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");
                var table = provider.Server.Databases.First()
                                    .Schemas.First(s => s.Name == "dbo")
                                    .Tables.First(t => t.Name == "BooksAuthors");

                // Materialising these must not throw; that is the regression being guarded.
                var indexNames = table.Indexes.Select(i => i.Name).ToList();
                var constraintNames = table.Constraints.Select(c => c.Name).ToList();

                Assert.AreEqual(2, indexNames.Count, "Anonymous PK and UNIQUE should each yield an index");
                CollectionAssert.DoesNotContain(indexNames, string.Empty, "Synthesized names must not be empty");
                Assert.AreEqual(
                    indexNames.Count,
                    indexNames.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    $"Synthesized index names must be unique; got: {string.Join(", ", indexNames)}");

                Assert.AreEqual(2, constraintNames.Count, "Anonymous PK and UNIQUE should each yield a constraint");
                CollectionAssert.DoesNotContain(constraintNames, string.Empty,
                    "Synthesized constraint names must not be empty");
                Assert.AreEqual(
                    constraintNames.Count,
                    constraintNames.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    $"Synthesized constraint names must be unique; got: {string.Join(", ", constraintNames)}");
                CollectionAssert.AreEquivalent(indexNames, constraintNames,
                    "Constraints and their indexes must use names from the same key-definition pass");

                Assert.IsTrue(table.Indexes.OfType<IRelationalIndex>().All(i => i.IsSystemNamed),
                    "Indexes built from anonymous constraints should report IsSystemNamed");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        [Test]
        public void GeneratedKeyName_DoesNotDisplaceLaterDeclaredName()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string tableScript = @"
CREATE TABLE dbo.NameCollision (
    Id INT NOT NULL PRIMARY KEY,
    AlternateId INT NOT NULL,
    CONSTRAINT PK__NameCollision UNIQUE (AlternateId)
);
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "NameCollision.sql")), tableScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");
                var table = provider.Server.Databases.First()
                                    .Schemas.First(s => s.Name == "dbo")
                                    .Tables.First(t => t.Name == "NameCollision");

                var constraints = table.Constraints.Cast<IUniqueConstraintBase>().ToList();
                var declaredUnique = constraints.Single(c => c.Type == ConstraintType.Unique);
                var anonymousPrimaryKey = constraints.Single(c => c.Type == ConstraintType.PrimaryKey);

                Assert.AreEqual("PK__NameCollision", declaredUnique.Name,
                    "A synthesized name must not cause a later declared name to be rewritten");
                Assert.AreNotEqual(declaredUnique.Name, anonymousPrimaryKey.Name,
                    "The anonymous primary key should move to a unique synthesized name");
                Assert.IsTrue(anonymousPrimaryKey.IsSystemNamed);

                var indexes = table.Indexes.OfType<IRelationalIndex>().ToList();
                Assert.AreEqual(
                    "PK__NameCollision",
                    indexes.Single(i => i.IndexKey.Type == ConstraintType.Unique).Name,
                    "The index wrapper must preserve the declared constraint name too");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        [Test]
        public void MultipleAnonymousUniqueConstraints_KeepConstraintAndIndexNamesPairedByColumns()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string tableScript = @"
CREATE TABLE dbo.MultipleAnonymousKeys (
    Id INT NOT NULL,
    AlternateId INT NOT NULL,
    UNIQUE (Id),
    UNIQUE (AlternateId)
);
";
            project.SqlObjectScripts.Add(
                new SqlObjectScript(Path.Combine("Tables", "MultipleAnonymousKeys.sql")),
                tableScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");
                var table = provider.Server.Databases.First()
                                    .Schemas.First(s => s.Name == "dbo")
                                    .Tables.First(t => t.Name == "MultipleAnonymousKeys");

                // Materialize constraints first to guard the opposite access order from the existing test.
                var constraintsByColumn = table.Constraints.Cast<IUniqueConstraintBase>().ToDictionary(
                    c => c.AssociatedIndex.IndexedColumns.Single().Name,
                    c => c.Name);
                var indexesByColumn = table.Indexes.OfType<IRelationalIndex>().ToDictionary(
                    i => i.IndexedColumns.Single().Name,
                    i => i.Name);

                Assert.AreEqual(2, constraintsByColumn.Count);
                Assert.AreEqual(2, indexesByColumn.Count);
                Assert.AreEqual(2, indexesByColumn.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                CollectionAssert.AreEquivalent(constraintsByColumn.Keys, indexesByColumn.Keys);
                foreach (string column in constraintsByColumn.Keys)
                {
                    Assert.AreEqual(constraintsByColumn[column], indexesByColumn[column],
                        $"Constraint and index names must stay paired for column '{column}'");
                }
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }

        [Test]
        public void TableWithMultipleKeys_BindsAgainstProjectMetadataWithoutInternalError()
        {
            string projectPath = ProjectUtils.CreateTestProject();
            var project = SqlProject.OpenProject(projectPath);

            const string tableScript = @"
CREATE TABLE dbo.BindMultipleKeys (
    Id INT NOT NULL,
    AlternateId INT NOT NULL,
    CONSTRAINT PK_BindMultipleKeys PRIMARY KEY (Id),
    CONSTRAINT UQ_BindMultipleKeys UNIQUE (AlternateId)
);
";
            project.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Tables", "BindMultipleKeys.sql")), tableScript);

            TSqlModel? model = null;
            try
            {
                model = TSqlModelBuilder.LoadModel(project);
                var provider = new TSqlModelMetadataProvider(model, "TestDatabase");
                var parseResult = Parser.Parse(tableScript);
                var binder = BinderProvider.CreateBinder(provider);

                Assert.DoesNotThrow(() => binder.Bind(new[] { parseResult }, "TestDatabase", BindMode.Batch),
                    "Binding should not fail while copying the table's name-keyed constraint and index collections");
            }
            finally
            {
                model?.Dispose();
                ProjectUtils.DeleteTestProject(projectPath);
            }
        }
    }
}
