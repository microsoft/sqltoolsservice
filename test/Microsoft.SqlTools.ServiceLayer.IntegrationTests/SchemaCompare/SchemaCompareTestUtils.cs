//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
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
            service.PerformOperation(operation, TaskExecutionMode.Execute);

            return extractParams.PackageFilePath;
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
    }
}
