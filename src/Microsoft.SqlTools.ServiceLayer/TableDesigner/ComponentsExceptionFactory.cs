/**************************************************************
*  Copyright (C) Microsoft Corporation. All rights reserved.  *
**************************************************************/

using System;
using System.Diagnostics;
using Microsoft.Data.Tools.Components.Diagnostics;

namespace Microsoft.Data.Tools.Components.Common
{
    internal static class ComponentsExceptionFactory
    {
        public static void TraceException(Exception ex)
        {
            if (SqlTracer.ShouldTrace(TraceEventType.Warning))
            {
                SqlTracer.TraceException(TraceEventType.Warning, SqlTraceId.CoreServices, ex, "ExceptionFactory.TraceException(Exception)");
            }
        }

        public static ArgumentException CreateArgumentException()
        {
            return CreateArgumentException(string.Empty);
        }

        public static ArgumentException CreateArgumentException(string message)
        {
            return CreateArgumentException(message, null, null);
        }

        public static ArgumentException CreateArgumentException(string message, Exception innerException)
        {
            return CreateArgumentException(message, null, innerException);
        }

        public static ArgumentException CreateArgumentException(string message, string paramName)
        {
            return CreateArgumentException(message, paramName, null);
        }

        public static ArgumentException CreateArgumentException(string message, string paramName, Exception innerException)
        {
            ArgumentException ex = new ArgumentException(message, paramName, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentNullException CreateArgumentNullException()
        {
            return CreateArgumentNullException(null);
        }

        public static ArgumentNullException CreateArgumentNullException(string paramName)
        {
            ArgumentNullException ex = new ArgumentNullException(paramName);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentNullException CreateArgumentNullException(string message, Exception innerException)
        {
            ArgumentNullException ex = new ArgumentNullException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ArgumentNullException CreateArgumentNullException(string paramName, string message)
        {
            ArgumentNullException ex = new ArgumentNullException(paramName, message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }
        public static ObjectDisposedException CreateObjectDisposedException(string objectName)
        {
            ObjectDisposedException ex = new ObjectDisposedException(objectName);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ObjectDisposedException CreateObjectDisposedException(string message, Exception innerException)
        {
            ObjectDisposedException ex = new ObjectDisposedException(message, innerException);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }

        public static ObjectDisposedException CreateObjectDisposedException(string objectName, string message)
        {
            ObjectDisposedException ex = new ObjectDisposedException(objectName, message);
            ComponentsExceptionFactory.TraceException(ex);
            return ex;
        }
    }
}
