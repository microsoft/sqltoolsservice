//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol
{
    public class RequestContext<TResult>
    {
        private Message requestMessage;
        private MessageWriter messageWriter;

        public RequestContext(Message requestMessage, MessageWriter messageWriter)
        {
            this.requestMessage = requestMessage;
            this.messageWriter = messageWriter;
        }

        public async Task SendResult(TResult resultDetails)
        {
            await this.messageWriter.WriteResponse<TResult>(
                resultDetails,
                requestMessage.Method,
                requestMessage.Id);
        }

        public async Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            await this.messageWriter.WriteEvent(
                eventType,
                eventParams);
        }

        public async Task SendError(object errorDetails)
        {
            await this.messageWriter.WriteMessage(
                Message.ResponseError(
                    requestMessage.Id,
                    requestMessage.Method,
                    JToken.FromObject(errorDetails)));
        }
    }
}

