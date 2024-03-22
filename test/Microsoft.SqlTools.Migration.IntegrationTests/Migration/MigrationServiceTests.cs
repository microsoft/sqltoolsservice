//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Migration.IntegrationTests.Utility;
using Microsoft.SqlTools.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models.Sku;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Assert_ = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.SqlServer.Migration.SkuRecommendation.TargetProvisioning.Contracts;
using Microsoft.SqlServer.Migration.TargetProvisioning;

namespace Microsoft.SqlTools.Migration.IntegrationTests.Migration
{
    public class MigrationServiceTests
    {
        private static string serverLevelCollation = "Latin1_General_CI_AI";
        private static Dictionary<string, string> databaseLevelCollations = new Dictionary<string, string>()
        {
            { "TestDb", "Latin1_General_CI_AI"}
        };
        private static IProvisioningScriptServiceProvider provisioningScriptserviceProvider = new ProvisioningScriptServiceProvider();

        [Test]
        public async Task TestHandleMigrationAssessmentRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new MigrationAssessmentsParams()
                {
                    ConnectionString = connectionResult.ConnectionInfo.ConnectionDetails.ConnectionString
                };

                var requestContext = new Mock<RequestContext<MigrationAssessmentResult>>();

                MigrationService service = new MigrationService();
                await service.HandleMigrationAssessmentsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }

        [Test]
        [NUnit.Framework.Ignore("Disable failing test")]
        public async Task TestHandleMigrationGetSkuRecommendationsRequest()
        {
            GetSkuRecommendationsResult result = null;

            var requestParams = new GetSkuRecommendationsParams()
            {
                DataFolder = Path.Combine("..", "..", "..", "Migration", "Data"),
                TargetPlatforms = new List<string> { "AzureSqlManagedInstance" },
                TargetSqlInstance = "Test",
                TargetPercentile = 95,
                StartTime = new DateTime(2020, 01, 01).ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                PerfQueryIntervalInSec = 30,
                ScalingFactor = 1,
                DatabaseAllowList = new List<string> { "test", "test1" },
                IsPremiumSSDV2Enabled = true,
            };

            var requestContext = RequestContextMocks.Create<GetSkuRecommendationsResult>(r => result = r).AddErrorHandling(null);

            MigrationService service = new MigrationService();
            await service.HandleGetSkuRecommendationsRequest(requestParams, requestContext.Object);
            Assert.IsNotNull(result, "Get SKU Recommendation result is null");
            Assert.IsNotNull(result.SqlMiRecommendationResults, "Get MI SKU Recommendation baseline result is null");
            Assert.IsNotNull(result.ElasticSqlMiRecommendationResults, "Get MI SKU Recommendation elastic result is null");

            // TODO: Include Negative Justification in future when we start recommending more than one SKU.
            Assert.Greater(result.SqlMiRecommendationResults.First().PositiveJustifications.Count, 0, "No positive justification for MI SKU Recommendation result");
            Assert.Greater(result.ElasticSqlMiRecommendationResults.First().PositiveJustifications.Count, 0, "No positive justification for MI SKU elastic Recommendation result");

            Assert.IsNotNull(result.InstanceRequirements);
            Assert.AreEqual(result.InstanceRequirements.InstanceId, "TEST");
            Assert.AreEqual(result.InstanceRequirements.DatabaseLevelRequirements.Count, 2);
            Assert.AreEqual(result.InstanceRequirements.DatabaseLevelRequirements.Sum(db => db.FileLevelRequirements.Count), 4);
        }

