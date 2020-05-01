//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DacFx.DatabaseProject
{
    public class DatabaseProjecTests
    {
        [Fact]
        public void ValidateGetDacPacPath()
        {
            string projectFile = "ValidateGetDacPacPath.sqlproj";
            string dacpacFile = "bin\\TestDacPacPath\\BuildSimpleProject.dacpac";

            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }

            if (File.Exists(dacpacFile))
            {
                File.Delete(dacpacFile);
            }
            try
            {
                // gives null if dacpac not present
                File.WriteAllText(projectFile, DatabaseProjectTestData.ValidProjectXml);
                Assert.Null(ProjectBuildOperation.GetOutputDacPac(projectFile));

                // gives dacpac path if dacpac present
                Directory.CreateDirectory("bin\\TestDacPacPath");
                File.WriteAllText(dacpacFile, "");
                Assert.Equal(Path.Combine(Environment.CurrentDirectory, dacpacFile), ProjectBuildOperation.GetOutputDacPac(projectFile));

                // gives null is invalid path in project
                File.WriteAllText(projectFile, DatabaseProjectTestData.InvalidProjectXml);
                Assert.Null(ProjectBuildOperation.GetOutputDacPac(projectFile));
            }
            finally
            {
                File.Delete(projectFile);
                File.Delete(dacpacFile);
            }
        }

        [Fact]
        public void ValidateExecutePathWithoutDotNet()
        {
            string projectFile = "ValidateGetDacPacPath.sqlproj";

            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
            }
            try
            {
                File.WriteAllText(projectFile, DatabaseProjectTestData.ValidProjectXml);

                ProjectBuildParams projectBuildParams = new ProjectBuildParams()
                {
                    SqlProjectPath = projectFile,
                    DotNetRootPath = "dummydotnetpath"
                };
                ProjectBuildOperation operation = new ProjectBuildOperation(projectBuildParams);
                try
                {
                    operation.Execute(ServiceLayer.TaskServices.TaskExecutionMode.Execute);
                    throw new Exception("Code shouold not reach here");
                }
                catch (Exception ex)
                {
                    Assert.Equal("Dotnet exe path is not valid. Ensure that .NET core SDK is installed.", ex.Message);
                }
            }
            finally
            {
                File.Delete(projectFile);
            }
        }
    }
}
