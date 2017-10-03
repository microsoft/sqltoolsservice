//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.SqlClient;
using System.Linq;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// Extension methods and utils for exceptions
    /// </summary>
    internal static class ExceptionUtil
    {
        /// <summary>
        /// Returns true if given exception if any of the inner exceptions is UserNeedsAuthenticationException
        /// </summary>
        internal static bool IsUserNeedsReauthenticateException(this Exception ex)
        {            
            return ex.IsExceptionType(typeof(UserNeedsAuthenticationException));
        }

        /// <summary>
        /// Returns true if given exception if any of the inner exceptions is sql exception
        /// </summary>
        internal static bool IsSqlException(this Exception ex)
        {
            return ex.IsExceptionType(typeof (SqlException));
        }

        /// <summary>
        /// Returns true if given exception if any of the inner exceptions is same type of given type
        /// </summary>
        internal static bool IsExceptionType(this Exception ex, Type type)
        {
            if (ex == null)
            {
                return false;
            }
            if (ex is AggregateException)
            {
                var aggregateException = (AggregateException)ex;
                return aggregateException.InnerExceptions != null &&
                    aggregateException.InnerExceptions.Any(inner => inner.IsExceptionType(type));
            }
            else if (ex.GetType() == type || (ex.InnerException != null && ex.InnerException.IsExceptionType(type)))
            {
                return true;
            }
            return false;
        }

        internal static string GetExceptionMessage(this Exception ex)
        {
            string errorMessage = string.Empty;
            if (ex != null)
            {
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " " + ex.InnerException.Message;
                }
            }
            return errorMessage;
        }
    }
}
