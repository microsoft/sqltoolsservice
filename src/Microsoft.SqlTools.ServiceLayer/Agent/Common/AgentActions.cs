//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Xml;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{

    #region AgentAction class

    /// <summary>
    /// Main class all af the "immediate" agent actions derive from. These actions execute immediately
    /// are not scriptable. We use the progress reporting dialog to give the user feedback on progress
    /// etc.
    /// </summary>
    internal abstract class AgentAction
    {
        #region private members

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected Microsoft.SqlServer.Management.Smo.Server smoServer = null;        
        protected IManagedConnection managedConnection;
        protected Urn[] urnParameters;
        protected STParameters param = null;
        protected ProgressItemCollection actions = new ProgressItemCollection();

        #endregion

        protected object ActionObject;

        #region construction

        public AgentAction(XmlDocument document, IServiceProvider source)
            : this(document, source, null)
        {
        }

        public AgentAction(XmlDocument document, IServiceProvider source, object actionObject)
        {
            // parameter check
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (actionObject != null)
            {
                this.ActionObject = actionObject;
            }

            // get the managed connection
            managedConnection = source.GetService(typeof (IManagedConnection)) as IManagedConnection;

            // get the connection
            SqlOlapConnectionInfoBase ci = managedConnection.Connection;
            // get the server connection
            ServerConnection serverConnection =
                ((SqlConnectionInfoWithConnection) managedConnection.Connection).ServerConnection;
                
            smoServer = new Microsoft.SqlServer.Management.Smo.Server(serverConnection);

            // get the list or urn's that have been passed in
            param = new STParameters(document);
            StringCollection urnStrings = new StringCollection();

            // get a list of urns that have been passed in.
            param.GetParam("urn", urnStrings);

            // store the Urn's as real Urns
            urnParameters = new Urn[urnStrings.Count];
            for (int i = 0; i < urnStrings.Count; i++)
            {
                urnParameters[i] = new Urn(urnStrings[i]);
            }
        }

        protected void OnLoad(EventArgs e)
        {
            // ask derived classes to build a list of actions to be 
            // performed. Do not remove this call from OnLoad method!			
            GenerateActions();
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            if (this.managedConnection != null)
            {
                try
                {
                    if (disposing)
                    {
                        this.managedConnection.Close();
                    }
                    this.managedConnection = null;
                }
                catch (Exception)
                {                   
                }
            }
        }

        #endregion

        #region abstract methods

        /// <summary>
        /// Generate the actions the dialog will perform. Derived classes should add
        /// IAction based actions to the actions collection.
        /// </summary>
        protected abstract void GenerateActions();

        #endregion
    }

    #endregion

    #region Enable Alerts

    /// <summary>
    /// Enables one or more alerts.
    /// </summary>
    internal class EnableAgentAlerts : AgentAction
    {
        public EnableAgentAlerts(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {
        }

        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Alert alert = this.smoServer.GetSmoObject(urnParameters[i]) as Alert;

                    // check that the urn really points to an alert
                    this.actions.AddAction(new EnableAlertAction(alert));
                }
            }
        }

        /// <summary>
        /// Performs the actual enabling
        /// </summary>
        internal class EnableAlertAction : IProgressItem
        {
            private Alert alert;

            public EnableAlertAction(Alert alert)
            {
                if (alert == null)
                {
                    throw new ArgumentNullException("alert");
                }

                this.alert = alert;
            }

            /// <summary>
            /// Generate a user friendly description of this task.Used in the description
            /// of the progress dialog.
            /// </summary>
            /// <returns>Description of the aler</returns>
            public override string ToString()
            {
                if (this.alert == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.EnableAlertDescription(this.alert.Name)";
                }
            }


            /// <summary>
            /// Enable the alert
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                actions.Progress.AddActionInfoString(index, "AgentActionSR.EnablingAlert(this.alert.Name)");

                this.alert.IsEnabled = true;
                this.alert.Alter();

                // done
                actions.Progress.AddActionInfoString(index, "AgentActionSR.EnabledAlert(this.alert.Name)");
                actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                return ProgressStatus.Success;
            }
        }
    }

    #endregion

    #region Disable Alerts

    /// <summary>
    /// Disable one or more alerts
    /// </summary>
    internal class DisableAgentAlerts : AgentAction
    {
        public DisableAgentAlerts(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {         
        }

        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Alert alert = this.smoServer.GetSmoObject(urnParameters[i]) as Alert;

                    // check that the urn really points to an alert
                    this.actions.AddAction(new DisableAlertAction(alert));
                }
            }
        }

        /// <summary>
        /// Actually disable the alert
        /// </summary>
        internal class DisableAlertAction : IProgressItem
        {
            private Alert alert;

            public DisableAlertAction(Alert alert)
            {
                if (alert == null)
                {
                    throw new ArgumentNullException("alert");
                }

                this.alert = alert;
            }

            public override string ToString()
            {
                if (this.alert == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.DisableAlertDescription(this.alert.Name)";
                }
            }

            /// <summary>
            /// Disable the alert
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                actions.Progress.AddActionInfoString(index, "AgentActionSR.DisablingAlert(this.alert.Name)");

                this.alert.IsEnabled = false;
                this.alert.Alter();

                actions.Progress.AddActionInfoString(index, "AgentActionSR.DisabledAlert(this.alert.Name)");

                /// done
                actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                return ProgressStatus.Success;
            }
        }
    }

    #endregion

    #region JobAction

    internal class JobAction : AgentAction
    {
        public JobAction(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {

        }

        protected override void GenerateActions()
        {
            return;
        }

        /// <summary>
        /// Initialize context for actions on Jobs
        /// Job Activity monitor can call with list if jobids.
        /// All other existing callers may call with list of urns 
        /// To support above 2 scenarios,  this method in base class initializes urnParameters if it was not initialized by 
        /// AgentAction class's constructor
        /// </summary>
        protected void InitializeContext()
        {
            // If Urn parameters were not initialized it is possible that 
            // jobids were passed in by caller instead of list of urns
            if (null == urnParameters || urnParameters.Length == 0)
            {
                StringCollection jobIdStrings = new StringCollection();

                // get list of job ids that were passed in
                param.GetParam("jobid", jobIdStrings);

                urnParameters = new Urn[jobIdStrings.Count];
                int index = 0;
                if (jobIdStrings.Count > 0)
                {
                    foreach (string jobIdString in jobIdStrings)
                    {
                        Job job = smoServer.JobServer.Jobs.ItemById(Guid.Parse(jobIdString));
                        urnParameters[index++] = new Urn(job.Urn);
                    }
                }
            }
        }
    }

    #endregion

    #region Enable Jobs

    internal class EnableAgentJobs : JobAction
    {
        public EnableAgentJobs(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {
        }

        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                InitializeContext();
                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Job job = this.smoServer.GetSmoObject(urnParameters[i]) as Job;

                    // check that the urn really points to a Job
                    this.actions.AddAction(new EnableJobAction(job));
                }
            }
        }

        // class that actually enables the job
        internal class EnableJobAction : IProgressItem
        {
            private Job job;

            public EnableJobAction(Job job)
            {
                if (job == null)
                {
                    throw new ArgumentNullException("job");
                }

                this.job = job;
            }

            /// <summary>
            /// Generate user friendly description of the action. This is displayed in the
            /// progress dialog.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (this.job == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.EnableJobDescription(this.job.Name)";
                }
            }

            /// <summary>
            /// Enable the Job
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                actions.Progress.AddActionInfoString(index, "AgentActionSR.EnablingJob(this.job.Name)");

                this.job.IsEnabled = true;
                this.job.Alter();


                // done
                actions.Progress.AddActionInfoString(index, "AgentActionSR.EnabledJob(this.job.Name)");
                actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                return ProgressStatus.Success;
            }
        }
    }

    #endregion

    #region Disable Jobs

    /// <summary>
    /// Disable a job
    /// </summary>
    internal class DisableAgentJobs : JobAction
    {
        public DisableAgentJobs(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {
        }

        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                InitializeContext();
                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Job job = this.smoServer.GetSmoObject(urnParameters[i]) as Job;

                    // check that the urn really points to an job
                    this.actions.AddAction(new DisableJobAction(job));
                }
            }
        }

        internal class DisableJobAction : IProgressItem
        {
            private Job job;

            public DisableJobAction(Job job)
            {
                if (job == null)
                {
                    throw new ArgumentNullException("job");
                }

                this.job = job;
            }

            public override string ToString()
            {
                if (this.job == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.DisableJobDescription(this.job.Name)";
                }
            }

            /// <summary>
            /// Disable the Job
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                actions.Progress.AddActionInfoString(index, "AgentActionSR.DisablingJob(this.job.Name)");

                this.job.IsEnabled = false;
                this.job.Alter();

                // done
                actions.Progress.AddActionInfoString(index, "AgentActionSR.DisabledJob(this.job.Name)");
                actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                return ProgressStatus.Success;
            }
        }
    }

    #endregion

    #region Start Job

    /// <summary>
    /// Start an agent job. If the jobs have multiple steps we will show a dialog that asks
    /// which step the job should be started on.
    /// </summary>
    internal class StartAgentJobs : JobAction
    {
        public StartAgentJobs(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {
            this.actions.CloseOnUserCancel = true;
            this.actions.QuitOnError = true;
        }

        /// <summary>
        /// The method is generates list of actions and it is gets called from the OnLaod of base Form method
        /// </summary>
        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                InitializeContext();

                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Job job = this.smoServer.GetSmoObject(urnParameters[i]) as Job;
                    string selectedStep = null;
                    DataTable dtSteps = GetJobDataSteps(job);
                    if (dtSteps == null || dtSteps.Rows == null)
                    {
                        continue;
                    }

                    if (dtSteps.Rows.Count > 1) //check if job is multi step job
                    {
                        // selectedStep = ShowStepDialog(job, dtSteps);
                        if (selectedStep == null) //check if the job was canceled
                        {
                            continue;
                        }
                    }

                    //Copy the LastRunTime of the job into prevRunTime before the job started.                         
                    DateTime prevRunTime = job.LastRunDate;
                    this.actions.AddAction(new StartJobAction(job, selectedStep));
                    this.actions.AddAction(new WaitForJobToFinishAction(job, prevRunTime));
                }
            }
        }

        /// <summary>
        /// Returns list of steps of the given job
        /// </summary>
        /// <param name="job"></param>
        /// <returns>returns list of steps</returns>
        private DataTable GetJobDataSteps(Job job)
        {
            if (job == null || job.Parent == null || job.Parent.Parent == null)
            {
                return null;
            }

            // perform an enumerator query to get the steps. We could use the
            // SMO step object but this is too inefficient as it generates a batch 
            // per step.
            Request request = new Request();
            request.Fields = new string[] {"Name", "ID", "SubSystem"};
            request.Urn = job.Urn + "/Step";
            request.OrderByList = new OrderBy[] {new OrderBy("ID", OrderBy.Direction.Asc)};

            Enumerator en = new Enumerator();
            return en.Process(job.Parent.Parent.ConnectionContext, request);
        }

        /// <summary>
        /// This class implements the feature described in project tracking bug 37519.
        /// The point is to poll the server for the status of the job to give the user
        /// some indication of whether the job succeeded or failed.  Polls every 3 seconds.
        /// </summary>
        internal class WaitForJobToFinishAction : IProgressItem
        {
            private Job job;
            private DateTime prevRunTime;
            private ManualResetEvent abortEvent;
            private const int ServerPollingInterval = 3000;

            public WaitForJobToFinishAction(Job job, DateTime prevRunTime)
            {
                this.job = job;
                this.prevRunTime = prevRunTime;
                this.abortEvent = new ManualResetEvent(false); //initial set to busy
            }

            /// <summary>
            /// Prevent default constructor
            /// </summary>
            private WaitForJobToFinishAction()
            {
            }

            /// <summary>
            /// generates a friendly description of this step. Used by the progress dialog
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (this.job == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.ExecuteJob(job.Name)";
                }
            }

            /// <summary>
            /// This method triggers abort event for the action thread
            /// </summary>
            public void Abort()
            {
                this.abortEvent.Set();
            }

            /// <summary>
            /// Perform the action for this class
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">array index of this particular action</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                ProgressStatus status = ProgressStatus.Error;

                bool jobFinished = false;

                JobServer jobServer = job.Parent;
                JobCategory category = jobServer.JobCategories[job.Category];

                if (category.CategoryType == CategoryType.MultiServerJob)
                {
                    actions.Progress.UpdateActionDescription(index, "AgentActionSR.RequestPostedToTargetServers");
                    actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                    return ProgressStatus.Success;
                }

                status = ProgressStatus.Aborted;

                // now wait for job to finish...
                while (!this.abortEvent.WaitOne(WaitForJobToFinishAction.ServerPollingInterval))
                {
                    if (actions.Progress.IsAborted)
                    {
                        break;
                    }

                    this.job.Refresh();
                    // If this job hasn't started yet then don't check for its status
                    if (this.prevRunTime != job.LastRunDate)
                    {
                        switch (this.job.CurrentRunStatus)
                        {
                            case JobExecutionStatus.Idle:
                                actions.Progress.UpdateActionProgress(index, 100);

                                // see if the job succeeded.
                                if (this.job.LastRunOutcome == CompletionResult.Failed)
                                {
                                    actions.Progress.UpdateActionStatus(index, ProgressStatus.Error);
                                    actions.Progress.AddActionException(index,
                                        new Exception("AgentActionSR.JobFailed(job.Name)"));
                                    status = ProgressStatus.Error;
                                }
                                else
                                {
                                    actions.Progress.UpdateActionDescription(index, "AgentActionSR.ExecuteJob(job.Name)");
                                    actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                                    status = ProgressStatus.Success;
                                }

                                jobFinished = true;
                                break;

                            case JobExecutionStatus.Suspended:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.Suspended");
                                break;

                            case JobExecutionStatus.BetweenRetries:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.BetweenRetries");
                                break;

                            case JobExecutionStatus.Executing:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.Executing");
                                break;

                            case JobExecutionStatus.PerformingCompletionAction:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.PerformingCompletionAction");
                                break;

                            case JobExecutionStatus.WaitingForStepToFinish:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.WaitingForStepToFinish");
                                break;

                            case JobExecutionStatus.WaitingForWorkerThread:
                                actions.Progress.UpdateActionProgress(index, "AgentActionSR.WaitingForWorkerThread");
                                break;

                            default:
                                // unknown JobExecutionStatus, keep waiting.
                                System.Diagnostics.Debug.Assert(false,
                                    "Unknown JobExecutionStatus found while waiting for job execution to finish");
                                break;
                        }
                    }

                    if (jobFinished)
                    {
                        break;
                    }

                    actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                }

                return status;
            }
        }

        /// <summary>
        /// starts a job
        /// </summary>
        internal class StartJobAction : IProgressItem
        {
            private Job job;
            //Represent selected job step if any
            private string currentJobStep;

            public StartJobAction(Job job, string jobStep)
            {
                // need a job. The delegate can be null
                if (job == null)
                {
                    throw new ArgumentNullException("job");
                }

                this.job = job;
                this.currentJobStep = jobStep;
            }

            /// <summary>
            /// generates a friendly description of this step. Used by the progress dialog
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (this.job == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.StartJobDescription(this.job.Name)";
                }
            }


            /// <summary>
            /// Start the Job
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                ProgressStatus status = ProgressStatus.Success;
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);

                // perform an enumerator query to get the steps. We could use the
                // SMO step object but this is too inefficient as it generates a batch 
                // per step.
                Request request = new Request();

                request.Fields = new string[] {"Name", "ID", "SubSystem"};
                request.Urn = this.job.Urn + "/Step";
                request.OrderByList = new OrderBy[] {new OrderBy("ID", OrderBy.Direction.Asc)};

                if (this.currentJobStep != null)
                {
                    actions.Progress.AddActionInfoString(index,
                        "AgentActionSR.StartJobWithStep(this.job.Name, this.currentJobStep)");
                    this.job.Start(this.currentJobStep);
                }
                else
                {
                    actions.Progress.AddActionInfoString(index, "AgentActionSR.StartingJob(this.job.Name)");
                    this.job.Start();
                }

                // done
                actions.Progress.UpdateActionStatus(index, status);
                return status;
            }
        }
    }

    #endregion

    #region Stop Job

    /// <summary>
    /// stop a job
    /// </summary>
    internal class StopAgentJobs : JobAction
    {
        public StopAgentJobs(XmlDocument document, IServiceProvider source)
            : base(document, source)
        {
        }

        protected override void GenerateActions()
        {
            if (this.smoServer != null)
            {
                InitializeContext();

                for (int i = 0; i < this.urnParameters.Length; i++)
                {
                    Job job = this.smoServer.GetSmoObject(urnParameters[i]) as Job;

                    // check that the urn really points to an job
                    this.actions.AddAction(new StopJobAction(job));
                }
            }
        }

        /// <summary>
        /// class that actually stops a running job
        /// </summary>
        internal class StopJobAction : IProgressItem
        {
            private Job job;

            public StopJobAction(Job job)
            {
                if (job == null)
                {
                    throw new ArgumentNullException("job");
                }

                this.job = job;
            }

            /// <summary>
            /// Generate a user friendly description of this task. Used in the description
            /// of the progress dialog.
            /// </summary>
            /// <returns>Description of the action</returns>
            public override string ToString()
            {
                if (this.job == null)
                {
                    return base.ToString();
                }
                else
                {
                    return "AgentActionSR.StopJobDescription(this.job.Name)";
                }
            }

            /// <summary>
            /// Stop the Job
            /// </summary>
            /// <param name="actions">Actions collection</param>
            /// <param name="index">this actions index into the actions collection</param>
            /// <returns></returns>
            public ProgressStatus DoAction(ProgressItemCollection actions, int index)
            {
                //parameter check
                if (actions == null)
                {
                    throw new ArgumentNullException("actions");
                }

                // in progress
                actions.Progress.UpdateActionStatus(index, ProgressStatus.InProgress);
                actions.Progress.AddActionInfoString(index, "AgentActionSR.StoppingJob(this.job.Name)");
                job.Stop();

                // done
                actions.Progress.AddActionInfoString(index, "AgentActionSR.StoppedJob(this.job.Name)");
                actions.Progress.UpdateActionStatus(index, ProgressStatus.Success);
                return ProgressStatus.Success;
            }
        }
    }

    #endregion
}
