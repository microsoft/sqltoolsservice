//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    [TestFixture]
    /// <summary>
    /// Unit tests for ProfilerService
    /// </summary>
    public class ProfilerServiceTests
    {
        /// <summary>
        /// Test starting a profiling session and receiving event callback
        /// </summary>
        /// <returns></returns>
        // TODO: Fix flaky test. See https://github.com/Microsoft/sqltoolsservice/issues/459
        //[Test]
        public async Task TestStartProfilingRequest()
        {
            string sessionId = null;
            bool recievedEvents = false;
            string testUri = "profiler_uri";
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    // capture the session id for sending the stop message
                    return Task.FromResult(0);
                });

            // capture Listener event notifications
            var mockListener = new Mock<IProfilerSessionListener>();
            mockListener.Setup(p => p.EventsAvailable(It.IsAny<string>(), It.IsAny<List<ProfilerEvent>>(), It.IsAny<bool>())).Callback(() =>
                {
                    recievedEvents = true;
                });

            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(mockListener.Object);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);
            profilerService.XEventSessionFactory = new TestXEventSessionFactory();

            var requestParams = new StartProfilingParams();
            requestParams.OwnerUri = testUri;
            requestParams.SessionName = "Standard";

            // start profiling session
            await profilerService.HandleStartProfilingRequest(requestParams, requestContext.Object);

            // wait for polling to finish, or for timeout
            System.Timers.Timer pollingTimer = new System.Timers.Timer();
            pollingTimer.Interval = 10000;
            pollingTimer.Start();
            bool timeout = false;
            pollingTimer.Elapsed += new System.Timers.ElapsedEventHandler((s_, e_) => { timeout = true; });
            while (sessionId == null && !timeout)
            {
                Thread.Sleep(250);
            }
            pollingTimer.Stop();

            requestContext.VerifyAll();

            // Check that the correct XEvent session was started
            Assert.AreEqual("1", sessionId);

            // check that the proper owner Uri was used
            Assert.True(recievedEvents);
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

            var sessionListener = new TestSessionListener();
            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(sessionListener);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);
            profilerService.XEventSessionFactory = new TestXEventSessionFactory();
            mockSession.SetupProperty(p => p.ConnectionDetails, connectionInfo.ConnectionDetails);
            mockSession.SetupProperty(p => p.Session, new Session(null, testUri));

            var requestParams = new StopProfilingParams();
            requestParams.OwnerUri = testUri;

            profilerService.SessionMonitor.StartMonitoringSession(testUri, mockSession.Object);

            await profilerService.HandleStopProfilingRequest(requestParams, requestContext.Object);

            requestContext.VerifyAll();

            // check that session was succesfully stopped and stop was called
            Assert.True(success);
            Assert.True(stopped);

            // should not be able to remove the session, it should already be gone
            ProfilerSession ps;
            Assert.False(profilerService.SessionMonitor.StopMonitoringSession(testUri, out ps));
        }

        /// <summary>
        /// Test pausing then resuming a session
        /// </summary>
        /// <returns></returns>
        [Test]
        // TODO: Add more in-depth testing for pause effects on events received.
        public async Task TestPauseProfilingRequest()
        {
            bool success = false;
            string testUri = "test_session";
    
            // capture pausing results
            var requestContext = new Mock<RequestContext<PauseProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<PauseProfilingResult>()))
                .Returns<PauseProfilingResult>((result) =>
                {
                    success = true;
                    return Task.FromResult(0);
                });

            // setup profiler service
            var mockListener = new TestSessionListener();
            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(mockListener);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);

            var testSession = new TestXEventSession1();
            testSession.ConnectionDetails = connectionInfo.ConnectionDetails;
            testSession.Session = new Session(null, testUri);

            var requestParams = new PauseProfilingParams();
            requestParams.OwnerUri = testUri;

            // begin monitoring session
            profilerService.SessionMonitor.StartMonitoringSession(testUri, testSession);

            // pause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success);

            success = false;

            // unpause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success);

            requestContext.VerifyAll();
        }

        /// <summary>
        /// Test notifications for stopped sessions
        /// </summary>
        [Test]
        public async Task TestStoppedSessionNotification()
        {
            bool sessionStopped = false;
            string testUri = "profiler_uri";

            // capture Listener event notifications
            var mockSession = new Mock<IXEventSession>();
            mockSession.Setup(p => p.GetTargetXml()).Callback(() =>
                {
                    throw new XEventException();
                });

            var mockListener = new Mock<IProfilerSessionListener>();
            mockListener.Setup(p => p.SessionStopped(It.IsAny<string>(), It.IsAny<int>())).Callback(() =>
            {
                sessionStopped = true;
            });

            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(mockListener.Object);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);
            mockSession.SetupProperty(p => p.ConnectionDetails, connectionInfo.ConnectionDetails);
            mockSession.SetupProperty(p => p.Session, new Session(null, testUri));

            // start monitoring test session
            profilerService.SessionMonitor.StartMonitoringSession(testUri, mockSession.Object);

            // Call stop session to simulate when a server has stopped a session on its side.
            profilerService.SessionMonitor.StopSession(0);

            // check that a stopped session notification was sent
            Assert.True(sessionStopped);
        }
    }
}
