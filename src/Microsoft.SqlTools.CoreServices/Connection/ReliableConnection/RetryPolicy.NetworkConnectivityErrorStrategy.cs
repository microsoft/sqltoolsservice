//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data.SqlClient;

namespace Microsoft.SqlTools.CoreServices.Connection.ReliableConnection
{
    internal abstract partial class RetryPolicy
    {
        /// <summary>
        /// Provides the error detection logic for temporary faults that are commonly found in SQL Azure.
        /// The same errors CAN occur on premise also, but they are not seen as often.
        /// </summary>
        internal sealed class NetworkConnectivityErrorDetectionStrategy : ErrorDetectionStrategyBase
        {
            private static NetworkConnectivityErrorDetectionStrategy instance = new NetworkConnectivityErrorDetectionStrategy();

            public static NetworkConnectivityErrorDetectionStrategy Instance
            {
                get { return instance; }
            }

            protected override bool CanRetrySqlException(SqlException sqlException)
            {
                // Enumerate through all errors found in the exception.
                bool foundRetryableError = false;
                foreach (SqlError err in sqlException.Errors)
                {
                    RetryPolicyUtils.AppendThrottlingDataIfIsThrottlingError(sqlException, err);
                    if (!RetryPolicyUtils.IsRetryableNetworkConnectivityError(err.Number))
                    {
                        // If any error is not retryable then cannot retry 
                        return false;
                    }
                    foundRetryableError = true;
                }
                return foundRetryableError;
            }
        }
    }
}
