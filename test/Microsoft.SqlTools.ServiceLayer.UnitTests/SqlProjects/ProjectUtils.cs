//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using Microsoft.SqlServer.Dac.Projects;

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
        /// Creates a simple test SQL project synchronously
        /// </summary>
        /// <param name="projectName">Optional unique name for the project. If null, uses test name with timestamp</param>
        /// <returns>Path to the created .sqlproj file</returns>
        public static string CreateTestProject(string? projectName = null)
        {
            // Use test name + timestamp for uniqueness if no name provided
            string uniqueName = projectName ?? $"{NUnit.Framework.TestContext.CurrentContext.Test.Name}_{DateTime.Now:yyyyMMddHHmmssfff}";
            string projectPath = GetTestProjectPath(uniqueName);
            
            // Delete any existing project first to handle reruns
            DeleteTestProject(projectPath);
            
            _ = SqlProject.CreateProjectAsync(projectPath).GetAwaiter().GetResult();
            return projectPath;
        }

        /// <summary>
        /// Deletes a test project and its directory
        /// </summary>
        /// <param name="projectPath">Path to the .sqlproj file</param>
        public static void DeleteTestProject(string projectPath)
        {
            string? projectDir = Path.GetDirectoryName(projectPath);
            if (projectDir != null && Directory.Exists(projectDir))
            {
                Directory.Delete(projectDir, recursive: true);
            }
        }
    }
}
