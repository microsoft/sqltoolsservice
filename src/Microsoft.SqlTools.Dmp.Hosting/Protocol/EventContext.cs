//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using Microsoft.SqlTools.Dmp.Contracts;
using Microsoft.SqlTools.Dmp.Hosting.Utility;

namespace Microsoft.SqlTools.Dmp.Hosting.Protocol
{
    /// <summary>
    /// Provides context for a received event so that handlers
    /// can write events back to the channel.
    /// </summary>
    public class EventContext : IEventSender
    {
        internal readonly BlockingCollection<Message> messageQueue;
        
        public EventContext(BlockingCollection<Message> outgoingMessageQueue)
        {
            // TODO: Either 1) make this constructor internal and provide a test framework for validating
            //       or 2) extract an interface for eventcontext to allow users to mock
            messageQueue = outgoingMessageQueue;
        }

        public void SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            messageQueue.Add(Message.CreateEvent(eventType, eventParams));
        }
    }
}

