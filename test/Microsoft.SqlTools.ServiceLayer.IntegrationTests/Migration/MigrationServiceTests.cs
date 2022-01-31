//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Migration;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Migration
{
    public class MigrationgentServiceTests
    {
        [Test]
        public async Task TestHandleMigrationAssessmentRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new MigrationAssessmentsParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<MigrationAssessmentResult>>();

                MigrationService service = new MigrationService();
                await service.HandleMigrationAssessmentsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }

        [Test]
        public async Task TestHandleMigrationGetSkuRecommendationsRequest()
        {
            GetSkuRecommendationsResult result = null;

            var requestParams = new GetSkuRecommendationsParams()
            {
                DataFolder = Path.Combine("..", "..", "..", "Migration"),
                TargetPlatforms = new List<string> { "AzureSqlManagedInstance" },
                TargetSqlInstance = "Test",
                TargetPercentile = 95,
                StartTime = new DateTime(2020, 01, 01).ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                PerfQueryIntervalInSec = 30,
                ScalingFactor = 1,
                DatabaseAllowList = new List<string> { "test", "test1" }
            };

            var requestContext = RequestContextMocks.Create<GetSkuRecommendationsResult>(r => result = r).AddErrorHandling(null);

            MigrationService service = new MigrationService();
            await service.HandleGetSkuRecommendationsRequest(requestParams, requestContext.Object);
            Assert.IsNotNull(result, "Get SKU Recommendation result is null");
            Assert.IsNotNull(result.SqlMiRecommendationResults, "Get MI SKU Recommendation result is null");
            Assert.AreEqual(result.SqlMiRecommendationResults.First().PositiveJustifications.Count, 6, "Invalid number of positive justifications for MI SKU Recommendation result");
        }
    }
}