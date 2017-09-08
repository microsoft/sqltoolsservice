//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    public class ProfilerSession
    {
        public string SessionId { get; set; }

        public ConnectionInfo ConnectionInfo { get; set; }

        public IXEventSession XEventSession { get; set; }

        public bool IsPolling { get; set; }
    }
}
