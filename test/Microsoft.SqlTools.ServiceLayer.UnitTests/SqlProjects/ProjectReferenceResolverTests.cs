//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Microsoft.Data.Tools.Schema.SchemaModel;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.References;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects
{
    /// <summary>
    /// Tests for resolving SQL project database references to dacpacs and referenced projects.
    /// </summary>
    public class ProjectReferenceResolverTests
    {
        private string workingDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            workingDirectory = Path.Combine(Path.GetTempPath(), "ProjectReferenceResolverTests", Guid.NewGuid().ToString("N"));
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

        [Test]
        public void Dacpac_WithDatabaseVariable_ResolvesPathBindingAndNames()
        {
            string dacpacPath = CreateDacpac(Path.Combine("Dacpacs", "Other.dacpac"), "CREATE TABLE [dbo].[OtherTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlCmdVariables.Add(new SqlCmdVariable("OtherDb", "OtherDbValue"));
            project.DatabaseReferences.Add(new DacpacReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, dacpacPath),
                suppressMissingDependencies: false,
                project.SqlCmdVariables.Get("OtherDb")));

            ResolvedProjectReferences resolved = Resolve(projectPath);

            ResolvedDatabaseReference reference = resolved.References.Single();
            Assert.AreEqual(DatabaseReferenceKind.Dacpac, reference.Kind);
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, reference.Resolution, reference.Message);
            Assert.AreEqual(Path.GetFullPath(dacpacPath), reference.ResolvedPath);
            Assert.AreEqual("OtherDb", reference.ModelReference!.DatabaseSqlCmdVariable);
            Assert.IsFalse(reference.IsSameDatabase);
            CollectionAssert.AreEqual(new[] { "$(OtherDb)", "OtherDbValue" }, reference.DatabaseNames);
            Assert.AreEqual("OtherDbValue", resolved.SqlCmdVariables["OtherDb"]);
            Assert.AreEqual(1, resolved.ModelReferences.Count);
        }

        [Test]
        public void Dacpac_SameDatabaseAndLiteral_HaveExpectedBindings()
        {
            string sameDbPath = CreateDacpac("Same.dacpac", "CREATE TABLE [dbo].[SameTable] ([Id] INT NOT NULL);");
            string literalPath = CreateDacpac("Literal.dacpac", "CREATE TABLE [dbo].[LiteralTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference(RelativeTo(projectPath, sameDbPath), suppressMissingDependencies: true));
            project.DatabaseReferences.Add(new DacpacReference(RelativeTo(projectPath, literalPath), suppressMissingDependencies: false, "LiteralDb"));

            ResolvedProjectReferences resolved = Resolve(projectPath);

            ResolvedDatabaseReference sameDb = resolved.References.Single(r => r.ResolvedPath == Path.GetFullPath(sameDbPath));
            Assert.IsTrue(sameDb.IsSameDatabase);
            Assert.IsTrue(sameDb.ModelReference!.SuppressMissingDependenciesErrors);
            Assert.IsEmpty(sameDb.DatabaseNames);

            ResolvedDatabaseReference literal = resolved.References.Single(r => r.ResolvedPath == Path.GetFullPath(literalPath));
            Assert.AreEqual("LiteralDb", literal.ModelReference!.DatabaseVariableLiteralValue);
            CollectionAssert.AreEqual(new[] { "LiteralDb" }, literal.DatabaseNames);
        }

        [Test]
        public void Dacpac_MissingFile_IsReportedNotFound()
        {
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference("Missing.dacpac", suppressMissingDependencies: false));

            ResolvedDatabaseReference reference = Resolve(projectPath).References.Single();

            Assert.AreEqual(DatabaseReferenceResolution.NotFound, reference.Resolution);
            Assert.IsNull(reference.ModelReference);
            StringAssert.Contains("Missing.dacpac", reference.Message);
        }

        [Test]
        public void Project_WithOwnReferences_IsLoadedFromItsScripts()
        {
            string nestedDacpac = CreateDacpac("Nested.dacpac", "CREATE TABLE [dbo].[NestedTable] ([Id] INT NOT NULL);");
            string referencedPath = CreateProject("Referenced");
            SqlProject referenced = SqlProject.OpenProject(referencedPath);
            referenced.SqlObjectScripts.Add(new SqlObjectScript(Path.Combine("Views", "ReferencedView.sql")),
                "CREATE VIEW [dbo].[ReferencedView] AS SELECT [Id] FROM [dbo].[NestedTable];");
            referenced.DatabaseReferences.Add(new DacpacReference(RelativeTo(referencedPath, nestedDacpac), suppressMissingDependencies: false));

            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new SqlProjectReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, referencedPath),
                projectGuid: null,
                suppressMissingDependencies: false,
                "ReferencedDb"));

            ResolvedDatabaseReference reference = Resolve(projectPath).References.Single();

            Assert.AreEqual(DatabaseReferenceKind.SqlProject, reference.Kind);
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, reference.Resolution, reference.Message);
            TSqlModelReference modelReference = reference.ModelReference!;
            Assert.IsTrue(modelReference.IsProject);
            Assert.AreEqual(Path.GetFullPath(referencedPath), modelReference.Path);
            Assert.AreEqual(referenced.Properties.DatabaseSchemaProvider, modelReference.Dsp);
            Assert.AreEqual(1, modelReference.TargetScripts.Count);
            Assert.IsTrue(File.Exists(modelReference.TargetScripts[0]), "Script paths are absolute.");
            Assert.AreEqual(Path.GetFullPath(nestedDacpac), modelReference.DatabaseReferences.Single().Path);
            CollectionAssert.AreEqual(new[] { "ReferencedDb" }, reference.DatabaseNames);
        }

        [Test]
        public void Project_CircularReference_IsReportedAndNotLoaded()
        {
            string firstPath = CreateProject("First");
            string secondPath = CreateProject("Second");
            SqlProject first = SqlProject.OpenProject(firstPath);
            SqlProject second = SqlProject.OpenProject(secondPath);
            first.DatabaseReferences.Add(new SqlProjectReference(RelativeTo(firstPath, secondPath), projectGuid: null, suppressMissingDependencies: false));
            second.DatabaseReferences.Add(new SqlProjectReference(RelativeTo(secondPath, firstPath), projectGuid: null, suppressMissingDependencies: false));

            ResolvedDatabaseReference reference = Resolve(firstPath).References.Single();

            // The first project's reference to the second resolves; the second's reference back to the first is dropped.
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, reference.Resolution, reference.Message);
            Assert.IsEmpty(reference.ModelReference!.DatabaseReferences);
        }

        [Test]
        public void SystemDatabase_ArtifactReference_UsesBundledDacpacForPlatform()
        {
            string systemRoot = CreateSystemDacpacsRoot(out string platformFolder, "160");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.Properties.TargetSqlPlatform = SqlPlatforms.Sql160;
            project.DatabaseReferences.Add(new SystemDatabaseReference(SystemDatabase.Master, suppressMissingDependencies: false, "master"));

            ResolvedDatabaseReference reference = new ProjectReferenceResolver(systemRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath)
                .References.Single();

            Assert.AreEqual(DatabaseReferenceResolution.Resolved, reference.Resolution, reference.Message);
            Assert.AreEqual(Path.Combine(systemRoot, platformFolder, "master.dacpac"), reference.ResolvedPath);
            Assert.AreEqual("master", reference.ModelReference!.DatabaseVariableLiteralValue);
            CollectionAssert.AreEqual(new[] { "master" }, reference.DatabaseNames);
        }

        [Test]
        public void SystemDatabase_PackageReference_UsesRestoredPackageAndFallsBackToBundled()
        {
            string systemRoot = CreateSystemDacpacsRoot(out _, "160");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.Properties.TargetSqlPlatform = SqlPlatforms.Sql160;
            project.DatabaseReferences.Add(new SystemDatabaseReference(SystemDatabase.MSDB, suppressMissingDependencies: false, referenceType: ReferenceType.PackageReference));

            // Not restored: the bundled msdb.dacpac is used and the reference says so.
            ResolvedDatabaseReference fallback = new ProjectReferenceResolver(systemRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath)
                .References.Single();
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, fallback.Resolution);
            StringAssert.StartsWith(systemRoot, fallback.ResolvedPath);
            Assert.IsNotNull(fallback.Message);
            Assert.AreEqual("msdb", fallback.ModelReference!.DatabaseVariableLiteralValue);

            // Restored: the package's dacpac is used.
            string packagesFolder = Path.Combine(workingDirectory, "packages");
            string packageDacpac = Path.Combine(packagesFolder, "microsoft.sqlserver.dacpacs.msdb", "160.1.4", "tools", "msdb.dacpac");
            Directory.CreateDirectory(Path.GetDirectoryName(packageDacpac)!);
            File.Copy(Path.Combine(systemRoot, "160", "msdb.dacpac"), packageDacpac);
            WriteAssetsFile(projectPath, packagesFolder, ("Microsoft.SqlServer.Dacpacs.Msdb", "160.1.4"));

            ResolvedDatabaseReference restored = new ProjectReferenceResolver(systemRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath)
                .References.Single();
            Assert.AreEqual(packageDacpac, restored.ResolvedPath);
            Assert.IsNull(restored.Message);
        }

        [Test]
        public void NuGetPackage_IsResolvedFromRestoreOutput()
        {
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new NugetPackageReference("Contoso.Reference", "1.2.0", suppressMissingDependencies: false, "ContosoDb"));

            ResolvedDatabaseReference notRestored = Resolve(projectPath).References.Single();
            Assert.AreEqual(DatabaseReferenceResolution.NotRestored, notRestored.Resolution);
            Assert.IsNull(notRestored.ModelReference);

            string packagesFolder = Path.Combine(workingDirectory, "packages");
            string packageDacpac = Path.Combine(packagesFolder, "contoso.reference", "1.2.0", "tools", "Contoso.Reference.dacpac");
            Directory.CreateDirectory(Path.GetDirectoryName(packageDacpac)!);
            SaveDacpac(packageDacpac, "CREATE TABLE [dbo].[ContosoTable] ([Id] INT NOT NULL);");
            WriteAssetsFile(projectPath, packagesFolder, ("Contoso.Reference", "1.2.0"));

            ResolvedDatabaseReference restored = Resolve(projectPath).References.Single();
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, restored.Resolution, restored.Message);
            Assert.AreEqual(packageDacpac, restored.ResolvedPath);
            CollectionAssert.AreEqual(new[] { "ContosoDb" }, restored.DatabaseNames);
        }

        [Test]
        public void SqlCmdVariables_UseLiteralValueOrDefaultAndSkipEmpty()
        {
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlCmdVariables.Add(new SqlCmdVariable("FromDefault", "DefaultValue"));
            project.SqlCmdVariables.Add(new SqlCmdVariable("Empty", string.Empty));

            IReadOnlyDictionary<string, string> values = ProjectReferenceResolver.GetSqlCmdVariableValues(SqlProject.OpenProject(projectPath, onlyLoadProperties: true));

            Assert.AreEqual("DefaultValue", values["FromDefault"], "A $(SqlCmdVar__N) value is supplied at publish time, so the default is used.");
            Assert.IsFalse(values.ContainsKey("Empty"), "Variables without a value are left out.");
        }

        [Test]
        public void DacpacPath_WithMsBuildProperties_IsExpanded()
        {
            string dacpacPath = CreateDacpac(Path.Combine("Main", "lib", "Lib.dacpac"), "CREATE TABLE [dbo].[LibTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference(@"$(MSBuildProjectDirectory)\lib\Lib.dacpac", suppressMissingDependencies: false));
            project.DatabaseReferences.Add(new DacpacReference(@"$(UndefinedRoot)\Other.dacpac", suppressMissingDependencies: false));
            string lowerCaseDacpacPath = CreateDacpac(Path.Combine("Main", "lib", "Lower.dacpac"), "CREATE TABLE [dbo].[LowerTable] ([Id] INT NOT NULL);");
            project.DatabaseReferences.Add(new DacpacReference(@"$(msbuildprojectdirectory)\lib\Lower.dacpac", suppressMissingDependencies: false));

            ResolvedProjectReferences resolved = Resolve(projectPath);

            ResolvedDatabaseReference lib = resolved.References.Single(reference => reference.Name.Contains("Lib.dacpac"));
            Assert.AreEqual(Path.GetFullPath(dacpacPath), lib.ResolvedPath, lib.Message);
            ResolvedDatabaseReference undefined = resolved.References.Single(reference => reference.Name.Contains("Other.dacpac"));
            Assert.AreEqual(DatabaseReferenceResolution.NotFound, undefined.Resolution);
            StringAssert.Contains("UndefinedRoot", undefined.Message);
            ResolvedDatabaseReference lowerCase = resolved.References.Single(reference => reference.Name.Contains("Lower.dacpac"));
            Assert.AreEqual(Path.GetFullPath(lowerCaseDacpacPath), lowerCase.ResolvedPath, "MSBuild property names are case-insensitive. " + lowerCase.Message);
        }

        [Test]
        public void DacpacPath_WithNestedMsBuildProperties_IsExpanded()
        {
            string dacpacPath = CreateDacpac(Path.Combine("Main", "refs", "Shared.dacpac"), "CREATE TABLE [dbo].[SharedTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            AddProperties(projectPath, ("ReferenceDir", @"$(MSBuildProjectDirectory)\refs"), ("Cycle", @"$(Loop)\a"), ("Loop", @"$(Cycle)\b"));
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference(@"$(ReferenceDir)\Shared.dacpac", suppressMissingDependencies: false));
            project.DatabaseReferences.Add(new DacpacReference(@"$(Cycle)\Other.dacpac", suppressMissingDependencies: false));

            ResolvedProjectReferences resolved = Resolve(projectPath);

            ResolvedDatabaseReference shared = resolved.References.Single(reference => reference.Name.Contains("Shared.dacpac"));
            Assert.AreEqual(Path.GetFullPath(dacpacPath), shared.ResolvedPath, "A property whose value uses another property is expanded. " + shared.Message);
            ResolvedDatabaseReference cyclic = resolved.References.Single(reference => reference.Name.Contains("Other.dacpac"));
            Assert.AreEqual(DatabaseReferenceResolution.NotFound, cyclic.Resolution, "A property that uses itself is undefined.");
        }

        [Test]
        public void ProjectPath_WithMsBuildProperties_IsExpanded()
        {
            string referencedPath = CreateProject("Referenced");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new SqlProjectReference(
                @"$(MSBuildProjectDirectory)\..\Referenced\Referenced.sqlproj",
                projectGuid: null,
                suppressMissingDependencies: false,
                "ReferencedDb"));

            ResolvedDatabaseReference reference = Resolve(projectPath).References.Single();

            Assert.AreEqual(DatabaseReferenceResolution.Resolved, reference.Resolution, reference.Message);
            Assert.AreEqual(Path.GetFullPath(referencedPath), reference.ResolvedPath);
        }

        [Test]
        public void SystemDatabase_PackageReference_UsesTheProjectsOwnPackage()
        {
            string systemRoot = CreateSystemDacpacsRoot(out _, "160");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.Properties.TargetSqlPlatform = SqlPlatforms.Sql160;
            project.DatabaseReferences.Add(new SystemDatabaseReference(SystemDatabase.Master, suppressMissingDependencies: false, referenceType: ReferenceType.PackageReference));

            // Another package brings in Azure's master package, which the assets file lists first
            string packagesFolder = Path.Combine(workingDirectory, "packages");
            string azureDacpac = Path.Combine(packagesFolder, "microsoft.sqlserver.dacpacs.azure.master", "170.0.0", "tools", "master.dacpac");
            string masterDacpac = Path.Combine(packagesFolder, "microsoft.sqlserver.dacpacs.master", "160.2.1", "tools", "master.dacpac");
            foreach (string packageDacpac in new[] { azureDacpac, masterDacpac })
            {
                Directory.CreateDirectory(Path.GetDirectoryName(packageDacpac)!);
                File.Copy(Path.Combine(systemRoot, "160", "master.dacpac"), packageDacpac);
            }

            WriteAssetsFile(projectPath, packagesFolder, new[] { "Microsoft.SqlServer.Dacpacs.Master" },
                ("Microsoft.SqlServer.Dacpacs.Azure.Master", "170.0.0"), ("Microsoft.SqlServer.Dacpacs.Master", "160.2.1"));
            ResolvedDatabaseReference reference = new ProjectReferenceResolver(systemRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath)
                .References.Single();
            Assert.AreEqual(masterDacpac, reference.ResolvedPath, "The project's own master package is used.");

            // Only the other platform's package is restored: the bundled dacpac for the project's platform is used instead
            WriteAssetsFile(projectPath, packagesFolder, new[] { "Microsoft.SqlServer.Dacpacs.Master" },
                ("Microsoft.SqlServer.Dacpacs.Azure.Master", "170.0.0"));
            reference = new ProjectReferenceResolver(systemRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath)
                .References.Single();
            Assert.AreEqual(Path.Combine(systemRoot, "160", "master.dacpac"), reference.ResolvedPath, "Another platform's master package isn't used.");
        }

        [TestCase("Microsoft.Data.Tools.Schema.Sql.Sql160DatabaseSchemaProvider", "160")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.Sql170DatabaseSchemaProvider", "170")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SqlAzureV12DatabaseSchemaProvider", "AzureV12")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SqlDwDatabaseSchemaProvider", "AzureDw")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SqlServerlessDatabaseSchemaProvider", "Serverless")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SqlDbFabricDatabaseSchemaProvider", "DbFabric")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SqlDwUnifiedDatabaseSchemaProvider", "DwUnified")]
        public void SystemDacpacFolder_MatchesSdkLayout(string databaseSchemaProvider, string expectedFolder)
        {
            Assert.AreEqual(expectedFolder, ProjectReferenceResolver.GetSystemDacpacFolderName(databaseSchemaProvider));
        }

        [Test]
        public void BundledSystemDacpacs_AreShippedForEveryPlatformFolder()
        {
            foreach (string folder in new[] { "130", "140", "150", "160", "170", "AzureV12", "AzureDw", "Serverless", "DbFabric", "DwUnified" })
            {
                Assert.IsTrue(File.Exists(Path.Combine(ProjectReferenceResolver.DefaultSystemDacpacsRoot, folder, "master.dacpac")),
                    $"master.dacpac for {folder} should be copied next to the service.");
            }
        }

        [Test]
        public async Task Cache_SharesOneResolutionAndRefreshesWhenTheProjectChanges()
        {
            const int requestCount = 8;
            string projectPath = CreateProject("Main");
            var cache = new ProjectReferenceCache();
            int resolutions = 0;
            using var requestsStarted = new CountdownEvent(requestCount);
            Func<ResolvedProjectReferences> resolve = () =>
            {
                Interlocked.Increment(ref resolutions);
                // Hold the first resolution until every request has started, so they all overlap it
                requestsStarted.Wait(TimeSpan.FromSeconds(30));
                Thread.Sleep(50);
                return new ProjectReferenceResolver(workingDirectory).Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath);
            };

            // Each request gets its own thread, so none waits for a pool thread held by another request
            Task<ResolvedProjectReferences>[] requests = Enumerable.Range(0, requestCount)
                .Select(_ => Task.Factory.StartNew(() =>
                {
                    requestsStarted.Signal();
                    return cache.GetOrResolve(projectPath, resolve);
                }, TaskCreationOptions.LongRunning))
                .ToArray();
            ResolvedProjectReferences[] results = await Task.WhenAll(requests);

            Assert.AreEqual(1, resolutions, "Concurrent requests for a project share one resolution.");
            Assert.IsTrue(results.All(result => ReferenceEquals(result, results[0])));

            File.SetLastWriteTimeUtc(projectPath, DateTime.UtcNow.AddMinutes(1));
            ResolvedProjectReferences refreshed = cache.GetOrResolve(projectPath, resolve);
            Assert.AreEqual(2, resolutions, "Editing the .sqlproj invalidates the cached references.");
            Assert.AreNotSame(results[0], refreshed);

            cache.Invalidate(projectPath);
            cache.GetOrResolve(projectPath, resolve);
            Assert.AreEqual(3, resolutions);
        }

        [Test]
        public void Cache_RefreshesWhenAMissingDacpacIsCreated()
        {
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new DacpacReference("Other.dacpac", suppressMissingDependencies: false, "OtherDb"));
            var cache = new ProjectReferenceCache();
            Assert.AreEqual(DatabaseReferenceResolution.NotFound, ResolveCached(cache, projectPath).References.Single().Resolution, "Setup");

            CreateDacpac(Path.Combine("Main", "Other.dacpac"), "CREATE TABLE [dbo].[OtherTable] ([Id] INT NOT NULL);");

            Assert.AreEqual(DatabaseReferenceResolution.Resolved, ResolveCached(cache, projectPath).References.Single().Resolution,
                "Creating the missing dacpac invalidates the cached references.");
        }

        [Test]
        public void Cache_RefreshesWhenAReferencedProjectsDependenciesChange()
        {
            string referencedPath = CreateProject("Referenced");
            SqlProject referenced = SqlProject.OpenProject(referencedPath);
            referenced.DatabaseReferences.Add(new DacpacReference("Nested.dacpac", suppressMissingDependencies: false));
            referenced.DatabaseReferences.Add(new NugetPackageReference("Contoso.Reference", "1.2.0", suppressMissingDependencies: false, "ContosoDb"));
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new SqlProjectReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, referencedPath),
                projectGuid: null,
                suppressMissingDependencies: false,
                "ReferencedDb"));
            var cache = new ProjectReferenceCache();
            Assert.AreEqual(0, NestedReferenceCount(cache, projectPath), "Setup: neither of the referenced project's references resolves");

            CreateDacpac(Path.Combine("Referenced", "Nested.dacpac"), "CREATE TABLE [dbo].[NestedTable] ([Id] INT NOT NULL);");
            Assert.AreEqual(1, NestedReferenceCount(cache, projectPath), "Creating the referenced project's missing dacpac invalidates the cached references.");

            string packagesFolder = Path.Combine(workingDirectory, "packages");
            CreateDacpac(Path.Combine("packages", "contoso.reference", "1.2.0", "tools", "Contoso.Reference.dacpac"), "CREATE TABLE [dbo].[ContosoTable] ([Id] INT NOT NULL);");
            WriteAssetsFile(referencedPath, packagesFolder, ("Contoso.Reference", "1.2.0"));
            Assert.AreEqual(2, NestedReferenceCount(cache, projectPath), "Restoring the referenced project's packages invalidates the cached references.");
        }

        [Test]
        public void Cache_RefreshesWhenAReferencedProjectChangesAfterItIsRead()
        {
            string referencedPath = CreateProject("Referenced");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new SqlProjectReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, referencedPath),
                projectGuid: null,
                suppressMissingDependencies: false,
                "ReferencedDb"));

            // The referenced project is edited right after the first resolution reads it
            bool edited = false;
            var resolver = new ProjectReferenceResolver(ProjectReferenceResolver.DefaultSystemDacpacsRoot, path =>
            {
                SqlProject opened = SqlProject.OpenProject(path, onlyLoadProperties: false);
                if (!edited)
                {
                    edited = true;
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
                }

                return opened;
            });
            var cache = new ProjectReferenceCache();
            int resolutions = 0;
            Func<ResolvedProjectReferences> resolve = () =>
            {
                resolutions++;
                return resolver.Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath);
            };

            cache.GetOrResolve(projectPath, resolve);
            cache.GetOrResolve(projectPath, resolve);
            Assert.AreEqual(2, resolutions, "The edit made after the referenced project was read invalidates the cached references.");
            cache.GetOrResolve(projectPath, resolve);
            Assert.AreEqual(2, resolutions, "Without further edits the references stay cached.");
        }

        [Test]
        public void Cache_RefreshesWhenScriptsAreAddedToOrDeletedFromAReferencedProject()
        {
            string referencedPath = CreateProject("Referenced");
            string referencedDirectory = Path.GetDirectoryName(referencedPath)!;
            File.WriteAllText(Path.Combine(referencedDirectory, "First.sql"), "CREATE TABLE [dbo].[First] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.DatabaseReferences.Add(new SqlProjectReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, referencedPath),
                projectGuid: null,
                suppressMissingDependencies: false,
                "ReferencedDb"));
            var cache = new ProjectReferenceCache();
            Assert.AreEqual(1, ReferencedScriptCount(cache, projectPath), "Setup: the SDK-style project includes the .sql file in its folder");

            Directory.CreateDirectory(Path.Combine(referencedDirectory, "Views"));
            File.WriteAllText(Path.Combine(referencedDirectory, "Views", "Second.sql"), "CREATE VIEW [dbo].[Second] AS SELECT [Id] FROM [dbo].[First];");
            Assert.AreEqual(2, ReferencedScriptCount(cache, projectPath), "Adding a script, even in a new folder, invalidates the cached references.");

            File.Delete(Path.Combine(referencedDirectory, "First.sql"));
            Assert.AreEqual(1, ReferencedScriptCount(cache, projectPath), "Deleting a script invalidates the cached references.");
        }

        [TestCase("{ \"libraries\": [ not json", TestName = "UnreadableAssetsFile_Malformed")]
        [TestCase("{ \"libraries\": { \"Contoso.Reference/1.2.0\": 42 } }", TestName = "UnreadableAssetsFile_UnexpectedType")]
        public void UnreadableAssetsFile_PackagesAreNotRestoredAndOtherReferencesResolve(string assetsJson)
        {
            string dacpacPath = CreateDacpac("Other.dacpac", "CREATE TABLE [dbo].[OtherTable] ([Id] INT NOT NULL);");
            string projectPath = CreateProject("Main");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlCmdVariables.Add(new SqlCmdVariable("OtherDb", "OtherDbValue"));
            project.DatabaseReferences.Add(new DacpacReference(
                Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, dacpacPath),
                suppressMissingDependencies: false,
                project.SqlCmdVariables.Get("OtherDb")));
            project.DatabaseReferences.Add(new NugetPackageReference("Contoso.Reference", "1.2.0", suppressMissingDependencies: false, "ContosoDb"));
            string assetsPath = ProjectAssets.GetAssetsFilePath(Path.GetDirectoryName(projectPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(assetsPath)!);
            File.WriteAllText(assetsPath, assetsJson);

            ResolvedProjectReferences resolved = Resolve(projectPath);

            ResolvedDatabaseReference dacpac = resolved.References.Single(reference => reference.Kind == DatabaseReferenceKind.Dacpac);
            Assert.AreEqual(DatabaseReferenceResolution.Resolved, dacpac.Resolution, dacpac.Message);
            Assert.AreEqual(DatabaseReferenceResolution.NotRestored, resolved.References.Single(reference => reference.Kind == DatabaseReferenceKind.NuGetPackage).Resolution);
            Assert.AreEqual("OtherDbValue", resolved.SqlCmdVariables["OtherDb"]);
        }

        /// <summary>
        /// Returns <paramref name="path"/> relative to the project, as clients add references: on Linux and macOS the SQL
        /// projects library stores an absolute path without its leading '/'.
        /// </summary>
        internal static string RelativeTo(string projectPath, string path) =>
            Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, path);

        private ResolvedProjectReferences ResolveCached(ProjectReferenceCache cache, string projectPath) =>
            cache.GetOrResolve(projectPath, () => Resolve(projectPath));

        /// <summary>Resolves through the cache and counts the references of the project's only (project) reference that resolved.</summary>
        private int NestedReferenceCount(ProjectReferenceCache cache, string projectPath) =>
            ResolveCached(cache, projectPath).References.Single().ModelReference!.DatabaseReferences.Count;

        /// <summary>Resolves through the cache and counts the scripts of the project's only (project) reference.</summary>
        private int ReferencedScriptCount(ProjectReferenceCache cache, string projectPath) =>
            ResolveCached(cache, projectPath).References.Single().ModelReference!.TargetScripts.Count;

        private ResolvedProjectReferences Resolve(string projectPath) =>
            new ProjectReferenceResolver(ProjectReferenceResolver.DefaultSystemDacpacsRoot)
                .Resolve(SqlProject.OpenProject(projectPath, onlyLoadProperties: true), projectPath);

        private string CreateProject(string name)
        {
            string projectPath = Path.Combine(workingDirectory, name, name + ".sqlproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            _ = SqlProject.CreateProjectAsync(projectPath).GetAwaiter().GetResult();
            return projectPath;
        }

        /// <summary>Adds a property group with <paramref name="properties"/> to the .sqlproj.</summary>
        private static void AddProperties(string projectPath, params (string Name, string Value)[] properties)
        {
            string group = "  <PropertyGroup>\n" + string.Concat(properties.Select(p => $"    <{p.Name}>{p.Value}</{p.Name}>\n")) + "  </PropertyGroup>\n";
            string xml = File.ReadAllText(projectPath);
            int end = xml.LastIndexOf("</Project>", StringComparison.Ordinal);
            File.WriteAllText(projectPath, xml.Insert(end, group));
        }

        private string CreateDacpac(string relativePath, string script)
        {
            string dacpacPath = Path.Combine(workingDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(dacpacPath)!);
            SaveDacpac(dacpacPath, script);
            return dacpacPath;
        }

        internal static void SaveDacpac(string dacpacPath, string script)
        {
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            model.AddObjects(script);
            DacPackageExtensions.BuildPackage(dacpacPath, model, new PackageMetadata());
        }

        /// <summary>Copies the bundled master and msdb dacpacs of a platform into a new system dacpacs folder.</summary>
        private string CreateSystemDacpacsRoot(out string platformFolder, string platform)
        {
            string root = Path.Combine(workingDirectory, "SystemDacpacs");
            platformFolder = platform;
            Directory.CreateDirectory(Path.Combine(root, platform));
            foreach (string database in new[] { "master", "msdb" })
            {
                File.Copy(
                    Path.Combine(ProjectReferenceResolver.DefaultSystemDacpacsRoot, platform, database + ".dacpac"),
                    Path.Combine(root, platform, database + ".dacpac"));
            }

            return root;
        }

        private static void WriteAssetsFile(string projectPath, string packagesFolder, params (string Id, string Version)[] packages) =>
            WriteAssetsFile(projectPath, packagesFolder, directPackageIds: null, packages);

        /// <param name="directPackageIds">The packages the project references itself, or null to leave them out of the file.</param>
        private static void WriteAssetsFile(string projectPath, string packagesFolder, string[]? directPackageIds, params (string Id, string Version)[] packages)
        {
            string libraries = string.Join(",", packages.Select(p =>
                $"\"{p.Id}/{p.Version}\": {{ \"type\": \"package\", \"path\": \"{p.Id.ToLowerInvariant()}/{p.Version}\" }}"));
            string project = directPackageIds == null
                ? string.Empty
                : $", \"project\": {{ \"frameworks\": {{ \"netstandard2.1\": {{ \"dependencies\": {{ {string.Join(",", directPackageIds.Select(id => $"\"{id}\": {{ \"target\": \"Package\" }}"))} }} }} }} }}";
            string json = $"{{ \"version\": 3, \"libraries\": {{ {libraries} }}, \"packageFolders\": {{ \"{packagesFolder.Replace("\\", "\\\\")}\": {{}} }}{project} }}";
            string assetsPath = ProjectAssets.GetAssetsFilePath(Path.GetDirectoryName(projectPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(assetsPath)!);
            File.WriteAllText(assetsPath, json);
        }
    }
}
