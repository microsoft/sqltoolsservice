// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Hosting.Protocol;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    public class ResultOnlyContext<TResult> : IEventSender
    {
        private readonly RequestContext<TResult> origContext;

        public ResultOnlyContext(RequestContext<TResult> context) {
            origContext = context;
        }

        public virtual async Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            // no op to swallow events
            // in the future this could be used to roll up events and send them back in the result
        }
    }
}