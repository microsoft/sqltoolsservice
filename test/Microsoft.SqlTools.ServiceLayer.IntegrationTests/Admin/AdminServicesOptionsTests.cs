//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Moq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.AdminServices
{
    /// <summary>
    /// Tests for the Admin Services tests
    /// </summary>
    public class AdminServicesOptionsTests
    {
        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Test.Common.Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async void GetAdminServicesOptions()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<AdminServiceOptionsResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<AdminServiceOptionsResponse>())).Returns(Task.FromResult(new object()));

            var dbParams = new AdminServiceOptionsParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
            };
        
            await AdminService.HandleOptionsRequest(dbParams, requestContext.Object);

            requestContext.VerifyAll();
        }
   
    }
}
