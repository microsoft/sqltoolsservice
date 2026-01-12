//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    /// <summary>
    /// Unit tests for ProfilerService
    /// </summary>
    public class ProfilerServiceTests
    {
        /// <summary>
        /// Test starting a profiling session and receiving event callback
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task StartProfilingRequest_creates_pausable_remote_session()
        {
            var sessionId = new SessionId("testsession_1", 1);
            string testUri = "profiler_uri";
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(result.CanPause, Is.True, "Result.CanPause for RingBuffer sessions");
                        Assert.That(result.UniqueSessionId, Is.EqualTo(sessionId.ToString()), "Result.UniqueSessionId");
                    });
                    return Task.FromResult(0);
                });

            // capture Listener event notifications
            var profilerService = new ProfilerService();
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.TryAdd(testUri, connectionInfo);
            profilerService.XEventSessionFactory = new TestXEventSessionFactory();

            var requestParams = new StartProfilingParams
            {
                OwnerUri = testUri,
                SessionName = "Standard"
            };

            // start profiling session
            await profilerService.HandleStartProfilingRequest(requestParams, requestContext.Object);            
            requestContext.VerifyAll();            
        }

        /// <summary>
        /// Test stopping a session and receiving event callback
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestStopProfilingRequest()
        {
            bool success = false;
            bool stopped = false;
            string testUri = "test_session";

            // capture stopping results
            var requestContext = new Mock<RequestContext<StopProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StopProfilingResult>()))
                .Returns<StopProfilingResult>((result) =>
                {
                    success = true;
                    return Task.FromResult(0);
                });

            // capture if session was stopped
            var mockSession = new Mock<IXEventSession>();
            mockSession.Setup(p => p.Stop()).Callback(() =>
                {
                    stopped = true;
                });

            mockSession.Setup(p => p.Id).Returns(new SessionId("test_1", 1));
            var sessionListener = new TestSessionListener();
            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(sessionListener);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.TryAdd(testUri, connectionInfo);
            profilerService.XEventSessionFactory = new TestXEventSessionFactory();

            var requestParams = new StopProfilingParams();
            requestParams.OwnerUri = testUri;

            profilerService.SessionMonitor.StartMonitoringSession(testUri, mockSession.Object);

            await profilerService.HandleStopProfilingRequest(requestParams, requestContext.Object);

            requestContext.VerifyAll();

            // check that session was succesfully stopped and stop was called
            Assert.True(success, nameof(success));
            Assert.True(stopped, nameof(stopped));

            // should not be able to remove the session, it should already be gone
            ProfilerSession ps;
            Assert.False(profilerService.SessionMonitor.StopMonitoringSession(testUri, out ps));
        }

        /// <summary>
        /// Test pausing then resuming a session
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestPauseProfilingRequest()
        {
            bool success = false;
            bool? lastPauseState = null;
            string testUri = "test_session";
            int eventsReceivedCount = 0;

            // capture pausing results
            var requestContext = new Mock<RequestContext<PauseProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<PauseProfilingResult>()))
                .Returns<PauseProfilingResult>((result) =>
                {
                    success = true;
                    lastPauseState = result.IsPaused;
                    return Task.FromResult(0);
                });

            // Create test events
            var testEvents = new List<TestXEvent>
            {
                new TestXEvent("test_event_1", DateTimeOffset.Now, new Dictionary<string, object> { { "data", "value1" } }),
                new TestXEvent("test_event_2", DateTimeOffset.Now.AddMilliseconds(100), new Dictionary<string, object> { { "data", "value2" } })
            };

            // Create fetcher with keepStreamOpen to prevent session from completing during test
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(50), keepStreamOpen: true);

            var sessionFactory = new TestLiveStreamSessionFactory(
                new[] { fetcher },
                maxReconnectAttempts: 0,
                reconnectDelay: TimeSpan.FromMilliseconds(10));

            // capture Listener event notifications
            var mockListener = new Mock<IProfilerSessionListener>();
            mockListener.Setup(p => p.EventsAvailable(It.IsAny<string>(), It.IsAny<List<ProfilerEvent>>())).Callback(() =>
                {
                    eventsReceivedCount++;
                });

            // setup profiler service
            var profilerService = new ProfilerService() { XEventSessionFactory = sessionFactory };
            profilerService.SessionMonitor.AddSessionListener(mockListener.Object);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.TryAdd(testUri, connectionInfo);

            var requestParams = new PauseProfilingParams();
            requestParams.OwnerUri = testUri;

            // begin monitoring session (events will be pushed automatically)
            profilerService.SessionMonitor.StartMonitoringSession(testUri, sessionFactory.GetXEventSession("testsession_1", connectionInfo));

            // wait for all events to be delivered (2 events with 50ms delay each)
            int retries = 30;
            while (retries-- > 0 && eventsReceivedCount < 2)
            {
                Thread.Sleep(100);
            }

            // confirm that events were received
            Assert.That(eventsReceivedCount, Is.GreaterThanOrEqualTo(1), "Should have received events before pausing");

            // Wait a bit for processing to stabilize
            Thread.Sleep(200);
            int eventsBeforePause = eventsReceivedCount;

            // pause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success, "Pause request should succeed");
            Assert.True(lastPauseState == true, "IsPaused should be true after pausing");

            // wait a bit and verify no more events received while paused
            Thread.Sleep(300);
            int eventsWhilePaused = eventsReceivedCount;
            Assert.That(eventsWhilePaused, Is.EqualTo(eventsBeforePause), "Should not receive events while paused");

            success = false;

            // unpause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success, "Unpause request should succeed");
            Assert.True(lastPauseState == false, "IsPaused should be false after unpausing");

            requestContext.VerifyAll();
        }

        /// <summary>
        /// Test notifications for stopped sessions when stream completes normally
        /// </summary>
        [Test]
        public void TestStoppedSessionNotification()
        {
            bool sessionStopped = false;
            string testUri = "profiler_uri";

            // Create a fetcher that delivers one event then completes
            var testEvents = new List<TestXEvent>
            {
                new TestXEvent("test_event", DateTimeOffset.Now, new Dictionary<string, object> { { "data", "value" } })
            };
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));

            var sessionFactory = new TestLiveStreamSessionFactory(
                new[] { fetcher },
                maxReconnectAttempts: 0,
                reconnectDelay: TimeSpan.FromMilliseconds(10));

            // capture Listener event notifications
            var mockListener = new Mock<IProfilerSessionListener>();
            mockListener.Setup(p => p.SessionStopped(It.IsAny<string>(), It.IsAny<SessionId>(), It.IsAny<string>())).Callback(() =>
            {
                sessionStopped = true;
            });

            var profilerService = new ProfilerService() { XEventSessionFactory = sessionFactory };
            profilerService.SessionMonitor.AddSessionListener(mockListener.Object);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.TryAdd(testUri, connectionInfo);

            // start monitoring test session
            profilerService.SessionMonitor.StartMonitoringSession(testUri, sessionFactory.GetXEventSession("test_session", connectionInfo));

            // wait for session to complete, or for timeout
            int retries = 30;
            while (!sessionStopped && retries-- > 0)
            {
                Thread.Sleep(100);
            }

            // check that a stopped session notification was sent
            Assert.True(sessionStopped, "Session stopped notification should have been received");
        }

        [Test]
        public void StartProfilingRequest_defaults_to_remote()
        { 
            var param = new StartProfilingParams();
            Assert.That(param.SessionType, Is.EqualTo(ProfilingSessionType.RemoteSession), nameof(param.SessionType));
        }

        [Test]
        public async Task StartProfilingRequest_creates_a_LocalFile_session_on_request()
        {
            var filePath = @"c:\folder\file.xel";
            var param = new StartProfilingParams() { OwnerUri = "someUri", SessionType = ProfilingSessionType.LocalFile, SessionName =  filePath};
            var mockSession = new Mock<IObservableXEventSession>();
            mockSession.Setup(p => p.Id).Returns(new SessionId("test_1", 1));
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    return Task.FromResult(0);
                });
            var sessionFactory = new Mock<IXEventSessionFactory>();
            sessionFactory.Setup(s => s.OpenLocalFileSession(filePath))
                .Returns (mockSession.Object)
                .Verifiable();
            var profilerService = new ProfilerService() { XEventSessionFactory = sessionFactory.Object };
            await profilerService.HandleStartProfilingRequest(param, requestContext.Object);
            sessionFactory.Verify();
            requestContext.VerifyAll();
        }

        [Test]
        public async Task ProfilerService_processes_localfile_session()
        {
            var viewerId = "someUri";
            var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Profiler", "TestXel_0.xel");
            var param = new StartProfilingParams() { OwnerUri = viewerId, SessionType = ProfilingSessionType.LocalFile, SessionName = filePath };
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(result.CanPause, Is.False, "local file session cannot be paused");
                        Assert.That(result.UniqueSessionId, Is.EqualTo(filePath), "UniqueSessionId should match file path");
                    });
                    return Task.FromResult(0);
                });
            var profilerService = new ProfilerService();
            var listener = new TestSessionListener();
            profilerService.SessionMonitor.AddSessionListener(listener);
            await profilerService.HandleStartProfilingRequest(param, requestContext.Object);
            var retries = 100;
            while (retries-- > 0 && !listener.StoppedSessions.Contains(viewerId))
            {
                Thread.Sleep(100);
            }
            Assert.Multiple(() =>
            {
                Assert.That(listener.StoppedSessions, Has.Member(viewerId), "session should have been stopped after reading the file");
                Assert.That(listener.AllEvents.Keys, Has.Member(viewerId), "session should have events logged for it");
                Assert.That(listener.AllEvents[viewerId]?.Count, Is.EqualTo(149), "all events from the xel should be in the buffer");
            });
        }

        [Test]
        public async Task ProfilerService_includes_ErrorMessage_in_session_stop_notification()
        {
            var param = new StartProfilingParams() { OwnerUri = "someUri", SessionName = "someSession" };

            // Create a fetcher that fails immediately with "test!" error after exhausting reconnect attempts
            var testException = new XEventException("test!");
            var failingFetchers = new List<IXEventFetcher>
            {
                new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: testException),
                new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: testException),
                new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: testException),
                new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: testException)
            };

            var sessionFactory = new TestLiveStreamSessionFactory(
                failingFetchers,
                maxReconnectAttempts: 3,
                reconnectDelay: TimeSpan.FromMilliseconds(10));

            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    return Task.FromResult(0);
                });

            var profilerService = new ProfilerService() { XEventSessionFactory = sessionFactory };
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            var connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.TryAdd("someUri", connectionInfo);

            var listener = new TestSessionListener();
            profilerService.SessionMonitor.AddSessionListener(listener);
            await profilerService.HandleStartProfilingRequest(param, requestContext.Object);
            var retries = 30;
            while (retries-- > 0 && !listener.StoppedSessions.Any())
            {
                Thread.Sleep(100);
            }
            Assert.Multiple(() =>
            {
                Assert.That(listener.ErrorMessages, Is.EqualTo(new[] { "test!" }), "listener.ErrorMessages");
                Assert.That(listener.StoppedSessions, Has.Member("someUri"), "listener.StoppedSessions");
            });
        }
    }
}
