//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobHelper
    {
        private JobHelper()
        {
        }

        private ServerConnection connection = null;
        private string jobName = string.Empty;
        private SMO.Server server = null;
        private Job job = null;

        //
        // ServerConnection object should be passed from caller,
        // who gets it from CDataContainer.ServerConnection
        //
        public JobHelper(ServerConnection connection)
        {
            this.connection = connection;
            server = new SMO.Server(connection);
        }

        public string JobName
        {
            get
            {
                return jobName;
            }
            set
            {
                if (server != null)
                {
                    Job j = server.JobServer.Jobs[value];
                    if (j != null)
                    {
                        job = j;
                        jobName = value;
                    }
                    else
                    {
                        throw new InvalidOperationException("Job not found");
                    }
                }
            }
        }

        public void Stop()
        {
            if (job != null)
            {
                job.Stop();
            }
        }

        public void Start()
        {
            if (job != null)
            {
                job.Start();
            }
        }

        public void Delete()
        {
            if (job != null)
            {
                job.Drop();

                //
                // you can't do anything with 
                // a job after you drop it!
                //
                job = null;
            }
        }

        public void Enable(bool enable)
        {
            if (job != null && job.IsEnabled != enable)
            {
                job.IsEnabled = enable;
                job.Alter();
            }
        }
    }
}
