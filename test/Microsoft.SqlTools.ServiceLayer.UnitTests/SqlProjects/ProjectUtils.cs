//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.Utility;
using CreateSqlProjectParams = Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts.CreateSqlProjectParams;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects
{
    public static class ProjectUtils
    {
        /// <summary>
        /// Gets a test project path in a temp directory
        /// </summary>
        /// <param name="projectName">Optional project name, defaults to a GUID</param>
        /// <returns>Full path to the .sqlproj file</returns>
        public static string GetTestProjectPath(string? projectName = null) 
        {
            string testName = projectName ?? Guid.NewGuid().ToString();
            string tempPath = Path.Combine(Path.GetTempPath(), "SqlProjectsTests", testName);
            Directory.CreateDirectory(tempPath);
            return Path.Combine(tempPath, $"{testName}.sqlproj");
        }

        /// <summary>
        /// Creates a SQL project using the SqlProjectsService
        /// </summary>
        /// <param name="projectType">SDK-style or Legacy-style project</param>
        /// <returns>URI of the created project</returns>
        public static async Task<string> CreateSqlProject(ProjectType projectType = ProjectType.SdkStyle)
        {
            SqlProjectsService service = new();
            string projectUri = GetTestProjectPath();

            MockRequest<ResultStatus> requestMock = new();
            await service.HandleCreateSqlProjectRequest(new CreateSqlProjectParams()
            {
                ProjectUri = projectUri,
                SqlProjectType = projectType
            }, requestMock.Object);

            if (!requestMock.Result.Success)
            {
                throw new Exception($"Failed to create SQL project: {requestMock.Result.ErrorMessage}");
            }

            return projectUri;
        }
    }
}
