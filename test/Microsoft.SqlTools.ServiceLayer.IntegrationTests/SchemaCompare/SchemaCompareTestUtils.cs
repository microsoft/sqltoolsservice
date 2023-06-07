//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using System;
using System.IO;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    internal static class SchemaCompareTestUtils
    {
        private static string sqlProjectsFolder = Path.Combine("..", "..", "..", "SchemaCompare", "SqlProjects");

        internal static void VerifyAndCleanup(string? path)
        {
            if (path == null)
            {
                return;
            }

            // verify it was created...
            Assert.True(File.Exists(path) || Directory.Exists(path), $"File or directory {path} was expected to exist but did not");

            FileAttributes attr = File.GetAttributes(path);

            // ...then clean it up
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                new DirectoryInfo(path).Delete(recursive: true);
            }
            else
            {
                File.Delete(path);
            }
        }

        internal static string CreateDacpac(SqlTestDb testdb)
        {
            var result = GetLiveAutoCompleteTestObjects();
            string folderPath = Path.Combine(Path.GetTempPath(), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new ExtractParams
            {
                DatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = "1.0.0.0"
            };

            DacFxService service = new DacFxService();
            ExtractOperation operation = new ExtractOperation(extractParams, result.ConnectionInfo);
            service.PerformOperation(operation, TaskExecutionMode.Execute);

            return extractParams.PackageFilePath;
        }

        /// <summary>
        /// Creates an SDK-style .sqlproj from the database
        /// </summary>
        /// <param name="testdb">Database to create the sql project from</param>
        /// <param name="projectName">Name of the project</param>
        /// <returns>Full path to the project folder</returns>
        internal static string CreateProject(SqlTestDb testdb, string projectName)
        {
            var result = GetLiveAutoCompleteTestObjects();
            string sqlprojFilePath = CreateSqlProj(projectName);
            string folderPath = Path.GetDirectoryName(sqlprojFilePath);

            var extractParams = new ExtractParams
            {
                DatabaseName = testdb.DatabaseName,
                ExtractTarget = DacExtractTarget.Flat,
                PackageFilePath = folderPath,
                ApplicationName = "test",
                ApplicationVersion = "1.0.0.0"
            };

            DacFxService service = new();
            ExtractOperation operation = new(extractParams, result.ConnectionInfo);
            service.PerformOperation(operation, TaskExecutionMode.Execute);

            return folderPath;
        }

        /// <summary>
        /// Creates an empty SDK-style .sqlproj
        /// </summary>
        /// <param name="projectName">name for the .sqlproj</param>
        /// <returns>Full path to the .sqlproj</returns>
        internal static string CreateSqlProj(string projectName)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest", $"{TestContext.CurrentContext?.Test?.Name}_{projectName}_{DateTime.Now.Ticks.ToString()}");
            Directory.CreateDirectory(folderPath);
            string sqlprojFilePath = Path.Combine(folderPath, projectName + ".sqlproj");
            File.Copy(Path.Combine(sqlProjectsFolder, "emptyTemplate.sqlproj"), sqlprojFilePath);

            return sqlprojFilePath;
        }

        internal static string[] GetProjectScripts(string projectPath)
        {
            return Directory.GetFiles(projectPath, "*.sql", SearchOption.AllDirectories);
        }

        internal static string CreateScmpPath()
        {
            var result = GetLiveAutoCompleteTestObjects();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);
            string fileName = TestContext.CurrentContext?.Test?.Name + "_" + DateTime.Now.Ticks.ToString();

            string path = Path.Combine(folderPath, string.Format("{0}.scmp", fileName));

            return path;
        }

        internal static LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            // Adding retry for reliability - otherwise it caused test to fail in lab
            TestConnectionResult result = null;
            int retry = 3;

            while (retry > 0)
            {
                result = LiveConnectionHelper.InitLiveConnectionInfo();
                if (result != null && result.ConnectionInfo != null)
                {
                    return result;
                }
                System.Threading.Thread.Sleep(1000);
                retry--;
            }

            return result;
        }

        internal static void CompareOptions(DeploymentOptions deploymentOptions, DacDeployOptions dacDeployOptions)
        {
            System.Reflection.PropertyInfo[] deploymentOptionsProperties = deploymentOptions.GetType().GetProperties();
            System.Reflection.PropertyInfo[] dacDeployProperties = dacDeployOptions.GetType().GetProperties();

            // Note that DatabaseSpecification and sql cmd variables list is not present in Sqltools service - its not settable with checkbox and is not used by ADS options.
            // They are not present in SSDT as well
            // TODO : update this test if the above options are added later
            // TODO: update with new options. Tracking issue: https://github.com/microsoft/azuredatastudio/issues/15336
            //Assert.True(deploymentOptionsProperties.Length == dacDeployProperties.Length - 2, $"Number of properties is not same Deployment options : {deploymentOptionsProperties.Length} DacFx options : {dacDeployProperties.Length}");

            foreach (PropertyInfo deployOptionsProp in deploymentOptionsProperties)
            {
                if (deployOptionsProp.Name != nameof(DeploymentOptions.BooleanOptionsDictionary))
                {
                    var dacProp = dacDeployOptions.GetType().GetProperty(deployOptionsProp.Name);
                    Assert.That(dacProp, Is.Not.Null, $"DacDeploy property not present for {deployOptionsProp.Name}");

                    var defaultP = deployOptionsProp.GetValue(deploymentOptions);
                    var defaultPValue = defaultP != null ? defaultP.GetType().GetProperty("Value").GetValue(defaultP) : defaultP;
                    var actualPValue = dacProp.GetValue(dacDeployOptions);

                    if (deployOptionsProp.Name != nameof(DeploymentOptions.ExcludeObjectTypes)) // do not compare for ExcludeObjectTypes because it will be different
                    {
                        // Verifying expected and actual deployment options properties are equal
                        Assert.True((defaultPValue == null && String.IsNullOrEmpty(actualPValue as string))
                            || (defaultPValue).Equals(actualPValue)
                        , $"DacFx DacDeploy property not equal to Tools Service DeploymentOptions for {deployOptionsProp.Name}, Actual value: {actualPValue} and Default value: {defaultPValue}");
                    }
                }
            }

            // Verify the booleanOptionsDictionary with the DacDeployOptions property values
            VerifyBooleanOptionsDictionary(deploymentOptions.BooleanOptionsDictionary, dacDeployOptions);
        }

        internal static bool ValidateOptionsEqualsDefault(SchemaCompareOptionsResult options)
        {
            DeploymentOptions defaultOpt = new DeploymentOptions();
            DeploymentOptions actualOpt = options.DefaultDeploymentOptions;

            System.Reflection.PropertyInfo[] deploymentOptionsProperties = defaultOpt.GetType().GetProperties();
            foreach (PropertyInfo v in deploymentOptionsProperties)
            {
                if (v.Name != nameof(DeploymentOptions.BooleanOptionsDictionary))
                {
                    var defaultP = v.GetValue(defaultOpt);
                    var defaultPValue = defaultP != null ? defaultP.GetType().GetProperty("Value").GetValue(defaultP) : defaultP;
                    var actualP = v.GetValue(actualOpt);
                    var actualPValue = actualP.GetType().GetProperty("Value").GetValue(actualP);

                    if (v.Name == nameof(DeploymentOptions.ExcludeObjectTypes))
                    {
                        Assert.That((defaultPValue as string[]).Length, Is.EqualTo((actualPValue as string[]).Length), $"Number of excluded objects is different.");
                    }
                    else
                    {
                        // Verifying expected and actual deployment options properties are equal
                        Assert.True((defaultPValue == null && String.IsNullOrEmpty(actualPValue as string))
                         || (defaultPValue).Equals(actualPValue)
                        , $"Actual Property from Service is not equal to default property for {v.Name}, Actual value: {actualPValue} and Default value: {defaultPValue}");
                    }
                }
            }

            // Verify the default booleanOptionsDictionary with the SchemaCompareOptionsResult options property values
            DacFxServiceTests dacFxServiceTests = new DacFxServiceTests();
            dacFxServiceTests.VerifyExpectedAndActualBooleanOptionsDictionary(defaultOpt.BooleanOptionsDictionary, options.DefaultDeploymentOptions.BooleanOptionsDictionary);

            return true;
        }

        /// <summary>
        /// Validates the DeploymentOptions booleanOptionsDictionary with the DacDeployOptions
        /// </summary>
        /// <param name="expectedBooleanOptionsDictionary"></param>
        /// <param name="dacDeployOptions"></param>
        private static void VerifyBooleanOptionsDictionary(Dictionary<string, DeploymentOptionProperty<bool>> expectedBooleanOptionsDictionary, DacDeployOptions dacDeployOptions)
        {
            foreach (KeyValuePair<string, DeploymentOptionProperty<bool>> optionRow in expectedBooleanOptionsDictionary)
            {
                var dacProp = dacDeployOptions.GetType().GetProperty(optionRow.Key);
                Assert.That(dacProp, Is.Not.Null, $"DacDeploy property not present for {optionRow.Key}");
                var actualValue = dacProp.GetValue(dacDeployOptions);
                var expectedValue = optionRow.Value.Value;

                Assert.That(actualValue, Is.EqualTo(expectedValue), $"Actual Property from Service is not equal to default property for {optionRow.Key}");
            }
        }
    }
}
