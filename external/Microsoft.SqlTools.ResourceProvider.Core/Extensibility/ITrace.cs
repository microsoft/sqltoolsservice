//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// Provides facility to trace code execution through calls to Trace* methods.
    /// Implementing classes must add a <see cref="ExportableAttribute" />
    /// to the class in order to be found by the extension manager
    /// </summary>  
    public interface ITrace : IExportable
    {
        /// <summary>
        /// Write a formatted trace event message to the underlying trace source.
        /// </summary>
        /// <param name="eventType">Event type that specifies the verbosity level of the trace.</param>
        /// <param name="traceId">The category of the caller's product feature.</param>
        /// <param name="message">Format string of the message to be traced along with the event.</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        /// <returns>True if event was successfully written</returns>
        bool TraceEvent(TraceEventType eventType, int traceId, string message, params object[] args);

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
        bool TraceException(TraceEventType eventType, int traceId, Exception exception, string message,
            [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "",
            [CallerMemberName] string memberName = "");
    } 
}
