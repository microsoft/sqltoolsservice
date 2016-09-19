//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal sealed class RetryPolicyDefaults
    {
        /// <summary>
        /// The default number of retry attempts.
        /// </summary>
        public const int DefaulSchemaRetryCount = 6;

        /// <summary>
        /// The default number of retry attempts for create database.
        /// </summary>
        public const int DefaultCreateDatabaseRetryCount = 5;

        /// <summary>
        /// The default amount of time defining an interval between retries.
        /// </summary>
        public static readonly TimeSpan DefaultSchemaMinInterval = TimeSpan.FromSeconds(2.75);

        /// <summary>
        /// The default factor to use when determining exponential backoff between retries.
        /// </summary>
        public const double DefaultBackoffIntervalFactor = 2.0;

        /// <summary>
        /// The default maximum time between retries.
        /// </summary>
        public static readonly TimeSpan DefaultMaxRetryInterval = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The default number of retry attempts.
        /// </summary>
        public static readonly int DefaultDataCommandRetryCount = 5;

        /// <summary>
        /// The default number of retry attempts for a connection related error
        /// </summary>
        public static readonly int DefaultConnectionRetryCount = 6;

        /// <summary>
        /// The default amount of time defining an interval between retries.
        /// </summary>
        public static readonly TimeSpan DefaultDataMinInterval = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// The default amount of time defining a time increment between retry attempts in the progressive delay policy.
        /// </summary>
        public static readonly TimeSpan DefaultProgressiveRetryIncrement = TimeSpan.FromMilliseconds(500);
    }
    
    /// <summary>
    /// Implements a collection of the RetryPolicyInfo elements holding retry policy settings.
    /// </summary>
    internal sealed class RetryPolicyFactory
    {
        /// <summary>
        /// Returns a default policy that does no retries, it just invokes action exactly once.
        /// </summary>
        public static readonly RetryPolicy NoRetryPolicy = RetryPolicyFactory.CreateNoRetryPolicy();

        /// <summary>
        /// Returns a default policy that does no retries, it just invokes action exactly once.
        /// </summary>
        public static readonly RetryPolicy PrimaryKeyViolationRetryPolicy = RetryPolicyFactory.CreatePrimaryKeyCommandRetryPolicy();

        /// <summary>
        /// Implements a strategy that ignores any transient errors.
        /// Internal for testing purposes only
        /// </summary>
        internal sealed class TransientErrorIgnoreStrategy : RetryPolicy.IErrorDetectionStrategy
        {
            private static readonly TransientErrorIgnoreStrategy _instance = new TransientErrorIgnoreStrategy();

            public static TransientErrorIgnoreStrategy Instance
            {
                get { return _instance; }
            }

            public bool CanRetry(Exception ex)
            {
                return false;
            }

            public bool ShouldIgnoreError(Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates and returns a default Retry Policy for Schema based operations.
        /// </summary>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreateDefaultSchemaCommandRetryPolicy(bool useRetry, int retriesPerPhase =  RetryPolicyDefaults.DefaulSchemaRetryCount)
        {
            RetryPolicy policy;

            if (useRetry)
            {
                policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                            RetryPolicy.SqlAzureTemporaryErrorDetectionStrategy.Instance,
                            retriesPerPhase,
                            RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                            RetryPolicyDefaults.DefaultSchemaMinInterval,
                            RetryPolicyDefaults.DefaultMaxRetryInterval);
                policy.FastFirstRetry = false;
            }
            else
            {
                policy = CreateNoRetryPolicy();
            }

            return policy;
        }

        /// <summary>
        /// Creates and returns a default Retry Policy for Schema based connection operations.
        /// </summary>
        /// <remarks>The RetryOccured event is wired to raise an RaiseAmbientRetryMessage message for a connection retry. </remarks>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreateSchemaConnectionRetryPolicy(int retriesPerPhase)
        {
            RetryPolicy policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                        RetryPolicy.SqlAzureTemporaryErrorDetectionStrategy.Instance,
                        retriesPerPhase,
                        RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                        RetryPolicyDefaults.DefaultSchemaMinInterval,
                        RetryPolicyDefaults.DefaultMaxRetryInterval);
            policy.RetryOccurred += DataConnectionFailureRetry;
            return policy;
        }

        /// <summary>
        /// Creates and returns a default Retry Policy for Schema based command operations.
        /// </summary>
        /// <remarks>The RetryOccured event is wired to raise an RaiseAmbientRetryMessage message for a command retry. </remarks>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreateSchemaCommandRetryPolicy(int retriesPerPhase)
        {
            RetryPolicy policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                        RetryPolicy.SqlAzureTemporaryErrorDetectionStrategy.Instance,
                        retriesPerPhase,
                        RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                        RetryPolicyDefaults.DefaultSchemaMinInterval,
                        RetryPolicyDefaults.DefaultMaxRetryInterval);
            policy.FastFirstRetry = false;
            policy.RetryOccurred += CommandFailureRetry;
            return policy;
        }

        /// <summary>
        /// Creates and returns a Retry Policy for database creation operations.
        /// </summary>
        /// <param name="ignorableErrorNumbers">Errors to ignore if they occur after first retry</param>
        /// <remarks>
        /// The RetryOccured event is wired to raise an RaiseAmbientRetryMessage message for a command retry. 
        /// The IgnoreErrorOccurred event is wired to raise an RaiseAmbientIgnoreMessage message for ignore. 
        /// </remarks>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreateDatabaseCommandRetryPolicy(params int[] ignorableErrorNumbers)
        {
            RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy errorDetectionStrategy =
                new RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy(ignorableErrorNumbers);

            // 30, 60, 60, 60, 60 second retries
            RetryPolicy policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                errorDetectionStrategy,
                RetryPolicyDefaults.DefaultCreateDatabaseRetryCount /* maxRetryCount */,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                TimeSpan.FromSeconds(30) /* minInterval */,
                TimeSpan.FromSeconds(60) /* maxInterval */);

            policy.FastFirstRetry = false;
            policy.RetryOccurred += CreateDatabaseCommandFailureRetry;
            policy.IgnoreErrorOccurred += CreateDatabaseCommandFailureIgnore;
            
            return policy;
        }

        /// <summary>
        /// Creates and returns an "ignoreable" command Retry Policy.
        /// </summary>
        /// <param name="ignorableErrorNumbers">Errors to ignore if they occur after first retry</param>
        /// <remarks>
        /// The RetryOccured event is wired to raise an RaiseAmbientRetryMessage message for a command retry. 
        /// The IgnoreErrorOccurred event is wired to raise an RaiseAmbientIgnoreMessage message for ignore. 
        /// </remarks>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreateElementCommandRetryPolicy(params int[] ignorableErrorNumbers)
        {
            Debug.Assert(ignorableErrorNumbers != null);

            RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy errorDetectionStrategy =
                new RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy(ignorableErrorNumbers);

            RetryPolicy policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                errorDetectionStrategy,
                RetryPolicyDefaults.DefaulSchemaRetryCount,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                RetryPolicyDefaults.DefaultSchemaMinInterval,
                RetryPolicyDefaults.DefaultMaxRetryInterval);

            policy.FastFirstRetry = false;
            policy.RetryOccurred += ElementCommandFailureRetry;
            policy.IgnoreErrorOccurred += ElementCommandFailureIgnore;

            return policy;
        }

        /// <summary>
        /// Creates and returns an "primary key violation" command Retry Policy.
        /// </summary>
        /// <param name="ignorableErrorNumbers">Errors to ignore if they occur after first retry</param>
        /// <remarks>
        /// The RetryOccured event is wired to raise an RaiseAmbientRetryMessage message for a command retry. 
        /// The IgnoreErrorOccurred event is wired to raise an RaiseAmbientIgnoreMessage message for ignore. 
        /// </remarks>
        /// <returns>An instance of <see cref="RetryPolicy"/> class.</returns>
        internal static RetryPolicy CreatePrimaryKeyCommandRetryPolicy()
        {
            RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy errorDetectionStrategy =
                new RetryPolicy.SqlAzureTemporaryAndIgnorableErrorDetectionStrategy(SqlErrorNumbers.PrimaryKeyViolationErrorNumber);

            RetryPolicy policy = new RetryPolicy.ExponentialDelayRetryPolicy(
                errorDetectionStrategy,
                RetryPolicyDefaults.DefaulSchemaRetryCount,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                RetryPolicyDefaults.DefaultSchemaMinInterval,
                RetryPolicyDefaults.DefaultMaxRetryInterval);

            policy.FastFirstRetry = true;
            policy.RetryOccurred += CommandFailureRetry;
            policy.IgnoreErrorOccurred += CommandFailureIgnore;

            return policy;
        }

        /// <summary>
        /// Creates a Policy that will never allow retries to occur.
        /// </summary>
        /// <returns></returns>
        public static RetryPolicy CreateNoRetryPolicy()
        {
            return new RetryPolicy.FixedDelayPolicy(TransientErrorIgnoreStrategy.Instance, 0, TimeSpan.Zero);
        }

        /// <summary>
        /// Creates a Policy that is optimized for data-related script update operations. 
        /// This is extremely error tolerant and uses a Time based delay policy that backs
        /// off until some overall length of delay has occurred. It is not as long-running
        /// as the ConnectionManager data transfer retry policy since that's intended for bulk upload
        /// of large amounts of data, whereas this is for individual batch scripts executed by the
        /// batch execution engine.
        /// </summary>
        /// <returns></returns>
        public static RetryPolicy CreateDataScriptUpdateRetryPolicy()
        {
            return new RetryPolicy.TimeBasedRetryPolicy(
                RetryPolicy.DataTransferErrorDetectionStrategy.Instance,
                TimeSpan.FromMinutes(7),
                TimeSpan.FromMinutes(7),
                0.1,
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromSeconds(30),
                1.5);
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling exceptions with SQL connections
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateFastDataRetryPolicy()
        {
            RetryPolicy retryPolicy = new RetryPolicy.FixedDelayPolicy(
                RetryPolicy.NetworkConnectivityErrorDetectionStrategy.Instance,
                RetryPolicyDefaults.DefaultDataCommandRetryCount,
                TimeSpan.FromMilliseconds(5));

            retryPolicy.FastFirstRetry = true;
            retryPolicy.RetryOccurred += DataConnectionFailureRetry;
            return retryPolicy;
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling exceptions with SQL connections.
        /// No logging or other message handler is attached to the policy
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateDefaultSchemaConnectionRetryPolicy()
        {
            return CreateDefaultConnectionRetryPolicy();
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling exceptions with SQL connections.
        /// Adds an event handler to log and notify listeners of data connection retries 
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateDefaultDataConnectionRetryPolicy()
        {
            RetryPolicy retryPolicy = CreateDefaultConnectionRetryPolicy();
            retryPolicy.RetryOccurred += DataConnectionFailureRetry;
            return retryPolicy;
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling exceptions with SQL connections
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateDefaultConnectionRetryPolicy()
        {
            // Note: No longer use Ado.net Connection Pooling and hence do not need TimeBasedRetryPolicy to
            // conform to the backoff requirements in this case
            RetryPolicy retryPolicy = new RetryPolicy.ExponentialDelayRetryPolicy(
                RetryPolicy.NetworkConnectivityErrorDetectionStrategy.Instance,
                RetryPolicyDefaults.DefaultConnectionRetryCount,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                RetryPolicyDefaults.DefaultSchemaMinInterval,
                RetryPolicyDefaults.DefaultMaxRetryInterval);

            retryPolicy.FastFirstRetry = true;
            return retryPolicy;
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling retryable conditions with data transfer SQL commands.
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateDefaultDataSqlCommandRetryPolicy()
        {
            RetryPolicy retryPolicy = new RetryPolicy.ExponentialDelayRetryPolicy(
                RetryPolicy.SqlAzureTemporaryErrorDetectionStrategy.Instance,
                RetryPolicyDefaults.DefaultDataCommandRetryCount,
                RetryPolicyDefaults.DefaultBackoffIntervalFactor,
                RetryPolicyDefaults.DefaultDataMinInterval,
                RetryPolicyDefaults.DefaultMaxRetryInterval);

            retryPolicy.FastFirstRetry = true;
            retryPolicy.RetryOccurred += CommandFailureRetry;
            return retryPolicy;
        }

        /// <summary>
        /// Returns the default retry policy dedicated to handling retryable conditions with data transfer SQL commands.
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateDefaultDataTransferRetryPolicy()
        {
            RetryPolicy retryPolicy = new RetryPolicy.TimeBasedRetryPolicy(
                RetryPolicy.DataTransferErrorDetectionStrategy.Instance,
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(240),
                0.1,
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMinutes(2),
                2);

            retryPolicy.FastFirstRetry = true;
            retryPolicy.RetryOccurred += CommandFailureRetry;
            return retryPolicy;
        }

        /// <summary>
        /// Returns the retry policy to handle data migration for column encryption.
        /// </summary>
        /// <returns>The RetryPolicy policy</returns>
        public static RetryPolicy CreateColumnEncryptionTransferRetryPolicy()
        {
            RetryPolicy retryPolicy = new RetryPolicy.TimeBasedRetryPolicy(
                RetryPolicy.DataTransferErrorDetectionStrategy.Instance,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                0.1,
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMinutes(2),
                2);

            retryPolicy.FastFirstRetry = true;
            retryPolicy.RetryOccurred += CommandFailureRetry;
            return retryPolicy;
        }

        private static void DataConnectionFailureRetry(RetryState retryState)
        {
            Logger.Write(LogLevel.Normal, string.Format(CultureInfo.InvariantCulture,
                "Connection retry number {0}. Delaying {1} ms before retry. Exception: {2}",
                retryState.RetryCount,
                retryState.Delay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                retryState.LastError.ToString()));

            RetryPolicyUtils.RaiseAmbientRetryMessage(retryState, SqlSchemaModelErrorCodes.ServiceActions.ConnectionRetry);
        }

        private static void CommandFailureRetry(RetryState retryState, string commandKeyword)
        {
            Logger.Write(LogLevel.Normal, string.Format(
                CultureInfo.InvariantCulture,
                "{0} retry number {1}. Delaying {2} ms before retry. Exception: {3}",
                commandKeyword,
                retryState.RetryCount,
                retryState.Delay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                retryState.LastError.ToString()));

            RetryPolicyUtils.RaiseAmbientRetryMessage(retryState, SqlSchemaModelErrorCodes.ServiceActions.CommandRetry);
        }

        private static void CommandFailureIgnore(RetryState retryState, string commandKeyword)
        {
            Logger.Write(LogLevel.Normal, string.Format(
                CultureInfo.InvariantCulture,
                "{0} retry number {1}. Ignoring failure. Exception: {2}",
                commandKeyword,
                retryState.RetryCount,
                retryState.LastError.ToString()));

            RetryPolicyUtils.RaiseAmbientIgnoreMessage(retryState, SqlSchemaModelErrorCodes.ServiceActions.CommandRetry);
        }

        private static void CommandFailureRetry(RetryState retryState)
        {
            CommandFailureRetry(retryState, "Command");
        }

        private static void CommandFailureIgnore(RetryState retryState)
        {
            CommandFailureIgnore(retryState, "Command");
        }

        private static void CreateDatabaseCommandFailureRetry(RetryState retryState)
        {
            CommandFailureRetry(retryState, "Database Command");
        }

        private static void CreateDatabaseCommandFailureIgnore(RetryState retryState)
        {
            CommandFailureIgnore(retryState, "Database Command");
        }

        private static void ElementCommandFailureRetry(RetryState retryState)
        {
            CommandFailureRetry(retryState, "Element Command");
        }

        private static void ElementCommandFailureIgnore(RetryState retryState)
        {
            CommandFailureIgnore(retryState, "Element Command");
        }
    }
}
