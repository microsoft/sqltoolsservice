//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    /// <summary>
    /// Tests for ProfilerSession class
    /// </summary>
    public class ProfilerSessionTests
    {   
        /// <summary>
        /// Test the FilterOldEvents method
        /// </summary>
        [Fact]
        public void TestFilterOldEvents()
        {
            // create a profiler session and get some test events
            var profilerSession = new ProfilerSession();
            var allEvents = ProfilerTestObjects.TestProfilerEvents;
            var profilerEvents = ProfilerTestObjects.TestProfilerEvents;

            // filter all the results from the first poll
            // these events happened before the profiler began
            profilerSession.FilterOldEvents(profilerEvents);
            Assert.Equal(0, profilerEvents.Count);

            // add a new event
            var newEvent = new ProfilerEvent("new event", "1/1/2017");
            newEvent.Values.Add("event_sequence", "4");
            allEvents.Add(newEvent);

            // poll all events
            profilerEvents.AddRange(allEvents);

            // filtering should leave only the new event
            profilerSession.FilterOldEvents(profilerEvents);
            Assert.Equal(1, profilerEvents.Count);
            Assert.True(profilerEvents[0].Equals(newEvent));

            //poll again with no new events
            profilerEvents.AddRange(allEvents);

            // filter should now filter all the events since they've been seen before
            profilerSession.FilterOldEvents(profilerEvents);
            Assert.Equal(0, profilerEvents.Count);
        }

        /// <summary>
        /// Test the FilterProfilerEvents method
        /// </summary>
        [Fact]
        public void TestFilterProfilerEvents()
        {
            // create a profiler session and get some test events
            var profilerSession = new ProfilerSession();
            var profilerEvents = ProfilerTestObjects.TestProfilerEvents;
                        
            int expectedEventCount = profilerEvents.Count;
                        
            // add a new "Profiler Polling" event
            var newEvent = new ProfilerEvent("sql_batch_completed", "1/1/2017");
            newEvent.Values.Add("batch_text", "SELECT target_data FROM sys.dm_xe_session_targets");
            newEvent.Values.Add("event_sequence", "4");
            profilerEvents.Add(newEvent);

            // verify that the polling event is removed
            Assert.Equal(profilerEvents.Count, expectedEventCount + 1);
            var newProfilerEvents = profilerSession.FilterProfilerEvents(profilerEvents);
            Assert.Equal(newProfilerEvents.Count, expectedEventCount);           
        }

        /// <summary>
        /// Test notifications for lost events
        /// </summary>
        [Fact]
        public void TestEventsLost()
        {
            // create a profiler session and get some test events
            var profilerSession = new ProfilerSession();
            var profilerEvents = ProfilerTestObjects.TestProfilerEvents;

            // filter all the results from the first poll
            // these events happened before the profiler began
            profilerSession.FilterOldEvents(profilerEvents);
            Assert.Equal(0, profilerEvents.Count);
            // No events should be lost
            Assert.False(profilerSession.EventsLost);

            // test all events are overwritten, but no events are lost
            profilerEvents.Clear();
            ProfilerEvent newEvent = new ProfilerEvent("event4", "6/18/2018");
            newEvent.Values.Add("event_sequence", "4");

            profilerEvents.Add(newEvent);         
            profilerSession.FilterOldEvents(profilerEvents);

            // should not show event loss
            Assert.False(profilerSession.EventsLost);

            // test all events are overwritten, and events are lost
            profilerEvents.Clear();
            newEvent = new ProfilerEvent("event7", "6/18/2018");
            newEvent.Values.Add("event_sequence", "7");

            profilerEvents.Add(newEvent);         
            profilerSession.FilterOldEvents(profilerEvents);

            // should show event loss
            Assert.True(profilerSession.EventsLost);

            //poll again with previously seen events
            profilerEvents.Add(newEvent);

            // old events were seen, no event loss occured
            profilerSession.FilterOldEvents(profilerEvents);           
            Assert.False(profilerSession.EventsLost);    
        }

        /// <summary>
        /// Test the TryEnterPolling method
        /// </summary>
        [Fact]
        public void TestTryEnterPolling()
        {
            DateTime startTime = DateTime.Now;

            // create new profiler session
            var profilerSession = new ProfilerSession();
            
            // enter the polling block
            Assert.True(profilerSession.TryEnterPolling());
            Assert.True(profilerSession.IsPolling);

            // verify we can't enter again
            Assert.False(profilerSession.TryEnterPolling());

            // set polling to false to exit polling block
            profilerSession.IsPolling = false;

            bool outsideDelay = DateTime.Now.Subtract(startTime) >= profilerSession.PollingDelay;

            // verify we can only enter again if we're outside polling delay interval
            Assert.Equal(profilerSession.TryEnterPolling(), outsideDelay);

            // reset IsPolling in case the delay has elasped on slow machine or while debugging
            profilerSession.IsPolling = false;

            // wait for the polling delay to elapse
            Thread.Sleep(profilerSession.PollingDelay);

            // verify we can enter the polling block again
            Assert.True(profilerSession.TryEnterPolling());
        }
    }
}
