//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepProperties.
    /// </summary>
    internal class JobStepProperties : ManagementActionBase
    {
        private JobStepSubSystems subSystems;
        private JobStepSubSystem selectedSubSystem = null;
        private bool needToUpdate = false;
        private const int jobIdLowerBound= 1;
        private int currentStepID = jobIdLowerBound;
        private int stepsCount = jobIdLowerBound;
        private IJobStepPropertiesControl activeControl = null;
        private JobStepData data;
        // used to persist state between job step types
        private JobStepData runtimeData;

        internal JobStepProperties(CDataContainer dataContainer, JobStepData context)
        {           
            this.DataContainer = dataContainer;
            this.data = context;
            this.runtimeData = new JobStepData(this.data);
            currentStepID = this.data.ID;
            stepsCount = this.data.StepCount;     
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

        private JobStepSubSystems SubSystems
        {
            get
            {
                if (this.subSystems == null)
                {
                    this.subSystems = new JobStepSubSystems(this.DataContainer, this.data);
                }
                return this.subSystems;
            }
        }
    }
}

      







