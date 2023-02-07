//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

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
