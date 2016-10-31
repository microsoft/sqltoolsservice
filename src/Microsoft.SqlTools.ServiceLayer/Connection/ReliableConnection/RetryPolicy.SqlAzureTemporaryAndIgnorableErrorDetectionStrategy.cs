//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal abstract partial class RetryPolicy
    {
        /// <summary>
        /// Provides the error detection logic for temporary faults that are commonly found in SQL Azure.
        /// This strategy is similar to SqlAzureTemporaryErrorDetectionStrategy, but it exposes ways
        /// to accept a certain exception and treat it as passing.
        /// For example, if we are retrying, and we get a failure that an object already exists, we might
        /// want to consider this as passing since the first execution that has timed out (or failed for some other temporary error)
        /// might have managed to create the object.
        /// </summary>
        internal class SqlAzureTemporaryAndIgnorableErrorDetectionStrategy : ErrorDetectionStrategyBase, IErrorDetectionStrategy
        {
            /// <summary>
            /// Azure error that can be ignored
            /// </summary>
            private readonly IList<int> ignorableAzureErrors = null;

            public SqlAzureTemporaryAndIgnorableErrorDetectionStrategy(params int[] ignorableErrors)
            {
                this.ignorableAzureErrors = ignorableErrors;
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
                        return false;
                    }

                    foundRetryableError = true;
                }
                return foundRetryableError;
            }

            protected override bool ShouldIgnoreSqlException(SqlException sqlException)
            {
                int errorNumber = sqlException.Number;

                if (ignorableAzureErrors == null)
                {
                    return false;
                }

                return ignorableAzureErrors.Contains(errorNumber);
            }
        }
    }
}