//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin;
using System;
using NUnit.Framework;

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
        [Test]
        public async Task CreateDatabaseWithValidInputTest()
        {
            var result = GetLiveAutoCompleteTestObjects();

            var databaseInfo = new DatabaseInfo();
            databaseInfo.Options.Add("name", "testdb_" + new Random().Next(10000000, 99999999));

            var dbParams = new CreateDatabaseParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                DatabaseInfo = databaseInfo
            };
        
            CreateDatabaseResponse response = await AdminService.HandleCreateDatabaseRequest(dbParams);
            Assert.NotNull(response);
        }

        /// <summary>
        /// Get a default database info object
        /// </summary>
        [Ignore("Test is failing in the integration test pipeline.")]
        public async Task GetDefaultDatebaseInfoTest()
        {
            var result = GetLiveAutoCompleteTestObjects();

            var dbParams = new DefaultDatabaseInfoParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri
            };

            DefaultDatabaseInfoResponse response = await AdminService.HandleDefaultDatabaseInfoRequest(dbParams);
            Assert.NotNull(response);
        }

        /// <summmary>
        /// Get database info test
        /// </summary>
        [Ignore("Test is failing in the integration test pipeline.")]
        public async Task GetDatabaseInfoTest()
        {
            var results = GetLiveAutoCompleteTestObjects();

            var dbParams = new GetDatabaseInfoParams
            {
                OwnerUri = results.ConnectionInfo.OwnerUri
            };

            GetDatabaseInfoResponse response = await AdminService.HandleGetDatabaseInfoRequest(dbParams);
            Assert.NotNull(response);
        }

    }
}
