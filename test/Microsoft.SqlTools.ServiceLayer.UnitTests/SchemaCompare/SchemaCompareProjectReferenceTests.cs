//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaCompare
{
    /// <summary>
    /// Schema Compare of SQL projects with database references and SQLCMD variables, resolved from the .sqlproj by STS.
    /// </summary>
    public class SchemaCompareProjectReferenceTests
    {
        private string workingDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            workingDirectory = Path.Combine(Path.GetTempPath(), "SchemaCompareProjectReferenceTests", Guid.NewGuid().ToString("N"));
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
        /// microsoft/vscode-mssql#21080: [$(OtherDatabase)] in the project compares equal to the variable's value in the target.
        /// </summary>
        [Test]
        public void ProjectWithDatabaseVariableReference_ComparesEqualToTheVariablesValue()
        {
            string otherDbDacpac = Path.Combine(workingDirectory, "OtherDb.dacpac");
            SaveDacpac(otherDbDacpac, "CREATE TABLE [dbo].[OtherTable] ([Id] INT NOT NULL);");

            // The target names the other database directly.
            string targetPath = CreateProject("Target");
            SqlProject.OpenProject(targetPath).DatabaseReferences.Add(
                new DacpacReference(otherDbDacpac, suppressMissingDependencies: false, "OtherDb"));
            string targetViewPath = WriteScript(targetPath, "CrossDatabaseView.sql",
                "CREATE VIEW [dbo].[CrossDatabaseView] AS SELECT [Id] FROM [OtherDb].[dbo].[OtherTable];");

            string projectPath = CreateProject("Project");
            SqlProject project = SqlProject.OpenProject(projectPath);
            project.SqlCmdVariables.Add(new SqlCmdVariable("OtherDatabase", "OtherDb"));
            project.DatabaseReferences.Add(new DacpacReference(otherDbDacpac, suppressMissingDependencies: false, project.SqlCmdVariables.Get("OtherDatabase")));
            string viewPath = WriteScript(projectPath, "CrossDatabaseView.sql",
                "CREATE VIEW [dbo].[CrossDatabaseView] AS SELECT [Id] FROM [$(OtherDatabase)].[dbo].[OtherTable];");

            SchemaCompareParams parameters = CreateParameters(projectPath, viewPath, targetPath, targetViewPath);

            using var withoutReferences = new SchemaCompareOperation(parameters, new ConnectionOnlyProvider());
            withoutReferences.Execute();
            Assert.IsTrue(withoutReferences.ComparisonResult.Differences.Any(),
                "Control: without the project's references and values the view definitions differ.");

            using var withReferences = new SchemaCompareOperation(parameters, new VsCodeConnectionProvider(null));
            withReferences.Execute();
            Assert.IsNull(withReferences.ErrorMessage, withReferences.ErrorMessage);
            Assert.IsTrue(withReferences.ComparisonResult.IsValid);
            Assert.AreEqual(0, withReferences.ComparisonResult.Differences.Count(),
                string.Join(", ", withReferences.ComparisonResult.Differences.Select(d => d.Name)));
        }

        [Test]
        public void ProjectWithProjectReference_ResolvesObjectsFromTheReferencedProjectsScripts()
        {
            const string tableScript = "CREATE TABLE [dbo].[ReferencedTable] ([Id] INT NOT NULL);";
            const string viewScript = "CREATE VIEW [dbo].[ReferencingView] AS SELECT [Id] FROM [dbo].[ReferencedTable];";
            // SDK-style projects include every .sql file under the project folder.
            string referencedPath = CreateProject("Referenced");
            WriteScript(referencedPath, "ReferencedTable.sql", tableScript);

            // The target gets the same table from a built dacpac instead of the referenced project.
            string referencedDacpac = Path.Combine(workingDirectory, "Referenced.dacpac");
            SaveDacpac(referencedDacpac, tableScript);
            string targetPath = CreateProject("Target");
            SqlProject.OpenProject(targetPath).DatabaseReferences.Add(
                new DacpacReference(referencedDacpac, suppressMissingDependencies: false));
            string targetViewPath = WriteScript(targetPath, "ReferencingView.sql", viewScript);

            string projectPath = CreateProject("Project");
            SqlProject.OpenProject(projectPath).DatabaseReferences.Add(
                new SqlProjectReference(referencedPath, projectGuid: null, suppressMissingDependencies: false));
            string viewPath = WriteScript(projectPath, "ReferencingView.sql", viewScript);

            using var operation = new SchemaCompareOperation(
                CreateParameters(projectPath, viewPath, targetPath, targetViewPath), new VsCodeConnectionProvider(null));
            operation.Execute();

            Assert.IsNull(operation.ErrorMessage, operation.ErrorMessage);
            Assert.IsTrue(operation.ComparisonResult.IsValid);
            Assert.IsFalse(operation.ComparisonResult.GetErrors().Any(),
                string.Join("; ", operation.ComparisonResult.GetErrors().Select(e => e.Message)));
            Assert.AreEqual(0, operation.ComparisonResult.Differences.Count(),
                string.Join(", ", operation.ComparisonResult.Differences.Select(d => d.Name)));
        }

        private static SchemaCompareParams CreateParameters(string projectPath, string scriptPath, string targetPath, string targetScriptPath) => new()
        {
            OperationId = Guid.NewGuid().ToString(),
            SourceEndpointInfo = CreateProjectEndpoint(projectPath, scriptPath),
            TargetEndpointInfo = CreateProjectEndpoint(targetPath, targetScriptPath),
        };

        private static SchemaCompareEndpointInfo CreateProjectEndpoint(string projectPath, string scriptPath) => new()
        {
            EndpointType = SchemaCompareEndpointType.Project,
            ProjectFilePath = projectPath,
            TargetScripts = new[] { scriptPath },
            DataSchemaProvider = "160",
        };

        private string CreateProject(string name)
        {
            string projectPath = Path.Combine(workingDirectory, name, name + ".sqlproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            _ = SqlProject.CreateProjectAsync(projectPath).GetAwaiter().GetResult();
            return projectPath;
        }

        private static string WriteScript(string projectPath, string fileName, string script)
        {
            string scriptPath = Path.Combine(Path.GetDirectoryName(projectPath)!, fileName);
            File.WriteAllText(scriptPath, script);
            return scriptPath;
        }

        private static void SaveDacpac(string dacpacPath, string script)
        {
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            model.AddObjects(script);
            DacPackageExtensions.BuildPackage(dacpacPath, model, new PackageMetadata());
        }

        /// <summary>A host that can connect to databases but does not resolve project references.</summary>
        private sealed class ConnectionOnlyProvider : ISchemaCompareConnectionProvider
        {
            public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo) => null!;

            public IUniversalAuthProvider GetAuthProvider(SchemaCompareEndpointInfo endpointInfo) => null!;

            public SchemaCompareEndpointInfo ParseConnectionString(string connectionString) => new();
        }
    }
}
