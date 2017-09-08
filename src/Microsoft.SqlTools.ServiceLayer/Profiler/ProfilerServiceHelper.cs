//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Helper methods for working with XEvent Profiler sessions
    /// </summary>
    public class ProfilerServiceHelper : IProfilerServiceHelper
    {
        public Session GetOrCreateSession(ConnectionDetails connectionDetails)
        {
            SqlConnectionStringBuilder connectionBuilder;          
            connectionBuilder = new SqlConnectionStringBuilder
            {
                IntegratedSecurity = false,
                ["Data Source"] = "localhost",
                ["User Id"] = "sa",
                ["Password"] = "Yukon900",
                ["Initial Catalog"] = "master"
            };

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());
            SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
            XEStore store = new XEStore(connection);
            Session session = store.Sessions["Profiler"];

            try
            {
                if (!session.IsRunning)
                {
                    session.Start();
                }
            }
            catch { }

            var target = session.Targets.First();
            var targetXml = target.GetTargetData();

            return session;
        }
    }
}
