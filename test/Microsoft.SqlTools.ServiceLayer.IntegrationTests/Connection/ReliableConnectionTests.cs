//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection.RetryPolicy;
using static Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection.RetryPolicy.TimeBasedRetryPolicy;
using static Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection.SqlSchemaModelErrorCodes;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Connection
{
    /// <summary>
    /// Tests for the ReliableConnection module.
    /// These tests all assume a live connection to a database on localhost using integrated auth.
    /// </summary>
    public class ReliableConnectionTests
    {
        internal class TestDataTransferErrorDetectionStrategy : DataTransferErrorDetectionStrategy
        {
            public bool InvokeCanRetrySqlException(SqlException exception)
            {
                return CanRetrySqlException(exception);
            }
        }
        internal class TestSqlAzureTemporaryAndIgnorableErrorDetectionStrategy : SqlAzureTemporaryAndIgnorableErrorDetectionStrategy
        {
            public TestSqlAzureTemporaryAndIgnorableErrorDetectionStrategy()
                : base (new int[] { 100 })
            {
            }

            public bool InvokeCanRetrySqlException(SqlException exception)
            {
                return CanRetrySqlException(exception);
            }

            public bool InvokeShouldIgnoreSqlException(SqlException exception)
            {
                return ShouldIgnoreSqlException(exception);
            }
        }

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

            public void DoOnIgnoreErrorOccurred(RetryState retryState)
            {
                OnIgnoreErrorOccurred(retryState);
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

        [Test]
        public void FixedDelayPolicyTest()
        {
            TestFixedDelayPolicy policy = new TestFixedDelayPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                intervalBetweenRetries: TimeSpan.FromMilliseconds(100));
            var retryState = new RetryStateEx();
            bool shouldRety = policy.InvokeShouldRetryImpl(retryState);
            policy.DoOnIgnoreErrorOccurred(retryState);
            Assert.True(shouldRety);
        }

        [Test]
        public void FixedDelayPolicyExecuteActionTest()
        {
            TestFixedDelayPolicy policy = new TestFixedDelayPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                intervalBetweenRetries: TimeSpan.FromMilliseconds(20));

            // execute an action that throws a retry limit exception
            CancellationToken token = new CancellationToken();
            Assert.AreEqual(default(int), policy.ExecuteAction<int>((s) => { throw new RetryLimitExceededException(); }, token));

            // execute an action that throws a retry limit exeception with an inner exception 
            Assert.Throws<Exception>(() => 
            {
                policy.ExecuteAction<int>((s) => 
                { 
                    var e = new RetryLimitExceededException("retry", new Exception());
                    throw e; 
                });
            });
        }

        [Test]
        public void IsRetryableExceptionTest()
        {
            TestFixedDelayPolicy policy = new TestFixedDelayPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                intervalBetweenRetries: TimeSpan.FromMilliseconds(20));
                
            Assert.False(policy.IsRetryableException(new Exception()));
        }

        [Test]
        public void ProgressiveRetryPolicyTest()
        {
            TestProgressiveRetryPolicy policy = new TestProgressiveRetryPolicy(
                strategy: new NetworkConnectivityErrorDetectionStrategy(),
                maxRetryCount: 3, 
                initialInterval: TimeSpan.FromMilliseconds(100), 
                increment: TimeSpan.FromMilliseconds(100));
            bool shouldRety = policy.InvokeShouldRetryImpl(new RetryStateEx());
            Assert.True(shouldRety);
            Assert.NotNull(policy.CommandTimeoutInSeconds);
            policy.ShouldIgnoreOnFirstTry = false;
            Assert.False(policy.ShouldIgnoreOnFirstTry);
        }
        
        [Test]
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


        [Test]
        public void GetErrorNumberWithNullExceptionTest()
        {
            Assert.Null(RetryPolicy.GetErrorNumber(null));
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
            ConnectParams connectParams = TestConnectionProfileService.Instance.GetConnectionParameters(TestServerType.OnPrem);
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
            csb.DataSource = connectParams.Connection.ServerName;
            csb.IntegratedSecurity = connectParams.Connection.AuthenticationType == AuthenticationType.Integrated.ToString();
            if (connectParams.Connection.UserName != null)
            {
                csb.UserID = connectParams.Connection.UserName;
            }
            if (connectParams.Connection.Password != null)
            {
                csb.Password = connectParams.Connection.Password;
            }
            csb.ConnectTimeout = connectParams.Connection.ConnectTimeout.HasValue ? connectParams.Connection.ConnectTimeout.Value: 30;
            
            csb.Encrypt = connectParams.Connection.Encrypt?.ToLowerInvariant() switch {
                "optional" or "false" or "no" => SqlConnectionEncryptOption.Optional,
                "mandatory" or "true" or "yes" => SqlConnectionEncryptOption.Mandatory,
                "strict" => SqlConnectionEncryptOption.Strict,
                _ => default
            };

            csb.TrustServerCertificate = connectParams.Connection.TrustServerCertificate.HasValue ? connectParams.Connection.TrustServerCertificate.Value : false;
            if (!string.IsNullOrEmpty(connectParams.Connection.HostNameInCertificate))
            {
                csb.HostNameInCertificate = connectParams.Connection.HostNameInCertificate;
            }

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

            ReliableSqlConnection connection = new ReliableSqlConnection(csb.ConnectionString, connectionRetryPolicy, commandRetryPolicy, null);
            return connection;
        }

        /// <summary>
        /// Test ReliableConnectionHelper.GetDefaultDatabaseFilePath()
        /// </summary>
        [Test]
        public void TestGetDefaultDatabaseFilePath()
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
                useRetry: false,
                azureAccountToken: null);

            Assert.False(string.IsNullOrWhiteSpace(filePath));
            Assert.False(string.IsNullOrWhiteSpace(logPath));
        }

        /// <summary>
        /// Test ReliableConnectionHelper.GetServerVersion()
        /// </summary>
        [Test]
        public void TestGetServerVersion()
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
        }

        /// <summary>
        /// Tests ReliableConnectionHelper.GetCompleteServerName()
        /// </summary>
        [Test]
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
        [Test]
        public void TestIsDatabaseReadonly()
        {
            var connectionBuilder = CreateTestConnectionStringBuilder();
            Assert.NotNull(connectionBuilder);

            bool isReadOnly = ReliableConnectionHelper.IsDatabaseReadonly(connectionBuilder, null);
            Assert.False(isReadOnly);
        }

        /// <summary>
        /// /// Tests ReliableConnectionHelper.IsDatabaseReadonly() with null builder parameter
        /// </summary>
        [Test]
        public void TestIsDatabaseReadonlyWithNullBuilder()
        {
            Assert.Throws<ArgumentNullException>(() => ReliableConnectionHelper.IsDatabaseReadonly(null, null));                        
        }

        /// <summary>
        /// Verify ANSI_NULL and QUOTED_IDENTIFIER settings can be set and retrieved for a session
        /// </summary>
        [Test]
        public void VerifyAnsiNullAndQuotedIdentifierSettingsReplayed()
        {
            using (ReliableSqlConnection conn = (ReliableSqlConnection) ReliableConnectionHelper.OpenConnection(CreateTestConnectionStringBuilder(), useRetry: true, azureAccountToken: null))
            {
                VerifySessionSettings(conn, true);
                VerifySessionSettings(conn, false);
            }
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
                Assert.AreEqual("ANSI_NULLS", settings[0].Item1);
                Assert.AreEqual(expectedSessionValue, settings[0].Item2);

                Assert.AreEqual("QUOTED_IDENTIFIER", settings[1].Item1);
                Assert.AreEqual(expectedSessionValue, settings[1].Item2);

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
                Assert.AreEqual(ansiNullsValue, actualAnsiNullsOnValue);
                Assert.AreEqual(quotedIdentifersValue, actualQuotedIdentifierOnValue);
            }

        }

        /// <summary>
        /// Test that the retry policy factory constructs all possible types of policies successfully.
        /// </summary>
        [Test]
        public void RetryPolicyFactoryConstructsPoliciesSuccessfully()
        {
            RunIfWrapper.RunIfWindows(() => 
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
        [Test]
        public void TestIsCloudIsFalseForLocalServer()
        {
            using (var connection = CreateTestConnection())
            {
                Assert.NotNull(connection);

                connection.Open();
                Assert.False(ReliableConnectionHelper.IsCloud(connection));
            }
        }

        /// <summary>
        /// Tests that ReliableConnectionHelper.OpenConnection() opens a connection if it is closed
        /// </summary>
        [Test]
        public void TestOpenConnectionOpensConnection()
        {
            using (var connection = CreateTestConnection())
            {
                Assert.NotNull(connection);

                Assert.True(connection.State == ConnectionState.Closed);
                ReliableConnectionHelper.OpenConnection(connection);
                Assert.True(connection.State == ConnectionState.Open);
            }
        }

        /// <summary>
        /// Tests that ReliableConnectionHelper.ExecuteNonQuery() runs successfully
        /// </summary>
        [Test]
        public void TestExecuteNonQuery()
        {
            var result = ReliableConnectionHelper.ExecuteNonQuery(
                CreateTestConnectionStringBuilder(),
                "SET NOCOUNT ON; SET NOCOUNT OFF;",
                ReliableConnectionHelper.SetCommandTimeout,
                null,
                true,
                null
                );
            Assert.NotNull(result);
        }

        /// <summary>
        /// Test that TryGetServerVersion() gets server information
        /// </summary>
        [Test]
        public void TestTryGetServerVersion()
        {
            ReliableConnectionHelper.ServerInfo info = null;
            var connBuilder = CreateTestConnectionStringBuilder();
            Assert.True(ReliableConnectionHelper.TryGetServerVersion(connBuilder.ConnectionString, out info, null));

            Assert.NotNull(info);
            Assert.That(info.ServerVersion, Is.Not.Null.Or.Empty);
        }

        /// <summary>
        /// Test that TryGetServerVersion() fails with invalid connection string
        /// </summary>
        [Test]
        public void TestTryGetServerVersionInvalidConnectionString()
        {
            RunIfWrapper.RunIfWindows(() =>
            {
                ReliableConnectionHelper.ServerInfo info = null;
                Assert.False(ReliableConnectionHelper.TryGetServerVersion("this is not a valid connstr", out info, null));
            });
        }

        /// <summary>
        /// Validate ambient static settings
        /// </summary>
        [Test]
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
        [Test]
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

        [Test]
        public void RaiseAmbientRetryMessageTest()
        {
            bool handlerCalled = false;
            var data = new AmbientSettings.AmbientData();
            data.ConnectionRetryMessageHandler = (a) => handlerCalled = true;
            AmbientSettings._defaultSettings = data;
            RetryPolicyUtils.RaiseAmbientRetryMessage(new RetryStateEx() { LastError = new Exception() }, 100);
            Assert.True(handlerCalled);
        }

        [Test]
        public void RaiseAmbientIgnoreMessageTest()
        {
            bool handlerCalled = false;
            var data = new AmbientSettings.AmbientData();
            data.ConnectionRetryMessageHandler = (a) => handlerCalled = true;
            AmbientSettings._defaultSettings = data;
            RetryPolicyUtils.RaiseAmbientIgnoreMessage(new RetryStateEx() { LastError = new Exception() }, 100);
            Assert.True(handlerCalled);
        }

        [Test]
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

        [Test]
        public void ReliableConnectionHelperTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var result = LiveConnectionHelper.InitLiveConnectionInfo(null, queryTempFile.FilePath);
                ConnectionInfo connInfo = result.ConnectionInfo;
                DbConnection connection = connInfo.ConnectionTypeToConnectionMap[ConnectionType.Default];
                connection.Open();

                Assert.True(connection.State == ConnectionState.Open, "Connection should be open.");
                Assert.True(ReliableConnectionHelper.IsAuthenticatingDatabaseMaster(connection));

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                Assert.True(ReliableConnectionHelper.IsAuthenticatingDatabaseMaster(builder, null));
                ReliableConnectionHelper.TryAddAlwaysOnConnectionProperties(builder, new SqlConnectionStringBuilder());

                Assert.NotNull(ReliableConnectionHelper.GetServerName(connection));
                Assert.NotNull(ReliableConnectionHelper.ReadServerVersion(connection));

                Assert.NotNull(ReliableConnectionHelper.GetAsSqlConnection(connection));

                ReliableConnectionHelper.ServerInfo info = ReliableConnectionHelper.GetServerVersion(connection);
                Assert.NotNull(ReliableConnectionHelper.IsVersionGreaterThan2012RTM(info));
            }
        }

        [Test]
        public void DataSchemaErrorTests()
        {
            var error = new DataSchemaError();
            Assert.NotNull(error);
            var isOnDisplay = error.IsOnDisplay;
            var isBuildErrorCodeDefined = error.IsBuildErrorCodeDefined;
            var buildErrorCode = error.BuildErrorCode;
            var isPriorityEditable = error.IsPriorityEditable; 
            var message = error.Message;
            var exception = error.Exception; 
            var prefix = error.Prefix;
            var column = error.Column;
            var line =error.Line;
            var errorCode =error.ErrorCode; 
            var severity = error.Severity;
            var document = error.Document;

            Assert.NotNull(error.ToString());
            Assert.NotNull(DataSchemaError.FormatErrorCode("ex", 1));
        }

        [Test]
        public void InitReliableSqlConnectionTest()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            DbConnection dbConnection;
            connInfo.TryGetConnection(ConnectionType.Default, out dbConnection);

            var connection = dbConnection as ReliableSqlConnection;
            var command = new ReliableSqlConnection.ReliableSqlCommand(connection);
            Assert.NotNull(command.Connection);

            var retryPolicy = connection.CommandRetryPolicy;
            connection.CommandRetryPolicy = retryPolicy;
            Assert.True(connection.CommandRetryPolicy == retryPolicy);
            connection.Open();
            connection.ChangeDatabase("master");
            Assert.True(connection.ConnectionTimeout > 0);
            connection.ClearPool();
        }

        [Test]
        public void ThrottlingReasonTests()
        {
            var reason = RetryPolicy.ThrottlingReason.Unknown;
            Assert.NotNull(reason.ThrottlingMode);
            Assert.NotNull(reason.ThrottledResources);

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.InitialCatalog = "master";
                builder.IntegratedSecurity = false;
                builder.DataSource = "localhost";
                builder.UserID = "invalid";
                builder.Password = "..";
                SqlConnection conn = new SqlConnection(builder.ToString());
                conn.Open();
            }
            catch (SqlException sqlException)
            {
                var exceptionReason = RetryPolicy.ThrottlingReason.FromException(sqlException);
                Assert.NotNull(exceptionReason);

                var errorReason = RetryPolicy.ThrottlingReason.FromError(sqlException.Errors[0]);
                Assert.NotNull(errorReason);

                var detectionStrategy = new TestDataTransferErrorDetectionStrategy();
                Assert.True(detectionStrategy.InvokeCanRetrySqlException(sqlException));
                Assert.True(detectionStrategy.CanRetry(new InvalidOperationException()));
                Assert.False(detectionStrategy.ShouldIgnoreError(new InvalidOperationException()));

                var detectionStrategy2 = new TestSqlAzureTemporaryAndIgnorableErrorDetectionStrategy();
                Assert.NotNull(detectionStrategy2.InvokeCanRetrySqlException(sqlException));
                Assert.NotNull(detectionStrategy2.InvokeShouldIgnoreSqlException(sqlException));
            }

            var unknownCodeReason = RetryPolicy.ThrottlingReason.FromReasonCode(-1);
            var codeReason = RetryPolicy.ThrottlingReason.FromReasonCode(2601);
            Assert.NotNull(codeReason);

            Assert.NotNull(codeReason.IsThrottledOnDataSpace);
            Assert.NotNull(codeReason.IsThrottledOnLogSpace);
            Assert.NotNull(codeReason.IsThrottledOnLogWrite);
            Assert.NotNull(codeReason.IsThrottledOnDataRead);
            Assert.NotNull(codeReason.IsThrottledOnCPU);
            Assert.NotNull(codeReason.IsThrottledOnDatabaseSize);
            Assert.NotNull(codeReason.IsThrottledOnWorkerThreads);
            Assert.NotNull(codeReason.IsUnknown);
            Assert.NotNull(codeReason.ToString());
        }

        [Test]
        public void RetryErrorsTest()
        {
            var sqlServerRetryError =  new SqlServerRetryError(
                "test message", new Exception(), 
                1, 200, ErrorSeverity.Warning);
            Assert.True(sqlServerRetryError.RetryCount == 1);
            Assert.NotNull(SqlServerRetryError.FormatRetryMessage(1, TimeSpan.FromSeconds(15), new Exception()));
            Assert.NotNull(SqlServerRetryError.FormatIgnoreMessage(1, new Exception()));

            var sqlServerError1 = new SqlServerError("test message", "document", ErrorSeverity.Warning);
            var sqlServerError2 = new SqlServerError("test message", "document", 1, ErrorSeverity.Warning);
            var sqlServerError3 = new SqlServerError(new Exception(), "document",1,  ErrorSeverity.Warning);
            var sqlServerError4 = new SqlServerError("test message", "document", 1, 2, ErrorSeverity.Warning);
            var sqlServerError5 = new SqlServerError(new Exception(), "document", 1, 2, 3, ErrorSeverity.Warning);
            var sqlServerError6 = new SqlServerError("test message", "document", 1, 2, 3, ErrorSeverity.Warning);
            var sqlServerError7 = new SqlServerError("test message", new Exception(), "document", 1, 2, 3, ErrorSeverity.Warning);

            Assert.True(SqlSchemaModelErrorCodes.IsParseErrorCode(46010));
            Assert.True(SqlSchemaModelErrorCodes.IsInterpretationErrorCode(Interpretation.InterpretationBaseCode+ 1));
            Assert.True(SqlSchemaModelErrorCodes.IsStatementFilterError(StatementFilter.StatementFilterBaseCode + 1));        
        }

        [Test]
        public void RetryCallbackEventArgsTest()
        {
            var exception = new Exception();
            var timespan = TimeSpan.FromMinutes(1);

            // Given a RetryCallbackEventArgs object with certain parameters
            var args = new RetryCallbackEventArgs(5, exception, timespan);

            // If I check the properties on the object
            // Then I expect the values to be the same as the values I passed into the constructor
            Assert.AreEqual(5, args.RetryCount);
            Assert.AreEqual(exception, args.Exception);
            Assert.AreEqual(timespan, args.Delay);
        }

        [Test]
        public void CheckStaticVariables()
        {
            Assert.NotNull(ReliableConnectionHelper.BuilderWithDefaultApplicationName);                        
        }

        [Test]
        public void SetLockAndCommandTimeoutThrowsOnNull()
        {
            Assert.Throws(typeof(ArgumentNullException), () => ReliableConnectionHelper.SetLockAndCommandTimeout(null));
        }

        [Test]
        public void StandardExceptionHandlerTests()
        {
            Assert.True(ReliableConnectionHelper.StandardExceptionHandler(new InvalidCastException()));
            Assert.False(ReliableConnectionHelper.StandardExceptionHandler(new Exception()));
        }

        [Test]
        public void GetConnectionStringBuilderNullConnectionString()
        {
            SqlConnectionStringBuilder builder;
            Assert.False(ReliableConnectionHelper.TryGetConnectionStringBuilder(null, out builder));                
        }

        [Test]
        public void GetConnectionStringBuilderExceptionTests()
        {
            SqlConnectionStringBuilder builder;

            // throws ArgumentException
            Assert.False(ReliableConnectionHelper.TryGetConnectionStringBuilder("IntegratedGoldFish=True", out builder));

            // throws FormatException
            Assert.False(ReliableConnectionHelper.TryGetConnectionStringBuilder("rabbits**frogs**lizards", out builder));            
        }

        [Test]
        public void GetCompleteServerNameTests()
        {
            Assert.Null(ReliableConnectionHelper.GetCompleteServerName(null));

            Assert.NotNull(ReliableConnectionHelper.GetCompleteServerName("localhost"));

            Assert.NotNull(ReliableConnectionHelper.GetCompleteServerName("mytestservername"));
        }

        [Test]
        public void ReliableSqlCommandConstructorTests()
        {
            // verify default constructor doesn't throw
            Assert.NotNull(new ReliableSqlConnection.ReliableSqlCommand());

            // verify constructor with null connection doesn't throw
            Assert.NotNull(new ReliableSqlConnection.ReliableSqlCommand(null));
        }

        [Test]
        public void ReliableSqlCommandProperties()
        {
            var command = new ReliableSqlConnection.ReliableSqlCommand();
            command.CommandText = "SELECT 1";
            Assert.AreEqual("SELECT 1", command.CommandText);
            Assert.NotNull(command.CommandTimeout);
            Assert.NotNull(command.CommandType);   
            command.DesignTimeVisible = true;
            Assert.True(command.DesignTimeVisible);
            command.UpdatedRowSource = UpdateRowSource.None;
            Assert.AreEqual(UpdateRowSource.None, command.UpdatedRowSource);
            Assert.NotNull(command.GetUnderlyingCommand());
            Assert.Throws<InvalidOperationException>(() => command.ValidateConnectionIsSet());
            command.Prepare();
            Assert.NotNull(command.CreateParameter());
            command.Cancel();            
        }

        [Test]
        public void ReliableConnectionResourcesTests()
        {
            Assert.NotNull(Resources.ConnectionPassedToIsCloudShouldBeOpen);
            Assert.NotNull(Resources.ExceptionCannotBeRetried);
            Assert.NotNull(Resources.FailedToCacheIsCloud);
            Assert.NotNull(Resources.FailedToParseConnectionString);
            Assert.NotNull(Resources.InvalidCommandType);
            Assert.NotNull(Resources.InvalidConnectionType);
            Assert.NotNull(Resources.OnlyReliableConnectionSupported);
            Assert.NotNull(Resources.UnableToAssignValue);
            Assert.NotNull(Resources.UnableToRetrieveAzureSessionId);
        }

        [Test]
        public void CalcExponentialRetryDelayWithSchemaDefaultsTest()
        {
            Assert.NotNull(RetryPolicyUtils.CalcExponentialRetryDelayWithSchemaDefaults(1));
        }

        [Test]
        public void IsSupportedCommandNullCommandTest()
        {
            Assert.False(DbCommandWrapper.IsSupportedCommand(null));
        }

        [Test]
        public void StatementCompletedTests()
        {
            StatementCompletedEventHandler handler = (s, e) => { }; 
            var command = new DbCommandWrapper(new SqlCommand());
            command.StatementCompleted += handler;
            command.StatementCompleted -= handler;
        }
    }
}