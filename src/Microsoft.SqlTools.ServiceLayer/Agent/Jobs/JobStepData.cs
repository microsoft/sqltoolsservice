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
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobStepData
    {
        #region fields
        // information required to show the job step information in
        // the Steps tab of the job dialog
        #region minmum information
        /// <summary>
        /// Urn of this job
        /// </summary>
        Urn urn;
        /// <summary>
        /// parent object
        /// </summary>
        JobStepsData parent = null;
        /// <summary>
        /// indicates whether this step has already been created on the server
        /// </summary>
        bool alreadyCreated;
        /// <summary>
        ///  indicates if this jobstep will be deleted.
        /// </summary>
        bool deleted = false;

        /// <summary>
        /// Original name of this step
        /// </summary>
        string originalName;
        /// <summary>
        /// Current name
        /// </summary>
        string currentName;
        /// <summary>
        /// Current step id
        /// </summary>
        int id;
        /// <summary>
        /// Original step id
        /// </summary>
        int originalId;
        /// <summary>
        /// Subsystem that will execute this step
        /// </summary>
        AgentSubSystem subSystem;
        /// <summary>
        /// action to take if the step fails
        /// </summary>
        StepCompletionAction failureAction;
        /// <summary>
        /// Action to take if the step succeeds
        /// </summary>
        StepCompletionAction successAction;

        // note we will have either the id or step
        // for the steps to go to on failure
        /// <summary>
        /// step that will be executed on failure
        /// </summary>
        JobStepData failStep = null;
        /// <summary>
        /// step that will be executed on failure
        /// </summary>
        int failStepId;
        /// <summary>
        /// step that will be executed on success
        /// </summary>
        int successStepId;
        /// <summary>
        /// step that will be executed on success
        /// </summary>
        JobStepData successStep = null;
        #endregion

        // information required to edit the job
        #region expanded information
        /// <summary>
        /// JobStep source. We will use this to cache jobstep information to 
        /// be loaded. If this value is null there is either nothing to load
        /// or it has already been loaded
        /// </summary>
        JobStep cachedSource = null;

        /// <summary>
        /// Command to execute
        /// </summary>
        string command;
        /// <summary>
        /// Success code for successful execution of the command
        /// </summary>
        int commandExecutionSuccessCode;
        /// <summary>
        /// Database this step will execute against
        /// </summary>
        string databaseName;
        /// <summary>
        /// database user name this step will execute against
        /// </summary>
        string databaseUserName;
        /// <summary>
        /// Server to execute this step against
        /// </summary>
        string server;
        /// <summary>
        /// Priority of the job
        /// </summary>
        OSRunPriority priority;
        /// <summary>
        /// output file name
        /// </summary>
        string outputFileName;
        /// <summary>
        /// indicates whether to append the output to a file
        /// </summary>
        bool appendToLogFile;
        /// <summary>
        /// indicates whether to append the output to the step history
        /// </summary>
        bool appendToStepHist;
        /// <summary>
        /// indicates whether to log to table
        /// </summary>
        bool writeLogToTable;
        /// <summary>
        /// append the output to the table
        /// </summary>
        bool appendLogToTable;
        /// <summary>
        /// number of rety attempts
        /// </summary>
        int retryAttempts;
        /// <summary>
        /// retrey interval
        /// </summary>
        int retryInterval;
        /// <summary>
        /// proxy name
        /// </summary>
        string proxyName;
        #endregion
        #endregion

        #region public properties
        #region general properties
        /// <summary>
        /// SMO jobstep that this is editing
        /// </summary>
        public JobStep JobStep
        {
            get
            {
                JobStep jobStep = null;
                if (this.Parent.Job != null && this.urn != null)
                {
                    jobStep = this.Parent.Job.Parent.Parent.GetSmoObject(this.urn) as JobStep;
                }
                return jobStep;
            }
        }
        /// <summary>
        /// Server version
        /// </summary>
        public Version Version
        {
            get
            {
                return this.parent.Version;
            }
        }
        /// <summary>
        /// indicates whether the job exists on the server
        /// </summary>
        internal bool Created
        {
            get
            {
                return this.alreadyCreated;
            }
        }
        public bool ToBeDeleted
        {
            get
            {
                return this.deleted;
            }
            set
            {
                this.deleted = value;
            }
        }
        public JobStepsData Parent
        {
            get
            {
                return this.parent;
            }
        }
        public string[] Databases
        {
            get
            {
                return this.parent.Databases;
            }
        }
        public bool StepIdChanged
        {
            get
            {
                // id hasn't changed if we haven't created the step yet
                return (this.originalId == -1) ?
                   true :
                   this.id != this.originalId;
            }
        }
        public bool IsReadOnly
        {
            get { return parent.IsReadOnly; }
        }
        #endregion

        #region Properties for Job Step
        public String Name
        {
            get
            {
                return this.currentName;
            }
            set
            {
                this.currentName = value.Trim();
            }
        }
        public String Command
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.command;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.command = value;
            }
        }
        public int CommandExecutionSuccessCode
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.commandExecutionSuccessCode;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.commandExecutionSuccessCode = value;
            }
        }
        public String DatabaseName
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.databaseName;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.databaseName = value;
            }
        }
        public String DatabaseUserName
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.databaseUserName;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.databaseUserName = value;
            }
        }
        public String Server
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.server;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.server = value;
            }
        }
        public int ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }
        public StepCompletionAction FailureAction
        {
            get
            {
                if (this.failureAction == StepCompletionAction.GoToStep
                   && (this.failStep == null || this.failStep.ToBeDeleted))
                {
                    return StepCompletionAction.QuitWithFailure;
                }
                return this.failureAction;
            }
        }
        public JobStepData FailStep
        {
            get
            {
                if (this.failStep == null || this.failStep.ToBeDeleted)
                {
                    return null;
                }
                return this.failStep;
            }
        }
        public StepCompletionAction SuccessAction
        {
            get
            {
                if (this.successAction == StepCompletionAction.GoToStep
                   && (this.successStep == null || this.successStep.ToBeDeleted))
                {
                    return StepCompletionAction.GoToNextStep;
                }
                return this.successAction;
            }
        }
        public JobStepData SuccessStep
        {
            get
            {
                if (this.successStep == null || this.successStep.ToBeDeleted)
                {
                    return null;
                }
                return this.successStep;
            }
        }
        public OSRunPriority Priority
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.priority;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.priority = value;
            }
        }
        public string OutputFileName
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.outputFileName;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.outputFileName = value;
            }
        }
        public bool AppendToLogFile
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.appendToLogFile;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.appendToLogFile = value;
            }
        }
        public bool AppendToStepHistory
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.appendToStepHist;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.appendToStepHist = value;
            }
        }
        public bool CanLogToTable
        {
            get
            {
                return this.Version.Major >= 9;
            }
        }
        public bool WriteLogToTable
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.writeLogToTable;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.writeLogToTable = value;
            }
        }
        public bool AppendLogToTable
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.appendLogToTable;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.appendLogToTable = value;
            }
        }
        public int RetryAttempts
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.retryAttempts;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.retryAttempts = value;
            }
        }
        public int RetryInterval
        {
            get
            {
                CheckAndLoadExpandedInformation();
                return this.retryInterval;
            }
            set
            {
                CheckAndLoadExpandedInformation();
                this.retryInterval = value;
            }
        }

        public AgentSubSystem SubSystem
        {
            get
            {
                return this.subSystem;
            }
            set
            {
                this.subSystem = value;
            }
        }

        public int StepCount
        {
            get
            {
                return this.Parent.Steps.Count;
            }
        }

        public ArrayList Steps
        {
            get
            {
                return this.Parent.Steps;
            }
        }

        public string ProxyName
        {
            get
            {
                CheckAndLoadExpandedInformation();
                if (this.proxyName.Length == 0)
                {
                    // Return sysadmin account name when proxy
                    // name is not set, so we match the setter logic
                    return AgentProxyAccount.SysadminAccount;
                }
                else
                {
                    return this.proxyName;
                }
            }
            set
            {
                CheckAndLoadExpandedInformation();
                if (value == AgentProxyAccount.SysadminAccount)
                {
                    // Sysadmin is just a special name used
                    // to reset proxy account 
                    this.proxyName = string.Empty;
                }
                else
                {
                    this.proxyName = value;
                }
            }
        }
        #endregion
        #endregion

        #region construction
        // brand new job step
        public JobStepData()
        {
            SetDefaults();
        }
        // new job step with context
        public JobStepData(JobStepsData parent)
        {
            this.parent = parent;
            SetDefaults();
        }
        // existing job step
        public JobStepData(JobStep source, JobStepsData parent)
        {
            this.parent = parent;
            LoadData(source);
        }
        // copy constructor
        public JobStepData(JobStepData source)
        {
            this.originalName = source.originalName;
            this.currentName = source.currentName;
            this.alreadyCreated = source.alreadyCreated;
            this.deleted = source.deleted;
            this.command = source.command;
            this.commandExecutionSuccessCode = source.commandExecutionSuccessCode;
            this.databaseName = source.databaseName;
            this.databaseUserName = source.databaseUserName;
            this.server = source.server;
            this.id = source.id;
            this.originalId = source.originalId;
            this.failureAction = source.failureAction;
            this.failStep = source.failStep;
            this.failStepId = source.failStepId;
            this.successAction = source.successAction;
            this.successStep = source.successStep;
            this.successStepId = source.successStepId;
            this.priority = source.priority;
            this.outputFileName = source.outputFileName;
            this.appendToLogFile = source.appendToLogFile;
            this.appendToStepHist = source.appendToStepHist;
            this.writeLogToTable = source.writeLogToTable;
            this.appendLogToTable = source.appendLogToTable;
            this.retryAttempts = source.retryAttempts;
            this.retryInterval = source.retryInterval;
            this.subSystem = source.subSystem;
            this.proxyName = source.proxyName;
            this.urn = source.urn;
            this.parent = source.parent;
        }
        #endregion

        #region overrrides
        /// <summary>
        /// Generate a string for the object that can be shown in the start step combo
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}",
               this.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
               this.Name);
        }
        #endregion

        #region non public properties
        private bool HaveLoadedExpandedData
        {
            get
            {
                return this.cachedSource == null;
            }
        }
        #endregion

        #region data loading
        /// <summary>
        /// load data from and existing step
        /// </summary>
        /// <param name="source"></param>
        private void LoadData(JobStep source)
        {
            this.alreadyCreated = true;
            currentName = originalName = source.Name;
            this.urn = source.Urn;
            this.successAction = source.OnSuccessAction;
            this.failureAction = source.OnFailAction;
            this.originalId = this.id = source.ID;
            this.subSystem = source.SubSystem;
            this.failStepId = source.OnFailStep;
            this.successStepId = source.OnSuccessStep;

            this.cachedSource = source;
        }
        /// <summary>
        /// Load all data nessesary to edit a job
        /// </summary>
        private void CheckAndLoadExpandedInformation()
        {
            if (HaveLoadedExpandedData)
            {
                return;
            }
            JobStep source = this.cachedSource;

            this.command = source.Command;
            this.commandExecutionSuccessCode = source.CommandExecutionSuccessCode;
            this.databaseName = source.DatabaseName;
            this.databaseUserName = source.DatabaseUserName;
            this.server = source.Server;

            this.priority = source.OSRunPriority;
            this.outputFileName = source.OutputFileName;
            this.appendToLogFile = ((source.JobStepFlags & JobStepFlags.AppendToLogFile) == JobStepFlags.AppendToLogFile);
            this.appendToStepHist = ((source.JobStepFlags & JobStepFlags.AppendToJobHistory) == JobStepFlags.AppendToJobHistory);
            if (this.SubSystem == AgentSubSystem.CmdExec || this.SubSystem == AgentSubSystem.Ssis || this.subSystem == AgentSubSystem.PowerShell)
            {
                // For cmdexec, PowerShell and ssis the flag is overwritten by the 
                // AppendAllCmdExecOutputToJobHistory. Those two flags are 
                // equivalent, but AppendToJobHistory is only recognized by 
                // the t-sql subsystem
                this.appendToStepHist = ((source.JobStepFlags & JobStepFlags.AppendAllCmdExecOutputToJobHistory) == JobStepFlags.AppendAllCmdExecOutputToJobHistory);
            }

            this.writeLogToTable = ((source.JobStepFlags & (JobStepFlags)24) > 0);
            this.appendLogToTable = ((source.JobStepFlags & (JobStepFlags)16) == (JobStepFlags)16);
            this.retryAttempts = source.RetryAttempts;
            this.retryInterval = source.RetryInterval;

            // Proxy name works only on Yukon and later
            if (source.Parent.Parent.Parent.ConnectionContext.ServerVersion.Major >= 9)
            {
                this.proxyName = source.ProxyName;
            }
            else
            {
                this.proxyName = string.Empty;
            }

            this.cachedSource = null;
        }
        /// <summary>
        /// Set defaults for a new empty job
        /// </summary>
        private void SetDefaults()
        {
            this.alreadyCreated = false;
            this.currentName = originalName = string.Empty;
            this.command = string.Empty;
            this.commandExecutionSuccessCode = 0;
            this.databaseName = "master";
            this.databaseUserName = string.Empty;
            this.server = string.Empty;
            this.originalId = this.id = -1;
            this.failureAction = StepCompletionAction.QuitWithFailure;
            this.failStep = null;
            this.failStepId = -1;
            this.successAction = StepCompletionAction.GoToNextStep;
            this.successStep = null;
            this.successStepId = -1;
            this.priority = OSRunPriority.Normal;
            this.outputFileName = String.Empty;
            this.appendToLogFile = false;
            this.appendToStepHist = false;
            this.writeLogToTable = false;
            this.appendLogToTable = false;
            this.retryAttempts = 0;
            this.retryInterval = 0;
            this.subSystem = AgentSubSystem.TransactSql;
            this.proxyName = string.Empty;
            this.urn = null;
        }
        /// <summary>
        /// Load the completion actions for the step
        /// </summary>
        internal void LoadCompletionActions()
        {
            if (this.successAction == StepCompletionAction.GoToStep)
            {
                this.successStep = this.parent.GetObjectForStep(this.successStepId);
            }
            if (this.failureAction == StepCompletionAction.GoToStep)
            {
                this.failStep = this.parent.GetObjectForStep(this.failStepId);
            }
        }
        #endregion

        #region saving
        /// <summary>
        /// Save changes to the job step
        /// </summary>
        /// <param name="job">owner job</param>
        /// <returns>True if any changes were saved</returns>
        public bool ApplyChanges(Job job)
        {
            return ApplyChanges(job, false);
        }
        /// <summary>
        /// Save changes to the job step
        /// </summary>
        /// <param name="job">owner job</param>
        /// <param name="forceCreate">indicates if the job should be dropped and recreated</param>
        /// <returns>True if any changes were saved</returns>
        public bool ApplyChanges(Job job, bool forceCreate)
        {
            if (!HaveLoadedExpandedData && forceCreate)
            {
                CheckAndLoadExpandedInformation();
            }
            if (this.IsReadOnly || !this.HaveLoadedExpandedData)
            {
                return false;
            }

            bool changesMade = false;

            JobStep jobStep = null;
            bool scripting = job.Parent.Parent.ConnectionContext.SqlExecutionModes == SqlExecutionModes.CaptureSql;

            try
            {
                bool creating = !this.alreadyCreated || forceCreate;
                // creating a new jobstep

                if (creating)
                {
                    jobStep = new JobStep(job, this.Name);
                    this.originalName = this.currentName;
                }
                else
                {
                    jobStep = parent.Job.JobSteps[this.originalName];
                    if (jobStep == null)
                    {
                        throw new InvalidOperationException();
                    }
                    if (jobStep.ID != this.id)
                    {
                        // delete
                        jobStep.Drop();
                        // recreate
                        this.alreadyCreated = false;
                        return ApplyChanges(job);
                    }
                }

                if (creating)
                {
                    jobStep.ID = this.id;
                    jobStep.JobStepFlags = JobStepFlags.None;
                    changesMade = true;
                }
                if (creating || jobStep.Command != this.Command)
                {
                    jobStep.Command = this.Command;
                    changesMade = true;
                }
                if (creating || jobStep.CommandExecutionSuccessCode != this.CommandExecutionSuccessCode)
                {
                    jobStep.CommandExecutionSuccessCode = this.CommandExecutionSuccessCode;
                    changesMade = true;
                }
                if (creating || jobStep.DatabaseName != this.DatabaseName)
                {
                    jobStep.DatabaseName = this.DatabaseName;
                    changesMade = true;
                }
                if (creating || jobStep.DatabaseUserName != this.DatabaseUserName)
                {
                    jobStep.DatabaseUserName = this.DatabaseUserName;
                    changesMade = true;
                }
                if (creating || jobStep.Server != this.Server)
                {
                    jobStep.Server = this.Server;
                    changesMade = true;
                }
                if (creating || jobStep.OnFailAction != this.FailureAction)
                {
                    jobStep.OnFailAction = this.FailureAction;
                    changesMade = true;
                }
                if (jobStep.OnFailAction == StepCompletionAction.GoToStep &&
                                (creating || (this.FailStep != null
                                              && jobStep.OnFailStep != this.FailStep.ID)))
                {
                    jobStep.OnFailStep = this.FailStep.ID;
                    changesMade = true;
                }
                if (creating || jobStep.OnSuccessAction != this.SuccessAction)
                {
                    jobStep.OnSuccessAction = this.SuccessAction;
                    changesMade = true;
                }
                // if this is the last step, make sure that it does not have a
                // success action of next step. Don't store this as the user could add
                // more steps later
                if (this.ID == Parent.Steps.Count && jobStep.OnSuccessAction == StepCompletionAction.GoToNextStep)
                {
                    jobStep.OnSuccessAction = StepCompletionAction.QuitWithSuccess;
                    changesMade = true;
                }
                if (jobStep.OnSuccessAction == StepCompletionAction.GoToStep
                                && (creating || (this.SuccessStep != null
                                                  && jobStep.OnSuccessStep != this.SuccessStep.ID)))
                {
                    jobStep.OnSuccessStep = this.SuccessStep.ID;
                    changesMade = true;
                }
                if (creating || jobStep.OSRunPriority != this.Priority)
                {
                    jobStep.OSRunPriority = this.Priority;
                    changesMade = true;
                }
                if (creating || jobStep.OutputFileName != this.OutputFileName)
                {
                    jobStep.OutputFileName = this.OutputFileName;
                    changesMade = true;
                }

                JobStepFlags jobStepFlags = JobStepFlags.None;
                if (this.AppendToLogFile)
                {
                    jobStepFlags |= JobStepFlags.AppendToLogFile;
                    changesMade = true;
                }

                JobStepFlags historyOutputMask = JobStepFlags.AppendToJobHistory;
                if (this.SubSystem == AgentSubSystem.CmdExec ||
                    this.SubSystem == AgentSubSystem.Ssis || 
                    this.subSystem == AgentSubSystem.PowerShell)
                {
                    // for cmdexec, PowerShell and ssis subsystems, the history output flag 
                    // is different, but it's the same UI checkbox.
                    historyOutputMask = JobStepFlags.AppendAllCmdExecOutputToJobHistory;
                }

                if (this.AppendToStepHistory)
                {
                    jobStepFlags |= historyOutputMask;
                    changesMade = true;
                }
                if (this.CanLogToTable && this.WriteLogToTable)
                {
                    if (this.AppendLogToTable)
                    {
                        jobStepFlags |= (JobStepFlags)16;
                    }
                    else
                    {
                        jobStepFlags |= (JobStepFlags)8;
                    }
                }

                // if this is a Cmd subsystem step, then don't lose
                // the ProvideStopProcessEvent flag.
                if (this.SubSystem == AgentSubSystem.CmdExec)
                {
                    if (0 != (jobStep.JobStepFlags & JobStepFlags.ProvideStopProcessEvent))
                    {
                        jobStepFlags |= JobStepFlags.ProvideStopProcessEvent;
                    }
                }

                if (creating || jobStep.JobStepFlags != jobStepFlags)
                {
                    jobStep.JobStepFlags = jobStepFlags;
                    changesMade = true;
                }
                if (creating || jobStep.RetryAttempts != this.RetryAttempts)
                {
                    jobStep.RetryAttempts = this.RetryAttempts;
                    changesMade = true;
                }
                if (creating || jobStep.RetryInterval != this.RetryInterval)
                {
                    jobStep.RetryInterval = this.RetryInterval;
                    changesMade = true;
                }
                if (creating || jobStep.SubSystem != this.SubSystem)
                {
                    jobStep.SubSystem = this.SubSystem;
                    changesMade = true;
                }
                if (job.Parent.Parent.ConnectionContext.ServerVersion.Major >= 9 &&
                    (creating
                    || jobStep.ProxyName != this.proxyName))
                {
                    jobStep.ProxyName = this.proxyName;
                    changesMade = true;
                }

                if (creating)
                {
                    jobStep.Create();
                    if (!scripting)
                    {
                        this.urn = jobStep.Urn;
                        this.originalId = this.id;
                        this.alreadyCreated = true;
                    }
                }
                else
                {
                    jobStep.Alter();
                    // handle rename
                    if (this.originalName != this.currentName)
                    {
                        jobStep.Rename(currentName);
                        this.originalName = this.currentName;
                        this.urn = jobStep.Urn;
                        changesMade = true;
                    }
                }

            }
            catch (Exception e)
            {
                if (!scripting && jobStep != null && jobStep.State != SMO.SqlSmoState.Existing)
                {
                    if (job.JobSteps.Contains(this.Name))
                    {
                        job.JobSteps.Remove(this.Name);
                    }
                }

                throw;
            }
            return changesMade;
        }

        public void Delete()
        {
            JobStep jobStep = parent.Job.JobSteps[this.originalName];
            if (jobStep != null)
            {
                jobStep.Drop();
            }
        }

        public void StepSuccessAction(StepCompletionAction action, JobStepData step)
        {
            // parameter check. must supply step if the action is GoToStep
            if (action == StepCompletionAction.GoToStep && step == null)
            {
                throw new InvalidArgumentException();
            }
            // don't supply step if it's an action other than GotoStep
            else if (action != StepCompletionAction.GoToStep && step != null)
            {
                throw new InvalidArgumentException();
            }
            else if (step == this)
            {
                throw new InvalidArgumentException("step");
            }

            this.successAction = action;
            this.successStep = step;
        }
        public void StepFailureAction(StepCompletionAction action, JobStepData step)
        {
            // parameter check. must supply step if the action is GoToStep
            if (action == StepCompletionAction.GoToStep && step == null)
            {
                throw new InvalidOperationException();
            }
            // don't supply step if it's an action other than GotoStep
            if (action != StepCompletionAction.GoToStep && step != null)
            {
                throw new InvalidOperationException();
            }
            else if (step == this)
            {
                throw new InvalidArgumentException("step");
            }

            this.failureAction = action;
            this.failStep = step;
        }


        #endregion
    }
}








