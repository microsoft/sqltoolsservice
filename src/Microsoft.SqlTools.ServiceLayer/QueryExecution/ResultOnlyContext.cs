// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Hosting.Protocol;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Implementation of IEventSender that swallows events without doing anything with them.
    /// In the future this class could be used to roll up all the events and send
    /// them all at once
    /// </summary>
    public class ResultOnlyContext<TResult> : IEventSender
    {
        private readonly RequestContext<TResult> OrigContext;

        public ResultOnlyContext(RequestContext<TResult> context) {
            OrigContext = context;
        }

        public virtual Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            // no op to swallow events
            // in the future this could be used to roll up events and send them back in the result
            return Task.FromResult(true);
        }

        public virtual Task SendError(string errorMessage, int errorCode = 0)
        {
            return OrigContext.SendError(errorMessage, errorCode);
        }
    }
}