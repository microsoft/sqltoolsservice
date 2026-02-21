//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    /// <summary>
    /// Tests for LiveStreamXEventSession and LiveStreamObservable classes.
    /// These tests verify push-based Extended Events streaming functionality with fail-fast behavior.
    /// </summary>
    public class LiveStreamXEventSessionTests
    {
        #region LiveStreamXEventSession Tests

        [Test]
        public void LiveStreamXEventSession_delivers_events_to_observers()
        {
            // Arrange
            var testEvents = CreateTestEvents(3);
            var fetcher = new TestLiveEventFetcher(testEvents);
            var sessionId = new SessionId("test_session", 1);
            var session = new LiveStreamXEventSession(() => fetcher, sessionId);
            var observer = new TestProfilerEventObserver();

            // Act
            session.ObservableSessionEvents.Subscribe(observer);
            session.Start();
            WaitForCompletion(observer, expectedEvents: 3);

            // Assert
            Assert.That(observer.Completed, Is.True, "Stream should complete after all events delivered");
            Assert.That(observer.ReceivedEvents.Count, Is.EqualTo(3), "Should receive all 3 events");
            Assert.That(observer.ReceivedEvents[0].Name, Is.EqualTo("event_0"));
            Assert.That(observer.ReceivedEvents[1].Name, Is.EqualTo("event_1"));
            Assert.That(observer.ReceivedEvents[2].Name, Is.EqualTo("event_2"));
        }

        [Test]
        public void LiveStreamXEventSession_converts_IXEvent_fields_to_ProfilerEvent_values()
        {
            // Arrange
            var fields = new Dictionary<string, object>
            {
                { "database_name", "TestDB" },
                { "duration", 1234 },
                { "cpu_time", 567 }
            };
            var actions = new Dictionary<string, object>
            {
                { "session_id", 42 },
                { "client_app_name", "TestApp" }
            };
            var testEvent = new TestXEvent("sql_statement_completed", DateTimeOffset.Now, fields, actions);
            var fetcher = new TestLiveEventFetcher(new[] { testEvent });
            var session = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var observer = new TestProfilerEventObserver();

            // Act
            session.ObservableSessionEvents.Subscribe(observer);
            session.Start();
            WaitForCompletion(observer, expectedEvents: 1);

            // Assert
            var profilerEvent = observer.ReceivedEvents.First();
            Assert.That(profilerEvent.Name, Is.EqualTo("sql_statement_completed"));
            Assert.That(profilerEvent.Values["database_name"], Is.EqualTo("TestDB"));
            Assert.That(profilerEvent.Values["duration"], Is.EqualTo("1234"));
            Assert.That(profilerEvent.Values["session_id"], Is.EqualTo("42"));
            Assert.That(profilerEvent.Values["client_app_name"], Is.EqualTo("TestApp"));
        }

        [Test]
        public void LiveStreamXEventSession_handles_field_and_action_name_collision()
        {
            // Arrange - both fields and actions have "session_id"
            var fields = new Dictionary<string, object> { { "session_id", "field_value" } };
            var actions = new Dictionary<string, object> { { "session_id", "action_value" } };
            var testEvent = new TestXEvent("test_event", DateTimeOffset.Now, fields, actions);
            var fetcher = new TestLiveEventFetcher(new[] { testEvent });
            var session = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var observer = new TestProfilerEventObserver();

            // Act
            session.ObservableSessionEvents.Subscribe(observer);
            session.Start();
            WaitForCompletion(observer, expectedEvents: 1);

            // Assert - action should be renamed to avoid collision
            var profilerEvent = observer.ReceivedEvents.First();
            Assert.That(profilerEvent.Values["session_id"], Is.EqualTo("field_value"));
            Assert.That(profilerEvent.Values["session_id (action)"], Is.EqualTo("action_value"));
        }

        [Test]
        public void LiveStreamXEventSession_Stop_cancels_stream()
        {
            // Arrange - create a slow stream that we can cancel
            var testEvents = CreateTestEvents(100);
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(50));
            var session = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var observer = new TestProfilerEventObserver();

            // Act
            session.ObservableSessionEvents.Subscribe(observer);
            session.Start();
            Thread.Sleep(150); // Let a few events through
            session.Stop();
            Thread.Sleep(100); // Give time for cancellation

            // Assert - should have received some but not all events
            Assert.That(observer.ReceivedEvents.Count, Is.LessThan(100), "Should not receive all events after stop");
            Assert.That(observer.Completed, Is.True, "Observer should be completed after stop");
        }

        #endregion

        #region Fail-Fast Behavior Tests

        [Test]
        public void LiveStreamObservable_reports_error_immediately_on_failure()
        {
            // Arrange - fetcher fails immediately (fail-fast: no reconnection attempts)
            var exception = new Exception("Connection failure");
            var observable = new LiveStreamObservable(
                () => new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: exception));
            var observer = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer);
            observable.Start();
            WaitForError(observer, timeoutMs: 2000);

            // Assert - should report error immediately without any retry
            Assert.That(observer.Error, Is.Not.Null, "Should report error immediately");
            Assert.That(observer.Error.Message, Does.Contain("Connection failure"));
            Assert.That(observer.Completed, Is.True, "Should complete after error");
        }

        [Test]
        public void LiveStreamObservable_reports_error_on_mid_stream_failure()
        {
            // Arrange - fetcher delivers some events then fails (fail-fast: no reconnection)
            var eventsBeforeFailure = CreateTestEvents(2);
            var midStreamException = new InvalidOperationException("Connection lost mid-stream");

            var failingFetcher = new TestLiveEventFetcher(
                eventsBeforeFailure,
                failAfterEvents: 2,
                exceptionToThrow: midStreamException);

            var observable = new LiveStreamObservable(() => failingFetcher);
            var observer = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer);
            observable.Start();
            WaitForError(observer, timeoutMs: 2000);

            // Assert - should have received events before failure and then error (no retry)
            Assert.That(observer.ReceivedEvents.Count, Is.EqualTo(2),
                "Should receive events before failure");
            Assert.That(observer.Error, Is.Not.Null, "Should report error without retry");
            Assert.That(observer.Error.Message, Does.Contain("Connection lost"));
        }

        [Test]
        public void LiveStreamObservable_reports_sql_exception_immediately()
        {
            // Arrange - simulate SQL Server error (fail-fast: no reconnection)
            var sqlException = new InvalidOperationException("A network-related or instance-specific error occurred");

            var failingFetcher = new TestLiveEventFetcher(
                Array.Empty<TestXEvent>(),
                failImmediately: true,
                exceptionToThrow: sqlException);

            var observable = new LiveStreamObservable(() => failingFetcher);
            var observer = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer);
            observable.Start();
            WaitForError(observer, timeoutMs: 2000);

            // Assert - should report error immediately without retry
            Assert.That(observer.Error, Is.Not.Null);
            Assert.That(observer.Error.Message, Does.Contain("network-related"));
        }

        #endregion

        #region Multiple Observer Tests

        [Test]
        public void LiveStreamObservable_delivers_events_to_multiple_observers()
        {
            // Arrange
            var testEvents = CreateTestEvents(3);
            var fetcher = new TestLiveEventFetcher(testEvents);
            var observable = new LiveStreamObservable(() => fetcher);
            var observer1 = new TestProfilerEventObserver();
            var observer2 = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer1);
            observable.Subscribe(observer2);
            observable.Start();
            WaitForCompletion(observer1, expectedEvents: 3);
            WaitForCompletion(observer2, expectedEvents: 3);

            // Assert - both observers should receive all events
            Assert.That(observer1.ReceivedEvents.Count, Is.EqualTo(3));
            Assert.That(observer2.ReceivedEvents.Count, Is.EqualTo(3));
        }

        [Test]
        public void LiveStreamObservable_unsubscribe_stops_delivery_to_that_observer()
        {
            // Arrange
            var testEvents = CreateTestEvents(10);
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(20));
            var observable = new LiveStreamObservable(() => fetcher);
            var observer1 = new TestProfilerEventObserver();
            var observer2 = new TestProfilerEventObserver();

            // Act
            var subscription1 = observable.Subscribe(observer1);
            observable.Subscribe(observer2);
            observable.Start();
            Thread.Sleep(50); // Let a few events through
            subscription1.Dispose(); // Unsubscribe observer1
            WaitForCompletion(observer2, expectedEvents: 10, timeoutMs: 1000);

            // Assert - observer2 should have more events than observer1
            Assert.That(observer2.ReceivedEvents.Count, Is.EqualTo(10));
            Assert.That(observer1.ReceivedEvents.Count, Is.LessThan(10));
        }

        #endregion

        #region ProfilerSession Integration Tests

        [Test]
        public void ProfilerSession_with_observable_session_triggers_immediate_polling()
        {
            // Arrange
            var testEvents = CreateTestEvents(2);
            var fetcher = new TestLiveEventFetcher(testEvents);
            var liveSession = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var profilerSession = new ProfilerSession(liveSession);

            // Act
            liveSession.Start();
            Thread.Sleep(200); // Allow events to be delivered

            // Assert - profilerSession should allow processing after push event
            Assert.That(profilerSession.TryEnterProcessing(), Is.True, "Should allow processing after push event");
            profilerSession.ExitProcessing();
        }

        [Test]
        public void ProfilerSessionMonitor_notifies_listeners_when_server_closes_session()
        {
            // Arrange - simulate a session that delivers events then completes (server closes the session)
            var testEvents = CreateTestEvents(2);
            var fetcher = new TestLiveEventFetcher(testEvents, delayBetweenEvents: TimeSpan.FromMilliseconds(10));
            var sessionFactory = new TestLiveStreamSessionFactory(new[] { fetcher });

            var monitor = new ProfilerSessionMonitor();
            var testListener = new TestSessionListener();
            monitor.AddSessionListener(testListener);
            
            string testViewerId = "test_viewer";
            var session = sessionFactory.CreateLiveStreamSession("server_closed_session");

            // Act - start monitoring the session
            monitor.StartMonitoringSession(testViewerId, session);

            // Wait for session to complete (server closes it after delivering all events)
            int retries = 50;
            while (testListener.StoppedSessions.Count == 0 && retries-- > 0)
            {
                Thread.Sleep(100);
            }

            // Assert - listener should be notified when server closes the session
            Assert.That(testListener.StoppedSessions, Has.Count.EqualTo(1), "Listener should be notified when server closes session");
            Assert.That(testListener.StoppedSessions[0], Is.EqualTo(testViewerId), "Notified viewer ID should match");
        }

        [Test]
        public void ProfilerSessionMonitor_notifies_listeners_with_error_when_server_disconnects()
        {
            // Arrange - simulate a session that fails with an error (server disconnects unexpectedly)
            var serverDisconnectError = new Exception("Server disconnected unexpectedly");
            var fetcher = new TestLiveEventFetcher(
                CreateTestEvents(1),
                failAfterEvents: 1,
                exceptionToThrow: serverDisconnectError,
                delayBetweenEvents: TimeSpan.FromMilliseconds(10));

            var sessionFactory = new TestLiveStreamSessionFactory(new[] { fetcher });

            var monitor = new ProfilerSessionMonitor();
            var testListener = new TestSessionListener();
            monitor.AddSessionListener(testListener);
            
            var session = sessionFactory.CreateLiveStreamSession("disconnected_session");

            // Act - start monitoring the session
            monitor.StartMonitoringSession("test_viewer", session);

            // Wait for session to fail
            int retries = 50;
            while (testListener.StoppedSessions.Count == 0 && retries-- > 0)
            {
                Thread.Sleep(100);
            }

            // Assert - listener should be notified with error message when server disconnects
            Assert.That(testListener.StoppedSessions, Has.Count.EqualTo(1), "Listener should be notified when server disconnects");
            Assert.That(testListener.ErrorMessages[0], Does.Contain("Server disconnected"), 
                "Error message should indicate server disconnection");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void LiveStreamObservable_throws_when_restarting_closed_stream()
        {
            // Arrange
            var observable = new LiveStreamObservable(
                () => new TestLiveEventFetcher(Array.Empty<TestXEvent>()));
            observable.Start();
            observable.Close();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(observable.Start);
        }

        [Test]
        public void LiveStreamObservable_Subscribe_throws_on_null_observer()
        {
            // Arrange
            var observable = new LiveStreamObservable(
                () => new TestLiveEventFetcher(Array.Empty<TestXEvent>()));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null));
        }

        [Test]
        public void LiveStreamXEventSession_constructor_throws_on_null_sessionId()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LiveStreamXEventSession(() => new TestLiveEventFetcher(Array.Empty<TestXEvent>()), null));
        }

        [Test]
        public void LiveStreamXEventSession_constructor_throws_on_null_factory()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LiveStreamXEventSession(null, new SessionId("test", 1)));
        }

        [Test]
        public void LiveStreamObservable_handles_null_actions_in_event()
        {
            // Arrange - event with null actions
            var testEvent = new TestXEvent("test_event", DateTimeOffset.Now,
                new Dictionary<string, object> { { "field1", "value1" } },
                actions: null);
            var fetcher = new TestLiveEventFetcher(new[] { testEvent });
            var session = new LiveStreamXEventSession(() => fetcher, new SessionId("test", 1));
            var observer = new TestProfilerEventObserver();

            // Act
            session.ObservableSessionEvents.Subscribe(observer);
            session.Start();
            WaitForCompletion(observer, expectedEvents: 1);

            // Assert - should handle null actions gracefully
            Assert.That(observer.ReceivedEvents.Count, Is.EqualTo(1));
            Assert.That(observer.ReceivedEvents[0].Values["field1"], Is.EqualTo("value1"));
        }

        [Test]
        public void LiveStreamObservable_handles_operation_canceled_as_graceful_stop()
        {
            // Arrange - OperationCanceledException should be treated as graceful stop, not error
            var events = CreateTestEvents(3);
            var fetcher = new TestLiveEventFetcher(events, delayBetweenEvents: TimeSpan.FromMilliseconds(50));
            var observable = new LiveStreamObservable(() => fetcher);
            var observer = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer);
            observable.Start();
            Thread.Sleep(75); // Let 1-2 events through
            observable.Close(); // This should cancel the operation

            // Give it time to process the cancellation
            Thread.Sleep(100);

            // Assert - should complete gracefully without error
            Assert.That(observer.Error, Is.Null, "Cancellation should not be reported as error");
            Assert.That(observer.ReceivedEvents.Count, Is.GreaterThan(0), "Should have received some events before cancel");
        }

        [Test]
        public void LiveStreamObservable_propagates_error_to_all_observers_on_failure()
        {
            // Arrange - multiple observers should all receive the error
            var exception = new Exception("Fatal streaming error");
            var observable = new LiveStreamObservable(
                () => new TestLiveEventFetcher(Array.Empty<TestXEvent>(), failImmediately: true, exceptionToThrow: exception));

            var observer1 = new TestProfilerEventObserver();
            var observer2 = new TestProfilerEventObserver();
            var observer3 = new TestProfilerEventObserver();

            // Act
            observable.Subscribe(observer1);
            observable.Subscribe(observer2);
            observable.Subscribe(observer3);
            observable.Start();
            WaitForError(observer1, timeoutMs: 2000);
            WaitForError(observer2, timeoutMs: 100);
            WaitForError(observer3, timeoutMs: 100);

            // Assert - all observers should receive the error
            Assert.That(observer1.Error, Is.Not.Null);
            Assert.That(observer2.Error, Is.Not.Null);
            Assert.That(observer3.Error, Is.Not.Null);
            Assert.That(observer1.Error.Message, Does.Contain("Fatal streaming error"));
        }

        #endregion

        #region Test Helpers

        private static List<TestXEvent> CreateTestEvents(int count, int startIndex = 0)
        {
            var events = new List<TestXEvent>();
            for (int i = 0; i < count; i++)
            {
                events.Add(new TestXEvent(
                    $"event_{startIndex + i}",
                    DateTimeOffset.Now.AddMilliseconds(i),
                    new Dictionary<string, object>
                    {
                        { "index", startIndex + i },
                        { "event_sequence", (startIndex + i + 1).ToString() }
                    }));
            }
            return events;
        }

        private static void WaitForCompletion(TestProfilerEventObserver observer, int expectedEvents, int timeoutMs = 5000)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            while (!observer.Completed && observer.ReceivedEvents.Count < expectedEvents && DateTime.Now < deadline)
            {
                Thread.Sleep(10);
            }
        }

        private static void WaitForError(TestProfilerEventObserver observer, int timeoutMs = 5000)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            while (observer.Error == null && !observer.Completed && DateTime.Now < deadline)
            {
                Thread.Sleep(10);
            }
        }

        #endregion
    }

    /// <summary>
    /// Test observer for ProfilerEvent streams.
    /// </summary>
    sealed class TestProfilerEventObserver : IObserver<ProfilerEvent>
    {
        public List<ProfilerEvent> ReceivedEvents { get; } = new List<ProfilerEvent>();
        public bool Completed { get; private set; }
        public Exception Error { get; private set; }

        public void OnCompleted()
        {
            Completed = true;
        }

        public void OnError(Exception error)
        {
            Error = error;
            Completed = true;
        }

        public void OnNext(ProfilerEvent value)
        {
            ReceivedEvents.Add(value);
        }
    }
}
