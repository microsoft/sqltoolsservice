//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.Azure.Core.Extensibility
{
    /// <summary>
    /// An abstract class to be used for classes that need to have trace feature
    /// </summary>
    public abstract class TraceableBase
    {
        /// <summary>
        /// Returns the first implementation of trace in the catalog that has highest priority
        /// </summary>
        public abstract ITrace Trace
        {
            get; 
            set;
        }

        /// <summary>
        ///  Write a trace event message to the underlying trace source.
        /// </summary>       
        public bool TraceEvent(TraceEventType eventType, TraceId traceId, string format, params object[] args)
        {
            return TraceEvent(eventType, (int)traceId, format, args);
        }

        /// <summary>
        ///  Write a trace event message to the underlying trace source.
        /// </summary>       
        public bool TraceEvent(TraceEventType eventType, int traceId, string format, params object[] args)
        {
            return SafeTrace(eventType, traceId, format, args);
        }       

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source and issue a Debug.Fail() call
        /// if condition is false.
        /// </summary>     
        public bool AssertTraceEvent(bool condition, TraceEventType eventType, TraceId traceId, string message)
        {
            return AssertTraceEvent(condition, eventType, (int)traceId, message);
        }

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source and issue a Debug.Fail() call
        /// if condition is false.
        /// </summary>     
        public bool AssertTraceEvent(bool condition, TraceEventType eventType, int traceId, string message)
        {
            if (!condition)
            {
                return DebugTraceEvent(eventType, traceId, message);
            }
            return false;
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
        public bool AssertTraceException(bool condition, TraceEventType eventType, TraceId traceId, Exception exception, string message)
        {
            return AssertTraceException(condition, eventType, (int) traceId, exception, message);
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
        public bool AssertTraceException2(bool condition, TraceEventType eventType, TraceId traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "")
        {
            if (!condition)
            {
                return DebugTraceException2(eventType, (int) traceId, exception, message, lineNumber, fileName, memberName);
            }
            return true;
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
        public bool AssertTraceException(bool condition, TraceEventType eventType, int traceId, Exception exception, string message)
        {
            if (!condition)
            {
                return DebugTraceException(eventType, traceId, exception, message);
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
        public bool DebugTraceException(TraceEventType eventType, int traceId, Exception exception, string message)
        {
            // Avoiding breaking change by not overloading this method. Passing default values to TraceException so this isn't
            // reported as the callsite for majority of exceptions
            bool success = TraceException(eventType, traceId, exception, message, 0, string.Empty, string.Empty);
            Debug.Fail(message);
            return success;
        }

        public bool DebugTraceException2(TraceEventType eventType, int traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "")
        {
            bool success = TraceException(eventType, traceId, exception, message, lineNumber, fileName, memberName);
            Debug.Fail(message);
            return success;
        }

        /// <summary>
        /// Write a trace event with a message and exception details to the underlying trace source.
        /// </summary>
        public bool TraceException(TraceEventType eventType, TraceId traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "")
        {
            return TraceException(eventType, (int)traceId, exception, message, lineNumber, fileName, memberName);
        }

        /// <summary>
        /// Write a trace event with a message and exception details to the underlying trace source.
        /// </summary>
        public bool TraceException(TraceEventType eventType, int traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "")
        {
            return SafeTraceException(eventType, traceId, exception, message, lineNumber, fileName, memberName);
        }

        /// <summary>
        /// Write a formatted trace event message to the underlying trace source and issue a Debug.Fail() with
        /// the same message.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="message">Message to be output via Debug.Fail() and traced along with the event.</param>
        /// <returns>True if event was successfully written</returns>
        public bool DebugTraceEvent(TraceEventType eventType, int traceId, string message)
        {
            bool success = TraceEvent(eventType, traceId, message);
            Debug.Fail(message);
            return success;
        }

        /// <summary>
        /// Verifies ITrace instance is  not null before tracing
        /// </summary>        
        private bool SafeTrace(TraceEventType eventType, int traceId, string format, params object[] args)
        {
            if (Trace != null)
            {
                return Trace.TraceEvent(eventType, traceId, format, args);
            }
            return false;
        }

        /// <summary>
        /// Verifies ITrace instance is  not null before tracing the exception
        /// </summary>        
        private bool SafeTraceException(TraceEventType eventType, int traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "")
        {
            if (Trace != null)
            {
                return Trace.TraceException(eventType, traceId, exception, message, lineNumber, fileName, memberName);
            }
            return false;
        }
    }
}
