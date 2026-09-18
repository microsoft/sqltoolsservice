//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using HostingProtocol = Microsoft.SqlTools.Hosting.Protocol;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    /// <summary>
    /// Regression tests for microsoft/vscode-mssql#22921 ("A query is already running for this
    /// editor session" with no way to cancel).
    ///
    /// Once the execute handler has sent its success response the client waits for a
    /// QueryCompleteEvent. Anything that fails between that response and the start of execution
    /// (connection lookup, session SET options) must still end in that event, otherwise the
    /// client keeps the editor marked as executing forever.
    /// </summary>
    public class AcceptedQueryCompletionTests
    {
        private sealed class CapturedEvents
        {
            public int Results;
            public int Completions;
            public string ReportedError;
        }

        private static (Mock<HostingProtocol.RequestContext<ExecuteRequestResult>> Context, CapturedEvents Events) CreateRequestContext()
        {
            var events = new CapturedEvents();
            var context = RequestContextMocks
                .Create<ExecuteRequestResult>(_ => events.Results++)
                .AddEventHandling(QueryCompleteEvent.Type, (_, __) => events.Completions++)
                .AddEventHandling(MessageEvent.Type, (_, p) =>
                {
                    if (p.Message.IsError)
                    {
                        events.ReportedError = p.Message.Message;
                    }
                });
            return (context, events);
        }

        private static QueryExecutionService CreateService(Mock<ConnectionService> connectionService)
        {
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            return new QueryExecutionService(connectionService.Object, workspaceService)
            {
                BufferFileStreamFactory = MemoryFileSystem.GetFileStreamFactory()
            };
        }

        private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        [Test]
        public async Task AcceptedQueryAlwaysCompletesEvenWhenPostAcceptStepFails()
        {
            ConnectionInfo ci = Common.CreateConnectedConnectionInfo(Common.StandardTestDataSet, false, false);

            // The connection is found while the query object is created, but the lookup that follows
            // the success response comes back empty (the editor was disconnected concurrently).
            // Later lookups succeed again so the editor can be re-used.
            int lookups = 0;
            var connectionService = new Mock<ConnectionService>();
            ConnectionInfo outVal;
            connectionService
                .Setup(s => s.TryFindConnection(It.IsAny<string>(), out outVal))
                .OutCallback((string owner, out ConnectionInfo connInfo) =>
                {
                    lookups++;
                    connInfo = lookups == 2 ? null : ci;
                })
                .Returns(() => lookups != 2);

            var queryService = CreateService(connectionService);
            var (requestContext, events) = CreateRequestContext();
            var queryParams = new ExecuteDocumentSelectionParams
            {
                QuerySelection = Common.WholeDocument,
                OwnerUri = Constants.OwnerUri
            };

            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);
            await queryService.WorkTask;
            await WaitUntil(() => events.Completions >= 1);

            Assert.Multiple(() =>
            {
                Assert.That(events.Results, Is.EqualTo(1), "the client was told the query was accepted");
                Assert.That(events.Completions, Is.EqualTo(1),
                    "an accepted query must always end with a QueryCompleteEvent");
                Assert.That(events.ReportedError, Is.Not.Null.And.Not.Empty, "the reason is reported to the user");
                Assert.That(queryService.ActiveQueries.TryGetValue(Constants.OwnerUri, out Query failed) && failed.HasErrored,
                    Is.True, "the query is marked errored so the next run replaces it");
            });

            // The editor is usable again: the next execute replaces the failed query and runs to completion.
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);
            await WaitUntil(() => events.Completions >= 2);

            Assert.Multiple(() =>
            {
                Assert.That(events.Results, Is.EqualTo(2));
                Assert.That(events.Completions, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task SessionOptionsFailureAfterAcceptIsReportedAsCompletion()
        {
            ConnectionInfo ci = Common.CreateConnectedConnectionInfo(Common.StandardTestDataSet, false, false);

            var connectionService = new Mock<ConnectionService>();
            ConnectionInfo outVal;
            connectionService
                .Setup(s => s.TryFindConnection(It.IsAny<string>(), out outVal))
                .OutCallback((string owner, out ConnectionInfo connInfo) => connInfo = ci)
                .Returns(true);
            // Applying the session SET options fails: broken connection, killed session, expired
            // token. This runs on the first query after every connect. It used to escape through an
            // async void method and terminate the service process.
            connectionService
                .Setup(s => s.GetOrOpenConnection(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new InvalidOperationException("simulated: applying session SET options failed"));

            var queryService = CreateService(connectionService);
            var (requestContext, events) = CreateRequestContext();
            var queryParams = new ExecuteDocumentSelectionParams
            {
                QuerySelection = Common.WholeDocument,
                OwnerUri = Constants.OwnerUri
            };

            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);
            await queryService.WorkTask;
            await WaitUntil(() => events.Completions >= 1);

            Assert.Multiple(() =>
            {
                Assert.That(events.Results, Is.EqualTo(1), "the client was told the query was accepted");
                Assert.That(events.Completions, Is.EqualTo(1),
                    "an accepted query must always end with a QueryCompleteEvent");
                Assert.That(events.ReportedError, Does.Contain("simulated"), "the reason is reported to the user");
            });
        }
    }
}
