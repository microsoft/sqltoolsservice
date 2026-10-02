//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class LiveExecutionPlanTests
    {
        private string ownerUri;
        private ConnectionInfo connectionInfo;
        private Mock<DbConnection> monitoringConnection;
        private Mock<DbConnection> defaultConnection;

        [SetUp]
        public void SetUp()
        {
            ownerUri = $"file:///live-plan-{Guid.NewGuid()}.sql";
            connectionInfo = new ConnectionInfo(null, ownerUri, TestObjects.GetTestConnectionDetails());
            defaultConnection = new Mock<DbConnection>();
            monitoringConnection = new Mock<DbConnection>();
            monitoringConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);
            connectionInfo.AddConnection(ConnectionType.Default, defaultConnection.Object);
            connectionInfo.AddConnection(ConnectionType.LiveQueryStatistics, monitoringConnection.Object);
            ConnectionService.Instance.OwnerToConnectionMap[ownerUri] = connectionInfo;
        }

        [TearDown]
        public void TearDown()
        {
            ConnectionService.Instance.OwnerToConnectionMap.TryRemove(ownerUri, out _);
        }

        [TestCase("string")]
        [TestCase("SqlXml")]
        [TestCase("XmlReader")]
        public async Task GetLiveExecutionPlanReturnsGraphs(string resultType)
        {
            using Stream stream = typeof(LiveExecutionPlanTests).Assembly.GetManifestResourceStream(
                "Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlan.TestShowPlan.xml");
            using var textReader = new StreamReader(stream);
            string planXml = textReader.ReadToEnd();
            using var xmlReader = XmlReader.Create(new StringReader(planXml));
            object plan = resultType switch
            {
                "SqlXml" => new SqlXml(xmlReader),
                "XmlReader" => xmlReader,
                _ => planXml
            };
            using var parameterOwner = new SqlCommand();
            var command = SetUpCommand(plan, parameterOwner);
            string error = null;
            GetExecutionPlanResult result = null;
            var requestContext = RequestContextMocks.Create<GetExecutionPlanResult>(r => result = r)
                .AddErrorHandling((message, _, _) => error = message);

            await ExecutionPlanService.Instance.HandleGetLiveExecutionPlan(
                new GetLiveExecutionPlanParams { OwnerUri = ownerUri, SessionId = 57 }, requestContext.Object);

            Assert.That(error, Is.Null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Graphs, Is.Not.Null.And.Not.Empty);
            Assert.That(command.Object.CommandText, Is.EqualTo(ExecutionPlanService.LiveExecutionPlanQuery));
            Assert.That(parameterOwner.Parameters.Count, Is.EqualTo(1));
            Assert.That(parameterOwner.Parameters[0].ParameterName, Is.EqualTo("@sessionId"));
            Assert.That(parameterOwner.Parameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(parameterOwner.Parameters[0].Value, Is.EqualTo(57));
            command.Protected().Verify("Dispose", Times.Once(), true);
            Assert.That(connectionInfo.TryGetConnection(ConnectionType.LiveQueryStatistics, out var connection), Is.True);
            Assert.That(connection, Is.SameAs(monitoringConnection.Object));
            monitoringConnection.Verify(c => c.Close(), Times.Never());
        }

        private static IEnumerable<TestCaseData> EmptyPlans => new[]
        {
            new TestCaseData(new object[] { null }).SetName("GetLiveExecutionPlanReturnsEmptyGraphsForNoRow"),
            new TestCaseData(DBNull.Value).SetName("GetLiveExecutionPlanReturnsEmptyGraphsForDbNull"),
            new TestCaseData(SqlXml.Null).SetName("GetLiveExecutionPlanReturnsEmptyGraphsForSqlXmlNull"),
            new TestCaseData("").SetName("GetLiveExecutionPlanReturnsEmptyGraphsForEmptyString")
        };

        [TestCaseSource(nameof(EmptyPlans))]
        public async Task GetLiveExecutionPlanReturnsEmptyGraphs(object plan)
        {
            using var parameterOwner = new SqlCommand();
            SetUpCommand(plan, parameterOwner);
            var results = new List<GetExecutionPlanResult>();
            string error = null;
            var requestContext = RequestContextMocks.Create<GetExecutionPlanResult>(results.Add)
                .AddErrorHandling((message, _, _) => error = message);

            // Poll twice to verify the same monitoring connection is kept for subsequent reads.
            for (int i = 0; i < 2; i++)
            {
                await ExecutionPlanService.Instance.HandleGetLiveExecutionPlan(
                    new GetLiveExecutionPlanParams { OwnerUri = ownerUri, SessionId = 57 }, requestContext.Object);
            }

            Assert.That(error, Is.Null);
            Assert.That(results.Count, Is.EqualTo(2));
            foreach (var result in results)
            {
                Assert.That(result.Graphs, Is.Not.Null.And.Empty);
            }
            monitoringConnection.Protected().Verify("CreateDbCommand", Times.Exactly(2));
            monitoringConnection.Verify(c => c.Open(), Times.Never());
            monitoringConnection.Verify(c => c.Close(), Times.Never());
            monitoringConnection.Protected().Verify("Dispose", Times.Never(), true);
        }

        private Mock<DbCommand> SetUpCommand(object plan, SqlCommand parameterOwner)
        {
            var command = new Mock<DbCommand>();
            command.SetupAllProperties();
            command.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() => new SqlParameter());
            command.Protected().SetupGet<DbParameterCollection>("DbParameterCollection").Returns(parameterOwner.Parameters);
            command.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plan);
            monitoringConnection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(command.Object);
            return command;
        }

        [Test]
        public async Task EndLiveExecutionPlanClosesOnlyTheMonitoringConnection()
        {
            bool? closed = null;
            var requestContext = RequestContextMocks.Create<bool>(r => closed = r);

            await ExecutionPlanService.Instance.HandleEndLiveExecutionPlan(
                new EndLiveExecutionPlanParams { OwnerUri = ownerUri }, requestContext.Object);

            Assert.That(closed, Is.True);
            Assert.That(connectionInfo.HasConnectionType(ConnectionType.LiveQueryStatistics), Is.False);
            Assert.That(connectionInfo.HasConnectionType(ConnectionType.Default), Is.True);
            monitoringConnection.Verify(c => c.Close(), Times.Once());
            monitoringConnection.Protected().Verify("Dispose", Times.Once(), true);
            defaultConnection.Verify(c => c.Close(), Times.Never());
            defaultConnection.Protected().Verify("Dispose", Times.Never(), true);

            await ExecutionPlanService.Instance.HandleEndLiveExecutionPlan(
                new EndLiveExecutionPlanParams { OwnerUri = ownerUri }, requestContext.Object);
            Assert.That(closed, Is.False);
            monitoringConnection.Verify(c => c.Close(), Times.Once());
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task EndLiveExecutionPlanReportsCleanupErrors(bool closeThrows, bool disposeThrows)
        {
            if (closeThrows)
            {
                monitoringConnection.Setup(c => c.Close()).Throws(new InvalidOperationException("Close failed"));
            }
            if (disposeThrows)
            {
                monitoringConnection.Protected().Setup("Dispose", true).Throws(new InvalidOperationException("Dispose failed"));
            }
            string error = null;
            bool? closed = null;
            var requestContext = RequestContextMocks.Create<bool>(r => closed = r)
                .AddErrorHandling((message, _, _) => error = message);

            await ExecutionPlanService.Instance.HandleEndLiveExecutionPlan(
                new EndLiveExecutionPlanParams { OwnerUri = ownerUri }, requestContext.Object);

            Assert.That(closed, Is.Null);
            Assert.That(error, Is.EqualTo(disposeThrows ? "Dispose failed" : "Close failed"));
            Assert.That(connectionInfo.HasConnectionType(ConnectionType.LiveQueryStatistics), Is.False);
            monitoringConnection.Verify(c => c.Close(), Times.Once());
            monitoringConnection.Protected().Verify("Dispose", Times.Once(), true);
            defaultConnection.Verify(c => c.Close(), Times.Never());
            defaultConnection.Protected().Verify("Dispose", Times.Never(), true);
        }

        [Test]
        public async Task GetLiveExecutionPlanReportsAnErrorForAnUnconnectedEditor()
        {
            string error = null;
            GetExecutionPlanResult result = null;
            var requestContext = RequestContextMocks.Create<GetExecutionPlanResult>(r => result = r)
                .AddErrorHandling((message, _, _) => error = message);

            await ExecutionPlanService.Instance.HandleGetLiveExecutionPlan(
                new GetLiveExecutionPlanParams { OwnerUri = "file:///not-connected.sql", SessionId = 57 },
                requestContext.Object);

            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task EndLiveExecutionPlanReportsWhenNoMonitoringConnectionIsOpen()
        {
            bool? closed = null;
            var requestContext = RequestContextMocks.Create<bool>(r => closed = r);

            await ExecutionPlanService.Instance.HandleEndLiveExecutionPlan(
                new EndLiveExecutionPlanParams { OwnerUri = "file:///not-connected.sql" },
                requestContext.Object);

            Assert.That(closed, Is.False);
        }

        [Test]
        public void LiveExecutionPlanQueryReadsTheSessionsInFlightPlan()
        {
            Assert.That(ExecutionPlanService.LiveExecutionPlanQuery, Does.Contain("sys.dm_exec_query_statistics_xml(@sessionId)"));
        }
    }
}
