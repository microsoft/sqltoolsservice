//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SqlProjects
{
    public class SqlProjectsServiceTests : TestBase
    {
        internal const string TEST_GUID = "BA5EBA11-C0DE-5EA7-ACED-BABB1E70A575";

        [Test]
        public async Task TestErrorDuringExecution()
        {
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject(); // validates result.Success == true

            // Validate that result indicates failure when there's an exception
            MockRequest<ResultStatus> requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = projectUri,
                SqlProjectType = ProjectType.SdkStyle

            }, requestMock.Object);

            Assert.IsFalse(requestMock.Result.Success, $"{nameof(service.HandleNewSqlProjectRequest)} when file already exists expected to fail");
            Assert.IsTrue(requestMock.Result.ErrorMessage!.Contains("Cannot create a new SQL project")
                       && requestMock.Result.ErrorMessage!.Contains("a file already exists at that location"),
                       $"Error message expected to mention that a file already exists, but instead was: '{requestMock.Result.ErrorMessage}'");
        }

        [Test]
        public async Task TestOpenCloseProject()
        {
            // Setup
            string sdkProjectUri = TestContext.CurrentContext.GetTestProjectPath(nameof(TestOpenCloseProject) + "Sdk");
            string legacyProjectUri = TestContext.CurrentContext.GetTestProjectPath(nameof(TestOpenCloseProject) + "Legacy");

            if (File.Exists(sdkProjectUri)) File.Delete(sdkProjectUri);
            if (File.Exists(legacyProjectUri)) File.Delete(legacyProjectUri);

            SqlProjectsService service = new();

            Assert.AreEqual(0, service.Projects.Count, "Baseline number of loaded projects");

            // Validate creating SDK-style project
            MockRequest<ResultStatus> requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = sdkProjectUri,
                SqlProjectType = ProjectType.SdkStyle
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleNewSqlProjectRequest), "SDK");
            Assert.AreEqual(1, service.Projects.Count, "Number of loaded projects after creating SDK not as expected");
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri), "Loaded project list expected to contain SDK project URI");
            Assert.AreEqual(ProjectType.SdkStyle, service.Projects[sdkProjectUri].SqlProjStyle, "SqlProj style expected to be SDK");

            // Validate creating Legacy-style project
            requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = legacyProjectUri,
                SqlProjectType = ProjectType.LegacyStyle
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleNewSqlProjectRequest), "Legacy");
            Assert.AreEqual(2, service.Projects.Count, "Number of loaded projects after creating Legacy");
            Assert.IsTrue(service.Projects.ContainsKey(legacyProjectUri), "Loaded project list expected to contain Legacy project URI");
            Assert.AreEqual(service.Projects[legacyProjectUri].SqlProjStyle, ProjectType.LegacyStyle, "SqlProj style");

            // Validate closing a project
            requestMock = new();
            await service.HandleCloseSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleCloseSqlProjectRequest));
            Assert.AreEqual(1, service.Projects.Count, "Number of loaded projects after closing SDK project");
            Assert.IsTrue(!service.Projects.ContainsKey(sdkProjectUri), "Loaded project list should not contain SDK after closing");

            // Validate opening a project
            requestMock = new();
            await service.HandleOpenSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleOpenSqlProjectRequest));
            Assert.AreEqual(2, service.Projects.Count, "Number of loaded projects after re-opening");
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri), "Loaded project list expected to contain SDK project URI after re-opening");
        }

        [Test]
        public async Task TestSqlObjectScriptAddDeleteExclude()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count, "Baseline number of SqlObjectScripts");

            // Validate adding a SQL object script
            MockRequest<ResultStatus> requestMock = new();
            string scriptRelativePath = "MyTable.sql";
            string scriptFullPath = Path.Join(Path.GetDirectoryName(projectUri), scriptRelativePath);
            await File.WriteAllTextAsync(scriptFullPath, "CREATE TABLE [MyTable] ([Id] INT)");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to be on disk");

            await service.HandleAddSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlObjectScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].SqlObjectScripts.Count, "SqlObjectScripts count after add");
            Assert.IsTrue(service.Projects[projectUri].SqlObjectScripts.Contains(scriptRelativePath), $"SqlObjectScripts expected to contain {scriptRelativePath}");

            // Validate excluding a SQL object script
            requestMock = new();
            await service.HandleExcludeSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleExcludeSqlObjectScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count, "SqlObjectScripts count after exclude");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to still exist on disk");

            // Re-add to set up for Delete
            requestMock = new();
            await service.HandleAddSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlObjectScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].SqlObjectScripts.Count, "SqlObjectScripts count after re-add");

            // Validate deleting a SQL object script
            requestMock = new();
            await service.HandleDeleteSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeleteSqlObjectScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count, "SqlObjectScripts count after delete");
            Assert.IsFalse(File.Exists(scriptFullPath), $"{scriptFullPath} expected to have been deleted from disk");
        }

        [Test]
        public async Task TestPreDeploymentScriptAddDeleteExclude()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();
            Assert.AreEqual(0, service.Projects[projectUri].PreDeployScripts.Count, "Baseline number of Pre-deployment scripts");

            // Validate adding a pre-deployment script
            MockRequest<ResultStatus> requestMock = new();
            string scriptRelativePath = "PreDeploymentScript.sql";
            string scriptFullPath = Path.Join(Path.GetDirectoryName(projectUri), scriptRelativePath);
            await File.WriteAllTextAsync(scriptFullPath, "SELECT 'Deployment starting...'");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to be on disk");

            await service.HandleAddPreDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddPreDeploymentScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].PreDeployScripts.Count, "PreDeployScript count after add");
            Assert.IsTrue(service.Projects[projectUri].PreDeployScripts.Contains(scriptRelativePath), $"PreDeployScripts expected to contain {scriptRelativePath}");

            // Validate excluding a pre-deployment script
            requestMock = new();
            await service.HandleExcludePreDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleExcludePreDeploymentScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].PreDeployScripts.Count, "PreDeployScripts count after exclude");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to still exist on disk");

            // Re-add to set up for Delete
            requestMock = new();
            await service.HandleAddPreDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddPreDeploymentScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].PreDeployScripts    .Count, "PreDeployScripts count after re-add");

            // Validate deleting a pre-deployment script
            requestMock = new();
            await service.HandleDeletePreDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeletePreDeploymentScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].PreDeployScripts.Count, "PreDeployScripts count after delete");
            Assert.IsFalse(File.Exists(scriptFullPath), $"{scriptFullPath} expected to have been deleted from disk");
        }

        [Test]
        public async Task TestPostDeploymentScriptAddDeleteExclude()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();
            Assert.AreEqual(0, service.Projects[projectUri].PostDeployScripts.Count, "Baseline number of Post-deployment scripts");

            // Validate adding a Post-deployment script
            MockRequest<ResultStatus> requestMock = new();
            string scriptRelativePath = "PostDeploymentScript.sql";
            string scriptFullPath = Path.Join(Path.GetDirectoryName(projectUri), scriptRelativePath);
            await File.WriteAllTextAsync(scriptFullPath, "SELECT 'Deployment finished!'");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to be on disk");

            await service.HandleAddPostDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddPostDeploymentScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].PostDeployScripts.Count, "PostDeployScript count after add");
            Assert.IsTrue(service.Projects[projectUri].PostDeployScripts.Contains(scriptRelativePath), $"PostDeployScripts expected to contain {scriptRelativePath}");

            // Validate excluding a Post-deployment script
            requestMock = new();
            await service.HandleExcludePostDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleExcludePostDeploymentScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].PostDeployScripts.Count, "PostDeployScripts count after exclude");
            Assert.IsTrue(File.Exists(scriptFullPath), $"{scriptFullPath} expected to still exist on disk");

            // Re-add to set up for Delete
            requestMock = new();
            await service.HandleAddPostDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddPostDeploymentScriptRequest));
            Assert.AreEqual(1, service.Projects[projectUri].PostDeployScripts.Count, "PostDeployScripts count after re-add");

            // Validate deleting a Post-deployment script
            requestMock = new();
            await service.HandleDeletePostDeploymentScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeletePostDeploymentScriptRequest));
            Assert.AreEqual(0, service.Projects[projectUri].PostDeployScripts.Count, "PostDeployScripts count after delete");
            Assert.IsFalse(File.Exists(scriptFullPath), $"{scriptFullPath} expected to have been deleted from disk");
        }

        #region Database reference tests

        [Test]
        public async Task TestDatabaseReferenceDelete()
        {
            var (service, projectUri, _, _) = await SetUpDatabaseReferenceTest();

            // directly add a reference so there's something to delete
            service.Projects[projectUri].DatabaseReferences.Add(new SystemDatabaseReference(SystemDatabase.Master, suppressMissingDependencies: true));

            Assert.AreEqual(1, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding reference");

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleDeleteDatabaseReferenceRequest(new DeleteDatabaseReferenceParams()
            {
                ProjectUri = projectUri,
                Name = SystemDatabase.Master.ToString()
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeleteDatabaseReferenceRequest));
            Assert.AreEqual(0, service.Projects[projectUri].DatabaseReferences.Count, "Database references after deleting reference");
        }

        [Test]
        public async Task TestSystemDatabaseReferenceAdd()
        {
            var (service, projectUri, _, _) = await SetUpDatabaseReferenceTest();

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleAddSystemDatabaseReferenceRequest(new AddSystemDatabaseReferenceParams()
            {
                ProjectUri = projectUri,
                SystemDatabase = SystemDatabase.MSDB,
                DatabaseLiteral = "EmEssDeeBee",
                SuppressMissingDependencies = false
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSystemDatabaseReferenceRequest));
            Assert.AreEqual(1, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding system db reference");
            SystemDatabaseReference systemDbRef = (SystemDatabaseReference)service.Projects[projectUri].DatabaseReferences.First(x => x is SystemDatabaseReference);
            Assert.AreEqual(SystemDatabase.MSDB, systemDbRef.SystemDb, "Referenced system DB");
            Assert.AreEqual("EmEssDeeBee", systemDbRef.DatabaseVariableLiteralName);
            Assert.IsFalse(systemDbRef.SuppressMissingDependencies, nameof(systemDbRef.SuppressMissingDependencies));
        }

        [Test]
        public async Task TestDacpacReferenceAdd()
        {
            var (service, projectUri, databaseVar, serverVar) = await SetUpDatabaseReferenceTest();

            // Validate adding a dacpac reference on the same server
            string mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "OtherDatabaseSameServer.dacpac");

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleAddDacpacReferenceRequest(new AddDacpacReferenceParams()
            {
                ProjectUri = projectUri,
                DacpacPath = mockReferencePath,
                SuppressMissingDependencies = false
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddDacpacReferenceRequest), "same server");
            Assert.AreEqual(1, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding dacpac reference (same server)");
            DacpacReference dacpacRef = (DacpacReference)service.Projects[projectUri].DatabaseReferences.Get(mockReferencePath);
            Assert.AreEqual(FileUtils.NormalizePath(mockReferencePath, PlatformID.Win32NT), dacpacRef.DacpacPath, "Referenced dacpac");
            Assert.IsFalse(dacpacRef.SuppressMissingDependencies, nameof(dacpacRef.SuppressMissingDependencies));
            Assert.IsNull(dacpacRef.DatabaseVariableLiteralName, nameof(dacpacRef.DatabaseVariableLiteralName));
            Assert.IsNull(dacpacRef.DatabaseVariable, nameof(dacpacRef.DatabaseVariable));

            // Validate adding a dacpac reference via SQLCMD variable
            mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "OtherDatabaseSqlCmd.dacpac");

            requestMock = new();
            await service.HandleAddDacpacReferenceRequest(new AddDacpacReferenceParams()
            {
                ProjectUri = projectUri,
                DacpacPath = mockReferencePath,
                SuppressMissingDependencies = false,
                DatabaseVariable = databaseVar.Name,
                ServerVariable = serverVar.Name
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddDacpacReferenceRequest), "sqlcmdvar");
            Assert.AreEqual(2, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding dacpac reference (sqlcmdvar)");
            dacpacRef = (DacpacReference)service.Projects[projectUri].DatabaseReferences.Get(mockReferencePath);
            Assert.AreEqual(FileUtils.NormalizePath(mockReferencePath, PlatformID.Win32NT), dacpacRef.DacpacPath, "Referenced dacpac");
            Assert.AreEqual(databaseVar.Name, dacpacRef.DatabaseVariable!.VarName);
            Assert.AreEqual(serverVar.Name, dacpacRef.ServerVariable!.VarName);
            Assert.IsFalse(dacpacRef.SuppressMissingDependencies, nameof(dacpacRef.SuppressMissingDependencies));

            // Validate adding a dacpac reference via database literal
            mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "OtherDatabaseLiteral.dacpac");

            requestMock = new();
            await service.HandleAddDacpacReferenceRequest(new AddDacpacReferenceParams()
            {
                ProjectUri = projectUri,
                DacpacPath = mockReferencePath,
                SuppressMissingDependencies = false,
                DatabaseLiteral = "DacpacLiteral"
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddDacpacReferenceRequest), "db literal");
            Assert.AreEqual(3, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding dacpac reference (db literal)");
            dacpacRef = (DacpacReference)service.Projects[projectUri].DatabaseReferences.Get(mockReferencePath);
            Assert.AreEqual(FileUtils.NormalizePath(mockReferencePath, PlatformID.Win32NT), dacpacRef.DacpacPath, "Referenced dacpac");
            Assert.AreEqual("DacpacLiteral", dacpacRef.DatabaseVariableLiteralName, nameof(dacpacRef.DatabaseVariableLiteralName));
            Assert.IsFalse(dacpacRef.SuppressMissingDependencies, nameof(dacpacRef.SuppressMissingDependencies));
        }

        [Test]
        public async Task TestSqlProjectReferenceAdd()
        {
            var (service, projectUri, databaseVar, serverVar) = await SetUpDatabaseReferenceTest();

            // Validate adding a project reference on the same server
            string mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "..", "SameDatabase", "SameDatabaseSqlCmd.sqlproj");

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleAddSqlProjectReferenceRequest(new AddSqlProjectReferenceParams()
            {
                ProjectUri = projectUri,
                ProjectPath = mockReferencePath,
                SuppressMissingDependencies = false
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlProjectReferenceRequest), "same server");
            Assert.AreEqual(1, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding SQL project reference (same server)");
            SqlProjectReference projectRef = (SqlProjectReference)service.Projects[projectUri].DatabaseReferences.Get(mockReferencePath);
            Assert.AreEqual(FileUtils.NormalizePath(mockReferencePath, PlatformID.Win32NT), projectRef.ProjectPath, "Referenced project");
            Assert.IsFalse(projectRef.SuppressMissingDependencies, nameof(projectRef.SuppressMissingDependencies));
            Assert.IsNull(projectRef.DatabaseVariableLiteralName, nameof(projectRef.DatabaseVariableLiteralName));
            Assert.IsNull(projectRef.DatabaseVariable, nameof(projectRef.DatabaseVariable));

            // Validate adding a project reference via SQLCMD variable
            mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "..", "OtherDatabase", "OtherDatabaseSqlCmd.sqlproj");

            requestMock = new();
            await service.HandleAddSqlProjectReferenceRequest(new AddSqlProjectReferenceParams()
            {
                ProjectUri = projectUri,
                ProjectPath = mockReferencePath,
                ProjectGuid = TEST_GUID,
                SuppressMissingDependencies = false,
                DatabaseVariable = databaseVar.Name,
                ServerVariable = serverVar.Name
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlProjectReferenceRequest));
            Assert.AreEqual(2, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding SQL project reference (sqlcmdvar)");
            projectRef = (SqlProjectReference)service.Projects[projectUri].DatabaseReferences.First(x => x is SqlProjectReference);
            Assert.AreEqual(mockReferencePath, projectRef.ProjectPath, "Referenced project");
            Assert.AreEqual(TEST_GUID, projectRef.ProjectGuid, "Referenced project GUID");
            Assert.AreEqual(databaseVar.Name, projectRef.DatabaseVariable!.VarName);
            Assert.AreEqual(serverVar.Name, projectRef.ServerVariable!.VarName);
            Assert.IsFalse(projectRef.SuppressMissingDependencies, nameof(projectRef.SuppressMissingDependencies));

            // Validate adding a project reference via database literal
            mockReferencePath = Path.Join(Path.GetDirectoryName(projectUri), "..", "OtherDatabase", "OtherDatabaseLiteral.sqlproj");

            requestMock = new();
            await service.HandleAddSqlProjectReferenceRequest(new AddSqlProjectReferenceParams()
            {
                ProjectUri = projectUri,
                ProjectPath = mockReferencePath,
                ProjectGuid = TEST_GUID,
                SuppressMissingDependencies = false,
                DatabaseLiteral = "ProjectLiteral"
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlProjectReferenceRequest));
            Assert.AreEqual(3, service.Projects[projectUri].DatabaseReferences.Count, "Database references after adding SQL project reference (db literal)");
            projectRef = (SqlProjectReference)service.Projects[projectUri].DatabaseReferences.First(x => x is SqlProjectReference);
            Assert.AreEqual(mockReferencePath, projectRef.ProjectPath, "Referenced project");
            Assert.AreEqual(TEST_GUID, projectRef.ProjectGuid, "Referenced project GUID");
            Assert.AreEqual("ProjectLiteral", projectRef.DatabaseVariableLiteralName);
            Assert.IsFalse(projectRef.SuppressMissingDependencies, nameof(projectRef.SuppressMissingDependencies));
        }

        #endregion

        [Test]
        public async Task TestFolderAddDelete()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();
            Assert.AreEqual(0, service.Projects[projectUri].Folders.Count, "Baseline number of folders");

            // Validate adding a folder
            MockRequest<ResultStatus> requestMock = new();
            FolderParams folderParams = new FolderParams()
            {
                ProjectUri = projectUri,
                Path = "TestFolder"
            };

            await service.HandleAddFolderRequest(folderParams, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddFolderRequest));
            Assert.AreEqual(1, service.Projects[projectUri].Folders.Count, "Folder count after add");
            Assert.IsTrue(Directory.Exists(Path.Join(Path.GetDirectoryName(projectUri), folderParams.Path)), $"Subfolder '{folderParams.Path}' expected to exist on disk");
            Assert.IsTrue(service.Projects[projectUri].Folders.Contains(folderParams.Path), $"SqlObjectScripts expected to contain {folderParams.Path}");

            // Validate deleting a folder
            requestMock = new();
            await service.HandleDeleteFolderRequest(folderParams, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeleteFolderRequest));
            Assert.AreEqual(0, service.Projects[projectUri].Folders.Count, "Folder count after delete");
        }

        [Test]
        public async Task TestSqlCmdVariablesAddDelete()
        {
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();

            Assert.AreEqual(0, service.Projects[projectUri].SqlCmdVariables.Count, "Baseline number of SQLCMD variables not as expected");

            // Validate adding a SQLCMD variable
            MockRequest<ResultStatus> requestMock = new();

            const string variableName = "TestVarName";

            await service.HandleAddSqlCmdVariableRequest(new AddSqlCmdVariableParams()
            {
                ProjectUri = projectUri,
                Name = variableName,
                DefaultValue = "$(TestVarDefaultValue)",
                Value = "$(TestVarValue)"
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleAddSqlCmdVariableRequest));
            Assert.AreEqual(1, service.Projects[projectUri].SqlCmdVariables.Count, "Number of SQLCMD variables after addition not as expected");
            Assert.IsTrue(service.Projects[projectUri].SqlCmdVariables.Contains(variableName), $"List of SQLCMD variables expected to contain {variableName}");

            // Validate updating a SQLCMD variable
            const string updatedDefaultValue = "$(UpdatedDefaultValue)";
            const string updatedValue = "$(UpdatedValue)";

            requestMock = new();
            await service.HandleUpdateSqlCmdVariableRequest(new AddSqlCmdVariableParams()
            {
                ProjectUri = projectUri,
                Name = variableName,
                DefaultValue = updatedDefaultValue,
                Value = updatedValue
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleUpdateSqlCmdVariableRequest));
            Assert.AreEqual(1, service.Projects[projectUri].SqlCmdVariables.Count, "Number of SQLCMD variables after update not as expected");
            Assert.AreEqual(updatedDefaultValue, service.Projects[projectUri].SqlCmdVariables.First().DefaultValue, "Updated default value");
            Assert.AreEqual(updatedValue, service.Projects[projectUri].SqlCmdVariables.First().Value, "Updated value");

            // Validate deleting a SQLCMD variable
            requestMock = new();
            await service.HandleDeleteSqlCmdVariableRequest(new DeleteSqlCmdVariableParams()
            {
                ProjectUri = projectUri,
                Name = variableName,
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleDeleteSqlCmdVariableRequest));
            Assert.AreEqual(0, service.Projects[projectUri].SqlCmdVariables.Count, "Number of SQLCMD variables after deletion not as expected");
        }

        [Test]
        public async Task TestCrossPlatformUpdates()
        {
            string inputProjectPath = Path.Join(Path.GetDirectoryName(typeof(SqlProjectsServiceTests).Assembly.Location), "SqlProjects", "Inputs", "SSDTProject.sqlproj");
            string projectPath = Path.Join(TestContext.CurrentContext.GetTestWorkingFolder(), "SSDTProject.sqlproj");

            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            File.Copy(inputProjectPath, projectPath);
            SqlProjectsService service = new();

            /// Validate that the cross-platform status can be fetched
            MockRequest<GetCrossPlatformCompatiblityResult> getRequestMock = new();
            await service.HandleGetCrossPlatformCompatibilityRequest(new SqlProjectParams()
            {
                ProjectUri = projectPath
            }, getRequestMock.Object);

            getRequestMock.AssertSuccess(nameof(service.HandleGetCrossPlatformCompatibilityRequest));
            Assert.IsFalse(getRequestMock.Result.IsCrossPlatformCompatible, "Input file should not be cross-platform compatible before conversion");

            // Validate that the project can be updated
            MockRequest<ResultStatus> updateRequestMock = new();
            await service.HandleUpdateProjectForCrossPlatformRequest(new SqlProjectParams()
            {
                ProjectUri = projectPath,
            }, updateRequestMock.Object);

            updateRequestMock.AssertSuccess(nameof(service.HandleUpdateProjectForCrossPlatformRequest));

            // Validate that the cross-platform status has changed
            getRequestMock = new();
            await service.HandleGetCrossPlatformCompatibilityRequest(new SqlProjectParams()
            {
                ProjectUri = projectPath
            }, getRequestMock.Object);

            getRequestMock.AssertSuccess(nameof(service.HandleGetCrossPlatformCompatibilityRequest));
            Assert.IsTrue(((GetCrossPlatformCompatiblityResult)getRequestMock.Result).IsCrossPlatformCompatible, "Input file should be cross-platform compatible after conversion");
        }

        #region Helpers

        private async Task<(SqlProjectsService Service, string ProjectUri, SqlCmdVariable DatabaseVar, SqlCmdVariable ServerVar)> SetUpDatabaseReferenceTest()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();

            SqlCmdVariable databaseVar = new SqlCmdVariable("$(OtherDb)", "OtherDbDefaultValue", "OtherDbValue");
            SqlCmdVariable serverVar = new SqlCmdVariable("$(OtherServer)", "OtherServerDefaultValue", "OtherServerValue");

            service.Projects[projectUri].SqlCmdVariables.Add(databaseVar);
            service.Projects[projectUri].SqlCmdVariables.Add(serverVar);

            Assert.AreEqual(0, service.Projects[projectUri].DatabaseReferences.Count, "Baseline number of database references");

            return (service, projectUri, databaseVar, serverVar);
        }

        #endregion
    }

    internal static class SqlProjectsExtensions
    {
        /// <summary>
        /// Uses the service to create a new SQL project
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public async static Task<string> CreateSqlProject(this SqlProjectsService service)
        {
            string projectUri = TestContext.CurrentContext.GetTestProjectPath();

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = projectUri,
                SqlProjectType = ProjectType.SdkStyle

            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(CreateSqlProject));

            return projectUri;
        }
    }
}
