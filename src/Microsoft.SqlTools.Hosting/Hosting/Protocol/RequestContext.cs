//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Error = Microsoft.SqlTools.Hosting.Contracts.Error;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class RequestContext<TResult> : IEventSender
    {
        private readonly Message? requestMessage;
        private readonly MessageWriter? messageWriter;

        public RequestContext(Message? requestMessage, MessageWriter messageWriter)
        {
            this.requestMessage = requestMessage;
            this.messageWriter = messageWriter;
        }

        public RequestContext() { }

        public virtual async Task SendResult(TResult? resultDetails)
        {
            if (this.requestMessage != null && this.messageWriter != null)
            {
                await this.messageWriter.WriteResponse(
                    resultDetails,
                    requestMessage.Method,
                    requestMessage.Id);
            }
        }

        public virtual async Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            if (this.messageWriter != null)
            {
                await this.messageWriter.WriteEvent(
                    eventType,
                    eventParams);
            }
        }

        public virtual async Task SendError(string? errorMessage, int errorCode = 0, string? data = null)
        {
            if (this.requestMessage != null && this.messageWriter != null)
            {
                // Build and send the error message
                await this.messageWriter.WriteError(
                        requestMessage.Method,
                        requestMessage.Id,
                        new Error
                        {
                            Message = errorMessage,
                            Code = errorCode,
                            Data = data
                        });
            }
        }

        public virtual async Task SendError(Exception? e)
        {
            if (e != null)
            {
                // Overload to use the parameterized error handler
                await SendError(e.Message, e.HResult, e.StackTrace);
            }
        }
    }
}

