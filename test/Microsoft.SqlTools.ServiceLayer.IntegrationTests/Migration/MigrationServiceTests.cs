//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Migration;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Migration
{
    public class MigrationgentServiceTests
    {     
        [Test]
        public async Task TestHandleMigrationSkuRecommendationsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new MigrationSkuRecommendationsParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<MigrationSkuRecommendationsResult>>();

                MigrationService service = new MigrationService();
                await service.HandleMigrationSkuRecommendations(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }     
        }
  
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

                var requestContext = new Mock<RequestContext<AssessmentResult<CheckInfo>>>();

                MigrationService service = new MigrationService();
                await service.HandleMigrationAssessmentsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }     
        }
    }
}
