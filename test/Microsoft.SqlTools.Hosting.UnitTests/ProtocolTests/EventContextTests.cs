//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.SqlTools.Hosting.Protocol;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    [TestFixture]
    public class EventContextTests
    {
        [Test]
        public void SendEvent()
        {
            // Setup: Create collection
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());

            // If: I construct an event context with a message writer
            //     And send an event with it
            var eventContext = new EventContext(bc);
            eventContext.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);

            // Then: The message should be added to the queue
            var messages = bc.ToArray().Select(m => m.MessageType);
            Assert.That(messages, Is.EqualTo(new[] { MessageType.Event }), "Single message of type event in the queue after SendEvent");
        }
    }
}