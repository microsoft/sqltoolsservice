//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for CmdExecJobSubSystemDefinition.
    /// </summary>
    internal sealed class CmdExecJobSubSystemDefinition : ManagementActionBase, IJobStepPropertiesControl
    {
        public CmdExecJobSubSystemDefinition()
        {
        }

        public CmdExecJobSubSystemDefinition(CDataContainer dataContainer)
        {
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
        
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
        }
    }
}
