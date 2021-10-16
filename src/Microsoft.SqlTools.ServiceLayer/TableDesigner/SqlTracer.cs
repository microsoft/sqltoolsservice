/**************************************************************
*  Copyright (C) Microsoft Corporation. All rights reserved.  *
**************************************************************/

using System;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Data.Tools.Components.Diagnostics
{
    /// <summary>
    /// Provides facility to trace code execution through calls to Trace* methods.
    /// </summary>
    /// <remarks>
    /// The primary API of this class is the TraceEvent method.
    /// For error tracing the TraceException and TraceHResult methods are provided.
    /// If a caller needs to perform some non-trivial work before writing a trace,
    /// the ShouldTrace method can be called to determine if the Tracer is logging at a particular verbosity level.
    /// </remarks>
    internal static partial class SqlTracer
    {
        private const string TraceSourceName = "Microsoft.Data.Tools.Diagnostics.Tracer";

        // Maximum length of string message sent in one ETW event.
        // If the message is larger than this size, it will be split into two or more chunks of at most this size.
        // jhutson - Testing of ETW logging through the Tracer shows that the maximum length of the message is 1949 characters.
        // Events with message strings greater than this length are dropped.
        // The chunk length is set to 90% of this length.
        private const int MessageChunkLength = 1754;

        private static TraceSource _traceSource;

        private static TraceSource TraceSource
        {
            get
            {
                if (_traceSource != null)
                {
                    return _traceSource;
                }

                // Initialize TraceSource
                TraceSource source = new TraceSource(TraceSourceName);
                System.Threading.Interlocked.CompareExchange(ref _traceSource, source, null);
                return _traceSource;
            }
        }

        /// <summary>
        /// SQl Trace Telemetry Provider
        /// </summary>
        public static ISqlTraceTelemetryProvider SqlTraceTelemetryProvider { get; set; }


        /// <summary>
        /// Determine if tracing at a given verbosity level is being logged.
        /// </summary>
        /// <param name="eventType">
        /// A <see cref="TraceEventType"/> enumeration value that specifies the desired verbosity level.
        /// </param>
        /// <returns>
        /// True if tracing at the specified verbosity level is being logged; otherwise false.
        /// </returns>
        internal static bool ShouldTrace(TraceEventType eventType)
        {
            // Return true if either the TraceSource or ETW is logging at the specified level.

            return TraceSource.Switch.ShouldTrace(eventType);
        }

        /// <summary>
        /// Write a trace event with an empty, zero-length message to the underlying trace source.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool TraceEvent(TraceEventType eventType, SqlTraceId traceId)
        {
            return TraceEvent(eventType, traceId, String.Empty);
        }

        /// <summary>
        /// Write a trace event message to the underlying trace source.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="message">String message to be traced along with the event.</param>
        /// <returns>True if event was successfully written</returns>
        /// <remarks>
        /// The primary API of this class is the TraceEvent method. 
        /// 
        /// For error tracing the TraceException and TraceHResult methods are provided.
        /// 
        /// If a caller needs to perform some non-trivial work before writing a trace, 
        /// the ShouldTrace method can be called to determine if the Tracer is logging at a particular verbosity level.
        /// 
        /// The return value will return true if the entire message was written. If ETW is disabled for the 
        /// event or if the entire message was not successfully written, false will be returned. With messages that exceed the
        /// max payload of an individual ETW, the Tracer splits the payload accross multiple events. If any of the event writes fail,
        /// the method will return false.
        /// </remarks>
        internal static bool TraceEvent(TraceEventType eventType, SqlTraceId traceId, string message)
        {
            bool success = true;

            TraceSource.TraceEvent(eventType, (int)traceId, message);
            TraceSource.Flush();

            Console.WriteLine(message);
            return success;
        }

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="format">Format string of the message to be traced along with the event.</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool TraceEvent(TraceEventType eventType, SqlTraceId traceId, string format, params object[] args)
        {
            return TraceEvent(eventType, traceId, String.Format(CultureInfo.CurrentCulture, format, args));
        }

        /// <summary>
        /// Write an Error trace event with exception details to the underlying trace source.
        /// </summary>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="lineNumber">Compile time property to trace the line number of the calling code. Used to trace location error occurred</param>
        /// <param name="fileName">Compile time property  to trace the fileName of the calling code. Used to trace location error occurred</param>
        /// <param name="memberName">Compile time property  to trace the name of the calling method. Used to trace location error occurred</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool TraceException(SqlTraceId traceId, Exception exception,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", [CallerMemberName] string memberName = "")
        {
            return TraceException(TraceEventType.Error, traceId, exception, String.Empty, lineNumber, fileName, memberName);
        }

        /// <summary>
        /// Write an Error trace event with a message and exception details to the underlying trace source.
        /// </summary>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">String message to be traced along with the event.</param>
        /// <param name="lineNumber">Compile time property to trace the line number of the calling code. Used to trace location error occurred</param>
        /// <param name="fileName">Compile time property  to trace the fileName of the calling code. Used to trace location error occurred</param>
        /// <param name="memberName">Compile time property  to trace the name of the calling method. Used to trace location error occurred</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool TraceException(SqlTraceId traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", [CallerMemberName] string memberName = "")
        {
            return TraceException(TraceEventType.Error, traceId, exception, message, lineNumber, fileName, memberName);
        }

        /// <summary>
        /// Write a trace event with a message and exception details to the underlying trace source.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">String message to be traced along with the event.</param>
        /// <param name="lineNumber">Compile time property to trace the line number of the calling code. Used to trace location error occurred</param>
        /// <param name="fileName">Compile time property  to trace the fileName of the calling code. Used to trace location error occurred</param>
        /// <param name="memberName">Compile time property  to trace the name of the calling method. Used to trace location error occurred</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool TraceException(TraceEventType eventType, SqlTraceId traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", [CallerMemberName] string memberName = "")
        {
            String exceptionMessage = GetMessageForException(exception);
            if (SqlTraceTelemetryProvider != null)
            {
                SqlTraceTelemetryProvider.PostEvent(eventType, traceId, exception, lineNumber, fileName, memberName);
            }
            return TraceEvent(eventType, traceId, String.Format(CultureInfo.CurrentCulture, "{0} {1}", message, exceptionMessage));
        }


        private static string GetMessageForException(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            if (exception is SqlException)
            {
                SqlException se = exception as SqlException;
                sb.AppendLine("SqlException:  Message = " + exception.Message);
                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                {
                    sb.AppendLine("               StackTrace = " + exception.StackTrace);
                }
                sb.AppendLine("Errors:");
                foreach (SqlError error in se.Errors)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Number = {0}, State = {1}, Server = {2}, Message = {3}", error.Number, error.State, error.Server, error.Message);
                }
            }
            else
            {
                sb.AppendLine("Exception:  Message = " + exception.Message);
                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                {
                    sb.AppendLine("               StackTrace = " + exception.StackTrace);
                }

            }

            return sb.ToString();
        }

        /// <summary>
        /// Write an Error trace event with HRESULT details to the underlying trace source
        /// if the supplied HRESULT value represents a failure code.
        /// </summary>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="hr">HRESULT error value</param>
        /// <returns>True if event was successfully written</returns>
        /// <remarks>
        /// If the HRESULT value represents a failure, it is mapped to an exception, and the exception details are logged.
        /// If the HRESULT value does not represent a failure, no tracing is performed.
        /// </remarks>
        internal static bool TraceHResult(SqlTraceId traceId, int hr)
        {
            return TraceHResult(traceId, hr, string.Empty);
        }

        /// <summary>
        /// Write an Error trace event with HRESULT details along with a message to the underlying trace source
        /// if the supplied HRESULT value represents a failure code.
        /// </summary>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="hr">HRESULT error value</param>
        /// <param name="message">String message to be traced along with the event.</param>
        /// <returns>True if event was successfully written</returns>
        /// <remarks>
        /// If the HRESULT value represents a failure, it is mapped to an exception, and the exception details are logged.
        /// If the HRESULT value does not represent a failure, no tracing is performed.
        /// </remarks>
        internal static bool TraceHResult(SqlTraceId traceId, int hr, string message)
        {
            //TODO: Define ETW template for logging of HRESULT
            Exception exception = Marshal.GetExceptionForHR(hr);
            if (exception != null)
            {
                return TraceException(traceId, exception, "HRESULT failure: " + message);
            }
            return false;
        }

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source and issue a Debug.Fail() with
        /// the same message.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="message">Message to be output via Debug.Fail() and traced along with the event.</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool DebugTraceEvent(TraceEventType eventType, SqlTraceId traceId, string message)
        {
            bool success = TraceEvent(eventType, traceId, message);
            Debug.Fail(message);
            return success;
        }

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source and issue a Debug.Fail() call
        /// if condition is false.
        /// </summary>
        /// <param name="condition">Must be false for Debug or Trace event to be issued.</param>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="message">Message to be output via Debug.Fail() and traced along with the event.</param>
        /// <returns>True if event was successfully written or the condition was true</returns>
        internal static bool AssertTraceEvent(bool condition, TraceEventType eventType, SqlTraceId traceId, string message)
        {
            if (!condition)
            {
                return DebugTraceEvent(eventType, traceId, message);
            }
            return true;
        }

        /// <summary>
        /// Write a trace event with a message and exception details to the underlying trace source and issue a
        /// Debug.Fail() call with the same message.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">Message to be traced along with the event.</param>
        /// <returns>True if event was successfully written</returns>
        internal static bool DebugTraceException(TraceEventType eventType, SqlTraceId traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", [CallerMemberName] string memberName = "")
        {
            bool success = TraceException(eventType, traceId, exception, message, lineNumber, fileName, memberName);
            Debug.Fail(message);
            return success;
        }

        /// <summary>
        /// Write a trace event with a message and exception details to the underlying trace source and issue a
        /// Debug.Fail() call if the condition is false.
        /// </summary>
        /// <remarks>
        /// Note: often the fact that an exception has been thrown by itself is enough to determine that the message
        /// should be logged. If so please use DebugTraceException() instead. This method is for if the exception should
        /// only be logged if some additional condition is also false.
        /// </remarks>
        /// <param name="condition">Must be false for Debug or Trace event to be issued.</param>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="exception">The exception to be logged.</param>
        /// <param name="message">Message to be traced along with the event.</param>
        /// <returns>True if event was successfully written or the condition was true</returns>
        internal static bool AssertTraceException(bool condition, TraceEventType eventType, SqlTraceId traceId, Exception exception, string message)
        {
            if (!condition)
            {
                return DebugTraceException(eventType, traceId, exception, message);
            }
            return true;
        }
    }
}
