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
using NUnit.Framework;
using Moq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin;
using System;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.AdminServices
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
        /// Validate creating a database with valid input
        /// </summary>
        // [Test]
        public async Task CreateDatabaseWithValidInputTest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<CreateDatabaseResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<CreateDatabaseResponse>())).Returns(Task.FromResult(new object()));

            var databaseInfo = new DatabaseInfo();
            databaseInfo.Options.Add("name", "testdb_" + new Random().Next(10000000, 99999999));

            var dbParams = new CreateDatabaseParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                DatabaseInfo = databaseInfo
            };
        
            await AdminService.HandleCreateDatabaseRequest(dbParams, requestContext.Object);

            requestContext.VerifyAll();
        }

        /// <summary>
        /// Get a default database info object
        /// </summary>
        // [Test]
        public async Task GetDefaultDatebaseInfoTest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<DefaultDatabaseInfoResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DefaultDatabaseInfoResponse>())).Returns(Task.FromResult(new object()));

            var dbParams = new DefaultDatabaseInfoParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri
            };

            await AdminService.HandleDefaultDatabaseInfoRequest(dbParams, requestContext.Object);

            requestContext.VerifyAll();
        }

        /// <summmary>
        /// Get database info test
        /// </summary>
        /// Test is failing in code coverage runs. Reenable when stable.
        /// [Test]
        public async Task GetDatabaseInfoTest()
        {
            var results = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<GetDatabaseInfoResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<GetDatabaseInfoResponse>())).Returns(Task.FromResult(new object()));

            var dbParams = new GetDatabaseInfoParams
            {
                OwnerUri = results.ConnectionInfo.OwnerUri
            };

            await AdminService.HandleGetDatabaseInfoRequest(dbParams, requestContext.Object);
            
            requestContext.VerifyAll();
        }

    }
}
