//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Interface for objects that can send events via the JSON RPC channel
    /// </summary>
    public interface IEventSender
    {
        /// <summary>
        /// Sends an event over the JSON RPC channel
        /// </summary>
        /// <param name="eventType">Configuration of the event to send</param>
        /// <param name="eventParams">Parameters for the event to send</param>
        /// <typeparam name="TParams">Type of the parameters for the event, defined in <paramref name="eventType"/></typeparam>
        void SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams);
    }
}
