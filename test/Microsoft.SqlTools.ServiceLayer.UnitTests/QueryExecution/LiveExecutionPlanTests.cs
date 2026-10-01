//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class LiveExecutionPlanTests
    {
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
