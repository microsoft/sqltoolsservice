//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// A ProtocolEndpoint is used for inter-process communication. Services can register to
    /// respond to requests and events, send their own requests, and listen for notifications
    /// sent by the other side of the endpoint
    /// </summary>
    public interface IProtocolEndpoint : IEventSender, IRequestSender
    {
        /// <summary>
        /// Set a request handler
        /// </summary>
        /// <typeparam name="TParams">type of parameter</typeparam>
        /// <typeparam name="TResult">type of result</typeparam>
        /// <param name="requestType">request type</param>
        /// <param name="requestHandler">request handler</param>
        /// <param name="isParallelProcessingSupported">whether this handler supports parallel processing</param>
        void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool isParallelProcessingSupported = false);

        /// <summary>
        /// Set an request handler
        /// </summary>
        /// <typeparam name="TParams">type of parameter</typeparam>
        /// <param name="eventType">type of event</param>
        /// <param name="eventHandler">event handler</param>
        /// <param name="isParallelProcessingSupported">whether this handler supports parallel processing</param>
        void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool isParallelProcessingSupported = false);

        /// <summary>
        /// Set an request handler
        /// </summary>
        /// <typeparam name="TParams">type of parameter</typeparam>
        /// <param name="eventType">type of event</param>
        /// <param name="eventHandler">event handler</param>
        /// <param name="overrideExisting">whether to override the existing handler for the same event type</param>
        /// <param name="isParallelProcessingSupported">whether this handler supports parallel processing</param>
        void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting,
            bool isParallelProcessingSupported = false);
    }
}
