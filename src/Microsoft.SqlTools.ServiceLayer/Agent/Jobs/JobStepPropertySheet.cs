//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepPropertySheet.
    /// </summary>
    internal class JobStepPropertySheet : ManagementActionBase
    {
        private JobStepData data = null;
        
        public JobStepPropertySheet(CDataContainer dataContainer, JobStepData data)
        {
            this.DataContainer = dataContainer;
            this.data = data;
        }

        public void Init()
        {
            JobPropertiesAdvanced advanced = new JobPropertiesAdvanced(this.DataContainer, this.data);
            JobStepProperties general = new JobStepProperties(this.DataContainer, this.data, advanced);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {             
            }
            base.Dispose(disposing);
        }

        public bool Create()
        {
            // Make sure the job step name is not blank.
            if (this.data.Name == null || this.data.Name.Length == 0)
            {
                throw new Exception("SRError.JobStepNameCannotBeBlank");
            }

            // Check to make sure that the user has not entered a job step name that already exists.
            for (int stepIndex = 0; stepIndex < this.data.Parent.Steps.Count; stepIndex++)
            {
                // don't compare if the id's are the same.
                if(data.ID != ((JobStepData)this.data.Parent.Steps[stepIndex]).ID && data.Name == ((JobStepData)this.data.Parent.Steps[stepIndex]).Name)
                {
                    // Throw an error if the job step name already exists
                    throw new Exception("JobSR.JobStepNameAlreadyExists(this.data.Name)");
                }
            }

            this.data.ApplyChanges(this.GetCurrentJob());

            // regular execution always takes place
            return true;
        }

        private Job GetCurrentJob()
        {
            Job job = null;
            string urn = String.Empty;
            string jobIdString = null;
            STParameters parameters = new STParameters(this.DataContainer.Document);
            parameters.GetParam("urn", ref urn);
            parameters.GetParam("jobid", ref jobIdString);

            // If JobID is passed in look up by jobID
            if (!string.IsNullOrEmpty(jobIdString))
            {
                job = this.DataContainer.Server.JobServer.Jobs.ItemById(Guid.Parse(jobIdString));
            }
            else
            {
                // or use urn path to query job 
                job = this.DataContainer.Server.GetSmoObject(urn) as Job;
            }

            return job;
        }

        /// <summary>
        /// We don't own the CDataContainer that we get from our creator. We need to
        /// return false here so that the base class won't dispose it in its Dispose method
        /// </summary>
        protected override bool OwnDataContainer
        {
            get
            {
                return false;
            }
        }

    }
}
