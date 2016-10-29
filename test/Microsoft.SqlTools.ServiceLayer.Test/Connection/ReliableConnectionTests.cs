//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#if LIVE_CONNECTION_TESTS

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection.RetryPolicy;
using static Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection.RetryPolicy.TimeBasedRetryPolicy;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for the ReliableConnection module.
    /// These tests all assume a live connection to a database on localhost using integrated auth.
    /// </summary>
    public class ReliableConnectionTests
    {
        internal class TestFixedDelayPolicy : FixedDelayPolicy
        {
            public TestFixedDelayPolicy(
                IErrorDetectionStrategy strategy, 
                int maxRetryCount, 
                TimeSpan intervalBetweenRetries)
                : base(strategy, 
                    maxRetryCount, 
                    intervalBetweenRetries)
            {
            }

            public bool InvokeShouldRetryImpl(RetryState retryStateObj)
            {
                return ShouldRetryImpl(retryStateObj);
            }
        }

        internal class TestProgressiveRetryPolicy : ProgressiveRetryPolicy
        {
            public TestProgressiveRetryPolicy(
                IErrorDetectionStrategy strategy, 
                int maxRetryCount, 
                TimeSpan initialInterval, 
                TimeSpan increment)
                : base(strategy, 
                    maxRetryCount, 
                    initialInterval, 
                    increment)
            {         
            }

            public bool InvokeShouldRetryImpl(RetryState retryStateObj)
            {
                return ShouldRetryImpl(retryStateObj);
            }
        }

        internal class TestTimeBasedRetryPolicy : TimeBasedRetryPolicy
        {
            public TestTimeBasedRetryPolicy(
                IErrorDetectionStrategy strategy,
                TimeSpan minTotalRetryTimeLimit,
                TimeSpan maxTotalRetryTimeLimit,
                double totalRetryTimeLimitRate,
                TimeSpan minInterval,
                TimeSpan maxInterval,
                double intervalFactor) 
                : base(
                    strategy,
                    minTotalRetryTimeLimit,
                    maxTotalRetryTimeLimit,
                    totalRetryTimeLimitRate,
                    minInterval,
                    maxInterval,
                    intervalFactor)
            {
            }

            public bool InvokeShouldRetryImpl(RetryState retryStateObj)
            {
                return ShouldRetryImpl(retryStateObj);
            }
        }

        [Fact]
        public void FixedDelayPolicyTest()
        {
            TestFixedDelayPolicy policy = new TestFixedDelayPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                intervalBetweenRetries: TimeSpan.FromMilliseconds(100));
            bool shouldRety = policy.InvokeShouldRetryImpl(new RetryStateEx());
            Assert.True(shouldRety);
        }

        [Fact]
        public void ProgressiveRetryPolicyTest()
        {
            TestProgressiveRetryPolicy policy = new TestProgressiveRetryPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                initialInterval: TimeSpan.FromMilliseconds(100), 
                increment: TimeSpan.FromMilliseconds(100));
            bool shouldRety = policy.InvokeShouldRetryImpl(new RetryStateEx());
            Assert.True(shouldRety);
        }
        
        [Fact]
        public void TimeBasedRetryPolicyTest()
        {
            TestTimeBasedRetryPolicy policy = new TestTimeBasedRetryPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                minTotalRetryTimeLimit: TimeSpan.FromMilliseconds(100),
                maxTotalRetryTimeLimit: TimeSpan.FromMilliseconds(100),
                totalRetryTimeLimitRate: 100,
                minInterval: TimeSpan.FromMilliseconds(100),
                maxInterval: TimeSpan.FromMilliseconds(100),
                intervalFactor: 1);
            bool shouldRety = policy.InvokeShouldRetryImpl(new RetryStateEx());
            Assert.True(shouldRety);
        }

        /// <summary>
        /// Environment variable that stores the name of the test server hosting the SQL Server instance.
        /// </summary>
        public static string TestServerEnvironmentVariable
        {
            get { return "TEST_SERVER"; }
        }

        private static Lazy<string> testServerName = new Lazy<string>(() => Environment.GetEnvironmentVariable(TestServerEnvironmentVariable));

        /// <summary>
        /// Name of the test server hosting the SQL Server instance.
        /// </summary>
        public static string TestServerName
        {
            get { return testServerName.Value; }
        }

        /// <summary>
        /// Helper method to create an integrated auth connection builder for testing.
        /// </summary>
        private SqlConnectionStringBuilder CreateTestConnectionStringBuilder()
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
            csb.DataSource = TestServerName;
            csb.IntegratedSecurity = true;

            return csb;
        }

        /// <summary>
        /// Helper method to create an integrated auth reliable connection for testing.
        /// </summary>
        private DbConnection CreateTestConnection()
        {
            SqlConnectionStringBuilder csb = CreateTestConnectionStringBuilder();

            RetryPolicy connectionRetryPolicy = RetryPolicyFactory.CreateDefaultConnectionRetryPolicy();
            RetryPolicy commandRetryPolicy = RetryPolicyFactory.CreateDefaultConnectionRetryPolicy();

            ReliableSqlConnection connection = new ReliableSqlConnection(csb.ConnectionString, connectionRetryPolicy, commandRetryPolicy);
            return connection;
        }

        /// <summary>
        /// Test ReliableConnectionHelper.GetDefaultDatabaseFilePath()
        /// </summary>
        [Fact]
        public void TestGetDefaultDatabaseFilePath()
        {
            TestUtils.RunIfWindows(() =>
            {
                var connectionBuilder = CreateTestConnectionStringBuilder();
                Assert.NotNull(connectionBuilder);

                string filePath = string.Empty;
                string logPath = string.Empty;

                ReliableConnectionHelper.OpenConnection(
                    connectionBuilder,
                    usingConnection: (conn) => 
                    {
                        filePath = ReliableConnectionHelper.GetDefaultDatabaseFilePath(conn);
                        logPath = ReliableConnectionHelper.GetDefaultDatabaseLogPath(conn);
                    },
                    catchException: null,
                    useRetry: false);

                Assert.False(string.IsNullOrWhiteSpace(filePath));
                Assert.False(string.IsNullOrWhiteSpace(logPath));
            });
        }

        /// <summary>
        /// Test ReliableConnectionHelper.GetServerVersion()
        /// </summary>
        [Fact]
        public void TestGetServerVersion()
        {
            TestUtils.RunIfWindows(() => 
            {
                using (var connection = CreateTestConnection())
                {
                    Assert.NotNull(connection);
                    connection.Open();

                    ReliableConnectionHelper.ServerInfo serverInfo = ReliableConnectionHelper.GetServerVersion(connection);
                    ReliableConnectionHelper.ServerInfo serverInfo2;
                    using (var connection2 = CreateTestConnection())
                    {
                        connection2.Open();
                        serverInfo2 = ReliableConnectionHelper.GetServerVersion(connection);
                    }

                    Assert.NotNull(serverInfo);
                    Assert.NotNull(serverInfo2);
                    Assert.True(serverInfo.ServerMajorVersion != 0);
                    Assert.True(serverInfo.ServerMajorVersion == serverInfo2.ServerMajorVersion);
                    Assert.True(serverInfo.ServerMinorVersion == serverInfo2.ServerMinorVersion);
                    Assert.True(serverInfo.ServerReleaseVersion == serverInfo2.ServerReleaseVersion);
                    Assert.True(serverInfo.ServerEdition == serverInfo2.ServerEdition);
                    Assert.True(serverInfo.IsCloud == serverInfo2.IsCloud);
                    Assert.True(serverInfo.AzureVersion == serverInfo2.AzureVersion);
                }
            });
        }

        /// <summary>
        /// Tests ReliableConnectionHelper.GetCompleteServerName()
        /// </summary>
        [Fact]
        public void TestGetCompleteServerName()
        {
            string name = ReliableConnectionHelper.GetCompleteServerName(@".\SQL2008");
            Assert.True(name.Contains(Environment.MachineName));

            name = ReliableConnectionHelper.GetCompleteServerName(@"(local)");
            Assert.True(name.Contains(Environment.MachineName));
        }

        /// <summary>
        /// Tests ReliableConnectionHelper.IsDatabaseReadonly()
        /// </summary>
        [Fact]
        public void TestIsDatabaseReadonly()
        {
            var connectionBuilder = CreateTestConnectionStringBuilder();
            Assert.NotNull(connectionBuilder);

            bool isReadOnly = ReliableConnectionHelper.IsDatabaseReadonly(connectionBuilder);
            Assert.False(isReadOnly);
        }

        /// <summary>
        /// Verify ANSI_NULL and QUOTED_IDENTIFIER settings can be set and retrieved for a session
        /// </summary>
        [Fact]
        public void VerifyAnsiNullAndQuotedIdentifierSettingsReplayed()
        {
            TestUtils.RunIfWindows(() =>
            {
                using (ReliableSqlConnection conn = (ReliableSqlConnection)ReliableConnectionHelper.OpenConnection(CreateTestConnectionStringBuilder(), useRetry: true))
                {
                    VerifySessionSettings(conn, true);
                    VerifySessionSettings(conn, false);
                }
            });
        }

        private void VerifySessionSettings(ReliableSqlConnection conn, bool expectedSessionValue)
        {
            Tuple<string, bool>[] settings = null;
            using (IDbCommand cmd = conn.CreateCommand())
            {
                if (expectedSessionValue)
                {
                    cmd.CommandText = "SET  ANSI_NULLS, QUOTED_IDENTIFIER ON";
                }
                else
                {
                    cmd.CommandText = "SET  ANSI_NULLS, QUOTED_IDENTIFIER OFF";
                }

                cmd.ExecuteNonQuery();

                //baseline assertion
                AssertSessionValues(cmd, ansiNullsValue: expectedSessionValue, quotedIdentifersValue: expectedSessionValue);

                // verify the initial values are correct
                settings = conn.CacheOrReplaySessionSettings(cmd, settings);

                // assert no change is session settings
                AssertSessionValues(cmd, ansiNullsValue: expectedSessionValue, quotedIdentifersValue: expectedSessionValue);

                // assert cached settings are correct
                Assert.Equal("ANSI_NULLS", settings[0].Item1);
                Assert.Equal(expectedSessionValue, settings[0].Item2);

                Assert.Equal("QUOTED_IDENTIFIER", settings[1].Item1);
                Assert.Equal(expectedSessionValue, settings[1].Item2);

                // invert session values and assert we reset them

                if (expectedSessionValue)
                {
                    cmd.CommandText = "SET  ANSI_NULLS, QUOTED_IDENTIFIER OFF";
                }
                else
                {
                    cmd.CommandText = "SET  ANSI_NULLS, QUOTED_IDENTIFIER ON";
                }
                cmd.ExecuteNonQuery();

                // baseline assertion
                AssertSessionValues(cmd, ansiNullsValue: !expectedSessionValue, quotedIdentifersValue: !expectedSessionValue);

                // replay cached value
                settings = conn.CacheOrReplaySessionSettings(cmd, settings);

                // assert session settings correctly set
                AssertSessionValues(cmd, ansiNullsValue: expectedSessionValue, quotedIdentifersValue: expectedSessionValue);
            }
        }

        private void AssertSessionValues(IDbCommand cmd, bool ansiNullsValue, bool quotedIdentifersValue)
        {
            // assert session was updated
            cmd.CommandText = "SELECT SESSIONPROPERTY ('ANSI_NULLS'), SESSIONPROPERTY ('QUOTED_IDENTIFIER')";
            using (IDataReader reader = cmd.ExecuteReader())
            {
                Assert.True(reader.Read(), "Missing session settings");
                bool actualAnsiNullsOnValue = ((int)reader[0] == 1);
                bool actualQuotedIdentifierOnValue = ((int)reader[1] == 1);
                Assert.Equal(ansiNullsValue, actualAnsiNullsOnValue);
                Assert.Equal(quotedIdentifersValue, actualQuotedIdentifierOnValue);
            }

        }

        /// <summary>
        /// Test that the retry policy factory constructs all possible types of policies successfully.
        /// </summary>
        [Fact]
        public void RetryPolicyFactoryConstructsPoliciesSuccessfully()
        {
            TestUtils.RunIfWindows(() => 
            {
                Assert.NotNull(RetryPolicyFactory.CreateColumnEncryptionTransferRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDatabaseCommandRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDataScriptUpdateRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDefaultConnectionRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDefaultDataConnectionRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDefaultDataSqlCommandRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDefaultDataTransferRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateDefaultSchemaCommandRetryPolicy(true));
                Assert.NotNull(RetryPolicyFactory.CreateDefaultSchemaConnectionRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateElementCommandRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateFastDataRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateNoRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreatePrimaryKeyCommandRetryPolicy());
                Assert.NotNull(RetryPolicyFactory.CreateSchemaCommandRetryPolicy(6));
                Assert.NotNull(RetryPolicyFactory.CreateSchemaConnectionRetryPolicy(6));
            });
        }

        /// <summary>
        /// ReliableConnectionHelper.IsCloud() should be false for a local server
        /// </summary>
        [Fact]
        public void TestIsCloudIsFalseForLocalServer()
        {
            TestUtils.RunIfWindows(() => 
            {
                using (var connection = CreateTestConnection())
                {
                    Assert.NotNull(connection);

                    connection.Open();
                    Assert.False(ReliableConnectionHelper.IsCloud(connection));
                }
            });
        }

        /// <summary>
        /// Tests that ReliableConnectionHelper.OpenConnection() opens a connection if it is closed
        /// </summary>
        [Fact]
        public void TestOpenConnectionOpensConnection()
        {
            TestUtils.RunIfWindows(() =>
            {
                using (var connection = CreateTestConnection())
                {
                    Assert.NotNull(connection);

                    Assert.True(connection.State == ConnectionState.Closed);
                    ReliableConnectionHelper.OpenConnection(connection);
                    Assert.True(connection.State == ConnectionState.Open);
                }
            });
        }

        /// <summary>
        /// Tests that ReliableConnectionHelper.ExecuteNonQuery() runs successfully
        /// </summary>
        [Fact]
        public void TestExecuteNonQuery()
        {
            TestUtils.RunIfWindows(() =>
            {
                var result = ReliableConnectionHelper.ExecuteNonQuery(
                    CreateTestConnectionStringBuilder(),
                    "SET NOCOUNT ON; SET NOCOUNT OFF;",
                    ReliableConnectionHelper.SetCommandTimeout,
                    null,
                    true
                );
                Assert.NotNull(result);
            });
        }

        /// <summary>
        /// Test that TryGetServerVersion() gets server information
        /// </summary>
        [Fact]
        public void TestTryGetServerVersion()
        {
            TestUtils.RunIfWindows(() =>
            {
                ReliableConnectionHelper.ServerInfo info = null;
                Assert.True(ReliableConnectionHelper.TryGetServerVersion(CreateTestConnectionStringBuilder().ConnectionString, out info));

                Assert.NotNull(info);
                Assert.NotNull(info.ServerVersion);
                Assert.NotEmpty(info.ServerVersion);
            });
        }


        /// <summary>
        /// Validate ambient static settings
        /// </summary>
        [Fact]
        public void AmbientSettingsStaticPropertiesTest()
        {
            var defaultSettings = AmbientSettings.DefaultSettings;
            Assert.NotNull(defaultSettings);
            var masterReferenceFilePath = AmbientSettings.MasterReferenceFilePath;
            var maxDataReaderDegreeOfParallelism = AmbientSettings.MaxDataReaderDegreeOfParallelism;
            var tableProgressUpdateInterval = AmbientSettings.TableProgressUpdateInterval;
            var traceRowCountFailure = AmbientSettings.TraceRowCountFailure;
            var useOfflineDataReader = AmbientSettings.UseOfflineDataReader;
            var streamBackingStoreForOfflineDataReading = AmbientSettings.StreamBackingStoreForOfflineDataReading;
            var disableIndexesForDataPhase = AmbientSettings.DisableIndexesForDataPhase;
            var reliableDdlEnabled = AmbientSettings.ReliableDdlEnabled;
            var importModelDatabase = AmbientSettings.ImportModelDatabase;
            var supportAlwaysEncrypted = AmbientSettings.SupportAlwaysEncrypted;
            var alwaysEncryptedWizardMigration = AmbientSettings.AlwaysEncryptedWizardMigration;
            var skipObjectTypeBlocking =AmbientSettings.SkipObjectTypeBlocking;
            var doNotSerializeQueryStoreSettings = AmbientSettings.DoNotSerializeQueryStoreSettings;
            var lockTimeoutMilliSeconds = AmbientSettings.LockTimeoutMilliSeconds;
            var queryTimeoutSeconds = AmbientSettings.QueryTimeoutSeconds;
            var longRunningQueryTimeoutSeconds = AmbientSettings.LongRunningQueryTimeoutSeconds;
            var alwaysRetryOnTransientFailure = AmbientSettings.AlwaysRetryOnTransientFailure;
            var connectionRetryMessageHandler = AmbientSettings.ConnectionRetryMessageHandler;
                        
            using (var settingsContext = AmbientSettings.CreateSettingsContext())
            {
                var settings = settingsContext.Settings;
                Assert.NotNull(settings);
            }
        }
        
        /// <summary>
        /// Validate ambient settings populate
        /// </summary>
        [Fact]
        public void AmbientSettingsPopulateTest()
        {
            var data = new AmbientSettings.AmbientData();

            var masterReferenceFilePath = data.MasterReferenceFilePath;
            data.MasterReferenceFilePath = masterReferenceFilePath;
            var lockTimeoutMilliSeconds = data.LockTimeoutMilliSeconds;
            data.LockTimeoutMilliSeconds = lockTimeoutMilliSeconds;
            var queryTimeoutSeconds = data.QueryTimeoutSeconds;
            data.QueryTimeoutSeconds = queryTimeoutSeconds;
            var longRunningQueryTimeoutSeconds = data.LongRunningQueryTimeoutSeconds;
            data.LongRunningQueryTimeoutSeconds = longRunningQueryTimeoutSeconds;
            var alwaysRetryOnTransientFailure = data.AlwaysRetryOnTransientFailure;
            data.AlwaysRetryOnTransientFailure = alwaysRetryOnTransientFailure;
            var connectionRetryMessageHandler = data.ConnectionRetryMessageHandler;
            data.ConnectionRetryMessageHandler = connectionRetryMessageHandler;
            var traceRowCountFailure = data.TraceRowCountFailure;
            data.TraceRowCountFailure = traceRowCountFailure;
            var tableProgressUpdateInterval = data.TableProgressUpdateInterval;
            data.TableProgressUpdateInterval = tableProgressUpdateInterval;
            var useOfflineDataReader = data.UseOfflineDataReader;
            data.UseOfflineDataReader = useOfflineDataReader;
            var streamBackingStoreForOfflineDataReading = data.StreamBackingStoreForOfflineDataReading;
            data.StreamBackingStoreForOfflineDataReading = streamBackingStoreForOfflineDataReading;
            var disableIndexesForDataPhase = data.DisableIndexesForDataPhase;
            data.DisableIndexesForDataPhase = disableIndexesForDataPhase;
            var reliableDdlEnabled = data.ReliableDdlEnabled;
            data.ReliableDdlEnabled = reliableDdlEnabled;
            var importModelDatabase = data.ImportModelDatabase;
            data.ImportModelDatabase = importModelDatabase;
            var supportAlwaysEncrypted = data.SupportAlwaysEncrypted;
            data.SupportAlwaysEncrypted = supportAlwaysEncrypted;
            var alwaysEncryptedWizardMigration = data.AlwaysEncryptedWizardMigration;
            data.AlwaysEncryptedWizardMigration = alwaysEncryptedWizardMigration;
            var skipObjectTypeBlocking = data.SkipObjectTypeBlocking;
            data.SkipObjectTypeBlocking = skipObjectTypeBlocking;
            var doNotSerializeQueryStoreSettings = data.DoNotSerializeQueryStoreSettings;
            data.DoNotSerializeQueryStoreSettings = doNotSerializeQueryStoreSettings;

            Dictionary<string, object> settings = new Dictionary<string, object>();
            settings.Add("LockTimeoutMilliSeconds", 10000);
            data.PopulateSettings(settings);
            settings["LockTimeoutMilliSeconds"] = 15000;
            data.PopulateSettings(settings);
            data.TraceSettings();
        }

        [Fact]
        public void RetryPolicyFactoryTest()
        {
            Assert.NotNull(RetryPolicyFactory.NoRetryPolicy);
            Assert.NotNull(RetryPolicyFactory.PrimaryKeyViolationRetryPolicy);

            RetryPolicy noRetyPolicy = RetryPolicyFactory.CreateDefaultSchemaCommandRetryPolicy(useRetry: false);

            var retryState = new RetryStateEx();
            retryState.LastError = new Exception();
            RetryPolicyFactory.DataConnectionFailureRetry(retryState);
            RetryPolicyFactory.CommandFailureRetry(retryState, "command");
            RetryPolicyFactory.CommandFailureIgnore(retryState, "command");
            RetryPolicyFactory.ElementCommandFailureIgnore(retryState);
            RetryPolicyFactory.ElementCommandFailureRetry(retryState);
            RetryPolicyFactory.CreateDatabaseCommandFailureIgnore(retryState);
            RetryPolicyFactory.CreateDatabaseCommandFailureRetry(retryState);  
            RetryPolicyFactory.CommandFailureIgnore(retryState);
            RetryPolicyFactory.CommandFailureRetry(retryState);

            var transientPolicy = new RetryPolicyFactory.TransientErrorIgnoreStrategy();
            Assert.False(transientPolicy.CanRetry(new Exception()));
            Assert.False(transientPolicy.ShouldIgnoreError(new Exception()));
        }
    }
}

#endif // LIVE_CONNECTION_TESTS
