//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Class to access underlying XEvent session.
    /// </summary>
    public class XEventSession : IXEventSession
    {
        public Session Session { get; set; }

        private SessionId sessionId;
        public SessionId Id
        {
            get { return sessionId ??= GetSessionId(); }
        }

        protected virtual SessionId GetSessionId()
        {
            return new SessionId($"{Session.Parent.Name}_{Session.ID}", Session?.ID);
        }
        public virtual void Start()
        {
            this.Session.Start();
        }

        public virtual void Stop()
        {
            this.Session.Stop();
        }

        public virtual string GetTargetXml()
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
