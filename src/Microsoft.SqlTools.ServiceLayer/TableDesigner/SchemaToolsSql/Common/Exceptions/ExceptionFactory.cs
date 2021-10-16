//------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Tools.Components.Common;
using Microsoft.Data.Tools.Contracts.Services;
using Microsoft.Data.Tools.Schema.Sql.Build;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions
{
    internal static class ExceptionFactory
    {
        public static void TraceException(Exception ex)
        {
            ComponentsExceptionFactory.TraceException(ex);
        }

        public static ArgumentException CreateArgumentException()
        {
            return ComponentsExceptionFactory.CreateArgumentException();
        }

        public static ArgumentException CreateArgumentException(string message)
        {
            return ComponentsExceptionFactory.CreateArgumentException(message);
        }

        public static ArgumentException CreateArgumentException(string message, Exception innerException)
        {
            return ComponentsExceptionFactory.CreateArgumentException(message, innerException);
        }

        public static ArgumentException CreateArgumentException(string message, string paramName)
        {
            return ComponentsExceptionFactory.CreateArgumentException(message, paramName);
        }

        public static ArgumentException CreateArgumentException(string message, string paramName, Exception innerException)
        {
            return ComponentsExceptionFactory.CreateArgumentException(message, paramName, innerException);
        }

        public static ArgumentNullException CreateArgumentNullException()
        {
            return CreateArgumentNullException(null);
        }

        public static ArgumentNullException CreateArgumentNullException(string paramName)
        {
            return ComponentsExceptionFactory.CreateArgumentNullException(paramName);
        }

        public static ArgumentNullException CreateArgumentNullException(string message, Exception innerException)
        {
            return ComponentsExceptionFactory.CreateArgumentNullException(message, innerException);
        }

        public static ArgumentNullException CreateArgumentNullException(string paramName, string message)
        {
            return ComponentsExceptionFactory.CreateArgumentNullException(paramName, message);
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException()
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException();
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string paramName)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(paramName);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string message, Exception innerException)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string paramName, string message)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(paramName, message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string paramName, object actualValue, string message)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(paramName, actualValue, message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }
        public static InvalidOperationException CreateInvalidOperationException(string message)
        {
            return CreateInvalidOperationException(message, null);
        }

        public static InvalidOperationException CreateInvalidOperationException(string message, Exception innerException)
        {
            InvalidOperationException ex = new InvalidOperationException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }
        public static ObjectDisposedException CreateObjectDisposedException(string objectName)
        {
            return ComponentsExceptionFactory.CreateObjectDisposedException(objectName);
        }

        public static ObjectDisposedException CreateObjectDisposedException(string message, Exception innerException)
        {
            return ComponentsExceptionFactory.CreateObjectDisposedException(message, innerException);
        }

        public static ObjectDisposedException CreateObjectDisposedException(string objectName, string message)
        {
            return ComponentsExceptionFactory.CreateObjectDisposedException(objectName, message);
        }
        public static OperationCanceledException CreateOperationCanceledException()
        {
            OperationCanceledException ex = new OperationCanceledException();
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static OperationCanceledException CreateOperationCanceledException(string message)
        {
            OperationCanceledException ex = new OperationCanceledException(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static OperationCanceledException CreateOperationCanceledException(string message, Exception innerException)
        {
            OperationCanceledException ex = new OperationCanceledException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static InvalidReferenceExceptionEx CreateInvalidReferenceExceptionEx(string message)
        {
            InvalidReferenceExceptionEx ex = new InvalidReferenceExceptionEx(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static DeveloperInstanceException CreateDeveloperInstanceException(string message)
        {
            DeveloperInstanceException ex = new DeveloperInstanceException(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static DeveloperInstanceException CreateDeveloperInstanceException(string message, Exception innerException)
        {
            DeveloperInstanceException ex = new DeveloperInstanceException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static NotImplementedException CreateNotImplementedException(string message)
        {
            var ex = new NotImplementedException(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static System.IO.InvalidDataException CreateInvalidDataException(string message)
        {
            var ex = new System.IO.InvalidDataException(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static IndexOutOfRangeException CreateIndexOutOfRangeException(string message)
        {
            var ex = new IndexOutOfRangeException(message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        /// <summary>
        /// Adds data to the exception
        /// </summary>
        public static void AddData(Exception ex, object key, object value)
        {
            if (ex != null &&
                key != null &&
                value != null)
            {
                System.Collections.IDictionary data = ex.Data;
                if (data != null &&
                    data.Contains(key) == false)
                {
                    data.Add(key, value);
                }
            }
        }
    }
}
