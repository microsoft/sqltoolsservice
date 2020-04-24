//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SqlAssessment
{
    public class SqlAssessmentServiceTests
    {
        private delegate Task<List<TResult>> AssessmentMethod<TResult>(SqlObjectLocator locator);

        private static readonly string[] AllowedSeverityLevels = { "Information", "Warning", "Critical" };

        [Fact]
        public async void GetAssessmentItemsServerTest()
        {
            var response = await CallAssessment<CheckInfo>(
                               nameof(SqlAssessmentService.GetAssessmentItems),
                               SqlObjectType.Server);

            Assert.All(
                response.Items,
                i =>
                {
                    AssertInfoPresent(i);
                });
        }

        [Fact]
        public async void InvokeSqlAssessmentServerTest()
        {
            var response = await CallAssessment<AssessmentResultItem>(
                               nameof(SqlAssessmentService.InvokeSqlAssessment),
                               SqlObjectType.Server);


            Assert.All(
                response.Items,
                i =>
                {
                    Assert.NotNull(i.Message);
                    Assert.NotEmpty(i.Message);

                    if (i.Kind == 0)
                    {
                        AssertInfoPresent(i);
                    }
                });
        }

        [Fact]
        public async void GetAssessmentItemsDatabaseTest()
        {
            const string DatabaseName = "tempdb";
            var response = await CallAssessment<CheckInfo>(
                               nameof(SqlAssessmentService.GetAssessmentItems),
                               SqlObjectType.Database,
                               DatabaseName);

            Assert.All(
                response.Items,
                i =>
                {
                    StringAssert.EndsWith("/" + DatabaseName, i.TargetName);
                    AssertInfoPresent(i);
                });
        }

        [Fact]
        public async void InvokeSqlAssessmentIDatabaseTest()
        {
            const string DatabaseName = "tempdb";
            var response = await CallAssessment<AssessmentResultItem>(
                               nameof(SqlAssessmentService.InvokeSqlAssessment),
                               SqlObjectType.Database,
                               DatabaseName);

            Assert.All(
                response.Items,
                i =>
                {
                    StringAssert.EndsWith("/" + DatabaseName, i.TargetName);
                    Assert.NotNull(i.Message);
                    Assert.NotEmpty(i.Message);

                    if (i.Kind == 0)
                    {
                        AssertInfoPresent(i);
                    }
                });
        }

        private static async Task<AssessmentResult<TResult>> CallAssessment<TResult>(
            string methodName,
            SqlObjectType sqlObjectType,
            string databaseName = "master")
            where TResult : AssessmentItemInfo
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            var connInfo = liveConnection.ConnectionInfo;

            AssessmentResult<TResult> response;

            using (var service = new SqlAssessmentService(
                TestServiceProvider.Instance.ConnectionService,
                TestServiceProvider.Instance.WorkspaceService))
            {
                string randomUri = Guid.NewGuid().ToString();
                AssessmentParams requestParams =
                    new AssessmentParams { OwnerUri = randomUri, TargetType = sqlObjectType };
                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = requestParams.OwnerUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                var methodInfo = typeof(SqlAssessmentService).GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(methodInfo);

                var func = (AssessmentMethod<TResult>)Delegate.CreateDelegate(
                    typeof(AssessmentMethod<TResult>),
                    service,
                    methodInfo);

                response = await service.CallAssessmentEngine<TResult>(
                               requestParams,
                               connectParams,
                               randomUri,
                               t => func(t));
            }

            Assert.NotNull(response);
            if (response.Success)
            {
                Assert.All(
                    response.Items,
                    i =>
                    {
                        Assert.Equal(sqlObjectType, i.TargetType);
                        Assert.Contains(i.Level, AllowedSeverityLevels);
                    });
            }

            return response;
        }

        private void AssertInfoPresent(AssessmentItemInfo item)
        {
            Assert.NotNull(item.CheckId);
            Assert.NotEmpty(item.CheckId);
            Assert.NotNull(item.DisplayName);
            Assert.NotEmpty(item.DisplayName);
            Assert.NotNull(item.Description);
            Assert.NotEmpty(item.Description);
            Assert.NotNull(item.Tags);
            Assert.All(item.Tags, t =>
            {
                Assert.NotNull(t);
                Assert.NotEmpty(t);
            });
        }
    }
}
