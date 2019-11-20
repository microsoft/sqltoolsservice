//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Profiler.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Profiler
{
    /// <summary>
    /// Class to access underlying XEvent session.
    /// </summary>
    public class XEventSession : IXEventSession
    {
        public Session Session { get; set; }

        public int Id
        {
            get { return Session.ID; }
        }

        public void Start()
        {
            this.Session.Start();
        }

        public void Stop()
        {
            this.Session.Stop();
        }

        public string GetTargetXml()
        {
            if (this.Session == null)
            {
                return string.Empty;
            }

            // try to read events from the first target
            Target defaultTarget = this.Session.Targets.FirstOrDefault();
            return defaultTarget != null ? defaultTarget.GetTargetData() : string.Empty;
        }
    }
}
