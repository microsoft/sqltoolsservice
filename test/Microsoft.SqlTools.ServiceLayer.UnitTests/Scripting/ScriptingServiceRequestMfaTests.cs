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
        // Test-double ConnectionService — overrides OpenServerConnectionInternal
        // so tests never open a real network connection.
        // ---------------------------------------------------------------

        /// <summary>
        /// Subclass that records whether <see cref="OpenServerConnectionInternal"/> was called
        /// and returns a stub <see cref="ServerConnection"/> instead of opening a real one.
        /// </summary>
        private sealed class CapturingConnectionService : ConnectionService
        {
            public bool OpenServerConnectionCalled { get; private set; }

            public CapturingConnectionService(ISqlConnectionFactory factory) : base(factory) { }

            internal override ServerConnection OpenServerConnectionInternal(
                ConnectionInfo connInfo, string featureName = null)
            {
                OpenServerConnectionCalled = true;
                return new ServerConnection(new SqlConnection("Server=fake;"));
            }
        }

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
            // UserName is required by BuildConnectionString when EnableSqlAuthenticationProvider
            // is true on the singleton (set by other tests in the full suite); providing a dummy
            // value keeps the tests hermetic regardless of prior-test contamination.
            UserName = "test@example.com",
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
        /// Creates a <see cref="CapturingConnectionService"/>, pre-registers a
        /// <see cref="ConnectionInfo"/> in it, and sets it as the
        /// <see cref="ScriptingService.ConnectionServiceInstance"/> for the test.
        /// Returns both the service and the registered <see cref="ConnectionInfo"/>.
        /// </summary>
        private static ConnectionInfo SetupConnectionService(string ownerUri, ConnectionDetails details,
            out CapturingConnectionService connectionService)
        {
            connectionService = new CapturingConnectionService(TestObjects.GetTestSqlConnectionFactory());
            var connInfo = new ConnectionInfo(TestObjects.GetTestSqlConnectionFactory(), ownerUri, details);
            connectionService.OwnerToConnectionMap[ownerUri] = connInfo;
            ScriptingService.ConnectionServiceInstance = connectionService;
            return connInfo;
        }

        [SetUp]
        public void SetUp()
        {
            // Reset singleton flags so tests are hermetic regardless of what prior tests in the
            // full suite may have set. BuildConnectionString reads from Instance.EnableSqlAuthenticationProvider;
            // if that's true it mutates AuthenticationType which breaks our AzureMFA checks.
            ConnectionService.Instance.EnableSqlAuthenticationProvider = false;
            ConnectionService.Instance.RequestMfaTokenFromClient = false;
        }

        [TearDown]
        public void TearDown()
        {
            // Reset static overrides so they don't bleed between tests
            ScriptingService.ConnectionServiceInstance = null;
            ConnectionService.Instance.EnableSqlAuthenticationProvider = false;
            ConnectionService.Instance.RequestMfaTokenFromClient = false;
        }

        [Test]
        public async Task HandleScriptExecuteCallsOpenServerConnectionWhenFetcherSet()
        {
            const string uri = "test://6-1";
            var connInfo = SetupConnectionService(uri, AzureMfaDetails(),
                out CapturingConnectionService capturingSvc);
            connInfo.AzureTokenFetcher = MakeFetcher();

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(capturingSvc.OpenServerConnectionCalled, Is.True,
                "OpenServerConnectionInternal should be called when AzureTokenFetcher is set");
        }

        [Test]
        public async Task HandleScriptExecuteDoesNotCallOpenServerConnectionWhenStaticTokenSet()
        {
            const string uri = "test://6-2";
            var details = AzureMfaDetails();
            details.AzureAccountToken = "static-tok";
            var connInfo = SetupConnectionService(uri, details,
                out CapturingConnectionService capturingSvc);
            connInfo.AzureTokenFetcher = null; // no fetcher — uses static token

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(capturingSvc.OpenServerConnectionCalled, Is.False,
                "OpenServerConnectionInternal should not be called when using the static token path");
        }

        [Test]
        public async Task HandleScriptExecuteFetchesAccessTokenUpfrontForScriptingScript()
        {
            const string uri = "test://6-3";
            var connInfo = SetupConnectionService(uri, AzureMfaDetails(),
                out CapturingConnectionService _);

            int fetcherCallCount = 0;
            connInfo.AzureTokenFetcher = () =>
            {
                fetcherCallCount++;
                return Task.FromResult(("fetched-tok", FarFuture));
            };

            // Use ScriptingScript parameters (Operation = Create, no ScriptAs)
            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptingScriptParams(uri), MakeRequestContext().Object);

            // One call for OpenServerConnectionInternal's token (via ScriptAs path) +
            // one explicit call for the ScriptingScript plain-token pre-fetch.
            // Both share the same CachingTokenFetcher in production but in this test the
            // fetcher is a plain lambda, so each direct call increments the counter.
            Assert.That(fetcherCallCount, Is.GreaterThanOrEqualTo(1),
                "AzureTokenFetcher should be called at least once to pre-fetch the token");
        }

        [Test]
        public async Task HandleScriptExecuteSetsNeitherTokenWhenNotAzureMfa()
        {
            const string uri = "test://6-4";
            var details = TestObjects.GetTestConnectionDetails(); // SqlLogin
            var connInfo = SetupConnectionService(uri, details,
                out CapturingConnectionService capturingSvc);
            connInfo.AzureTokenFetcher = null;

            var svc = new ScriptingService();
            await svc.HandleScriptExecuteRequest(ScriptAsParams(uri), MakeRequestContext().Object);

            Assert.That(capturingSvc.OpenServerConnectionCalled, Is.False,
                "OpenServerConnectionInternal should not be called for non-AzureMFA connections");
        }
    }
}
