//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.References;
using Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects;
using Microsoft.SqlTools.SqlCore.IntelliSense;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.IntelliSense
{
    /// <summary>
    /// Tests for project IntelliSense with database references: same-database, other-database (literal and SQLCMD variable),
    /// project references and master.
    /// </summary>
    public class SqlProjectReferenceIntelliSenseTests
    {
        private string workingDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            workingDirectory = Path.Combine(Path.GetTempPath(), "SqlProjectReferenceIntelliSenseTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the folder is under the temp directory.
            }
        }

        /// <summary>
        /// microsoft/vscode-mssql#22890: a project reference with a database SQLCMD variable is available as a database
        /// under both [$(Variable)] and the variable's value.
        /// </summary>
        [Test]
        public void ProjectReferenceWithDatabaseVariable_IsADatabaseUnderTheVariableAndItsValue()
        {
            string referencedPath = CreateProject("ProjectB");
            SqlProject referenced = SqlProject.OpenProject(referencedPath);
            referenced.SqlObjectScripts.Add(new SqlObjectScript("SomeTable.sql"), "CREATE TABLE [dbo].[SomeTable] ([Id] INT NOT NULL, [Name] NVARCHAR(50) NULL);");

            string projectPath = CreateProject("ProjectA");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlCmdVariables.Add(new SqlCmdVariable("ProjectB", "ProjectB"));
            project.SqlObjectScripts.Add(new SqlObjectScript("LocalTable.sql"), "CREATE TABLE [dbo].[LocalTable] ([Id] INT NOT NULL);");
            project.DatabaseReferences.Add(new SqlProjectReference(referencedPath, projectGuid: null, suppressMissingDependencies: false, project.SqlCmdVariables.Get("ProjectB")));

            using TSqlModel model = TSqlModelBuilder.LoadModel(SqlProject.OpenProject(projectPath));
            var provider = new TSqlModelMetadataProvider(model, "ProjectA");

            IReadOnlyList<string> failures = ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));

            Assert.IsEmpty(failures);
            foreach (string databaseName in new[] { "$(ProjectB)", "ProjectB" })
            {
                IDatabase database = provider.Server.Databases[databaseName];
                Assert.IsNotNull(database, $"Database {databaseName} should be available. Databases: {DatabaseNames(provider)}");
                ITable table = database.Schemas["dbo"].Tables["SomeTable"];
                Assert.IsNotNull(table);
                CollectionAssert.AreEquivalent(new[] { "Id", "Name" }, table.Columns.Select(c => c.Name).ToArray());
                Assert.IsFalse(table.IsSystemObject);
            }

            Assert.IsNotNull(provider.Server.Databases["ProjectA"].Schemas["dbo"].Tables["LocalTable"], "The project's own objects are unchanged.");
            Assert.IsNull(provider.Server.Databases["ProjectA"].Schemas["dbo"].Tables["SomeTable"], "Objects of another database are not in the project database.");

            AssertBinds(provider, "ProjectA", "SELECT [Id], [Name] FROM [$(ProjectB)].[dbo].[SomeTable];");
            AssertBinds(provider, "ProjectA", "SELECT [Id] FROM [ProjectB].[dbo].[SomeTable];");
        }

        [Test]
        public void SameDatabaseReference_ObjectsJoinTheProjectDatabaseAndProjectObjectsWin()
        {
            string dacpacPath = Path.Combine(workingDirectory, "Shared.dacpac");
            ProjectReferenceResolverTests.SaveDacpac(dacpacPath,
                "CREATE TABLE [dbo].[SharedTable] ([Id] INT NOT NULL);\r\nGO\r\nCREATE TABLE [dbo].[Overlap] ([FromReference] INT NOT NULL);\r\nGO\r\nCREATE SCHEMA [shared];\r\nGO\r\nCREATE TABLE [shared].[InOwnSchema] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlObjectScripts.Add(new SqlObjectScript("Overlap.sql"), "CREATE TABLE [dbo].[Overlap] ([FromProject] INT NOT NULL);");
            project.DatabaseReferences.Add(new DacpacReference(dacpacPath, suppressMissingDependencies: true));

            using TSqlModel model = TSqlModelBuilder.LoadModel(SqlProject.OpenProject(projectPath));
            var provider = new TSqlModelMetadataProvider(model, "Main");
            ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));

            IDatabase database = provider.Server.Databases["Main"];
            ITable shared = database.Schemas["dbo"].Tables["SharedTable"];
            Assert.IsNotNull(shared);
            Assert.IsFalse(shared.IsSystemObject, "Objects of a same-database reference are user objects.");
            CollectionAssert.AreEqual(new[] { "Id" }, shared.Columns.Select(c => c.Name).ToArray());
            Assert.IsNotNull(database.Schemas["shared"]?.Tables["InOwnSchema"], "Schemas that only the reference uses are added.");
            CollectionAssert.AreEqual(new[] { "FromProject" }, database.Schemas["dbo"].Tables["Overlap"].Columns.Select(c => c.Name).ToArray(),
                "The project's own definition wins over the reference's.");
            Assert.AreEqual(1, provider.Server.Databases.Count, "A same-database reference adds no database.");
            AssertBinds(provider, "Main", "SELECT [Id] FROM [dbo].[SharedTable];");
        }

        [Test]
        public void MasterReference_AddsSystemViewsAndAMasterDatabase()
        {
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.Properties.TargetSqlPlatform = SqlPlatforms.Sql160;
            project.DatabaseReferences.Add(new SystemDatabaseReference(SystemDatabase.Master, suppressMissingDependencies: false, "master"));

            using TSqlModel model = TSqlModelBuilder.LoadModel(SqlProject.OpenProject(projectPath));
            var provider = new TSqlModelMetadataProvider(model, "Main");
            Assert.IsNull(provider.Server.Databases["Main"].Schemas["sys"]?.Views["tables"], "Without master there is no sys.tables.");

            IReadOnlyList<string> failures = ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));

            Assert.IsEmpty(failures);
            IView sysTables = provider.Server.Databases["Main"].Schemas["sys"].Views["tables"];
            Assert.IsNotNull(sysTables, "sys.tables comes from the master reference.");
            Assert.IsTrue(sysTables.IsSystemObject);
            CollectionAssert.Contains(sysTables.Columns.Select(c => c.Name).ToArray(), "object_id", "System view columns are available.");
            Assert.IsNotNull(provider.Server.Databases["master"]?.Schemas["sys"]?.Views["databases"], "master.sys.databases resolves by three-part name.");
            AssertBinds(provider, "Main", "SELECT [name] FROM [sys].[tables];");
        }

        [Test]
        public void ApplyReferences_ReplacesPreviouslyLoadedReferences()
        {
            string firstDacpac = Path.Combine(workingDirectory, "First.dacpac");
            string secondDacpac = Path.Combine(workingDirectory, "Second.dacpac");
            ProjectReferenceResolverTests.SaveDacpac(firstDacpac, "CREATE TABLE [dbo].[FirstTable] ([Id] INT NOT NULL);");
            ProjectReferenceResolverTests.SaveDacpac(secondDacpac, "CREATE TABLE [dbo].[SecondTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference(firstDacpac, suppressMissingDependencies: false, "FirstDb"));

            using TSqlModel model = TSqlModelBuilder.LoadModel(SqlProject.OpenProject(projectPath));
            var provider = new TSqlModelMetadataProvider(model, "Main");
            ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));
            Assert.IsNotNull(provider.Server.Databases["FirstDb"]);

            project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Delete(project.DatabaseReferences.Single().Name);
            project.DatabaseReferences.Add(new DacpacReference(secondDacpac, suppressMissingDependencies: false, "SecondDb"));
            ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));

            Assert.IsNull(provider.Server.Databases["FirstDb"], "The removed reference's database is gone.");
            Assert.IsNotNull(provider.Server.Databases["SecondDb"]?.Schemas["dbo"]?.Tables["SecondTable"]);
            Assert.AreEqual(1, model.References.Count);
        }

        [Test]
        public void UnresolvedReference_IsReportedAndDoesNotBlockOtherReferences()
        {
            string dacpacPath = Path.Combine(workingDirectory, "Good.dacpac");
            ProjectReferenceResolverTests.SaveDacpac(dacpacPath, "CREATE TABLE [dbo].[GoodTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference(Path.Combine(workingDirectory, "Missing.dacpac"), suppressMissingDependencies: false, "MissingDb"));
            project.DatabaseReferences.Add(new DacpacReference(dacpacPath, suppressMissingDependencies: false, "GoodDb"));

            using TSqlModel model = TSqlModelBuilder.LoadModel(SqlProject.OpenProject(projectPath));
            var provider = new TSqlModelMetadataProvider(model, "Main");
            IReadOnlyList<string> failures = ProjectReferenceLoader.ApplyReferences(model, provider, Resolve(projectPath));

            Assert.AreEqual(1, failures.Count);
            StringAssert.Contains("Missing.dacpac", failures[0]);
            Assert.IsNotNull(provider.Server.Databases["GoodDb"]?.Schemas["dbo"]?.Tables["GoodTable"]);
        }

        private string CreateProject(string name)
        {
            string projectPath = Path.Combine(workingDirectory, name, name + ".sqlproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            _ = SqlProject.CreateProjectAsync(projectPath).GetAwaiter().GetResult();
            return projectPath;
        }

        private static ResolvedProjectReferences Resolve(string projectPath) =>
            new ProjectReferenceResolver(ProjectReferenceResolver.DefaultSystemDacpacsRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath);

        private static string DatabaseNames(TSqlModelMetadataProvider provider) =>
            string.Join(", ", provider.Server.Databases.Select(d => d.Name));

        /// <summary>Binds <paramref name="query"/> against the project metadata the way the language service does.</summary>
        private static void AssertBinds(TSqlModelMetadataProvider provider, string databaseName, string query)
        {
            ParseResult parseResult = Parser.Parse(query);
            IBinder binder = BinderProvider.CreateBinder(provider);
            Assert.DoesNotThrow(() => binder.Bind(new[] { parseResult }, databaseName, BindMode.Batch), query);
        }
    }
}
