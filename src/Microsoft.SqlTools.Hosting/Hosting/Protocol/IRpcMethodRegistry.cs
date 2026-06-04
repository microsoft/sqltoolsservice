//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public interface IRpcMethodRegistry
    {
        void RegisterRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, Task<TResult>> requestHandler);

        void RegisterNotificationHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, Task> eventHandler);
    }
}
