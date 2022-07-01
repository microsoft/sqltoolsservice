//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;

namespace Microsoft.SqlTools.BatchParser.Utility
{
    /// <summary>
    /// Ordinal value of each LogEvent value corresponds to a unique event id to be used in trace.
    /// By convention explicitly specify the integer value so that when this list grows large it is easy to figure out
    /// enumeration corresponding to a numeric value. We could be reserving ranges of values for specific areas or logEvents.
    /// Maximum value assignable to LogEvent enum value is 65,535.
    /// </summary>
    public enum LogEvent : ushort
    {
        Default = 0,
        IoFileSystem = 1,
        OsSubSystem = 2,
    }

    /// <summary>
    /// Provides a simple logging interface built on top of .Net tracing frameworks
    /// </summary>
    /// <remarks>This functionality is a simplified version of other similar implementations such as from Microsoft.SqlTools.Hosting, just without 
    /// the Listener implementation. This will log events to the TraceSource but it is up to whatever is using this library to set up the listener
    /// for handling the events (such as writing to disk)</remarks>
    public static class Logger
    {
        internal const string MANAGEDBATCHPARSER_TRACE_SOURCE = "managedbatchparser";

        public static TraceSource TraceSource { get; set; } = new TraceSource(MANAGEDBATCHPARSER_TRACE_SOURCE); // TODO: Need default trace level here?

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="eventType">The level at which the message will be written.</param>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Write(TraceEventType eventType, string logMessage) => Write(eventType, LogEvent.Default, logMessage);

        /// <summary>
        /// Writes a message to the log file with the Verbose event level
        /// </summary>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Verbose(string logMessage) => Write(TraceEventType.Verbose, logMessage);

        /// <summary>
        /// Writes a message to the log file with the Information event level
        /// </summary>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Information(string logMessage) => Write(TraceEventType.Information, logMessage);

        /// <summary>
        /// Writes a message to the log file with the Warning event level
        /// </summary>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Warning(string logMessage) => Write(TraceEventType.Warning, logMessage);

        /// <summary>
        /// Writes a message to the log file with the Error event level
        /// </summary>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Error(string logMessage) => Write(TraceEventType.Error, logMessage);

        /// <summary>
        /// Writes a message to the log file with the Critical event level
        /// </summary>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Critical(string logMessage) => Write(TraceEventType.Critical, logMessage);

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="eventType">The level at which the message will be written.</param>
        ///  <param name="logEvent">The event id enumeration for the log event.</param>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Write(
            TraceEventType eventType,
            LogEvent logEvent,
            string logMessage)
        {
            TraceSource.TraceEvent(eventType, (ushort)logEvent, logMessage);
            //switch (eventType)
            //{
            //    case TraceEventType.Critical:
            //    case TraceEventType.Error:
            //        if (eventType == TraceEventType.Critical)
            //        {
            //            logMessage = $@"event={eventType}: {logMessage}";
            //        }

            //        Trace.TraceError(logMessage);
            //        break;
            //    case TraceEventType.Warning:
            //        Trace.TraceWarning(logMessage);
            //        break;
            //    case TraceEventType.Information:
            //    case TraceEventType.Resume:
            //    case TraceEventType.Start:
            //    case TraceEventType.Stop:
            //    case TraceEventType.Suspend:
            //    case TraceEventType.Transfer:
            //    case TraceEventType.Verbose:
            //        if (eventType != TraceEventType.Information)
            //        {
            //            logMessage = $@"event={eventType}: {logMessage}";
            //        }

            //        Trace.TraceInformation(logMessage);
            //        break;
            //}
        }
    }
}
