//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SqlAssessment
{
    public class SqlAssessmentServiceTests
    {
        private delegate Task<List<TResult>> AssessmentMethod<TResult>(SqlObjectLocator locator);

        private static readonly string[] AllowedSeverityLevels = { "Information", "Warning", "Critical" };

        [Test]
        public async Task InvokeSqlAssessmentServerTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");

            var connection = liveConnection.ConnectionInfo.AllConnections.FirstOrDefault();
            Debug.Assert(connection != null, "Live connection is always expected providing a connection");

            var serverInfo = ReliableConnectionHelper.GetServerVersion(connection);

            var response = await CallAssessment<AssessmentResultItem>(
                               nameof(SqlAssessmentService.InvokeSqlAssessment),
                               SqlObjectType.Server,
                               liveConnection);

            Assert.Multiple(() =>
            {
                Assert.That(response.Items.Select(i => i.Message), Has.All.Not.Null.Or.Empty);
                Assert.That(response.Items.Select(i => i.TargetName), Has.All.EqualTo(serverInfo.ServerName));
                foreach (var i in response.Items.Where(i => i.Kind == 0))
                {
                    AssertInfoPresent(i);
                }
            });
        }

        [Test]
        public async Task GetAssessmentItemsServerTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");

            var connection = liveConnection.ConnectionInfo.AllConnections.FirstOrDefault();
            Debug.Assert(connection != null, "Live connection is always expected providing a connection");

            var serverInfo = ReliableConnectionHelper.GetServerVersion(connection);

            var response = await CallAssessment<CheckInfo>(
                               nameof(SqlAssessmentService.GetAssessmentItems),
                               SqlObjectType.Server,
                               liveConnection);

            Assert.Multiple(() =>
            {
                Assert.That(response.Items.Select(i => i.TargetName), Has.All.EqualTo(serverInfo.ServerName));
                foreach (var i in response.Items)
                {
                    AssertInfoPresent(i);
                }
            });
        }

        [Test]
        public async Task GetAssessmentItemsDatabaseTest()
        {
            const string DatabaseName = "tempdb";
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(DatabaseName);
            var response = await CallAssessment<CheckInfo>(
                               nameof(SqlAssessmentService.GetAssessmentItems),
                               SqlObjectType.Database,
                               liveConnection);

            Assert.Multiple(() =>
            {
                Assert.That(response.Items.Select(i => i.TargetName), Has.All.EndsWith(":" + DatabaseName));
                foreach (var i in response.Items)
                {
                    AssertInfoPresent(i);
                }
            });
        }

        [Test]
        public async Task InvokeSqlAssessmentIDatabaseTest()
        {
            const string DatabaseName = "tempdb";
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(DatabaseName);
            var response = await CallAssessment<AssessmentResultItem>(
                               nameof(SqlAssessmentService.InvokeSqlAssessment),
                               SqlObjectType.Database,
                               liveConnection);

            Assert.Multiple(() =>
            {
                Assert.That(response.Items.Select(i => i.Message), Has.All.Not.Null.Or.Empty);
                Assert.That(response.Items.Select(i => i.TargetName), Has.All.EndsWith(":" + DatabaseName));
                foreach (var i in response.Items.Where(i => i.Kind == 0))
                {
                    AssertInfoPresent(i);
                }
            });
        }

        private static async Task<AssessmentResult<TResult>> CallAssessment<TResult>(
            string methodName,
            SqlObjectType sqlObjectType,
            LiveConnectionHelper.TestConnectionResult liveConnection)
            where TResult : AssessmentItemInfo
        {
            var connInfo = liveConnection.ConnectionInfo;

            AssessmentResult<TResult> response;

            using (var service = new SqlAssessmentService(
                TestServiceProvider.Instance.ConnectionService,
                TestServiceProvider.Instance.WorkspaceService))
            {
                AddTestRules(service);

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
                Assert.That(response.Items.Select(i => i.TargetType), Has.All.EqualTo(sqlObjectType));
                Assert.That(response.Items.Select(i => i.Level), Has.All.AnyOf(AllowedSeverityLevels));
            }

            return response;
        }

        private static void AddTestRules(SqlAssessmentService service)
        {
            const string TestRuleset = @"
            {
                'name': 'Tags & Checks',
                'version': '0.3',
                'schemaVersion': '1.0',
                'rules': [
                    {
                        'id': 'ServerRule',
                        'itemType': 'definition',
                        'tags': [ 'Test' ],
                        'displayName': 'Test server check',
                        'description': 'This check always fails for testing purposes.',
                        'message': 'This check intentionally fails',
                        'target': { 'type': 'Server' }
                    },
                    {
                        'id': 'DatabaseRule',
                        'itemType': 'definition',
                        'tags': [ 'Test' ],
                        'displayName': 'Test server check',
                        'description': 'This check always fails for testing purposes.',
                        'message': 'This check intentionally fails',
                        'target': { 'type': 'Database' }
                    }
                ]
            }
";
            using (var reader = new StringReader(TestRuleset))
            {
                service.Engine.PushRuleFactoryJson(reader);
            }
        }

        private void AssertInfoPresent(AssessmentItemInfo item)
        {
            Assert.Multiple(() =>
            {
                Assert.That(item.CheckId, Is.Not.Null.Or.Empty);
                Assert.That(item.DisplayName, Is.Not.Null.Or.Empty);
                Assert.That(item.Description, Is.Not.Null.Or.Empty);
                Assert.NotNull(item.Tags);
                Assert.That(item.Tags, Has.All.Not.Null.Or.Empty);
            });
        }
    }
}