        [Test]
        public async Task TestHandleStartStopPerfDataCollectionRequest()
        {
            StartPerfDataCollectionResult result = null;
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkuRecommendationTest");
                Directory.CreateDirectory(folderPath);

                var requestParams = new StartPerfDataCollectionParams()
                {
                    ConnectionString = connectionResult.ConnectionInfo.ConnectionDetails.ConnectionString,
                    DataFolder = folderPath,
                    PerfQueryIntervalInSec = 30,
                    NumberOfIterations = 20,
                    StaticQueryIntervalInSec = 3600,
                };

                var requestContext = RequestContextMocks.Create<StartPerfDataCollectionResult>(r => result = r).AddErrorHandling(null);

                MigrationService service = new MigrationService();
                await service.HandleStartPerfDataCollectionRequest(requestParams, requestContext.Object);
                Assert.IsNotNull(result, "Start Perf Data Collection result is null");
                Assert.IsNotNull(result.DateTimeStarted, "Time perf data collection started is null");

                // Stop data collection
                StopPerfDataCollectionResult stopResult = null;
                var stopRequestParams = new StopPerfDataCollectionParams()
                {

                };

                var stopRequestContext = RequestContextMocks.Create<StopPerfDataCollectionResult>(r => stopResult = r).AddErrorHandling(null);

                await service.HandleStopPerfDataCollectionRequest(stopRequestParams, stopRequestContext.Object);
                Assert.IsNotNull(stopResult, "Stop Perf Data Collection result is null");
                Assert.IsNotNull(stopResult.DateTimeStopped, "Time perf data collection stoped is null");
            }
        }

        [Test]
        public void GenerateProvisioningScript_DatabaseRecommendation_ReturnsDBArmTemplate()
        {
            // Arrange
            var recs = new List<SkuRecommendationResult>
        {
            new SkuRecommendationResult
            {
                SqlInstanceName = "TestServer",
                DatabaseName = "TestDb",
                ServerCollation = serverLevelCollation,
                DatabaseCollation = databaseLevelCollations["TestDb"],
                TargetSku = new AzureSqlPaaSSku(
                    new AzureSqlSkuPaaSCategory(
                        AzureSqlTargetPlatform.AzureSqlDatabase,
                        AzureSqlPurchasingModel.vCore,
                AzureSqlPaaSServiceTier.GeneralPurpose,
                        ComputeTier.Provisioned,
                        AzureSqlPaaSHardwareType.Gen5),
                    2,
                    1)
                {
                }
            }
        };

            // Act
            var result = provisioningScriptserviceProvider.GenerateProvisioningScript(recs);

            // Assert
            Assert_.IsInstanceOfType(result, typeof(SqlArmTemplate));
            Assert.AreEqual(result.resources.Count, 2);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Sql/servers").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Sql/servers/databases").Count(), 1);
            Assert.AreEqual(result.parameters.Where(par => par.Key == "Server collation").FirstOrDefault().Value.defaultValue, "Latin1_General_CI_AI");
            Assert.AreEqual(result.parameters.Where(par => par.Key == "Collation for database testdb").FirstOrDefault().Value.defaultValue, "Latin1_General_CI_AI");
            Assert.AreEqual(result.parameters.Count, 9);
        }

        [Test]
        public void GenerateProvisioningScript_ManagedInstanceRecommendation_ReturnsMIArmTemplate()
        {
            // Arrange
            var recs = new List<SkuRecommendationResult>
        {
            new SkuRecommendationResult
            {
                SqlInstanceName = "TestServer",
                ServerCollation = serverLevelCollation,
                TargetSku = new AzureSqlPaaSSku(
                    new AzureSqlSkuPaaSCategory(
                        AzureSqlTargetPlatform.AzureSqlManagedInstance,
                        AzureSqlPurchasingModel.vCore,
                        AzureSqlPaaSServiceTier.GeneralPurpose,
                ComputeTier.Provisioned,
                        AzureSqlPaaSHardwareType.Gen5),
                    4,
                    32)
                {
                }
            }
        };

            // Act
            var result = provisioningScriptserviceProvider.GenerateProvisioningScript(recs);

            // Assert
            Assert_.IsInstanceOfType(result, typeof(SqlArmTemplate));
            Assert.AreEqual(result.resources.Count, 4);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/networkSecurityGroups").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/routeTables").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/virtualNetworks").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Sql/managedInstances").Count(), 1);
            Assert.AreEqual(result.parameters.Where(par => par.Key == "Server collation").FirstOrDefault().Value.defaultValue, "Latin1_General_CI_AI");
            Assert.AreEqual(result.parameters.Count, 11);
        }

        [Test]
        public void GenerateProvisioningScript_VirtualMachineRecommendation_ReturnsVMArmTemplate()
        {
            // Arrange
            var recs = new List<SkuRecommendationResult>
        {
            new SkuRecommendationResult
            {
                SqlInstanceName = "TestServer",
                ServerCollation = serverLevelCollation,
                TargetSku = new AzureSqlIaaSSku(
                    new AzureSqlSkuIaaSCategory(VirtualMachineFamily.standardDASv4Family),
                    new AzureVirtualMachineSku()
                    {
                        VirtualMachineFamily = VirtualMachineFamily.standardDASv4Family,
                        SizeName = "D2as_v4",
                        ComputeSize = 2,
                    },
                    new List<AzureManagedDiskSku>()
                    {
                        new AzureManagedDiskSku()
                        {
                            Type = AzureManagedDiskType.PremiumSSDV2,
                            MaxSizeInGib = 64,
                            MaxThroughputInMbps = 125,
                            MaxIOPS = 3000,
                            Size = "64 GB, 3000 IOPS, 125 MB/s",
                        }
                    },
                    new List<AzureManagedDiskSku>()
                    {
                        new AzureManagedDiskSku()
                        {
                            Type = AzureManagedDiskType.PremiumSSDV2,
                            MaxSizeInGib = 64,
                            MaxThroughputInMbps = 125,
                            MaxIOPS = 3000,
                            Size = "64 GB, 3000 IOPS, 125 MB/s",
                        }
                    },
                    new List<AzureManagedDiskSku>()
                    {
                        new AzureManagedDiskSku()
                        {
                            Type = AzureManagedDiskType.PremiumSSDV2,
                            MaxSizeInGib = 64,
                            MaxThroughputInMbps = 125,
                            MaxIOPS = 3000,
                            Size = "64 GB, 3000 IOPS, 125 MB/s",
                        }
                    })
            }
        };

            // Act
            var result = provisioningScriptserviceProvider.GenerateProvisioningScript(recs);

            // Assert
            Assert_.IsInstanceOfType(result, typeof(SqlArmTemplate));
            Assert.AreEqual(result.resources.Count, 6);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/networkSecurityGroups").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.SqlVirtualMachine/SqlVirtualMachines").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/virtualNetworks").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Compute/virtualMachines").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/publicIpAddresses").Count(), 1);
            Assert.AreEqual(result.resources.Where(res => res.type == "Microsoft.Network/networkInterfaces").Count(), 1);
            Assert.AreEqual(result.parameters.Where(par => par.Key == "Server collation").FirstOrDefault().Value.defaultValue, "Latin1_General_CI_AI");
            Assert.AreEqual(result.parameters.Count, 14);
            // Add additional assertions based on the expected outcome for the virtual machine case
        }

        [Test]
        public void GenerateProvisioningScript_InvalidTargetPlatform_ThrowsArgumentException()
        {
            // Arrange
            var recs = new List<SkuRecommendationResult>
        {
            new SkuRecommendationResult
            {
                SqlInstanceName = "TestServer",
                ServerCollation = serverLevelCollation,
                TargetSku = new AzureSqlPaaSSku(
                    new AzureSqlSkuPaaSCategory(
                        (AzureSqlTargetPlatform)999,
                        AzureSqlPurchasingModel.vCore,
                        AzureSqlPaaSServiceTier.GeneralPurpose,
                        ComputeTier.Provisioned,
                        AzureSqlPaaSHardwareType.Gen5),
                    4,
                    32)
                {
                }
            }
        };

            // Act & Assert
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<ArgumentException>(() => provisioningScriptserviceProvider.GenerateProvisioningScript(recs));
        }


    }
}