﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Simple EventContext for testing that just swallows all events.
    /// </summary>
    public class TestEventContext : EventContext
    {
        public override async Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            await Task.FromResult(0);
        }
    }
}
