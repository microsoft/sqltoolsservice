//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using ErrorContract = Microsoft.SqlTools.Hosting.Contracts.Error;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class RequestContext<TResult> : IEventSender
    {
        private readonly Message requestMessage;
        private readonly MessageWriter messageWriter;

        public RequestContext(Message requestMessage, MessageWriter messageWriter)
        {
            this.requestMessage = requestMessage;
            this.messageWriter = messageWriter;
        }

        public RequestContext() { }

        public virtual async Task SendResult(TResult resultDetails)
        {
            await this.messageWriter.WriteResponse(
                resultDetails,
                requestMessage.Method,
                requestMessage.Id);
        }

        public virtual async Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            await this.messageWriter.WriteEvent(
                eventType,
                eventParams);
        }

        public virtual async Task SendError(string errorMessage, int errorCode = 0)
        {
            // Build the error message
            ErrorContract error = new ErrorContract
            {
                Message = errorMessage,
                Code = errorCode
            };

            // Send the message
            await this.messageWriter.WriteMessage(
                Message.ResponseError(
                    requestMessage.Id,
                    requestMessage.Method,
                    JToken.FromObject(error)));
        }
    }
}

