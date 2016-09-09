//------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    internal abstract partial class RetryPolicy
    {
        public interface IErrorDetectionStrategy
        {
            /// <summary>
            /// Determines whether the specified exception represents a temporary failure that can be compensated by a retry.
            /// </summary>
            /// <param name="ex">The exception object to be verified.</param>
            /// <returns>True if the specified exception is considered as temporary, otherwise false.</returns>
            bool CanRetry(Exception ex);

            /// <summary>
            /// Determines whether the specified exception can be ignored.
            /// </summary>
            /// <param name="ex">The exception object to be verified.</param>
            /// <returns>True if the specified exception is considered as non-harmful.</returns>
            bool ShouldIgnoreError(Exception ex);
        }

        /// <summary>
        /// Base class with common retry logic. The core behavior for retrying non SqlExceptions is the same
        /// across retry policies
        /// </summary>
        internal abstract class ErrorDetectionStrategyBase : IErrorDetectionStrategy
        {
            public bool CanRetry(Exception ex)
            {
                if (ex != null)
                {
                    SqlException sqlException;
                    if ((sqlException = ex as SqlException) != null)
                    {
                        return CanRetrySqlException(sqlException);
                    }
                    if (ex is InvalidOperationException)
                    {
                        // Operations can throw this exception if the connection is killed before the write starts to the server
                        // However if there's an inner SqlException it may be a CLR load failure or other non-transient error
                        if (ex.InnerException != null
                            && ex.InnerException is SqlException)
                        {
                            return CanRetry(ex.InnerException);
                        }
                        return true;
                    }
                    if (ex is TimeoutException)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool ShouldIgnoreError(Exception ex)
            {
                if (ex != null)
                {
                    SqlException sqlException;
                    if ((sqlException = ex as SqlException) != null)
                    {
                        return ShouldIgnoreSqlException(sqlException);
                    }
                    if (ex is InvalidOperationException)
                    {
                        // Operations can throw this exception if the connection is killed before the write starts to the server
                        // However if there's an inner SqlException it may be a CLR load failure or other non-transient error
                        if (ex.InnerException != null
                            && ex.InnerException is SqlException)
                        {
                            return ShouldIgnoreError(ex.InnerException);
                        }
                    }
                }

                return false;
            }

            protected virtual bool ShouldIgnoreSqlException(SqlException sqlException)
            {
                return false;
            }

            protected abstract bool CanRetrySqlException(SqlException sqlException);
        }
    }
}
