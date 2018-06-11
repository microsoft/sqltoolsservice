//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

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
        // TODO: Fix flaky test. See https://github.com/Microsoft/sqltoolsservice/issues/459
        [Fact]
        public async Task TestStartProfilingRequest()
        {
            string sessionId = null;
            string testUri = "profiler_uri";
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) =>
                {
                    // capture the session id for sending the stop message
                    sessionId = result.SessionId;
                    return Task.FromResult(0);
                });

            var sessionListener = new TestSessionListener();

            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(sessionListener);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);
            profilerService.XEventSessionFactory = new TestXEventSessionFactory();

            var requestParams = new StartProfilingParams();
            requestParams.OwnerUri = testUri;
            requestParams.TemplateName = "Standard";

            await profilerService.HandleStartProfilingRequest(requestParams, requestContext.Object);

            // wait a bit for profile sessions to be polled
            Thread.Sleep(1000);

            requestContext.VerifyAll();

            // Check that the correct XEvent session was started
            Assert.Equal(sessionId, "1");
            // check that the proper owner Uri was used
            Assert.Equal(sessionListener.PreviousSessionId, testUri);
            Assert.Equal(sessionListener.PreviousEvents.Count, 1);
        }

        /// <summary>
        /// Test stopping a session and receiving event callback
        /// </summary>
        /// <returns></returns>
        [Fact]
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
                    success = result.Succeeded;
                    return Task.FromResult(0);
                });

            // capture if session was dropped
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
        [Fact]
        public async Task TestPauseProfilingRequest()
        {
            bool success = false;
            string testUri = "test_session";

            // capture stopping results
            var requestContext = new Mock<RequestContext<PauseProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<PauseProfilingResult>()))
                .Returns<PauseProfilingResult>((result) =>
                {
                    success = result.Succeeded;
                    return Task.FromResult(0);
                });

            var sessionListener = new TestSessionListener();
            var profilerService = new ProfilerService();
            profilerService.SessionMonitor.AddSessionListener(sessionListener);
            profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);

            var requestParams = new PauseProfilingParams();
            requestParams.OwnerUri = testUri;

            profilerService.SessionMonitor.StartMonitoringSession(testUri, new TestXEventSession1());

            // wait a bit for profile sessions to be polled
            Thread.Sleep(1500);

            Assert.Equal(sessionListener.PreviousSessionId, testUri);

            // pause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success);

            sessionListener.PreviousSessionId = "";
            success = false;

            Thread.Sleep(1500);

            // check that session wasn't polled, and no events were sent to listener
            Assert.Equal(sessionListener.PreviousSessionId, "");

            // unpause viewer
            await profilerService.HandlePauseProfilingRequest(requestParams, requestContext.Object);
            Assert.True(success);

            Thread.Sleep(1500);
            // check that polling resumed, and events got sent to listener
            Assert.Equal(sessionListener.PreviousSessionId, testUri);

            requestContext.VerifyAll();
        }
    }
}
