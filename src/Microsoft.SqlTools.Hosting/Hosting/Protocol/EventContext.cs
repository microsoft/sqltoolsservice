//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Provides context for a received event so that handlers
    /// can write events back to the channel.
    /// </summary>
    public class EventContext
    {
        private readonly MessageWriter messageWriter;

        /// <summary>
        /// Parameterless constructor required for mocking
        /// </summary>
        public EventContext() { }

        public EventContext(MessageWriter messageWriter)
        {
            this.messageWriter = messageWriter;
        }

        public virtual async Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            await this.messageWriter.WriteEvent(
                eventType,
                eventParams);
        }
    }
}

