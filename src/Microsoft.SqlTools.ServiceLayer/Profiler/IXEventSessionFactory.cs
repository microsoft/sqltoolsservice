//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public interface IXEventSessionFactory
    {
        /// <summary>
        /// Gets or creates an XEvent session with the given template
        /// </summary>
        IXEventSession GetOrCreateXEventSession(string template, ConnectionInfo connInfo);
    }
}
