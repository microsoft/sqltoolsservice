//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


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
using Microsoft.SqlTools.ServiceLayer.Admin;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobAlertsData
    {
        #region data members
        // collection of job steps.
        private ArrayList jobAlerts;
        private ArrayList deletedJobAlerts;
        private JobData parent;
        private CDataContainer context;
        private bool isReadOnly;
        #endregion

        #region construction
        public JobAlertsData(CDataContainer context, JobData parent)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            this.context = context;
            this.parent = parent;

            this.deletedJobAlerts = new ArrayList();

            // if we're creating a new job
            if (this.parent.Mode != JobData.ActionMode.Edit)
            {
                SetDefaults();
            }
            else
            {
                // load the JobStep objects
                LoadData();
            }
            IsReadOnly = parent.IsReadOnly;
        }
        #endregion

        #region public methods
        public void AddAlert(JobAlertData alert)
        {
            if (alert == null)
            {
                throw new ArgumentNullException("step");
            }
            this.jobAlerts.Add(alert);
        }
        public void DeleteAlert(JobAlertData alert)
        {
            if (alert == null)
            {
                throw new ArgumentNullException("step");
            }
            if (this.jobAlerts.Contains(alert))
            {
                this.jobAlerts.Remove(alert);
                this.deletedJobAlerts.Add(alert);
            }
        }
        public ArrayList Alerts
        {
            get
            {
                return this.jobAlerts;
            }
        }

        public bool IsReadOnly
        {
            get { return this.isReadOnly; }
            set { this.isReadOnly = value; }

        }
        #endregion

        #region data loading
        private void LoadData()
        {
            STParameters parameters = new STParameters(this.context.Document);
            string urn = String.Empty;
            string jobIdString = null;
            parameters.GetParam("urn", ref urn);
            parameters.GetParam("jobid", ref jobIdString);

            Job job = null;
            // If JobID is passed in look up by jobID
            if (!String.IsNullOrEmpty(jobIdString))
            {
                job = this.context.Server.JobServer.Jobs.ItemById(Guid.Parse(jobIdString));
            }
            else
            {
                // or use urn path to query job 
                job = this.context.Server.GetSmoObject(urn) as Job;
            }

            // load the data
            if (job != null)
            {
                AlertCollection alerts = job.Parent.Alerts;

                // allocate the array list
                this.jobAlerts = new ArrayList();

                for (int i = 0; i < alerts.Count; i++)
                {
                    // only get alerts that point to this job.
                    if (alerts[i].JobID == job.JobID)
                    {
                        //Since this job was just return from SMO, it is an existing object
                        //Flag it with true to indicate is has already been created.
                        this.jobAlerts.Add(new JobAlertData(alerts[i], true));
                    }
                }
            }
            else
            {
                SetDefaults();
            }
        }
        private void SetDefaults()
        {
            this.jobAlerts = new ArrayList();
        }
        #endregion

        #region data saving
        public void ApplyChanges(Job job)
        {
            if (this.IsReadOnly)
            {
                return;
            }
            // add any new items to the job
            foreach (JobAlertData jobAlert in this.jobAlerts)
            {
                if (!jobAlert.Created)
                {
                    Alert agentAlert = job.Parent.Alerts[jobAlert.Name];
                    if (agentAlert != null)
                    {
                        agentAlert.JobID = job.JobID;
                        agentAlert.Alter();
                    }
                }
            }
            foreach (JobAlertData jobAlert in this.deletedJobAlerts)
            {
                Alert agentAlert = job.Parent.Alerts[jobAlert.Name];
                if (agentAlert != null)
                {
                    agentAlert.JobID = Guid.Empty;
                    agentAlert.Alter();
                }
            }
        }
        #endregion

        #region events

        #endregion
    }
}
