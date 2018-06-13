//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// JobStepsActions
    /// </summary>
    internal class JobStepsActions : ManagementActionBase
    {
        private JobStepData data;
        private JobData jobData;

        public JobStepsActions(
            CDataContainer dataContainer, 
            JobData jobData,
            AgentJobStepInfo stepInfo)
        {
            this.DataContainer = dataContainer;
            this.jobData = jobData;
            this.data = new JobStepData(jobData.JobSteps);

            // load properties from AgentJobStepInfo
            this.data.ID = stepInfo.Id;
            this.data.Name = stepInfo.StepName;
            this.data.Command = stepInfo.Script;
        }

        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);

            // Make sure the job step name is not blank.
            if (this.data.Name == null || this.data.Name.Length == 0)
            {
                throw new Exception(SR.JobStepNameCannotBeBlank);
            }

            // Check to make sure that the user has not entered a job step name that already exists.
            for (int stepIndex = 0; stepIndex < this.data.Parent.Steps.Count; ++stepIndex)
            {
                // don't compare if the id's are the same.
                if (data.ID != ((JobStepData)this.data.Parent.Steps[stepIndex]).ID && data.Name == ((JobStepData)this.data.Parent.Steps[stepIndex]).Name)
                {
                    // Throw an error if the job step name already exists
                    throw new Exception(SR.JobStepNameAlreadyExists(this.data.Name));
                }
            }

            if (runType == RunType.RunNow)
            {                   
                this.data.ApplyChanges(this.jobData.Job);
            }

            // regular execution always takes place
            return true;
        }
    }
}
