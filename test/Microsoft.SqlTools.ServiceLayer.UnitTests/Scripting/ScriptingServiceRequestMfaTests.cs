//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;
using static Microsoft.SqlTools.Utility.SqlConstants;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Scripting
{
    /// <summary>
    /// Unit tests for ScriptingService token routing when RequestMfaTokenFromClient is enabled.
    /// </summary>
    [TestFixture]
    public class ScriptingServiceRequestMfaTests
    {
        private static readonly DateTimeOffset FarFuture = DateTimeOffset.UtcNow.AddHours(1);

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static Func<Task<(string token, DateTimeOffset expiresOn)>> MakeFetcher()
            => () => Task.FromResult(("fake-token", FarFuture));

        /// <summary>
        /// Returns a ScriptingParams that routes through ShouldCreateScriptAsOperation = true.
        /// </summary>
        private static ScriptingParams ScriptAsParams(string ownerUri) => new ScriptingParams
        {
            OwnerUri = ownerUri,
            Operation = ScriptingOperationType.Select,
            ScriptingObjects = new List<ScriptingObject>
            {
                new ScriptingObject { Type = "Table", Name = "dbo.T", Schema = "dbo" }
            }
        };

        /// <summary>
        /// Returns a ScriptingParams that routes through ShouldCreateScriptAsOperation = false.
        /// </summary>
        private static ScriptingParams ScriptingScriptParams(string ownerUri) => new ScriptingParams
        {
            OwnerUri = ownerUri,
            Operation = ScriptingOperationType.Create, // default — goes to ScriptingScriptOperation
            ScriptingObjects = new List<ScriptingObject>
            {
                new ScriptingObject { Type = "Table", Name = "dbo.T", Schema = "dbo" }
            }
        };

        private static ConnectionDetails AzureMfaDetails() => new ConnectionDetails
        {
            ServerName = "fake-server",
            DatabaseName = "fake-db",
            AuthenticationType = AzureMFA,
            AccountId = "account-id",
            TenantId = "tenant-id",
        };

        private static Mock<RequestContext<ScriptingResult>> MakeRequestContext()
        {
            var ctx = new Mock<RequestContext<ScriptingResult>>();
            ctx.Setup(x => x.SendResult(It.IsAny<ScriptingResult>()))
               .Returns(Task.FromResult(new object()));
            // Allow any SendEvent calls (fired by background task event handlers)
            ctx.Setup(x => x.SendError(It.IsAny<string>()))
               .Returns(Task.FromResult(new object()));
            return ctx;
        }

        /// <summary>
        /// Pre-registers a ConnectionInfo in a fresh ConnectionService and sets it as the
        /// ScriptingService's ConnectionServiceInstance for the duration of the test.
        /// Returns the ConnectionInfo for further property manipulation.
        /// </summary>
        private static ConnectionInfo SetupConnectionService(string ownerUri, ConnectionDetails details,
            out ConnectionService connectionService)
        {
            connectionService = new ConnectionService(TestObjects.GetTestSqlConnectionFactory());
            var connInfo = new ConnectionInfo(TestObjects.GetTestSqlConnectionFactory(), ownerUri, details);
            connectionService.OwnerToConnectionMap[ownerUri] = connInfo;
            ScriptingService.ConnectionServiceInstance = connectionService;
            return connInfo;
        }

        [TearDown]
        public void TearDown()
        {
            // Reset static overrides so they don't bleed between tests
            ConnectionService.OpenServerConnectionOverride = null;
            ScriptingService.ConnectionServiceInstance = null;
        }

        [Test]
        public async Task HandleScriptExecuteCallsOpenServerConnectionWhenFetcherSet()
        {
            const string uri = "test://6-1";
            var connInfo = SetupConnectionService(uri, AzureMfaDetails(), out _);
            connInfo.AzureTokenFetcher = MakeFetcher();

            bool overrideCalled = false;
            ConnectionService.OpenServerConnectionOverride = (ci, fn) =>
            {
                overrideCalled = true;
                return new ServerConnection(new SqlConnection("Server=fake;"));
            };

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(overrideCalled, Is.True,
                "OpenServerConnection should be called when AzureTokenFetcher is set");
        }

        [Test]
        public async Task HandleScriptExecuteDoesNotCallOpenServerConnectionWhenStaticTokenSet()
        {
            const string uri = "test://6-2";
            var details = AzureMfaDetails();
            details.AzureAccountToken = "static-tok";
            var connInfo = SetupConnectionService(uri, details, out _);
            connInfo.AzureTokenFetcher = null; // no fetcher — uses static token

            bool overrideCalled = false;
            ConnectionService.OpenServerConnectionOverride = (ci, fn) =>
            {
                overrideCalled = true;
                return new ServerConnection(new SqlConnection("Server=fake;"));
            };

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(overrideCalled, Is.False,
                "OpenServerConnection should not be called when using the static token path");
        }

        [Test]
        public async Task HandleScriptExecuteFetchesAccessTokenUpfrontForScriptingScript()
        {
            const string uri = "test://6-3";
            var connInfo = SetupConnectionService(uri, AzureMfaDetails(), out _);

            int fetcherCallCount = 0;
            connInfo.AzureTokenFetcher = () =>
            {
                fetcherCallCount++;
                return Task.FromResult(("fetched-tok", FarFuture));
            };

            // Bypass OpenServerConnection so no real network call is made.
            // (HandleScriptExecuteRequest always calls OpenServerConnection when AzureTokenFetcher
            // is set, even for the ScriptingScript path; the resulting ServerConnection is discarded.)
            ConnectionService.OpenServerConnectionOverride =
                (ci, fn) => new ServerConnection(new SqlConnection("Server=fake;"));

            // Use ScriptingScript parameters (Operation = Create, no ScriptAs)
            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptingScriptParams(uri), MakeRequestContext().Object);

            Assert.That(fetcherCallCount, Is.EqualTo(1),
                "AzureTokenFetcher should be called once to pre-fetch the token for ScriptingScript");
        }

        [Test]
        public async Task HandleScriptExecuteSetsNeitherTokenWhenNotAzureMfa()
        {
            const string uri = "test://6-4";
            var details = TestObjects.GetTestConnectionDetails(); // SqlLogin
            var connInfo = SetupConnectionService(uri, details, out _);
            connInfo.AzureTokenFetcher = null;

            bool overrideCalled = false;
            ConnectionService.OpenServerConnectionOverride = (ci, fn) =>
            {
                overrideCalled = true;
                return new ServerConnection(new SqlConnection("Server=fake;"));
            };

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(overrideCalled, Is.False,
                "OpenServerConnection should not be called for non-AzureMFA connections");
        }
    }
}
