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

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class CreateDatabaseTests
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
        public async void CreateDatabaseWithValidInputTest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<CreateDatabaseResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<CreateDatabaseResponse>())).Returns(Task.FromResult(new object()));

            var dbParams = new CreateDatabaseParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                DatabaseInfo = new DatabaseInfo()
            };
        
            await AdminService.HandleCreateDatabaseRequest(dbParams, requestContext.Object);

            requestContext.VerifyAll();
        }
   
    }
}
