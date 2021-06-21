//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.SqlTools.CoreServices.Connection.ReliableConnection
{
    /// <summary>
    /// Captures extended information about a specific error and a retry
    /// </summary>
    internal class SqlServerRetryError : SqlServerError
    {
        private int _retryCount;
        private int _errorCode;

        public SqlServerRetryError(string message, Exception ex, int retryCount, int errorCode, ErrorSeverity severity)
            : base(ex, message, errorCode, severity)
        {
            _retryCount = retryCount;
            _errorCode = errorCode;
        }

        public int RetryCount
        {
            get { return _retryCount; }
        }

        public static string FormatRetryMessage(int retryCount, TimeSpan delay, Exception transientException)
        {
            string message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.RetryOnException,
                retryCount,
                delay.TotalMilliseconds.ToString(CultureInfo.CurrentCulture),
                transientException.ToString());

            return message;
        }

        public static string FormatIgnoreMessage(int retryCount, Exception exception)
        {
            string message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.IgnoreOnException,
                retryCount,
                exception.ToString());

            return message;
        }
    }
}
