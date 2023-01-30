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

            Assert.IsFalse(requestMock.Result.Success);
            Assert.IsTrue(requestMock.Result.ErrorMessage!.Contains("Cannot create a new SQL project"));
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

            Assert.AreEqual(0, service.Projects.Count);

            // Validate creating SDK-style project
            MockRequest<ResultStatus> requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = sdkProjectUri,
                SqlProjectType = ProjectType.SdkStyle

            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(1, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri));
            Assert.AreEqual(service.Projects[sdkProjectUri].SqlProjStyle, ProjectType.SdkStyle);

            // Validate creating Legacy-style project
            requestMock = new();
            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = legacyProjectUri,
                SqlProjectType = ProjectType.LegacyStyle
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(2, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(legacyProjectUri));
            Assert.AreEqual(service.Projects[legacyProjectUri].SqlProjStyle, ProjectType.LegacyStyle);

            // Validate closing a project
            requestMock = new();
            await service.HandleCloseSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(1, service.Projects.Count);
            Assert.IsTrue(!service.Projects.ContainsKey(sdkProjectUri));

            // Validate opening a project
            requestMock = new();
            await service.HandleOpenSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(2, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri));
        }

        [Test]
        public async Task TestSqlObjectScriptAddDeleteExclude()
        {
            // Setup
            SqlProjectsService service = new();
            string projectUri = await service.CreateSqlProject();
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count);

            // Validate adding a SQL object script
            MockRequest<ResultStatus> requestMock = new();
            string scriptRelativePath = "MyTable.sql";
            string scriptFullPath = Path.Join(Path.GetDirectoryName(projectUri), scriptRelativePath);
            await File.WriteAllTextAsync(scriptFullPath, "CREATE TABLE [MyTable] ([Id] INT)");

            await service.HandleAddSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(1, service.Projects[projectUri].SqlObjectScripts.Count);
            Assert.IsTrue(service.Projects[projectUri].SqlObjectScripts.Contains(scriptRelativePath));

            // Validate excluding a SQL object script
            requestMock = new();
            await service.HandleExcludeSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count);
            Assert.IsTrue(File.Exists(scriptFullPath));

            // Re-add to set up for Delete
            requestMock = new();
            await service.HandleAddSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(1, service.Projects[projectUri].SqlObjectScripts.Count);

            // Validate deleting a SQL object script
            requestMock = new();
            await service.HandleDeleteSqlObjectScriptRequest(new SqlProjectScriptParams()
            {
                ProjectUri = projectUri,
                Path = scriptRelativePath
            }, requestMock.Object);

            Assert.IsTrue(requestMock.Result.Success);
            Assert.AreEqual(0, service.Projects[projectUri].SqlObjectScripts.Count);
            Assert.IsFalse(File.Exists(scriptFullPath));
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

            Assert.IsTrue(requestMock.Result.Success);

            return projectUri;
        }
    }
}
