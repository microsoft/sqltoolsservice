//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using System;
using System.IO;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    internal static class SchemaCompareTestUtils
    {
        internal static void VerifyAndCleanup(string filePath)
        {
            // Verify it was created
            Assert.True(File.Exists(filePath));

            // Remove the file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        internal static string CreateDacpac(SqlTestDb testdb)
        {
            var result = GetLiveAutoCompleteTestObjects();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
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
            service.PerformOperation(operation);

            return extractParams.PackageFilePath;
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

            // Note that DatabaseSpecification and sql cmd variables list is not present in Sqltools service - its not settable and is not used by ADS options.
            // They are not present in SSDT as well
            // TODO : update this test if the above options are added later
            Assert.True(deploymentOptionsProperties.Length == dacDeployProperties.Length - 2, $"Number of properties is not same Deployment options : {deploymentOptionsProperties.Length} DacFx options : {dacDeployProperties.Length}");

            foreach (var deployOptionsProp in deploymentOptionsProperties)
            {
                var dacProp = dacDeployOptions.GetType().GetProperty(deployOptionsProp.Name);
                Assert.True(dacProp != null, $"DacDeploy property not present for {deployOptionsProp.Name}");

                var deployOptionsValue = deployOptionsProp.GetValue(deploymentOptions);
                var dacValue = dacProp.GetValue(dacDeployOptions);

                if (deployOptionsProp.Name != "ExcludeObjectTypes") // do not compare for ExcludeObjectTypes because it will be different
                {
                    Assert.True((deployOptionsValue == null && dacValue == null) || deployOptionsValue.Equals(dacValue), $"DacFx DacDeploy property not equal to Tools Service DeploymentOptions for { deployOptionsProp.Name}, SchemaCompareOptions value: {deployOptionsValue} and DacDeployOptions value: {dacValue} ");
                }
            }
        }

        internal static bool ValidateOptionsEqualsDefault(SchemaCompareOptionsResult options)
        {
            DeploymentOptions defaultOpt = new DeploymentOptions();
            DeploymentOptions actualOpt = options.DefaultDeploymentOptions;

            System.Reflection.PropertyInfo[] deploymentOptionsProperties = defaultOpt.GetType().GetProperties();
            foreach (var v in deploymentOptionsProperties)
            {
                var defaultP = v.GetValue(defaultOpt);
                var actualP = v.GetValue(actualOpt);
                if (v.Name == "ExcludeObjectTypes")
                {
                    Assert.True((defaultP as ObjectType[]).Length == (actualP as ObjectType[]).Length, $"Number of excluded objects is different; expected: {(defaultP as ObjectType[]).Length} actual: {(actualP as ObjectType[]).Length}");
                }
                else
                {
                    Assert.True((defaultP == null && actualP == null) || defaultP.Equals(actualP), $"Actual Property from Service is not equal to default property for { v.Name}, Actual value: {actualP} and Default value: {defaultP}");
                }
            }
            return true;
        }
    }
}
