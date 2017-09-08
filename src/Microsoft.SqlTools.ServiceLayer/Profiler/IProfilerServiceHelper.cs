//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// 
    /// </summary>
    public interface IProfilerServiceHelper
    {
        Session GetOrCreateSession(ConnectionDetails connectionDetails);
    }
}
