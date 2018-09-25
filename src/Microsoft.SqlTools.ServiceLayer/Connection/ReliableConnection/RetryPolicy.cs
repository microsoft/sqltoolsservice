//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// This code is copied from the source described in the comment below.

// =======================================================================================
// Microsoft Windows Server AppFabric Customer Advisory Team (CAT) Best Practices Series
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/appfabriccat/ and  copied from
// sqlmain ./sql/manageability/mfx/common/
//
// =======================================================================================
// Copyright © 2012 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// =======================================================================================

// namespace Microsoft.SQL.CAT.BestPractices.SqlAzure.Framework
// namespace Microsoft.SqlServer.Management.Common

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Implements a policy defining and implementing the retry mechanism for unreliable actions.
    /// </summary>
    internal abstract partial class RetryPolicy
    {
        /// <summary>
        /// Defines a callback delegate which will be invoked whenever a retry condition is encountered.
        /// </summary>
        /// <param name="retryState">The state of current retry attempt.</param>
        internal delegate void RetryCallbackDelegate(RetryState retryState);

        /// <summary>
        /// Defines a callback delegate which will be invoked whenever an error is ignored on retry.
        /// </summary>
        /// <param name="retryState">The state of current retry attempt.</param>
        internal delegate void IgnoreErrorCallbackDelegate(RetryState retryState);

        private readonly IErrorDetectionStrategy _errorDetectionStrategy;

        protected RetryPolicy(IErrorDetectionStrategy strategy)
        {
            Contract.Assert(strategy != null);

            _errorDetectionStrategy = strategy;
            this.FastFirstRetry = true;

            //TODO Defect 1078447 Validate whether CommandTimeout needs to be used differently in schema/data scenarios
            this.CommandTimeoutInSeconds = AmbientSettings.LongRunningQueryTimeoutSeconds;
        }

        /// <summary>
        /// An instance of a callback delegate which will be invoked whenever a retry condition is encountered.
        /// </summary>
        public event RetryCallbackDelegate RetryOccurred;

        /// <summary>
        /// An instance of a callback delegate which will be invoked whenever an error is ignored on retry.
        /// </summary>
        public event IgnoreErrorCallbackDelegate IgnoreErrorOccurred;

        /// <summary>
        /// Gets or sets a value indicating whether or not the very first retry attempt will be made immediately
        /// whereas the subsequent retries will remain subject to retry interval.
        /// </summary>
        public bool FastFirstRetry { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds of sql commands
        /// </summary>
        public int CommandTimeoutInSeconds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the error detection strategy of this retry policy
        /// </summary>
        internal IErrorDetectionStrategy ErrorDetectionStrategy
        {
            get
            {
                return _errorDetectionStrategy;
            }
        }

        /// <summary>
        /// We should only ignore errors if they happen after the first retry.
        /// This flag is used to allow the ignore even on first try, for testing purposes.
        /// </summary>
        /// <remarks>
        /// This flag is currently being used for TESTING PURPOSES ONLY.
        /// </remarks>
        internal bool ShouldIgnoreOnFirstTry
        {
            get;
            set;
        }

        protected static bool IsLessThanMaxRetryCount(int currentRetryCount, int maxRetryCount)
        {
            return currentRetryCount <= maxRetryCount;
        }

        /// <summary>
        /// Repetitively executes the specified action while it satisfies the current retry policy.
        /// </summary>
        /// <param name="action">A delegate representing the executable action which doesn't return any results.</param>
        /// <param name="token">Cancellation token to cancel action between retries.</param>
        public void ExecuteAction(Action action, CancellationToken? token = null)
        {
            ExecuteAction(
                _ => action(), token);
        }

        /// <summary>
        /// Repetitively executes the specified action while it satisfies the current retry policy.
        /// </summary>
        /// <param name="action">A delegate representing the executable action which doesn't return any results.</param>
        /// <param name="token">Cancellation token to cancel action between retries.</param>
        public void ExecuteAction(Action<RetryState> action, CancellationToken? token = null)
        {
            ExecuteAction<object>(
                retryState =>
                {
                    action(retryState);
                    return null;
                }, token);
        }

        /// <summary>
        /// Repetitively executes the specified action while it satisfies the current retry policy.
        /// </summary>
        /// <typeparam name="T">The type of result expected from the executable action.</typeparam>
        /// <param name="func">A delegate representing the executable action which returns the result of type T.</param>
        /// <param name="token">Cancellation token to cancel action between retries.</param>
        /// <returns>The result from the action.</returns>
        public T ExecuteAction<T>(Func<T> func, CancellationToken? token = null)
        {
            return ExecuteAction(
                _ => func(), token);
        }

        /// <summary>
        /// Repetitively executes the specified action while it satisfies the current retry policy.
        /// </summary>
        /// <typeparam name="R">The type of result expected from the executable action.</typeparam>
        /// <param name="func">A delegate representing the executable action which returns the result of type R.</param>
        /// <param name="token">Cancellation token to cancel action between retries.</param>
        /// <returns>The result from the action.</returns>
        public R ExecuteAction<R>(Func<RetryState, R> func, CancellationToken? token = null)
        {
            RetryState retryState = CreateRetryState();
            
            if (token != null)
            {
                token.Value.ThrowIfCancellationRequested();
            }

            while (true)
            {
                try
                {
                    return func(retryState);
                }
                catch (RetryLimitExceededException limitExceededEx)
                {
                    // The user code can throw a RetryLimitExceededException to force the exit from the retry loop.
                    // The RetryLimitExceeded exception can have an inner exception attached to it. This is the exception
                    // which we will have to throw up the stack so that callers can handle it.
                    if (limitExceededEx.InnerException != null)
                    {
                        throw limitExceededEx.InnerException;
                    }
                    
                    return default(R);
                }
                catch (Exception ex)
                {
                    retryState.LastError = ex;

                    if (retryState.RetryCount > 0 || this.ShouldIgnoreOnFirstTry)
                    {
                        // If we can ignore this error, then break out of the loop and consider this execution as passing
                        // We return the default value for the type R
                        if (ShouldIgnoreError(retryState))
                        {
                            OnIgnoreErrorOccurred(retryState);
                            return default(R);
                        }
                    }

                    retryState.RetryCount++;

                    if (!ShouldRetry(retryState))
                    {
                        throw;
                    }
                }

                OnRetryOccurred(retryState);

                if ((retryState.RetryCount > 1 || !FastFirstRetry) && !retryState.IsDelayDisabled)
                {
                    Thread.Sleep(retryState.Delay);
                }

                // check for cancellation after delay.
                if (token != null)
                {
                    token.Value.ThrowIfCancellationRequested();
                }
            }
        }

        protected virtual RetryState CreateRetryState()
        {
            return new RetryState();
        }

        public bool IsRetryableException(Exception ex)
        {
            return ErrorDetectionStrategy.CanRetry(ex);
        }

        public bool ShouldRetry(RetryState retryState)
        {
            bool canRetry = ErrorDetectionStrategy.CanRetry(retryState.LastError);
            bool shouldRetry =  canRetry
                   && ShouldRetryImpl(retryState);

            Logger.Write(TraceEventType.Error,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Retry requested: Retry count = {0}. Delay = {1}, SQL Error Number = {2}, Can retry error = {3}, Will retry = {4}", 
                    retryState.RetryCount, 
                    retryState.Delay,
                    GetErrorNumber(retryState.LastError), 
                    canRetry, 
                    shouldRetry));
           
            // Perform an extra check in the delay interval. Should prevent from accidentally ending up with the value of -1 which will block a thread indefinitely. 
            // In addition, any other negative numbers will cause an ArgumentOutOfRangeException fault which will be thrown by Thread.Sleep.
            if (retryState.Delay.TotalMilliseconds < 0)
            {
                retryState.Delay = TimeSpan.Zero;
            }
            return shouldRetry;
        }

        public bool ShouldIgnoreError(RetryState retryState)
        {
            bool shouldIgnoreError = ErrorDetectionStrategy.ShouldIgnoreError(retryState.LastError);

            Logger.Write(TraceEventType.Error,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Ignore Error requested: Retry count = {0}. Delay = {1}, SQL Error Number = {2}, Should Ignore Error = {3}",
                    retryState.RetryCount,
                    retryState.Delay,
                    GetErrorNumber(retryState.LastError), 
                    shouldIgnoreError));

            return shouldIgnoreError;
        }

        /* TODO - Error code does not exist in SqlException for .NET Core
        private static int? GetErrorCode(Exception ex)
        {
            SqlException sqlEx= ex as SqlException;
            if (sqlEx == null)
            {
                return null;
            }

            return sqlEx.ErrorCode;
        }
        */

        internal static int? GetErrorNumber(Exception ex)
        {
            SqlException sqlEx = ex as SqlException;
            if (sqlEx == null)
            {
                return null;
            }

            return sqlEx.Number;
        }

        protected abstract bool ShouldRetryImpl(RetryState retryState);

        /// <summary>
        /// Notifies the subscribers whenever a retry condition is encountered.
        /// </summary>
        /// <param name="retryState">The state of current retry attempt.</param>
        protected virtual void OnRetryOccurred(RetryState retryState)
        {
            var retryOccurred = RetryOccurred;
            if (retryOccurred != null)
            {
                retryOccurred(retryState);
            }
        }

        /// <summary>
        /// Notifies the subscribers whenever an error is ignored on retry.
        /// </summary>
        /// <param name="retryState">The state of current retry attempt.</param>
        protected virtual void OnIgnoreErrorOccurred(RetryState retryState)
        {
            var ignoreErrorOccurred = IgnoreErrorOccurred;
            if (ignoreErrorOccurred != null)
            {
                ignoreErrorOccurred(retryState);
            }
        }

        internal class FixedDelayPolicy : RetryPolicy
        {
            private readonly int _maxRetryCount;
            private readonly TimeSpan _intervalBetweenRetries;

            /// <summary>
            /// Constructs a new instance of the TRetryPolicy class with the specified number of retry attempts and time interval between retries.
            /// </summary>
            /// <param name="strategy">The <see cref="RetryPolicy.IErrorDetectionStrategy"/> to use when checking whether an error is retryable</param>
            /// <param name="maxRetryCount">The max number of retry attempts. Should be 1-indexed.</param>
            /// <param name="intervalBetweenRetries">The interval between retries.</param>
            public FixedDelayPolicy(IErrorDetectionStrategy strategy, int maxRetryCount, TimeSpan intervalBetweenRetries)
                : base(strategy)
            {
                Contract.Assert(maxRetryCount >= 0, "maxRetryCount cannot be a negative number");
                Contract.Assert(intervalBetweenRetries.Ticks >= 0, "intervalBetweenRetries cannot be negative");

                _maxRetryCount = maxRetryCount;
                _intervalBetweenRetries = intervalBetweenRetries;
            }

            protected override bool ShouldRetryImpl(RetryState retryState)
            {
                Contract.Assert(retryState != null);

                if (IsLessThanMaxRetryCount(retryState.RetryCount, _maxRetryCount))
                {
                    retryState.Delay = _intervalBetweenRetries;
                    return true;
                }

                retryState.Delay = TimeSpan.Zero;
                return false;
            }
        }

        internal class ProgressiveRetryPolicy : RetryPolicy
        {
            private readonly int _maxRetryCount;
            private readonly TimeSpan _initialInterval;
            private readonly TimeSpan _increment;

            /// <summary>
            /// Constructs a new instance of the TRetryPolicy class with the specified number of retry attempts and parameters defining the progressive delay between retries.
            /// </summary>
            /// <param name="strategy">The <see cref="RetryPolicy.IErrorDetectionStrategy"/> to use when checking whether an error is retryable</param>
            /// <param name="maxRetryCount">The maximum number of retry attempts. Should be 1-indexed.</param>
            /// <param name="initialInterval">The initial interval which will apply for the first retry.</param>
            /// <param name="increment">The incremental time value which will be used for calculating the progressive delay between retries.</param>
            public ProgressiveRetryPolicy(IErrorDetectionStrategy strategy, int maxRetryCount, TimeSpan initialInterval, TimeSpan increment)
                : base(strategy)
            {
                Contract.Assert(maxRetryCount >= 0, "maxRetryCount cannot be a negative number");
                Contract.Assert(initialInterval.Ticks >= 0, "retryInterval cannot be negative");
                Contract.Assert(increment.Ticks >= 0, "retryInterval cannot be negative");

                _maxRetryCount = maxRetryCount;
                _initialInterval = initialInterval;
                _increment = increment;
            }

            protected override bool ShouldRetryImpl(RetryState retryState)
            {
                Contract.Assert(retryState != null);

                if (IsLessThanMaxRetryCount(retryState.RetryCount, _maxRetryCount))
                {
                    retryState.Delay = TimeSpan.FromMilliseconds(_initialInterval.TotalMilliseconds + (_increment.TotalMilliseconds * (retryState.RetryCount - 1)));
                    return true;
                }

                retryState.Delay = TimeSpan.Zero;
                return false;
            }
        }

        internal class ExponentialDelayRetryPolicy : RetryPolicy
        {
            private readonly int _maxRetryCount;
            private readonly double _intervalFactor;
            private readonly TimeSpan _minInterval;
            private readonly TimeSpan _maxInterval;

            /// <summary>
            /// Constructs a new instance of the TRetryPolicy class with the specified number of retry attempts and parameters defining the progressive delay between retries.
            /// </summary>
            /// <param name="strategy">The <see cref="RetryPolicy.IErrorDetectionStrategy"/> to use when checking whether an error is retryable</param>
            /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
            /// <param name="intervalFactor">Controls the speed at which the delay increases - the retryCount is raised to this power as
            /// part of the function </param>
            /// <param name="minInterval">Minimum interval between retries. The basis for all backoff calculations</param>
            /// <param name="maxInterval">Maximum interval between retries. Backoff will not take longer than this period.</param>
            public ExponentialDelayRetryPolicy(IErrorDetectionStrategy strategy, int maxRetryCount, double intervalFactor, TimeSpan minInterval, TimeSpan maxInterval)
                : base(strategy)
            {
                Contract.Assert(maxRetryCount >= 0, "maxRetryCount cannot be a negative number");
                Contract.Assert(intervalFactor > 1, "intervalFactor Must be > 1 so that the delay increases exponentially");
                Contract.Assert(minInterval.Ticks >= 0, "minInterval cannot be negative");
                Contract.Assert(maxInterval.Ticks >= 0, "maxInterval cannot be negative");
                Contract.Assert(maxInterval.Ticks >= minInterval.Ticks, "maxInterval must be greater than minInterval");

                _maxRetryCount = maxRetryCount;
                _intervalFactor = intervalFactor;
                _minInterval = minInterval;
                _maxInterval = maxInterval;
            }

            protected override bool ShouldRetryImpl(RetryState retryState)
            {
                Contract.Assert(retryState != null);

                if (IsLessThanMaxRetryCount(retryState.RetryCount, _maxRetryCount))
                {
                    retryState.Delay = RetryPolicyUtils.CalcExponentialRetryDelay(retryState.RetryCount, _intervalFactor, _minInterval, _maxInterval);
                    return true;
                }

                retryState.Delay = TimeSpan.Zero;
                return false;
            }
        }

        internal class TimeBasedRetryPolicy : RetryPolicy
        {
            private readonly TimeSpan _minTotalRetryTimeLimit;
            private readonly TimeSpan _maxTotalRetryTimeLimit;
            private readonly double _totalRetryTimeLimitRate;

            private readonly TimeSpan _minInterval;
            private readonly TimeSpan _maxInterval;
            private readonly double _intervalFactor;

            private readonly Stopwatch _stopwatch;

            public TimeBasedRetryPolicy(
                IErrorDetectionStrategy strategy,
                TimeSpan minTotalRetryTimeLimit,
                TimeSpan maxTotalRetryTimeLimit,
                double totalRetryTimeLimitRate,
                TimeSpan minInterval,
                TimeSpan maxInterval,
                double intervalFactor)
                : base(strategy)
            {
                Contract.Assert(minTotalRetryTimeLimit.Ticks >= 0);
                Contract.Assert(maxTotalRetryTimeLimit.Ticks >= minTotalRetryTimeLimit.Ticks);
                Contract.Assert(totalRetryTimeLimitRate >= 0);

                Contract.Assert(minInterval.Ticks >= 0);
                Contract.Assert(maxInterval.Ticks >= minInterval.Ticks);
                Contract.Assert(intervalFactor >= 1);

                _minTotalRetryTimeLimit = minTotalRetryTimeLimit;
                _maxTotalRetryTimeLimit = maxTotalRetryTimeLimit;
                _totalRetryTimeLimitRate = totalRetryTimeLimitRate;

                _minInterval = minInterval;
                _maxInterval = maxInterval;
                _intervalFactor = intervalFactor;

                _stopwatch = Stopwatch.StartNew();
            }

            protected override bool ShouldRetryImpl(RetryState retryStateObj)
            {
                Contract.Assert(retryStateObj is RetryStateEx);
                RetryStateEx retryState = (RetryStateEx)retryStateObj;

                // Calculate the delay as exponential value based on the number of retries.
                retryState.Delay =
                    RetryPolicyUtils.CalcExponentialRetryDelay(
                        retryState.RetryCount,
                        _intervalFactor,
                        _minInterval,
                        _maxInterval);

                // Add the delay to the total retry time
                retryState.TotalRetryTime = retryState.TotalRetryTime + retryState.Delay;

                // Calculate the maximum total retry time depending on how long ago was the task (this retry policy) started.
                // Longer running tasks are less eager to abort since, more work is has been done.
                TimeSpan totalRetryTimeLimit = checked(TimeSpan.FromMilliseconds(
                    Math.Max(
                        Math.Min(
                            _stopwatch.ElapsedMilliseconds * _totalRetryTimeLimitRate,
                            _maxTotalRetryTimeLimit.TotalMilliseconds),
                        _minTotalRetryTimeLimit.TotalMilliseconds)));

                if (retryState.TotalRetryTime <= totalRetryTimeLimit)
                {
                    return true;
                }

                retryState.Delay = TimeSpan.Zero;
                return false;
            }

            protected override RetryState CreateRetryState()
            {
                return new RetryStateEx { TotalRetryTime = TimeSpan.Zero };
            }

            internal sealed class RetryStateEx : RetryState
            {
                public TimeSpan TotalRetryTime { get; set; }
            }
        }
    }
}
