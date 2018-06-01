
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;

using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class MsaJobTargetServer
    {
        #region members
        private string name = string.Empty;
        // is the job currently applied to the target server
        private bool isJobAppliedToTarget = false;
        // will the job be applied to the target server in the future?
        private bool willJobBeAppliedToTarget = false;
        #endregion

        #region properties
        public string Name
        {
            get
            {
                return this.name;
            }
        }
        public bool IsJobAppliedToTarget
        {
            get
            {
                return this.isJobAppliedToTarget;
            }
            set
            {
                this.isJobAppliedToTarget = value;
            }
        }
        public bool WillJobBeAppliedToTarget
        {
            get
            {
                return this.willJobBeAppliedToTarget;
            }
            set
            {
                this.willJobBeAppliedToTarget = value;
            }
        }
        #endregion

        #region construction
        public MsaJobTargetServer()
        {
        }
        public MsaJobTargetServer(String name)
        {
            this.name = name;
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return this.name;
        }
        #endregion
    }
}
