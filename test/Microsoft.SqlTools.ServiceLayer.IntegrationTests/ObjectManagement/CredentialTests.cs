//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Credential management component
    /// </summary>
    public class CredentialTests
    {
        /// <summary>
        /// TestHandleCreateCredentialRequest
        /// </summary>
        [Test]
        public async Task TestCredentialOperations()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup, drop credential if exists.
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var credential = ObjectManagementTestUtils.GetTestCredentialInfo();
                var objUrn = ObjectManagementTestUtils.GetCredentialURN(credential.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                // create and update
                var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Credential, "", "");
                await ObjectManagementTestUtils.SaveObject(parametersForCreation, credential);

                var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.Credential, "", objUrn);
                await ObjectManagementTestUtils.SaveObject(parametersForUpdate, credential);

                // cleanup
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);
            }
        }
    }
}
