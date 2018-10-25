//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Microsoft.SqlTools.Utility
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
    public static class Logger
    {
        internal const SourceLevels defaultTracingLevel = SourceLevels.Critical; 
        internal const string defaultTraceSource = "sqltools";
        private static SourceLevels tracingLevel = defaultTracingLevel;
        private static string logFileFullPath;

        internal static TraceSource TraceSource { get; set; }
        internal static string LogFileFullPath
        {
            get => logFileFullPath;
            private set
            {
                //If the log file path has a directory component then ensure that the directory exists.
                if (!string.IsNullOrEmpty(Path.GetDirectoryName(value)) && !Directory.Exists(Path.GetDirectoryName(value)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(value));
                }

                logFileFullPath = value;
                ConfigureListener();
            }
        }

        private static SqlToolsTraceListener Listener { get; set; }

        private static void ConfigureLogFile(string logFilePrefix) => LogFileFullPath = GenerateLogFilePath(logFilePrefix);

        /// <summary>
        /// Calling this method will turn on inclusion CallStack in the log for all future traces
        /// </summary>
        public static void StartCallStack() => Listener.TraceOutputOptions |= TraceOptions.Callstack;

        /// <summary>
        /// Calling this method will turn off inclusion of CallStack in the log for all future traces
        /// </summary>
        public static void StopCallStack() => Listener.TraceOutputOptions &= ~TraceOptions.Callstack;

        /// <summary>
        /// Calls flush on defaultTracingLevel configured listeners.
        /// </summary>
        public static void Flush()
        {
            TraceSource.Flush();
            Trace.Flush();
        }

        public static void Close()
        {
            Flush();
            TraceSource.Close();
            Trace.Close();
            Listener = null; // Since we have closed the listener, set listener to null.
        }
        public static SourceLevels TracingLevel
        {
            get => tracingLevel;
            set
            {
                // configures the source level filter. This alone is not enough for tracing that is done via "Trace" class instead of "TraceSource" object
                TraceSource.Switch = new SourceSwitch(TraceSource.Name, value.ToString());
                // configure the listener level filter
                tracingLevel = value;
                Listener.Filter = new EventTypeFilter(tracingLevel);
            }
        }

        public static bool AutoFlush { get; set; } = false;

        /// <summary>
        /// Initializes the Logger for the current process.
        /// </summary>
        /// <param name="tracingLevel">
        /// Optional. Specifies the minimum log message level to write to the log file.
        /// </param>
        /// <param name="logFilePath">
        /// Optional. Specifies the log name prefix for the log file name at which log messages will be written.
        /// <param name="traceSource">
        /// Optional. Specifies the tracesource name.
        /// </param>
        public static void Initialize(
            SourceLevels tracingLevel = defaultTracingLevel,
            string logFilePath = null,
            string traceSource = defaultTraceSource,
            bool autoFlush = false)
        {
            Logger.tracingLevel = tracingLevel;
            Logger.AutoFlush = autoFlush;
            TraceSource = new TraceSource(traceSource, Logger.tracingLevel);
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                logFilePath = GenerateLogFilePath(traceSource);
            }

            LogFileFullPath = logFilePath;
            Write(TraceEventType.Information, $"Initialized the {traceSource} logger. Log file is: {LogFileFullPath}");
        }

        /// <summary>
        /// Initializes the Logger for the current process.
        /// </summary>
        /// </param>
        /// <param name="tracingLevel">
        /// Optional. Specifies the minimum log message level to write to the log file.
        /// </param>
        /// <param name="logFilePath">
        /// Optional. Specifies the log name prefix for the log file name at which log messages will be written.
        /// <param name="traceSource">
        /// Optional. Specifies the tracesource name.
        /// </param>
        public static void Initialize(string tracingLevel, string logFilePath = null, string traceSource = defaultTraceSource)
        {
            Initialize(Enum.TryParse<SourceLevels>(tracingLevel, out SourceLevels sourceTracingLevel)
                    ? sourceTracingLevel
                    : defaultTracingLevel
                , logFilePath
                , traceSource);
        }

        /// <summary>
        /// Configures the LogfilePath for the tracelistener in use for this process.
        /// </summary>
        /// <returns>
        /// Returns the log file path corresponding to logfilePrefix
        /// </returns>
        public static string GenerateLogFilePath(string logFilePrefix = defaultTraceSource)
        {
            if (string.IsNullOrWhiteSpace(logFilePrefix))
            {
                throw new ArgumentOutOfRangeException(nameof(logFilePrefix), $"LogfilePath cannot be configured if argument {nameof(logFilePrefix)} has not been set");
            }
            // Create the log directory
            string logDir = Path.GetDirectoryName(logFilePrefix);
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                if (!Directory.Exists(logDir))
                {
                    try
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    catch (Exception ex)
                    {
                        Write(TraceEventType.Error, LogEvent.IoFileSystem, $"Unable to create directory:{logDir}\nException encountered:{ex}");
                    }
                }
            }

            // get a unique number to prevent conflicts of two process launching at the same time
            int uniqueId;
            try
            {
                uniqueId = Process.GetCurrentProcess().Id;
            }
            catch (Exception ex)
            {
                Write(TraceEventType.Information, LogEvent.OsSubSystem, $"Unable to get process id of current running process\nException encountered:{ex}");
                // if the pid look up fails for any reason, just use a random number
                uniqueId = new Random().Next(1000, 9999);
            }

            // make the log path unique
            return $"{logFilePrefix}_{DateTime.Now.Year,4:D4}{DateTime.Now.Month,2:D2}{DateTime.Now.Day,2:D2}{DateTime.Now.Hour,2:D2}{DateTime.Now.Minute,2:D2}{DateTime.Now.Second,2:D2}_{uniqueId}.log";
        }

        private static void ConfigureListener()
        {
            if (string.IsNullOrWhiteSpace(LogFileFullPath))
            {
                throw new InvalidOperationException("Listeners cannot be configured if LogFileFullPath has not been set");
            }
            Listener = new SqlToolsTraceListener(LogFileFullPath)
            {
                TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId,
                Filter = new EventTypeFilter(TracingLevel),
            };
            TraceSource.Listeners.Clear();
            TraceSource.Listeners.Add(Listener);
            Trace.Listeners.Clear();
            Trace.Listeners.Add(Listener);
        }

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="eventType">The level at which the message will be written.</param>
        /// <param name="logMessage">The message text to be written.</param>
        public static void Write(TraceEventType eventType, string logMessage) => Write(eventType, LogEvent.Default, logMessage);

        /// <summary>
        /// Writes a message to the log file with accompanying callstack.
        /// </summary>
        /// <param name="eventType">The level at which the message will be written.</param>
        /// <param name="logMessage">The message text to be written.</param>
        /// <remarks>
        /// The callstack logging gets turned on globally and any other log writes that happens in the time window
        /// while this log write is happening will also get callstack information logged. This is not considered
        /// and trying to isolate the callstack logging to be turned of for just one call is unnecessarily complex.
        /// </remarks>
        public static void WriteWithCallstack(TraceEventType eventType, string logMessage) => WriteWithCallstack(eventType, LogEvent.Default, logMessage);

        /// <summary>
        /// Writes a message to the log file with accompanying callstack.
        /// </summary>
        /// <param name="eventType">The level at which the message will be written.</param>
        ///  <param name="logEvent">The event id enumeration for the log event.</param>
        /// <param name="logMessage">The message text to be written.</param>
        /// <remarks>
        /// The callstack logging gets turned on globally and any other log writes that happens in the time window
        /// while this log write is happening will also get callstack information logged. This is not considered
        /// and trying to isolate the callstack logging to be turned of for just one call is unnecessarily complex.
        /// </remarks>
        public static void WriteWithCallstack(TraceEventType eventType, LogEvent logEvent, string logMessage)
        {
            Logger.StartCallStack();
            Write(eventType, logEvent, logMessage);
            Logger.StopCallStack();
        }

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
            // If logger is initialized then use TraceSource else use Trace
            if (TraceSource != null)
            {
                TraceSource.TraceEvent(eventType, (ushort)logEvent, logMessage);
            }
            else
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
            if (AutoFlush)
            {
                Flush();
            }
        }
    }

    /// <summary>
    /// This listener has the same behavior as TextWriterTraceListener except it controls how the 
    /// options: TraceOptions.DateTime, TraceOptions.ProcessId and TraceOptions.ThreadId is written to the output stream.
    /// This listener writes the above options, if turned on, inline with the message 
    /// instead of writing them to indented fields as is the case with TextWriterTraceListener.
    /// This implementation also lazily initializes the underlying tracelistener
    /// </summary>
    /// <remarks>
    /// Implementation of this is a lazily initialize trace listener that is partly inspired 
    /// by: https://stackoverflow.com/questions/30664527/how-to-stop-streamwriter-to-not-to-create-file-if-nothing-to-write
    /// </remarks>
    internal sealed class SqlToolsTraceListener : TraceListener
    {
        Lazy<TextWriterTraceListener> _lazyListener;
        private TextWriterTraceListener Listener => _lazyListener.Value;
        private bool IsListenerCreated => _lazyListener.IsValueCreated;
        public SqlToolsTraceListener(string file, string listenerName = "") : base(listenerName)
        {
            // Wrapping around lazy to make sure that we do not create file if the log.Write events are getting filtered out. i.e. the log file actually gets created the first time an actual write to log file happens.
            _lazyListener = new Lazy<TextWriterTraceListener>(
                valueFactory: () => new TextWriterTraceListener(new StreamWriter(file, append: true), listenerName),
                // LazyThreadSafetyMode.PublicationOnly mode ensures that we keep trying to create the listener (especially the file that write) on all future log.write events even if previous attempt(s) have failed
                mode: LazyThreadSafetyMode.PublicationOnly
             );
        }
        #region forward actual write/close/flush/dispose calls to the underlying listener.
        public override void Write(string message) => Listener.Write(message);

        public override void WriteLine(string message) => Listener.WriteLine(message);

        /// <Summary> 
        /// Closes the <see cref="System.Diagnostics.TextWriterTraceListener.Writer"> so that it no longer 
        ///    receives tracing or debugging output.</see>
        /// Make sure that we do not Close if the lazy listener never got created.
        /// </Summary> 
        public override void Close()
        {
            if (IsListenerCreated)
            {
                Listener.Close();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SqlToolsTraceListener"/>
        /// No unmanaged resources in this class, and it is sealed.
        /// No finalizer needed. See http://stackoverflow.com/a/3882819/613130
        /// We skip disposing if the lazy listener never got created. 
        /// </summary>
        public new void Dispose()
        {
            if (IsListenerCreated)
            {
                Listener.Dispose();
            }
        }

        /// <summary>
        /// Flushes the output buffer for the <see cref="System.Diagnostics.TextWriterTraceListener.Writer">.
        /// Make sure that we do not Flush if the lazy listener never got created.
        /// </summary>
        public override void Flush()
        {
            if (IsListenerCreated)
            {
                Listener.Flush();
            }
        }
        #endregion

        public override void TraceEvent(TraceEventCache eventCache, String source, TraceEventType eventType, int id)
        {
            TraceEvent(eventCache, source, eventType, id, String.Empty);
        }

        // All other TraceEvent methods come through this one.
        public override void TraceEvent(TraceEventCache eventCache, String source, TraceEventType eventType, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            WriteHeader(eventCache, source, eventType, id);
            WriteLine(message);
            WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache eventCache, String source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }

            WriteHeader(eventCache, source, eventType, id);
            if (args != null)
            {
                WriteLine(String.Format(CultureInfo.InvariantCulture, format, args));
            }
            else
            {
                WriteLine(format);
            }
            WriteFooter(eventCache);
        }

        private void WriteHeader(TraceEventCache eventCache, String source, TraceEventType eventType, int id)
            => Write(FormatHeader(eventCache, String.Format(CultureInfo.InvariantCulture, "{0} {1}: {2} : ", source, eventType.ToString(), id.ToString(CultureInfo.InvariantCulture))));

        private void WriteFooter(TraceEventCache eventCache)
        {
            if (eventCache == null)
            {
                return;
            }

            IndentLevel++;
            if (TraceOutputOptions.HasFlag(TraceOptions.LogicalOperationStack))
            {
                WriteLine("LogicalOperationStack=" + eventCache.LogicalOperationStack);
            }

            if (TraceOutputOptions.HasFlag(TraceOptions.Callstack))
            {
                WriteLine("Callstack=" + eventCache.Callstack);
            }

            IndentLevel--;
        }

        private string FormatHeader(TraceEventCache eventCache, string message)
        {
            if (eventCache == null)
            {
                return message;
            }

            return $"{(IsEnabled(TraceOptions.DateTime) ? string.Format(CultureInfo.InvariantCulture, "{0} ", eventCache.DateTime.ToLocalTime().ToString("yy-MM-dd H:mm:ss.fffffff", CultureInfo.InvariantCulture)) : string.Empty)}"
                 + $"{(IsEnabled(TraceOptions.ProcessId) ? string.Format(CultureInfo.InvariantCulture, "pid:{0} ", eventCache.ProcessId.ToString(CultureInfo.InvariantCulture)) : string.Empty)}"
                 + $"{(IsEnabled(TraceOptions.ThreadId) ? string.Format(CultureInfo.InvariantCulture, "tid:{0} ", eventCache.ThreadId.ToString(CultureInfo.InvariantCulture)) : string.Empty)}"
                 + message;
        }

        private bool IsEnabled(TraceOptions opt) => TraceOutputOptions.HasFlag(opt);

    }
}
