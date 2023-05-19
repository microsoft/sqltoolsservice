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
        /// Test the basic Create Database method handler
        /// </summary>
        [Test]
        public async Task TestHandleCreateDatabaseRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup, drop database if exists.
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                // create and update
                var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);

                var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.Database, "", objUrn);
                await ObjectManagementTestUtils.SaveObject(parametersForUpdate, testDatabase);

                // cleanup
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);
            }
        }
    }
}
