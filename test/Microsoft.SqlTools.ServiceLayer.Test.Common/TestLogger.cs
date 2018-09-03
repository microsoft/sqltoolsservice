using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestLogger
    {
        private string logFilePrefix;
        private string logMessage;
        private string logContents;
        private string logFileName;
        private string topFrame;

        public bool ShouldVerifyCallstack { get; set; } = false;
        public string TestName { get => testName ?? TraceSource; set => testName = value; }
        public string TraceSource { get; set; } = "sqltoolsTest";
        public string LogMessage { get => logMessage ?? $"{TestName} test message"; set => logMessage = value; }
        public string LogFilePrefix { get => logFilePrefix ?? Path.Combine(Directory.GetCurrentDirectory(), TraceSource); set => logFilePrefix = value; }
        public TraceEventType EventType { get; set; } = TraceEventType.Information;
        public SourceLevels TracingLevel { get; set; } = SourceLevels.Critical;
        public bool DoNotUseTraceSource { get; set; } = false;

        private List<Action> pendingVerifications;
        private string testName;

        public string CallstackMessage { get => $"Callstack=\\s*{TopFrame}"; }

        public string LogFileName { get => logFileName ?? Logger.LogFileFullPath; set => logFileName = value; }
        public void Initialize() =>
            Logger.Initialize(TracingLevel, LogFilePrefix, TraceSource); // initialize the logger
        public string LogContents
        {
            get
            {
                if (logContents == null)
                {
                    Logger.Close();
                    Assert.True(!string.IsNullOrWhiteSpace(LogFileName));
                    Assert.True(LogFileName.Length > "{TraceSource}_.log".Length);
                    Assert.True(File.Exists(LogFileName));
                    logContents = File.ReadAllText(LogFileName);
                }
                return logContents;
            }
            set => logContents = value;
        }

        public string TopFrame { get => topFrame ?? "at System.Environment.get_StackTrace()"; set => topFrame = value; }

        public List<Action> PendingVerifications
        {
            get
            {
                if (pendingVerifications == null)
                    pendingVerifications = new List<Action>();
                return pendingVerifications;
            }
            set => pendingVerifications = value;
        }

        public void Write()
        {
            // write test log
            if (DoNotUseTraceSource)
            {
                TraceSource savedTraceSource = Logger.TraceSource;
                Logger.TraceSource = null;
                Logger.Write(EventType, LogMessage);
                Logger.TraceSource = savedTraceSource;
            }
            else
                Logger.Write(EventType, LogMessage);
        }

        public void WriteWithCallstack()
        {
            // write test log with callstack
            Logger.WriteWithCallstack(EventType, LogMessage);
            ShouldVerifyCallstack = true;
        }

        public void Verify(bool expectLogMessage = true) => Verify(ShouldVerifyCallstack, expectLogMessage);

        public void Verify(bool shouldVerifyCallstack, bool expectLogMessage = true) => Verify(EventType, LogMessage, CallstackMessage, shouldVerifyCallstack, expectLogMessage);

        public void Verify(TraceEventType eventType, string message, string callstackMessage, bool shouldVerifyCallstack = false, bool expectLogMessage = true)
        {
            if (expectLogMessage)
            {
                Assert.True(Regex.IsMatch(LogContents, $@"\b{eventType}:\s+\d+\s+:\s+{message}", RegexOptions.Compiled));
            }
            else
            {
                Assert.False(Regex.IsMatch(LogContents, $@"\b{eventType}:\s+\d+\s+:\s+{message}", RegexOptions.Compiled));
            }
            if (shouldVerifyCallstack)
                VerifyCallstack(callstackMessage, expectLogMessage);
        }

        /// <summary>
        /// Perform all the pending verifications
        /// </summary>
        public void VerifyPending()
        {
            foreach (var pv in PendingVerifications)
            {
                pv.Invoke();
            }
        }

        public void VerifyCallstack(bool expectLogMessage = true) => VerifyCallstack(CallstackMessage, expectLogMessage);

        public void VerifyCallstack(string message, bool expectLogMessage = true)
        {
            if (expectLogMessage)
            {
                Assert.True(Regex.IsMatch(LogContents, $"{message}", RegexOptions.Compiled));
            }
            else
            {
                Assert.False(Regex.IsMatch(LogContents, $"{message}", RegexOptions.Compiled));
            }
        }

        public void Cleanup() => Cleanup(Logger.LogFileFullPath);

        public void Cleanup(string logFileName)
        {
            // delete the test log file. We should have already asserted that this log file exists during verification.
            File.Delete(logFileName);
        }
    }
}
