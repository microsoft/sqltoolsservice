//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepPropertySheet.
    /// </summary>
    internal class JobStepPropertySheet : SqlMgmtTreeViewControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        JobStepData data = null;
        
        // accept edits to job step dialog and on OK commit the changes
        // Caller: logviewer -> can launch job step dialog and user can make changes to this specific step
        string acceptedits = string.Empty;
        bool commitEditsToJobStep = false;

        public JobStepPropertySheet()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call
        }

        public JobStepPropertySheet(CDataContainer dataContainer, JobStepData data, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            CUtils utils = new CUtils();
            this.Icon = utils.LoadIcon("Job_steps.ico");

            this.DataContainer = dataContainer;
            this.data = data;
            Init(serviceProvider);
        }

        public void Init(IServiceProvider serviceProvider)
        {
            STParameters parameters = new STParameters(this.DataContainer.Document);
            parameters.GetParam("acceptedits", ref acceptedits);
            commitEditsToJobStep = (acceptedits == "true") ? true : false;

            PanelTreeNode node;
            PanelTreeNode auxNode;
            JobPropertiesAdvanced advanced = new JobPropertiesAdvanced(this.DataContainer, this.data, serviceProvider);
            JobStepProperties general = new JobStepProperties(this.DataContainer, this.data, advanced, serviceProvider);

            AddView(general);
            AddView(advanced);

            node = new PanelTreeNode();
            node.Text = JobSR.Job;
            node.Type = eNodeType.Folder;
            node.Tag = 0;

            auxNode = new PanelTreeNode();
            auxNode.Text = JobSR.General;
            auxNode.Tag = 1;
            auxNode.Type = eNodeType.Item;
            node.Nodes.Add(auxNode);
            SelectNode(auxNode);

            auxNode = new PanelTreeNode();
            auxNode.Text = JobSR.Advanced;
            auxNode.Tag = 2;
            auxNode.Type = eNodeType.Item;

            node.Nodes.Add(auxNode);
            AddNode(node);

            // creating
            if(this.data.Name.Length == 0)
            {
                this.Text = JobSR.NewJobStep;
            }
            else
            {
                this.Text = JobSR.EditJobStep(this.data.Name);
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion

        private DialogResult dialogResult = DialogResult.Cancel;
        public DialogResult DialogResult
        {
            get { return this.dialogResult; }
        }

        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);

            //Make sure the job step name is not blank.
            if(this.data.Name == null || this.data.Name.Length == 0)
            {
                throw new Exception(SRError.JobStepNameCannotBeBlank);
            }

            //Check to make sure that the user has not entered a job step name that already exists.
            for(int stepIndex = 0; stepIndex < this.data.Parent.Steps.Count; stepIndex++)
            {
                // don't compare if the id's are the same.
                if(data.ID != ((JobStepData)this.data.Parent.Steps[stepIndex]).ID && data.Name == ((JobStepData)this.data.Parent.Steps[stepIndex]).Name)
                {
                    //Throw an error if the job step name already exists
                    throw new Exception(JobSR.JobStepNameAlreadyExists(this.data.Name));
                }
            }

            // simulate IDOK
            if(runType == RunType.RunNowAndExit)
            {
                if (commitEditsToJobStep)
                {
                    // accept edits to job step dialog and on OK commit the changes
                    // Caller: logviewer -> can launch job step dialog and user can make changes to this specific step
                    this.data.ApplyChanges(this.GetCurrentJob());
                }
                this.dialogResult = DialogResult.OK;
            }
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
            if (!String.IsNullOrEmpty(jobIdString))
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








