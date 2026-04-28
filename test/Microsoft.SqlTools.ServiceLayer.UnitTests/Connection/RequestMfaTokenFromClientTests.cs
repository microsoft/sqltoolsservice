//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.SqlCore.Connection;
using static Microsoft.SqlTools.Utility.SqlConstants;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    /// <summary>
    /// Unit tests for the RequestMfaTokenFromClient feature across ConnectionService —
    /// covers TryOpenConnection (cat 3), TryRequestRefreshAuthToken (cat 4),
    /// and the static helpers ConfigureSqlConnectionAuth / CreateServerConnection (cat 5).
    /// </summary>
    [TestFixture]
    public class RequestMfaTokenFromClientTests
    {
#region Test helpers    
        private static readonly DateTimeOffset FarFuture = DateTimeOffset.UtcNow.AddHours(1);

        private static Func<Task<(string token, DateTimeOffset expiresOn)>> MakeFetcher(string token = "fake-token")
            => () => Task.FromResult((token, FarFuture));

        /// <summary>
        /// Factory that creates real ReliableSqlConnections and exposes the last one created,
        /// so tests can inspect properties set before Open() is called.
        /// </summary>
        private sealed class CapturingReliableConnectionFactory : ISqlConnectionFactory
        {
            public ReliableSqlConnection LastCreated { get; private set; }
            /// <summary>The azureAccountToken argument passed to the last CreateSqlConnection call.</summary>
            public string LastTokenArgument { get; private set; }

            public System.Data.Common.DbConnection CreateSqlConnection(
                string connectionString,
                string azureAccountToken,
                SqlRetryLogicBaseProvider retryProvider = null)
            {
                LastTokenArgument = azureAccountToken;
                var policy = RetryPolicyFactory.CreateNoRetryPolicy();
                var conn = new ReliableSqlConnection(connectionString, policy, policy, azureAccountToken);
                LastCreated = conn;
                return conn;
            }
        }

        /// <summary>
        /// ConnectionService subclass that overrides OpenConnectionAsync to throw immediately,
        /// bypassing the real network open without any static hook in production code.
        /// </summary>
        private sealed class FastFailConnectionService : ConnectionService
        {
            public FastFailConnectionService(ISqlConnectionFactory factory) : base(factory) { }

            protected override Task OpenConnectionAsync(DbConnection connection, CancellationToken token)
                => Task.FromException(new InvalidOperationException("Unit test: fast-fail open"));
        }

        /// <summary>
        /// Returns ConnectionDetails configured for AzureMFA with dummy account/tenant IDs.
        /// </summary>
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

#endregion

        [SetUp]
        public void SetUp()
        {
            // Reset singleton flags so tests are hermetic regardless of what prior tests in the
            // full suite may have set. BuildConnectionString reads from Instance, not from a local
            // ConnectionService instance, so we need the singleton to be clean.
            ConnectionService.Instance.EnableSqlAuthenticationProvider = false;
            ConnectionService.Instance.RequestMfaTokenFromClient = false;
        }

        [TearDown]
        public void TearDown()
        {
            ConnectionService.Instance.EnableSqlAuthenticationProvider = false;
            ConnectionService.Instance.RequestMfaTokenFromClient = false;
        }

        // ---------------------------------------------------------------
        // Category 3 — TryOpenConnection
        // ---------------------------------------------------------------

        [Test]
        public async Task TryOpenConnectionSetsAccessTokenCallbackWhenFlagTrue()
        {
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = true };

            await svc.Connect(new ConnectParams
            {
                OwnerUri = "test://uri-3-1",
                Connection = AzureMfaDetails()
            });

            Assert.That(factory.LastCreated, Is.Not.Null, "factory should have created a connection");
            Assert.That(factory.LastCreated.AccessTokenCallback, Is.Not.Null,
                "AccessTokenCallback should be set in RequestMfaTokenFromClient mode");
            Assert.That(factory.LastCreated.GetUnderlyingConnection().AccessToken, Is.Null,
                "static AccessToken should be null when using the callback path");
        }

        [Test]
        public async Task TryOpenConnectionSetsStaticTokenWhenFlagFalse()
        {
            const string staticToken = "static-azure-token";
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = false };

            var details = AzureMfaDetails();
            details.AzureAccountToken = staticToken;

            await svc.Connect(new ConnectParams
            {
                OwnerUri = "test://uri-3-2",
                Connection = details
            });

            Assert.That(factory.LastCreated, Is.Not.Null);
            Assert.That(factory.LastTokenArgument, Is.EqualTo(staticToken),
                "factory should have been called with the static token when RequestMfaTokenFromClient is false");
            Assert.That(factory.LastCreated.AccessTokenCallback, Is.Null,
                "AccessTokenCallback should be null in the static-token path");
        }

        [Test]
        public async Task TryOpenConnectionSetsAzureTokenFetcherOnConnectionInfoWhenFlagTrue()
        {
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = true };
            const string uri = "test://uri-3-3";

            // Pre-register a ConnectionInfo so it stays in the map even after a failed open
            var connInfo = new ConnectionInfo(factory, uri, AzureMfaDetails());
            svc.OwnerToConnectionMap[uri] = connInfo;

            await svc.Connect(new ConnectParams { OwnerUri = uri, Connection = AzureMfaDetails() });

            svc.TryFindConnection(uri, out var registeredInfo);
            Assert.That(registeredInfo?.AzureTokenFetcher, Is.Not.Null,
                "AzureTokenFetcher should be set on ConnectionInfo when RequestMfaTokenFromClient is true");
        }

        [Test]
        public async Task TryOpenConnectionDoesNotSetFetcherForNonAzureMfaAuth()
        {
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = true };

            var details = TestObjects.GetTestConnectionDetails(); // SqlLogin, no AzureMFA

            await svc.Connect(new ConnectParams
            {
                OwnerUri = "test://test-uri",
                Connection = details
            });

            Assert.That(factory.LastCreated, Is.Not.Null);
            Assert.That(factory.LastCreated.AccessTokenCallback, Is.Null,
                "AccessTokenCallback should not be set for non-AzureMFA connections");
            Assert.That(factory.LastCreated.GetUnderlyingConnection().AccessToken, Is.Null,
                "AccessToken should not be set for non-AzureMFA connections");
        }

        [Test]
        public async Task TryOpenConnectionSharesFetcherAcrossConnectionsWithSameAccountAndTenant()
        {
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = true };
            const string uri1 = "test://test-uri-1";
            const string uri2 = "test://test-uri-2";

            // Same account + tenant → should share one CachingTokenFetcher via the dictionary.
            var details = AzureMfaDetails();
            var connInfo1 = new ConnectionInfo(factory, uri1, details);
            var connInfo2 = new ConnectionInfo(factory, uri2, details);
            svc.OwnerToConnectionMap[uri1] = connInfo1;
            svc.OwnerToConnectionMap[uri2] = connInfo2;

            await svc.Connect(new ConnectParams { OwnerUri = uri1, Connection = details });
            await svc.Connect(new ConnectParams { OwnerUri = uri2, Connection = details });

            svc.TryFindConnection(uri1, out var info1);
            svc.TryFindConnection(uri2, out var info2);

            Assert.That(info1?.AzureTokenFetcher, Is.Not.Null);
            Assert.That(info2?.AzureTokenFetcher, Is.Not.Null);
            Assert.That(info1.AzureTokenFetcher.Target, Is.SameAs(info2.AzureTokenFetcher.Target),
                "connections with the same accountId + tenantId should share one CachingTokenFetcher");
        }

        [Test]
        public async Task TryOpenConnectionUsesSeparateFetchersForDifferentAccounts()
        {
            var factory = new CapturingReliableConnectionFactory();
            var svc = new FastFailConnectionService(factory) { RequestMfaTokenFromClient = true };
            const string uri1 = "test://test-uri-1";
            const string uri2 = "test://test-uri-2";

            var details1 = AzureMfaDetails();
            var details2 = AzureMfaDetails();
            details2.AccountId = "different-account-id";

            var connInfo1 = new ConnectionInfo(factory, uri1, details1);
            var connInfo2 = new ConnectionInfo(factory, uri2, details2);
            svc.OwnerToConnectionMap[uri1] = connInfo1;
            svc.OwnerToConnectionMap[uri2] = connInfo2;

            await svc.Connect(new ConnectParams { OwnerUri = uri1, Connection = details1 });
            await svc.Connect(new ConnectParams { OwnerUri = uri2, Connection = details2 });

            svc.TryFindConnection(uri1, out var info1);
            svc.TryFindConnection(uri2, out var info2);

            Assert.That(info1?.AzureTokenFetcher, Is.Not.Null);
            Assert.That(info2?.AzureTokenFetcher, Is.Not.Null);
            Assert.That(info1.AzureTokenFetcher.Target, Is.Not.SameAs(info2.AzureTokenFetcher.Target),
                "connections with different accountIds should use separate CachingTokenFetcher instances");
        }

        [Test]
        public async Task TryRequestRefreshAuthToken()
        {
            var svc = new ConnectionService(TestObjects.GetTestSqlConnectionFactory())
            {
                RequestMfaTokenFromClient = true
            };

            bool result = await svc.TryRequestRefreshAuthToken("test://test-uri");
            Assert.That(result, Is.False, "TryRequestRefreshAuthToken should return false in RequestMfaTokenFromClient mode");

            svc = new ConnectionService(TestObjects.GetTestSqlConnectionFactory())
            {
                EnableSqlAuthenticationProvider = true
            };

            result = await svc.TryRequestRefreshAuthToken("test://test-uri");
            Assert.That(result, Is.False, "TryRequestRefreshAuthToken should return false when EnableSqlAuthenticationProvider is true");
        }

        [Test]
        public void ConfigureSqlConnectionAuthSetsAccessTokenCallbackWhenFetcherSet()
        {
            var connInfo = TestObjects.GetTestConnectionInfo();
            connInfo.ConnectionDetails.AuthenticationType = AzureMFA;
            connInfo.AzureTokenFetcher = MakeFetcher();

            var sqlConn = new SqlConnection("Server=fake;");
            ConnectionService.ConfigureSqlConnectionAuth(sqlConn, connInfo);

            Assert.That(sqlConn.AccessTokenCallback, Is.Not.Null,
                "AccessTokenCallback should be set when AzureTokenFetcher is present");
            Assert.That(sqlConn.AccessToken, Is.Null,
                "static AccessToken should remain null when using the callback path");
        }

        [Test]
        public void ConfigureSqlConnectionAuthSetsAccessTokenWhenFetcherNullAndTokenSet()
        {
            const string token = "static-tok";
            var connInfo = TestObjects.GetTestConnectionInfo();
            connInfo.ConnectionDetails.AuthenticationType = AzureMFA;
            connInfo.ConnectionDetails.AzureAccountToken = token;
            connInfo.AzureTokenFetcher = null;

            var sqlConn = new SqlConnection("Server=fake;");
            ConnectionService.ConfigureSqlConnectionAuth(sqlConn, connInfo);

            Assert.That(sqlConn.AccessToken, Is.EqualTo(token),
                "static AccessToken should be set when AzureAccountToken is present and no fetcher");
            Assert.That(sqlConn.AccessTokenCallback, Is.Null,
                "AccessTokenCallback should remain null in the static-token path");
        }

        [Test]
        public void ConfigureSqlConnectionAuthSetsNeitherTokenWhenNotAzureMfa()
        {
            var connInfo = TestObjects.GetTestConnectionInfo();
            connInfo.ConnectionDetails.AuthenticationType = SqlLogin;
            connInfo.AzureTokenFetcher = MakeFetcher();
            connInfo.ConnectionDetails.AzureAccountToken = "some-token";

            var sqlConn = new SqlConnection("Server=fake;");
            ConnectionService.ConfigureSqlConnectionAuth(sqlConn, connInfo);

            Assert.That(sqlConn.AccessToken, Is.Null, "AccessToken should not be set for non-AzureMFA auth");
            Assert.That(sqlConn.AccessTokenCallback, Is.Null, "AccessTokenCallback should not be set for non-AzureMFA auth");
        }

        [Test]
        public void CreateServerConnectionReturnsCallbackServerConnectionWhenFetcherSet()
        {
            var connInfo = TestObjects.GetTestConnectionInfo();
            connInfo.ConnectionDetails.AuthenticationType = AzureMFA;
            connInfo.AzureTokenFetcher = MakeFetcher();

            var sqlConn = new SqlConnection("Server=fake;");
            ServerConnection serverConn = ConnectionService.CreateServerConnection(sqlConn, connInfo);

            Assert.That(serverConn.AccessToken, Is.InstanceOf<CallbackAzureAccessToken>(),
                "ServerConnection should use CallbackAzureAccessToken when AzureTokenFetcher is set");
        }

        [Test]
        public void CreateServerConnectionReturnsAzureAccessTokenServerConnectionWhenStaticTokenSet()
        {
            var connInfo = TestObjects.GetTestConnectionInfo();
            connInfo.ConnectionDetails.AuthenticationType = AzureMFA;
            connInfo.ConnectionDetails.AzureAccountToken = "static-tok";
            connInfo.AzureTokenFetcher = null;

            var sqlConn = new SqlConnection("Server=fake;");
            ServerConnection serverConn = ConnectionService.CreateServerConnection(sqlConn, connInfo);

            Assert.That(serverConn.AccessToken, Is.InstanceOf<AzureAccessToken>(),
                "ServerConnection should use AzureAccessToken when only a static token is available");
        }

        [Test]
        public void CreateServerConnectionReturnsPlainServerConnectionWhenNoToken()
        {
            var connInfo = TestObjects.GetTestConnectionInfo();
            // Default details have no Azure token and SqlLogin auth
            connInfo.AzureTokenFetcher = null;

            var sqlConn = new SqlConnection("Server=fake;");
            ServerConnection serverConn = ConnectionService.CreateServerConnection(sqlConn, connInfo);

            Assert.That(serverConn.AccessToken, Is.Null,
                "ServerConnection should have no IRenewableToken when no Azure token is configured");
        }
    }
}
