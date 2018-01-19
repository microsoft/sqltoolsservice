//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;
using Xunit;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.ProtocolTests
{
    public class EventContextTests
    {
        [Fact]
        public void SendEvent()
        {
            // Setup: Create collection
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());

            // If: I construct an event context with a message writer
            //     And send an event with it
            var eventContext = new EventContext(bc);
            eventContext.SendEvent(Common.EventType, Common.TestMessageContents.DefaultInstance);
            
            // Then: The message should be added to the queue
            Assert.Equal(1, bc.ToArray().Length);
            Assert.Equal(MessageType.Event, bc.ToArray()[0].MessageType);
        }
    }
}