//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    /// <summary>
    /// Tests for ProfilerSession class
    /// </summary>
    public class ProfilerSessionTests
    {
        private static TestXEvent CreateTestEvent(string name = "test_event")
        {
            return new TestXEvent(name, DateTimeOffset.UtcNow, new Dictionary<string, object>());
        }

        /// <summary>
        /// Test the TryEnterProcessing method for concurrent access control
        /// </summary>
        [Test]
        public void TestTryEnterProcessing()
        {
            // create new profiler session
            var profilerSession = new ProfilerSession(new TestXEventSession());

            // enter the processing block
            Assert.True(profilerSession.TryEnterProcessing());
            Assert.True(profilerSession.IsProcessing);

            // verify we can't enter again while processing
            Assert.False(profilerSession.TryEnterProcessing());

            // exit processing
            profilerSession.ExitProcessing();
            Assert.False(profilerSession.IsProcessing);

            // verify we can enter again after exiting
            Assert.True(profilerSession.TryEnterProcessing());
            Assert.True(profilerSession.IsProcessing);

            // clean up
            profilerSession.ExitProcessing();
        }

        /// <summary>
        /// Test XEventSession property returns the wrapped session
        /// </summary>
        [Test]
        public void TestXEventSessionProperty()
        {
            var xeSession = new TestXEventSession();
            var profilerSession = new ProfilerSession(xeSession);

            Assert.That(profilerSession.XEventSession, Is.SameAs(xeSession));
        }

        /// <summary>
        /// Test that Completed returns false for non-observable sessions
        /// </summary>
        [Test]
        public void TestCompleted_ReturnsFalse_ForNonObservableSession()
        {
            var profilerSession = new ProfilerSession(new TestXEventSession());

            Assert.That(profilerSession.Completed, Is.False);
        }

        /// <summary>
        /// Test that Error returns null for non-observable sessions
        /// </summary>
        [Test]
        public void TestError_ReturnsNull_ForNonObservableSession()
        {
            var profilerSession = new ProfilerSession(new TestXEventSession());

            Assert.That(profilerSession.Error, Is.Null);
        }

        /// <summary>
        /// Test that GetCurrentEvents returns empty for non-observable sessions
        /// </summary>
        [Test]
        public void TestGetCurrentEvents_ReturnsEmpty_ForNonObservableSession()
        {
            var profilerSession = new ProfilerSession(new TestXEventSession());

            var events = profilerSession.GetCurrentEvents();

            Assert.That(events, Is.Empty);
        }

        /// <summary>
        /// Test that GetCurrentEvents returns buffered events from observable session
        /// </summary>
        [Test]
        public void TestGetCurrentEvents_ReturnsBufferedEvents_FromObservableSession()
        {
            // Arrange - create an observable session that delivers events
            var testEvents = new List<IXEvent>
            {
                CreateTestEvent("event1"),
                CreateTestEvent("event2"),
                CreateTestEvent("event3")
            };
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var profilerSession = new ProfilerSession(liveSession);

            // Act - start the session and wait for events
            liveSession.Start();
            Thread.Sleep(200); // Allow events to be delivered

            var events = profilerSession.GetCurrentEvents().ToList();

            // Assert
            Assert.That(events, Has.Count.EqualTo(3), "Should receive all 3 events");
        }

        /// <summary>
        /// Test that GetCurrentEvents clears the buffer (atomic swap)
        /// </summary>
        [Test]
        public void TestGetCurrentEvents_ClearsBuffer_AfterRetrieving()
        {
            // Arrange - create an observable session that delivers events
            var testEvents = new List<IXEvent>
            {
                CreateTestEvent("event1"),
                CreateTestEvent("event2")
            };
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var profilerSession = new ProfilerSession(liveSession);

            // Act - start the session and wait for events
            liveSession.Start();
            Thread.Sleep(200);

            // First call should return events
            var firstCall = profilerSession.GetCurrentEvents().ToList();
            // Second call should return empty (buffer was swapped)
            var secondCall = profilerSession.GetCurrentEvents().ToList();

            // Assert
            Assert.That(firstCall, Has.Count.EqualTo(2), "First call should return events");
            Assert.That(secondCall, Is.Empty, "Second call should return empty (buffer cleared)");
        }

        /// <summary>
        /// Test that onSessionActivity callback is invoked when events arrive
        /// </summary>
        [Test]
        public void TestOnSessionActivityCallback_InvokedOnEvents()
        {
            // Arrange
            int callbackCount = 0;
            var testEvents = new List<IXEvent> { CreateTestEvent("event1") };
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            Action<ProfilerSession> callback = session => callbackCount++;
            _ = new ProfilerSession(liveSession, onSessionActivity: callback);

            // Act
            liveSession.Start();
            Thread.Sleep(200);

            // Assert - callback should be invoked at least once (for event + completion)
            Assert.That(callbackCount, Is.GreaterThan(0), "Callback should be invoked when events arrive");
        }

        /// <summary>
        /// Test that Dispose can be called safely
        /// </summary>
        [Test]
        public void TestDispose_CanBeCalledSafely()
        {
            var profilerSession = new ProfilerSession(new TestXEventSession());

            // Should not throw
            Assert.DoesNotThrow(profilerSession.Dispose);

            // Should be safe to call multiple times
            Assert.DoesNotThrow(profilerSession.Dispose);
        }

        /// <summary>
        /// Test that Completed becomes true when observable session completes
        /// </summary>
        [Test]
        public void TestCompleted_BecomesTrue_WhenSessionCompletes()
        {
            // Arrange - session that completes after delivering events
            var testEvents = new List<IXEvent> { CreateTestEvent("event1") };
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var profilerSession = new ProfilerSession(liveSession);

            Assert.That(profilerSession.Completed, Is.False, "Should not be completed before start");

            // Act
            liveSession.Start();
            Thread.Sleep(300); // Allow session to complete

            // Assert
            Assert.That(profilerSession.Completed, Is.True, "Should be completed after stream ends");
        }

        /// <summary>
        /// Test that Error is populated when observable session encounters an error
        /// </summary>
        [Test]
        public void TestError_IsPopulated_WhenSessionErrors()
        {
            // Arrange - session that fails with an error
            var expectedError = new Exception("Test error");
            var fetcher = new TestLiveEventFetcher(
                new List<IXEvent> { CreateTestEvent("event1") },
                failAfterEvents: 1,
                exceptionToThrow: expectedError,
                delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var profilerSession = new ProfilerSession(liveSession);

            Assert.That(profilerSession.Error, Is.Null, "Should not have error before start");

            // Act
            liveSession.Start();
            Thread.Sleep(300); // Allow session to error

            // Assert
            Assert.That(profilerSession.Error, Is.Not.Null, "Should have error after stream fails");
            Assert.That(profilerSession.Error.Message, Does.Contain("Test error"));
            Assert.That(profilerSession.Completed, Is.True, "Should be completed after error");
        }
    }
}
