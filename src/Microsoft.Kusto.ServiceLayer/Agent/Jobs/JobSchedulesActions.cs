//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.Kusto.ServiceLayer.Agent.Contracts;
using Microsoft.Kusto.ServiceLayer.Management;

namespace Microsoft.Kusto.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobSchedulesActions.
    /// </summary>
    internal class JobSchedulesActions : ManagementActionBase
    {
        private bool sharedSchedulesSupported = false;
        private JobData data;
        private JobScheduleData scheduleData = null;
        private AgentScheduleInfo scheduleInfo;
        private ConfigAction configAction;

        public JobSchedulesActions(CDataContainer dataContainer, JobData data, AgentScheduleInfo scheduleInfo, ConfigAction configAction)
        {
            this.DataContainer = dataContainer;
            this.data = data;
            this.configAction = configAction;
            this.scheduleInfo = scheduleInfo;
            this.sharedSchedulesSupported = this.DataContainer.Server.Information.Version.Major >= 9;

            if (configAction == ConfigAction.Create)
            {
                this.scheduleData = new JobScheduleData(this.data.Job);
                this.scheduleData.SetJobSchedule(new JobSchedule());
            }
            else
            {
                // get the JobScheduleData from the urn
                string urn = null;
                STParameters parameters = new STParameters();
                parameters.SetDocument(this.DataContainer.Document);
                parameters.GetParam("urn", ref urn);
            
                JobSchedule jobStep = this.data.Job.Parent.Parent.GetSmoObject(urn) as JobSchedule;
                if (jobStep != null)
                {
                    this.scheduleData = new JobScheduleData(jobStep);
                }

                if (configAction == ConfigAction.Update && this.scheduleData == null)
                {
                    throw new Exception("Schedule urn parameter cannot be null");
                }
            }

            // copy properties from AgentScheduelInfo
            if (this.scheduleData != null)
            {
                this.scheduleData.Name = scheduleInfo.Name;
            }
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

        /// <summary>
        /// called by PreProcessExecution to enable derived classes to take over execution
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);
            if (this.scheduleData == null)
            {
                return false;
            }

            if (this.configAction == ConfigAction.Drop)
            {
                var jobSchedule = this.scheduleData.SourceSchedule;
                if (jobSchedule != null)
                {
                    jobSchedule.DropIfExists();
                }
            }
            else
            {
                this.scheduleData.ApplyChanges();
            }
            return false;
        }
    }
}
