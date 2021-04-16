//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    /// <summary>
    /// Interface for objects that can will handle messages. The methods exposed via this interface
    /// allow users to what to do when a specific message is received.
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Sets the function to run when a request message of a specific 
        /// <paramref name="requestType"/> is received
        /// </summary>
        /// <param name="requestType">Configuration of the request message <paramref name="requestHandler"/> will handle</param>
        /// <param name="requestHandler">What to do when a request message of <paramref name="requestType"/> is received</param>
        /// <param name="overrideExisting">If <c>true</c>, any existing handler will be replaced with this one</param>
        /// <typeparam name="TParams">Type of the parameters for the request, defined by <paramref name="requestType"/></typeparam>
        /// <typeparam name="TResult">Type of the response to the request, defined by <paramref name="requestType"/></typeparam>
        void SetAsyncRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool overrideExisting = false);
        
        /// <summary>
        /// Sets the function to run when a request message of a specific 
        /// <paramref name="requestType"/> is received
        /// </summary>
        /// <param name="requestType">Configuration of the request message <paramref name="requestHandler"/> will handle</param>
        /// <param name="requestHandler">What to do when a request message of <paramref name="requestType"/> is received</param>
        /// <param name="overrideExisting">If <c>true</c>, any existing handler will be replaced with this one</param>
        /// <typeparam name="TParams">Type of the parameters for the request, defined by <paramref name="requestType"/></typeparam>
        /// <typeparam name="TResult">Type of the response to the request, defined by <paramref name="requestType"/></typeparam>
        void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Action<TParams, RequestContext<TResult>> requestHandler,
            bool overrideExisting = false);

        /// <summary>
        /// Sets the function to run when an event message of a specific configurat
        /// <paramref name="eventType"/> is received
        /// </summary>
        /// <param name="eventType">Configuration of the event message <paramref name="eventHandler"/> will handle</param>
        /// <param name="eventHandler">What to do when an event message of <paramref name="eventType"/> is received</param>
        /// <param name="overrideExisting">If <c>true</c>, any existing handler will be replaced with this one</param>
        /// <typeparam name="TParams">Type of the parameters for the event</typeparam>
        void SetAsyncEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting = false);

        /// <summary>
        /// Sets the function to run when an event message of a specific 
        /// <paramref name="eventType"/> is received
        /// </summary>
        /// <param name="eventType">Configuration of the event message <paramref name="eventHandler"/> will handle</param>
        /// <param name="eventHandler">What to do when an event message of <paramref name="eventType"/> is received</param>
        /// <param name="overrideExisting">If <c>true</c>, any existing handler will be replaced with this one</param>
        /// <typeparam name="TParams">Type of the parameters for the event</typeparam>
        void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Action<TParams, EventContext> eventHandler,
            bool overrideExisting = false);
    }
}