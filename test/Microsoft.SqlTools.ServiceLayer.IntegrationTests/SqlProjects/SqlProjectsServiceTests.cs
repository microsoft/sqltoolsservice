//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SqlProjects
{
    public class SqlProjectsServiceTests : TestBase
    {
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
                DefaultValue = "TestVarDefaultValue",
                Value = "TestVarValue"
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success, "Successful add request result expected");
            Assert.AreEqual(1, service.Projects[projectUri].SqlCmdVariables.Count, "Number of SQLCMD variables after addition not as expected");
            Assert.IsTrue(service.Projects[projectUri].SqlCmdVariables.Contains(variableName), $"List of SQLCMD variables expected to contain {variableName}");

            // Validate updating a SQLCMD variable
            const string updatedDefaultValue = "UpdatedDefaultValue";
            const string updatedValue = "UpdatedValue";

            requestMock = new();
            await service.HandleUpdateSqlCmdVariableRequest(new AddSqlCmdVariableParams()
            {
                ProjectUri = projectUri,
                Name = variableName,
                DefaultValue = updatedDefaultValue,
                Value = updatedValue
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success, "Successful update request result expected");
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

            Assert.IsTrue(requestMock.Result.Success, "Successful delete request result expected");
            Assert.AreEqual(0, service.Projects[projectUri].SqlCmdVariables.Count, "Number of SQLCMD variables after deletion not as expected");
        }
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

            Assert.IsTrue(requestMock.Result.Success, $"Failed to create project: {requestMock.Result.ErrorMessage}");

            return projectUri;
        }
    }
}
