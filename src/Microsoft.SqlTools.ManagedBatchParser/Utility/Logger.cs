//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;

namespace Microsoft.SqlTools.BatchParser.Utility
{
    /// <summary>
    /// Provides a simple logging interface built on top of .Net tracing frameworks
    /// </summary>
    /// <remarks>This functionality is a simplified version of other similar implementations such as from Microsoft.SqlTools.Hosting, using
    /// just pure Trace calls instead of setting up a TraceSource and/or listeners to keep this as simple as possible.</remarks>
    public static class Logger
    {
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
            string logMessage)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    if (eventType == TraceEventType.Critical)
                    {
                        logMessage = $@"event={eventType}: {logMessage}";
                    }

                    Trace.TraceError(logMessage);
                    break;
                case TraceEventType.Warning:
                    Trace.TraceWarning(logMessage);
                    break;
                case TraceEventType.Information:
                case TraceEventType.Resume:
                case TraceEventType.Start:
                case TraceEventType.Stop:
                case TraceEventType.Suspend:
                case TraceEventType.Transfer:
                case TraceEventType.Verbose:
                    if (eventType != TraceEventType.Information)
                    {
                        logMessage = $@"event={eventType}: {logMessage}";
                    }

                    Trace.TraceInformation(logMessage);
                    break;
            }
        }
    }
}
