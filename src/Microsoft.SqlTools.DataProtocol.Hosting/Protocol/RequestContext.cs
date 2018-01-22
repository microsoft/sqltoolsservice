//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using Microsoft.SqlTools.DataProtocol.Contracts;
using Microsoft.SqlTools.DataProtocol.Contracts.Hosting;
using Microsoft.SqlTools.DataProtocol.Hosting.Utility;

namespace Microsoft.SqlTools.DataProtocol.Hosting.Protocol
{
    public class RequestContext<TResult> : IEventSender
    {
        internal readonly BlockingCollection<Message> messageQueue;
        internal readonly Message requestMessage;
        
        public RequestContext(Message message, BlockingCollection<Message> outgoingMessageQueue)
        {
            // TODO: Either 1) make this constructor internal and provide a tes framework for validating
            //       or 2) extract an interface for requestcontext to allow users to mock
            requestMessage = message;
            messageQueue = outgoingMessageQueue;
        }
        
        public virtual void SendResult(TResult resultDetails)
        {
            Message message = Message.CreateResponse(requestMessage.Id, resultDetails);
            messageQueue.Add(message);
        }

        public virtual void SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            Message message = Message.CreateEvent(eventType, eventParams);
            messageQueue.Add(message);
        }

        public virtual void SendError(string errorMessage, int errorCode = 0)
        {
            // Build the error message
            Error error = new Error
            {
                Message = errorMessage,
                Code = errorCode
            };
            Message message = Message.CreateResponseError(requestMessage.Id, error);
            messageQueue.Add(message);
        }

        public virtual void SendError(Exception e)
        {
            // Overload to use the parameterized error handler
            SendError(e.Message, e.HResult);
        }
    }
}

