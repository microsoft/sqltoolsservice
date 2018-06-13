//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobStepsData
    {
        #region fields
        /// <summary>
        /// collection of job steps.
        /// </summary>
        private ArrayList jobSteps;
        /// <summary>
        /// List of job steps to be deleted
        /// </summary>
        private ArrayList deletedJobSteps;
        /// <summary>
        /// Parent JobData
        /// </summary>
        private JobData parent;
        /// <summary>
        /// Server context
        /// </summary>
        private CDataContainer context;
        /// <summary>
        /// Start Step
        /// </summary>
        private JobStepData startStep;
        /// <summary>
        /// list of available databases
        /// </summary>
        private string[] databases = null;
        #endregion

        #region public properties
        /// <summary>
        /// JobData structure this object is part of
        /// </summary>
        public JobData Parent
        {
            get
            {
                return this.parent;
            }
        }
        /// <summary>
        /// Server Version
        /// </summary>
        public Version Version
        {
            get
            {
                return this.parent.Version;
            }
        }
        /// <summary>
        /// Mode in which the dialog has been launched
        /// </summary>
        JobData.ActionMode Mode
        {
            get
            {
                if (this.parent != null)
                {
                    return this.parent.Mode;
                }
                else
                {
                    return JobData.ActionMode.Unknown;
                }
            }
        }
        /// <summary>
        /// List of steps in this job
        /// </summary>
        public ArrayList Steps
        {
            get
            {
                return this.jobSteps;
            }
        }
        /// <summary>
        /// The default start step
        /// </summary>
        public JobStepData StartStep
        {
            get
            {
                // we can't point to a step that is marked for deletion
                if (this.startStep != null && this.startStep.ToBeDeleted == true)
                {
                    this.startStep = null;
                }
                // if the start step is null, and we have job steps, just point
                // the start step to the first one.
                if (this.startStep == null && this.jobSteps.Count > 0)
                {
                    this.startStep = (JobStepData)this.jobSteps[0];
                }

                return this.startStep;
            }
            set
            {
                this.startStep = value;
            }
        }
        /// <summary>
        /// List of all available databases on the server
        /// </summary>
        public string[] Databases
        {
            get
            {
                CheckAndLoadDatabases();
                return this.databases;
            }
        }
        /// <summary>
        /// Indicates whether or not the order of the steps has changed
        /// </summary>
        public bool HasStepOrderChanged
        {
            get
            {
                bool orderChanged = false;
                foreach (JobStepData jsd in this.jobSteps)
                {
                    if (jsd.StepIdChanged == true)
                    {
                        orderChanged = true;
                        break;
                    }
                }
                return orderChanged;
            }
        }
        /// <summary>
        /// Indicates whether or not the Job is read only
        /// </summary>
        public bool IsReadOnly
        {
            get { return parent.IsReadOnly; }
        }
        #endregion

        #region Events
        public event EventHandler StepOrderChanged;
        #endregion

        #region construction
        /// <summary>
        /// Create a new JobStepsData object with a new job step
        /// </summary>
        /// <param name="context">server context</param>
        /// <param name="script">script for the job step</param>
        /// <param name="parent">owning data object</param>
        public JobStepsData(CDataContainer context, string script, JobData parent)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (script == null)
            {
                throw new ArgumentNullException("strint");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            CommonInit(context, parent, script);
        }
        /// <summary>
        /// Create a new jobsteps data object
        /// </summary>
        /// <param name="context">server context</param>
        /// <param name="parent">owning data object</param>
        public JobStepsData(CDataContainer context, JobData parent)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            CommonInit(context, parent, null);
        }
        /// <summary>
        /// Common initialization routines for constructrs
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <param name="script"></param>
        private void CommonInit(CDataContainer context, JobData parent, string script)
        {
            this.context = context;
            this.parent = parent;

            this.deletedJobSteps = new ArrayList();

            // if we're creating a new job
            if (this.parent.Mode != JobData.ActionMode.Edit)
            {
                SetDefaults();
                if (script != null && script.Length != 0)
                {
                    LoadFromScript(script);
                }
            }
            else
            {
                // load the JobStep objects
                LoadData();
            }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Add a new existing step to the end of the job step collection
        /// </summary>
        /// <param name="step"></param>
        public void AddStep(JobStepData step)
        {
            this.jobSteps.Add(step);
            RecalculateStepIds();
        }
        /// <summary>
        /// Insert a jobstep into an existing location
        /// </summary>
        /// <param name="index"></param>
        /// <param name="step"></param>
        public void InsertStep(int index, JobStepData step)
        {
            this.jobSteps.Insert(index, step);
            RecalculateStepIds();
        }
        /// <summary>
        /// Delete a jobstep
        /// </summary>
        /// <param name="step"></param>
        public void DeleteStep(JobStepData step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }
            if (this.jobSteps.Contains(step))
            {
                this.jobSteps.Remove(step);
                // make a note to delete the step
                this.deletedJobSteps.Add(step);
                step.ToBeDeleted = true;
            }
            RecalculateStepIds();
        }
        /// <summary>
        /// Get a JobStepData object for a step id
        /// </summary>
        /// <param name="stepId"></param>
        /// <returns></returns>
        public JobStepData GetObjectForStep(int stepId)
        {
            JobStepData jobStep = null;

            if (this.jobSteps != null)
            {
                foreach (JobStepData jsd in this.jobSteps)
                {
                    if (jsd.ID == stepId)
                    {
                        jobStep = jsd;
                        break;
                    }
                }
            }
            return jobStep;
        }

        /// <summary>
        /// Check for any job steps that are unreachable.
        /// Because there are only two paths and we don't care about circular references
        /// we can use a simplified search, rather than a full graph dfs or bfs.
        /// </summary>
        /// <returns>List of unreachable steps, or an empty list if there are none</returns>
        public List<JobStepData> FindUnreachableJobSteps()
        {
            // array used to keep track of whether or not a step is reachable
            bool[] stepReachable = new bool[this.jobSteps.Count];

            // mark the start step as reachable
            if (this.startStep != null && this.startStep.ID > 0 && this.startStep.ID <= this.jobSteps.Count)
            {
                stepReachable[this.startStep.ID - 1] = true;
            }

            // steps indexes start at 1
            foreach (JobStepData step in this.jobSteps)
            {
                // check success actions
                if (step.SuccessAction == StepCompletionAction.GoToNextStep)
                {
                    // if we aren't on the last step mark the next step as valid
                    if (step.ID < this.jobSteps.Count)
                    {
                        stepReachable[step.ID] = true;
                    }
                }
                else if (step.SuccessAction == StepCompletionAction.GoToStep)
                {
                    if (step.SuccessStep != null && step.SuccessStep.ID <= this.jobSteps.Count)
                    {
                        stepReachable[step.SuccessStep.ID - 1] = true;
                    }
                }

                // check failure actions
                if (step.FailureAction == StepCompletionAction.GoToNextStep)
                {
                    // if we aren't on the last step mark the next step as valid
                    if (step.ID < this.jobSteps.Count)
                    {
                        stepReachable[step.ID] = true;
                    }
                }
                else if (step.FailureAction == StepCompletionAction.GoToStep)
                {
                    if (step.FailStep != null && step.FailStep.ID <= this.jobSteps.Count)
                    {
                        stepReachable[step.FailStep.ID - 1] = true;
                    }
                }
            }

            // walk through the array indicating if a step is reachable, and 
            // add any that are not to the list of unreachable steps
            List<JobStepData> unreachableSteps = new List<JobStepData>();
            for (int i = 0; i < stepReachable.Length; i++)
            {
                if (stepReachable[i] == false)
                {
                    unreachableSteps.Add(this.jobSteps[i] as JobStepData);
                }
            }
            return unreachableSteps;
        }
        /// <summary>
        /// Checks to see if the Last steps success completion action will change.
        /// It will if we are editing a job, and the last steps Success Completion
        /// action is GoToNextStep
        /// </summary>
        /// <returns>true if changes will be automatically made to the last step</returns>
        public bool CheckIfLastStepCompletionActionWillChange()
        {
            bool lastStepCompletionActionWillChange = false;
            if(this.jobSteps.Count > 0)
            {
                // get the last step
                JobStepData lastStep = this.jobSteps[this.jobSteps.Count-1] as JobStepData;
                if (lastStep != null && parent.Mode == JobData.ActionMode.Edit && lastStep.SuccessAction == StepCompletionAction.GoToNextStep)
                {
                    lastStepCompletionActionWillChange = true;
                }
            }
            return lastStepCompletionActionWillChange;
        }
        #endregion

        #region private/internal helpers
        /// <summary>
        /// Recalculate the step ids of the contained job steps
        /// </summary>
        internal void RecalculateStepIds()
        {
            for (int i = 0; i < this.jobSteps.Count; i++)
            {
                JobStepData jsd = jobSteps[i] as JobStepData;

                if (jsd != null)
                {
                    jsd.ID = i + 1;
                }
            }
            OnStepOrderChanged(EventArgs.Empty);
        }
        /// <summary>
        /// Delayed loading of database information
        /// </summary>
        private void CheckAndLoadDatabases()
        {
            if (this.databases != null)
            {
                return;
            }
            // load databases collection
            this.databases = new string[this.context.Server.Databases.Count];
            for (int i = 0; i < this.context.Server.Databases.Count; i++)
            {
                this.databases[i] = this.context.Server.Databases[i].Name;
            }
        }
        /// <summary>
        /// fire the StepOrderChanged event 
        /// </summary>
        private void OnStepOrderChanged(EventArgs args)
        {
            if (this.StepOrderChanged != null)
            {
                this.StepOrderChanged(this, args);
            }
        }
        /// <summary>
        ///  SMO job object we are manipulating
        /// </summary>
        internal Job Job
        {
            get
            {
                Job job = null;
                if (this.parent != null)
                {
                    job = parent.Job;
                }
                return job;
            }
        }

        #endregion

        #region data loading
        /// <summary>
        /// Load a job step from a script
        /// </summary>
        /// <param name="script"></param>
        private void LoadFromScript(string script)
        {
            this.jobSteps = new ArrayList();
            JobStepData jsd = new JobStepData(this);
            jsd.Command = script;
            jsd.SubSystem = AgentSubSystem.TransactSql;
            jsd.ID = 1;
            jsd.Name = "1";
            this.jobSteps.Add(jsd);
        }
        /// <summary>
        /// Load job steps from the server
        /// </summary>
        private void LoadData()
        {
            STParameters parameters = new STParameters(this.context.Document);
            string urn = string.Empty;
            string jobIdString = string.Empty;
            parameters.GetParam("urn", ref urn);
            parameters.GetParam("jobid", ref jobIdString);
            
            // save current state of default fields 
            StringCollection originalFields = this.context.Server.GetDefaultInitFields(typeof(JobStep));

            // Get all JobStep properties since the JobStepData class is going to use themn
            this.context.Server.SetDefaultInitFields(typeof(JobStep), true);

            try
            {
                Job job = null;
                // If JobID is passed in look up by jobID
                if (!string.IsNullOrEmpty(jobIdString))
                {
                    job = this.context.Server.JobServer.Jobs.ItemById(Guid.Parse(jobIdString));
                }
                else
                {
                    // or use urn path to query job 
                    job = this.context.Server.GetSmoObject(urn) as Job;
                }

                // load the data
                JobStepCollection steps = job.JobSteps;

                // allocate the array list
                this.jobSteps = new ArrayList(steps.Count);

                for (int i = 0; i < steps.Count; i++)
                {
                    // add them in step id order
                    int ii = 0;
                    for (; ii < this.jobSteps.Count; ii++)
                    {
                        if (steps[i].ID < ((JobStepData)this.jobSteps[ii]).ID)
                        {
                            break;
                        }
                    }
                    this.jobSteps.Insert(ii, new JobStepData(steps[i], this));
                }
                // figure out the start step
                this.startStep = GetObjectForStep(job.StartStepID);

                // fixup all of the jobsteps failure/completion actions
                foreach (JobStepData jobStep in this.jobSteps)
                {
                    jobStep.LoadCompletionActions();
                }
            }
            finally
            {
                // revert to initial default fields for this type
                this.context.Server.SetDefaultInitFields(typeof(JobStep), originalFields);
            }
        }
        
        /// <summary>
        /// Set default values for a new empty job
        /// </summary>
        private void SetDefaults()
        {
            this.jobSteps = new ArrayList();
        }
        #endregion

        #region saving
        /// <summary>
        /// Save changes to all job steps
        /// </summary>
        /// <param name="job">owner job</param>
        /// <returns>True if any changes were saved</returns>
        public bool ApplyChanges(Job job)
        {
            bool changesMade = false;

            if (this.IsReadOnly)
            {
                return false;
            }
            bool scripting = this.context.Server.ConnectionContext.SqlExecutionModes == SqlExecutionModes.CaptureSql;
            // delete all of the deleted steps
            for (int i = deletedJobSteps.Count - 1; i >= 0; i--)
            {
                JobStepData step = this.deletedJobSteps[i] as JobStepData;
                if (step != null)
                {
                    if (step.Created)
                    {
                        step.Delete();
                        changesMade = true;
                    }
                }
                // don't clear the list if we are just scripting the action.
                if (!scripting)
                {
                    deletedJobSteps.RemoveAt(i);
                }
            }

            bool forceRebuildingOfSteps = HasStepOrderChanged;
            // check to see if the step id's have changed. if so we will have to
            // drop and recreate all of the steps
            if (forceRebuildingOfSteps)
            {
                for (int i = this.jobSteps.Count - 1; i >= 0; --i)
                {
                    JobStepData step = this.jobSteps[i] as JobStepData;
                    // only delete steps that exist on the server
                    if (step.Created)
                    {
                        step.Delete();
                        changesMade = true;
                    }
                }
            }

            // update the remaining steps
            foreach (JobStepData step in this.jobSteps)
            {
                if (step.ApplyChanges(job, forceRebuildingOfSteps))
                {
                    changesMade = true;
                }

            }

            // update the start step
            if (StartStep == null && job.StartStepID != 0)
            {
                job.StartStepID = 0;
                changesMade = true;
            }
            else if (parent.Mode == JobData.ActionMode.Create || job.StartStepID != this.startStep.ID)
            {
                job.StartStepID = this.startStep.ID;
                job.Alter();
                changesMade = true;
            }

            return changesMade;
        }
        #endregion
    }
}







