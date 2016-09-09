//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal abstract partial class RetryPolicy
    {
        /// <summary>
        /// Provides the error detection logic for temporary faults that are commonly found in SQL Azure.
        /// The same errors CAN occur on premise also, but they are not seen as often.
        /// </summary>
        internal sealed class SqlAzureTemporaryErrorDetectionStrategy : ErrorDetectionStrategyBase, IErrorDetectionStrategy
        {
            private static SqlAzureTemporaryErrorDetectionStrategy instance = new SqlAzureTemporaryErrorDetectionStrategy();

            public static SqlAzureTemporaryErrorDetectionStrategy Instance
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
                    if (!RetryPolicyUtils.IsRetryableAzureError(err.Number))
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
