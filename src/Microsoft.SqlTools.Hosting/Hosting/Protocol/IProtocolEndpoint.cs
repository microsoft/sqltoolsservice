//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Hosting.Protocol
{
    /// <summary>
    /// A ProtocolEndpoint is used for inter-process communication. Services can register to
    /// respond to requests and events, send their own requests, and listen for notifications
    /// sent by the other side of the endpoint
    /// </summary>
    public interface IProtocolEndpoint : IEventSender, IRequestSender
    {
        void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler);

        void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler);

        void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting);
    }
}
