//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Database management component
    /// </summary>
    public class DatabaseTests
    {
        /// <summary>
        /// Test the basic Create Database method handler by creating, updating, and then deleting a database.
        /// </summary>
        [Test]
        public async Task DatabaseCreateAndUpdateTest_OnPrem()
        {
            await RunDatabaseCreateAndUpdateTest(TestServerType.OnPrem);
        }

        // [Test]
        // public async Task DatabaseCreateAndUpdateTest_Azure()
        // {
        //     await RunDatabaseCreateAndUpdateTest(TestServerType.Azure);
        // }

        private async Task RunDatabaseCreateAndUpdateTest(TestServerType serverType)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup, drop database if exists.
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath, serverType: serverType);
                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    // create and update
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);

                    var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.Database, "", objUrn);
                    await ObjectManagementTestUtils.SaveObject(parametersForUpdate, testDatabase);
                }
                finally
                {
                    // cleanup
                    await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);
                }
            }
        }
    }

}
