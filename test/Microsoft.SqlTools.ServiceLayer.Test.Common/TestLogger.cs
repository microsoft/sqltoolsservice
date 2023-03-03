﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestLogger
    {
        private string logFilePath;
        private string logMessage;
        private string logContents;
        private string logFileName;
        private string topFrame;

        public bool ShouldVerifyCallstack { get; set; } = false;
        public string TestName { get => testName ?? TraceSource; set => testName = value; }
        public string TraceSource { get; set; } = "sqltoolsTest";
        public string LogMessage { get => logMessage ?? $"{TestName} test message"; set => logMessage = value; }
        public string LogFilePath { get => logFilePath ?? Logger.GenerateLogFilePath(Path.Combine(Directory.GetCurrentDirectory(), TraceSource)); set => logFilePath = value; }
        public TraceEventType EventType { get; set; } = TraceEventType.Information;
        public SourceLevels TracingLevel { get; set; } = SourceLevels.Critical;
        public bool DoNotUseTraceSource { get; set; } = false;

        public bool IsPiiEnabled { get; set; } = false;

        public bool AutoFlush { get; set; } = false;

        private List<Action> pendingVerifications;
        private string testName;

        public string CallstackMessage { get => $"Callstack=\\s*{TopFrame}"; }

        public void Close() => Logger.Close();

        public string LogFileName { get => logFileName ?? Logger.LogFileFullPath; set => logFileName = value; }
        public void Initialize() =>
            Logger.Initialize(TracingLevel, IsPiiEnabled, LogFilePath, TraceSource, AutoFlush); // initialize the logger
        public string LogContents
        {
            get
            {
                if (logContents == null)
                {
                    Close();
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
                pendingVerifications ??= new List<Action>();
                return pendingVerifications;
            }
            set => pendingVerifications = value;
        }

        public void Write() => Write(LogMessage);

        public void Write(string logMessage)
        {
            // write test log
            if (DoNotUseTraceSource)
            {
                TraceSource savedTraceSource = Logger.TraceSource;
                Logger.TraceSource = null;
                Logger.Write(EventType, logMessage);
                Logger.TraceSource = savedTraceSource;
            }
            else
            {
                Logger.Write(EventType, logMessage);
            }
        }

        public void WriteWithCallstack() => WriteWithCallstack(LogMessage);

        public void WriteWithCallstack(string logMessage)
        {
            // write test log with callstack
            Logger.WriteWithCallstack(EventType, logMessage);
            ShouldVerifyCallstack = true;
        }

        public void Verify(bool expectLogMessage = true) => Verify(ShouldVerifyCallstack, expectLogMessage);

        public void Verify(bool shouldVerifyCallstack, bool expectLogMessage = true) => Verify(EventType, LogMessage, CallstackMessage, shouldVerifyCallstack, expectLogMessage);

        public void Verify(TraceEventType eventType, string message, string callstackMessage, bool shouldVerifyCallstack = false, bool expectLogMessage = true)
        {
            if (!AutoFlush)
            {
                Logger.Flush();
            }
            // The Regex uses .* between the severity and the message to allow SMO to vary the content. 140 SMO has nothing there, 150 has a timestamp
            if (expectLogMessage)
            {
                Assert.True(File.Exists(Logger.LogFileFullPath) && Regex.IsMatch(LogContents, $@"\b{eventType}:.*{message}", RegexOptions.Compiled));
            }
            else
            {
                Assert.False(File.Exists(Logger.LogFileFullPath) && Regex.IsMatch(LogContents, $@"\b{eventType}:.*{message}", RegexOptions.Compiled));
            }
            if (shouldVerifyCallstack)
            {
                VerifyCallstack(callstackMessage, expectLogMessage);
            }
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
                Assert.True(File.Exists(Logger.LogFileFullPath) && Regex.IsMatch(LogContents, $"{message}", RegexOptions.Compiled));
            }
            else
            {
                Assert.False(File.Exists(Logger.LogFileFullPath) && Regex.IsMatch(LogContents, $"{message}", RegexOptions.Compiled));
            }
        }

        public static void VerifyInitialization(SourceLevels expectedTracingLevel, string expectedTraceSource, string logFilePath, bool isLogFileExpectedToExist, int? testNo = null)
        {
            string FailureMessagePrefix = testNo.HasValue ? $"For Test No:{testNo.Value.ToString()}," : string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(logFilePath), $"{FailureMessagePrefix} LogFilePath should not be  null or whitespaces");
            Assert.True(expectedTracingLevel == Logger.TracingLevel, $"{FailureMessagePrefix} expected Tracing Level did not match the configured value");
            if (isLogFileExpectedToExist)
            {
                Assert.True(File.Exists(logFilePath), $"{FailureMessagePrefix} logFilePath:{logFilePath} must exist");
            }
            else
            {
                Assert.False(File.Exists(logFilePath), $"{FailureMessagePrefix} {logFilePath} must not exist");
            }
            Assert.True(string.Compare(expectedTraceSource, Logger.TraceSource.Name, ignoreCase: true) == 0, $"{FailureMessagePrefix} expected Trace Source Name did not match the configured value");
        }

        public void Cleanup() => Cleanup(Logger.LogFileFullPath);

        public static void Cleanup(string logFileName)
        {
            if (File.Exists(logFileName))
            {
                Logger.Close();
                File.Delete(logFileName);
            }
        }

    }
}
