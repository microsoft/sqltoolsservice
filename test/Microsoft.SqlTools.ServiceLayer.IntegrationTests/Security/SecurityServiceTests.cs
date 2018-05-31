//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    /// <summary>
    /// Tests for the security service component
    /// </summary>
    public class SecuritygServiceTests
    {
        /// <summary>
        /// Verify the script object request
        /// </summary>
        [Fact]
        public async Task TestHandleCreateCredentialRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateCredentialResult>>();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var service = new SecurityService();
                var credential = new CredentialInfo()
                {                
                };

                await service.HandleCreateCredentialRequest(new CreateCredentialParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Credential = credential
                }, createContext.Object);

                createContext.VerifyAll();
            }
        }  
    }
}
