//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    /// <summary>
    /// Logger test cases
    /// </summary>
    public class LoggerTests
    {
        /// <summary>
        /// Test to verify that the logger initialization is generating a valid file
        /// Verifies that a test log entries is succesfully written to a default log file.
        /// </summary>
        [Fact]
        public void LoggerDefaultFile()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Information,
                TracingLevel = SourceLevels.Verbose,
            };

            test.Initialize();
            test.Write();
            test.Verify();
            test.Cleanup();
        }

        /// <summary>
        /// Test to verify that there is no log file created if TracingLevel is set to off.
        /// </summary>
        [Fact]
        public void LoggerTracingLevelOff()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Information,
                TracingLevel = SourceLevels.Off,
            };

            test.Initialize();
            test.Write();
            test.Verify(expectLogMessage: false); // The log message should be absent since the tracing level is set to Off.
            Assert.True((new FileInfo(test.LogFileName)).Length == 0, "File length of log file when Logging is set to off for a newly started process should be zero");
            test.Cleanup();
        }

        /// <summary>
        /// Test to verify that the tracinglevel setting filters message logged at lower levels.
        /// Verifies that a test log entries logged at Information level are not present in log when tracingLevel
        /// is set to 'Critical'
        /// </summary>
        [Fact]
        public void LoggerInformationalNotLoggedWithCriticalTracingLevel()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Information,
                TracingLevel = SourceLevels.Critical,
            };

            test.Initialize();
            test.Write();
            test.Verify(expectLogMessage:false); // The log message should be absent since the tracing level is set to collect messages only at 'Critical' logging level
            test.Cleanup();
        }

        /// <summary>
        /// Test to verify that WriteWithCallstack() method turns on the callstack logging
        /// </summary>
        [Fact]
        public void LoggerWithCallstack()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Warning,
                TracingLevel = SourceLevels.Information,
            };

            test.Initialize();
            test.WriteWithCallstack();
            test.Verify(); // This should verify the logging of callstack fields as well.
            test.Cleanup();
        }

        /// <summary>
        /// Test to verify that callstack logging is turned on, it does not get logged because tracing level filters them out.
        /// </summary>
        [Fact]
        public void LoggerWithCallstackFilteredOut()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Information,
                TracingLevel = SourceLevels.Error,
            };

            test.Initialize();
            test.WriteWithCallstack();
            test.Verify(expectLogMessage:false); // The log message and corresponding callstack details should be absent since the tracing level is set to collect messages only at 'Error' logging level
            test.Cleanup();
        }

        /// <summary>
        /// No TraceSource test to verify that WriteWithCallstack() method turns on the callstack logging
        /// </summary>
        [Fact]
        public void LoggerNoTraceSourceWithCallstack()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Warning,
                TracingLevel = SourceLevels.Information,
                DoNotUseTraceSource = true,
            };

            test.Initialize();
            test.WriteWithCallstack();
            test.Verify(); // This should verify the logging of callstack fields as well.
            test.Cleanup();
        }

        /// <summary>
        /// No TraceSrouce test to verify that callstack logging is turned on, it does not get logged because tracing level filters them out.
        /// </summary>
        [Fact]
        public void LoggerNoTraceSourceWithCallstackFilteredOut()
        {
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                EventType = TraceEventType.Information,
                TracingLevel = SourceLevels.Error,
                DoNotUseTraceSource = true,
            };

            test.Initialize();
            test.WriteWithCallstack();
            test.Verify(expectLogMessage: false); // The log message and corresponding callstack details should be absent since the tracing level is set to collect messages only at 'Error' logging level
            test.Cleanup();
        }

        /// <summary>
        /// Tests to verify that upon changing TracingLevel from Warning To Error, 
        /// after the change, messages of Error type are present in the log and those logged with warning type are not present.
        /// </summary>
        [Fact]
        public void LoggerTracingLevelFromWarningToError()
        {
            // setup the test object
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
            };
            TestTracingLevelChangeFromWarningToError(test);
        }

        /// <summary>
        /// Tests to verify that upon changing TracingLevel from Error To Warning,  
        /// after the change, messages of Warning as well as of Error type are present in the log.
        /// </summary>
        [Fact]
        public void LoggerTracingLevelFromErrorToWarning()
        {
            // setup the test object
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
            };
            TestTracingLevelChangeFromErrorToWarning(test);
        }

        /// <summary>
        /// When not use TraceSource, test to verify that upon changing TracingLevel from Warning To Error, 
        /// after the change, messages of Error type are present in the log and those logged with warning type are not present.
        /// </summary>
        [Fact]
        public void LoggerNoTraceSourceTracingLevelFromWarningToError()
        {
            // setup the test object
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                DoNotUseTraceSource = true,
            };
            TestTracingLevelChangeFromWarningToError(test);
        }

        /// <summary>
        ///  When not use TraceSource, test to verify that upon changing TracingLevel from Error To Warning, 
        ///  after the change, messages of Warning as well as of Error type are present in the log.
        /// </summary>
        [Fact]
        public void LoggerNoTraceSourceTracingLevelFromErrorToWarning()
        {
            // setup the test object
            TestLogger test = new TestLogger()
            {
                TraceSource = MethodInfo.GetCurrentMethod().Name,
                DoNotUseTraceSource = true,
            };
            TestTracingLevelChangeFromErrorToWarning(test);
        }

        private static void TestTracingLevelChangeFromWarningToError(TestLogger test)
        {
            test.Initialize();
            Logger.TracingLevel = SourceLevels.Warning;
            string oldMessage = @"Old Message with Tracing Level set to Warning";
            test.LogMessage = oldMessage;
            // Initially with TracingLevel at Warning, logging of Warning type does not get filtered out.
            {
                test.EventType = TraceEventType.Warning;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Warning, message: oldMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }
            // and logging of Error type also succeeeds
            {
                test.EventType = TraceEventType.Error;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Error, message: oldMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }

            //Now Update the tracing level to Error. Now logging both of Warning type gets filtered and only Error type should succeed.
            Logger.TracingLevel = SourceLevels.Error;
            string newMessage = @"New Message After Tracing Level set to Error";
            test.LogMessage = newMessage;

            // Now with TracingLevel at Error, logging of Warning type gets filtered out.
            {
                test.EventType = TraceEventType.Warning;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Warning, message: newMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: false);
                });
            }
            // but logging of Error type succeeds
            {
                test.EventType = TraceEventType.Error;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Error, message: newMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }

            test.VerifyPending();
            test.Cleanup();
        }

        private static void TestTracingLevelChangeFromErrorToWarning(TestLogger test)
        {
            test.Initialize();
            Logger.TracingLevel = SourceLevels.Error;
            string oldMessage = @"Old Message with Tracing Level set to Error";
            test.LogMessage = oldMessage;
            // Initially with TracingLevel at Error, logging of Warning type gets filtered out.
            {
                test.EventType = TraceEventType.Warning;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Warning, message: oldMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: false);
                });
            }
            // But logging of Error type succeeeds
            {
                test.EventType = TraceEventType.Error;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Error, message: oldMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }

            //Now Update the tracing level to Warning. Now logging both of Error type and Warning type should succeed.
            Logger.TracingLevel = SourceLevels.Warning;
            string newMessage = @"New Message After Tracing Level set to Warning";
            test.LogMessage = newMessage;

            // Now with TracingLevel at Warning, logging of Warning type does not get filtered out.
            {
                test.EventType = TraceEventType.Warning;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Warning, message: newMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }
            // and logging of Error type also succeeds
            {
                test.EventType = TraceEventType.Error;
                test.Write();
                test.PendingVerifications.Add(() =>
                {
                    test.Verify(eventType: TraceEventType.Error, message: newMessage, callstackMessage: null, shouldVerifyCallstack: false, expectLogMessage: true);
                });
            }

            test.VerifyPending();
            test.Cleanup();
        }
    }
}
