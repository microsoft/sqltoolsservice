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
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    [Flags]
    internal enum UserRoles
    {
        NotSet = -1, None = 0, AgentUser = 1, AgentReader = 2, AgentOperator = 4, SysAdmin = 8
    };

    /// <summary>
    /// Summary description for JobPropertiesPrototype.
    /// </summary>
    internal class JobData
    {
        #region constants
        /// <summary>
        /// Mode the dialog has been launched
        /// </summary>
        internal enum ActionMode { Create, Edit, Unknown };
        /// <summary>
        /// If the job has been pushed from an MSX then it's category ID will always be 1
        /// </summary>
        public readonly int JobFromMsxId = 1;
        #endregion

        #region fields
        /*
         * fields that are always populated
         */
        /// <summary>
        /// Urn of the Job object we were launched against. This will be null if we are creating a new job.
        /// </summary>
        private Urn urn = null;
        /// <summary>
        /// Datacontainer that represents everything we need to know about the server.
        /// </summary>
        private CDataContainer context;
        /// <summary>
        /// Mode we are working in.
        /// </summary>
        private ActionMode mode;

        /*
         * fields that map to the SMO Job object
         */

        /// <summary>
        /// original name of the job
        /// </summary>
        private string originalName = null;

        /// <summary>
        /// Job GUID stored as string
        /// </summary>
        private string jobIdString = null;

        #region general page
        bool generalInfoLoaded = false;
        /// <summary>
        /// current name of the job
        /// </summary>
        private string currentName = null;
        /// <summary>
        /// job owner
        /// </summary>
        private string owner = null;
        /// <summary>
        /// category for this job
        /// </summary>
        private LocalizableCategory category = null;
        /// <summary>
        /// job description
        /// </summary>
        private string description = null;
        /// <summary>
        /// is the job enabled
        /// </summary>
        private bool enabled;
        /// <summary>
        /// source server for this job
        /// </summary>
        private string source = null;
        /// <summary>
        /// originating server
        /// </summary>
        private string originatingServer = null;
        /// <summary>
        /// Date and time this job was created
        /// </summary>
        private DateTime created;
        /// <summary>
        /// when was the job last modified
        /// </summary>
        private DateTime lastModified;
        /// <summary>
        /// when was the job last executed
        /// </summary>
        private DateTime lastExecution;
        #endregion

        #region notifications
        bool notificationsLoaded = false;
        /// <summary>
        /// operator that will be emailed
        /// </summary>
        private string operatorToEmail = null;
        /// <summary>
        /// when the operator will be emailed
        /// </summary>
        private CompletionAction emailLevel;
        /// <summary>
        /// operator will be paged
        /// </summary>
        private string operatorToPage = null;
        /// <summary>
        /// when they will be paged
        /// </summary>
        private CompletionAction pageLevel;
        /// <summary>
        /// when will an entry be written to the event log
        /// </summary>
        private CompletionAction eventLogLevel;
        /// <summary>
        /// when will the job be deleted
        /// </summary>
        private CompletionAction deleteLevel;
        #endregion

        // msx / tsx information
        private bool msaInformationLoaded = false;
        private bool targetLocalServer = true;
        private bool originallyTargetLocalServer = true;
        private MsaJobTargetServer[] targetServers;

        // cached server information
        private UserRoles userRole = UserRoles.NotSet;
        private string trueLogin = null;
        private string[] owners = null;
        private LocalizableCategory[] smoCategories = null;
        private LocalizableCategory[] displayableCategories = null;
        private string[] operators = null;
        private Version version = null;

        // cached proxy objects
        private JobStepsData jobSteps = null;
        private JobSchedulesData jobSchedules = null;
        private JobAlertsData jobAlerts = null;

        // other information
        private string script = null;
        private string scriptName = null;

        #endregion

        #region public properties
        public Version Version
        {
            get
            {
                if (this.version == null)
                {
                    LoadVersion();
                }
                return this.version;
            }
        }
        public UserRoles UserRole
        {
            get
            {
                if (this.userRole == UserRoles.NotSet)
                {
                    LoadUserRoles();
                }
                return this.userRole;
            }
        }
        public CDataContainer OriginalContext
        {
            get
            {
                return this.context;
            }
        }
        public ActionMode Mode
        {
            get
            {
                return this.mode;
            }
        }
        public string Name
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.currentName;
            }
            set
            {
                CheckAndLoadGeneralData();
                this.currentName = value.Trim();
            }
        }
        public string Owner
        {
            get
            {
                CheckAndLoadOwner();
                return this.owner;
            }
            set
            {
                this.owner = value;
            }
        }

        public string[] Owners
        {
            get
            {
                if (this.owners == null)
                {
                    LoadLogins();
                }
                return this.owners;
            }
        }

        public LocalizableCategory Category
        {
            get
            {
                CheckAndLoadCategory();
                return this.category;
            }
            set
            {
                this.category = value;
            }
        }

        public LocalizableCategory[] Categories
        {
            get
            {
                CheckAndLoadDisplayableCategories();
                return this.displayableCategories;
            }
        }

        private LocalizableCategory[] SmoCategories
        {
            get
            {
                CheckAndLoadSmoCategories();
                return this.smoCategories;
            }
        }

        public String Description
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.description;
            }
            set
            {
                CheckAndLoadGeneralData();
                this.description = value;
            }
        }
        public bool Enabled
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.enabled;
            }
            set
            {
                CheckAndLoadGeneralData();
                this.enabled = value;
            }
        }
        public string Source
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.source;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public DateTime DateCreated
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.created;
            }
        }

        public DateTime DateLastModified
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.lastModified;
            }
        }
        public DateTime LastRunDate
        {
            get
            {
                CheckAndLoadGeneralData();
                return this.lastExecution;
            }
        }
        public string OperatorToEmail
        {
            get
            {
                CheckAndLoadNotifications();
                return this.operatorToEmail;
            }
            set
            {
                CheckAndLoadNotifications();
                this.operatorToEmail = value;
            }
        }
        public CompletionAction EmailLevel
        {
            get
            {
                CheckAndLoadNotifications();
                return this.emailLevel;
            }
            set
            {
                CheckAndLoadNotifications();
                this.emailLevel = value;
            }
        }
        public string OperatorToPage
        {
            get
            {
                CheckAndLoadNotifications();
                return this.operatorToPage;
            }
            set
            {
                CheckAndLoadNotifications();
                this.operatorToPage = value;
            }
        }
        public CompletionAction PageLevel
        {
            get
            {
                CheckAndLoadNotifications();
                return this.pageLevel;
            }
            set
            {
                CheckAndLoadNotifications();
                this.pageLevel = value;
            }
        }
        
        public CompletionAction EventLogLevel
        {
            get
            {
                CheckAndLoadNotifications();
                return this.eventLogLevel;
            }
            set
            {
                CheckAndLoadNotifications();
                this.eventLogLevel = value;
            }
        }
        public CompletionAction DeleteLevel
        {
            get
            {
                CheckAndLoadNotifications();
                return this.deleteLevel;
            }
            set
            {
                CheckAndLoadNotifications();
                this.deleteLevel = value;
            }
        }

        public string[] Operators
        {
            get
            {
                if (this.operators == null)
                {
                    LoadOperators();
                }
                return this.operators;
            }
        }
        public JobStepsData JobSteps
        {
            get
            {
                CheckAndLoadJobSteps();
                return this.jobSteps;
            }
        }
        public JobSchedulesData JobSchedules
        {
            get
            {
                CheckAndLoadJobSchedules();
                return this.jobSchedules;
            }
        }
        public JobAlertsData JobAlerts
        {
            get
            {
                CheckAndLoadJobAlerts();
                return this.jobAlerts;
            }
        }
        public bool IsMsx
        {
            get
            {
                return this.AvailableTargetServers.Length > 0;
            }
        }
        public MsaJobTargetServer[] AvailableTargetServers
        {
            get
            {
                if (this.targetServers == null)
                {
                    LoadTargetServers();
                }
                return this.targetServers;
            }
        }
        public bool IsLocalJob
        {
            get
            {
                return this.targetLocalServer;
            }
        }
        public bool IsRemotelyOriginated
        {
            get
            {
                return (this.category == null) ? false : this.Category.SmoCategory.ID == JobFromMsxId;
            }
        }
        public string OriginatingServer
        {
            get
            {
                return this.originatingServer;
            }
        }
        public bool IsUserAgentAdmin
        {
            get
            {
                return (UserRole & UserRoles.SysAdmin) > 0;
            }
        }
        public bool OriginallyTargetLocalServer
        {
            get
            {
                CheckAndLoadMsaInformation();
                return this.originallyTargetLocalServer;
            }
            set
            {
                CheckAndLoadMsaInformation();
                this.originallyTargetLocalServer = value;
            }
        }
        public bool TargetLocalServer
        {
            get
            {
                CheckAndLoadMsaInformation();
                return this.targetLocalServer;
            }
            set
            {
                CheckAndLoadMsaInformation();
                //If a change in the targetLocalServer was detected, then fire the OnCategoriesChanged
                //event so that the categories drop down list is properly populated.
                if (this.targetLocalServer != value)
                {
                    this.targetLocalServer = value;
                    this.displayableCategories = null;
                    CheckAndLoadDisplayableCategories();
                    OnCategoriesChanged();
                    //TODO: add method to do this?
                    this.owners = null;
                    OnOwnersChanged();
                }
            }
        }

        /// If we're looking at an existing Job and this.Job is not
        /// null, then check if the job's category is local. We do
        /// this for the case where the job exists but has no targets,
        /// and therefore this.targetLocalServer is false even though
        /// we're in a local job category.
        public bool JobCategoryIsLocal
        {
            get
            {
                Job job = this.Job;
                if (job == null)
                {
                    return false;
                }
                return (CategoryType.LocalJob == (SMO.Agent.CategoryType)(job.CategoryType));
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return IsRemotelyOriginated
            || (this.mode == ActionMode.Edit && !IsUserAgentAdmin && this.TrueLogin != this.Owner);
            }
        }

        public bool AllowEnableDisable
        {
            get
            {
                // Enable/disable for non-read only jobs should always be true
                // for remotely originate jobs it will always be false
                // if the user is an agent operator and they do not own the job then it will be true
                return IsReadOnly
                    ? (!IsRemotelyOriginated && ((UserRole & UserRoles.AgentOperator) > 0))
                    : true;
            }

        }

        private void OnCategoriesChanged()
        {
            //Fire the categories changed event.
            if (this.CategoriesChanged != null)
            {
                this.CategoriesChanged(this, EventArgs.Empty);
            }
        }
        private void OnOwnersChanged()
        {
            //Fire the categories changed event.
            if (this.OwnersChanged != null)
            {
                this.OwnersChanged(this, EventArgs.Empty);
            }
        }
        #endregion

        #region events
        public event EventHandler CategoriesChanged;
        public event EventHandler OwnersChanged;
        #endregion

        #region construction
        public JobData(CDataContainer data, AgentJobInfo jobInfo = null)
        {
            this.context = data;

            // get the job information
            STParameters parameters = new STParameters(this.context.Document);

            parameters.GetParam("job", ref this.originalName);
            parameters.GetParam("jobid", ref this.jobIdString);
            parameters.GetParam("script", ref this.script);
            parameters.GetParam("scriptName", ref this.scriptName);

            // get the Urn
            string urn = string.Empty;

            parameters.GetParam("urn", ref urn);

            if (urn != null && urn.Length > 0)
            {
                this.urn = new Urn(urn);
            }

            bool isMsxJob = false;
            parameters.GetParam("msxjob", ref isMsxJob);

            //If this is an MSX, initially set TargetLocalServers to false;
            if (isMsxJob)
            {
                this.targetLocalServer = false;
            }
            // we are in properties mode.
            if (this.originalName.Length > 0 || !string.IsNullOrEmpty(this.jobIdString))
            {
                this.mode = ActionMode.Edit;

            }
            else if (this.script.Length > 0)
            {
                // we are creating a new job, but prepopulating 
                // one step with the script passed to us
                this.mode = ActionMode.Create;
                this.Name = this.scriptName;
                SetDefaults();
                this.jobSteps = new JobStepsData(context, script, this);
            }
            // creating a new job
            else
            {
                this.mode = ActionMode.Create;
                // set defaults that do not involve going to the server to retrieve
                SetDefaults();
            }

            // load AgentJobInfo data
            if (jobInfo != null)
            {
                this.currentName = jobInfo.Name;
                this.owner = jobInfo.Owner;
                this.description = jobInfo.Description;
            }
        }
        #endregion

        #region properties - non public
        internal Job Job
        {
            get
            {
                Job job = null;
                // If JobID is passed in look up by jobID
                if (!string.IsNullOrEmpty(this.jobIdString))
                {
                    job = this.context.Server.JobServer.Jobs.ItemById(Guid.Parse(this.jobIdString));

                    // Job name might not be passed in context, let us fix job name and urn
                    this.originalName = job.Name;
                    this.urn = job.Urn;
                }
                else
                {
                    // or use urn path to query job 
                    job = this.context.Server.GetSmoObject(this.urn) as Job;
                }
             
                return job;
            }
        }
        internal Urn Urn
        {
            get
            {
                return this.urn;
            }
        }

        internal string TrueLogin
        {
            get
            {
                if (this.trueLogin == null)
                {
                    LoadTrueLogin();
                }
                return trueLogin;
            }

        }
        #endregion

        #region data loading / initialization

        private void CheckAndLoadMsaInformation()
        {
            if (msaInformationLoaded || this.mode == ActionMode.Create)
            {
                return;
            }

            Job job = this.Job;

            msaInformationLoaded = true;

            DataTable table = job.EnumTargetServers();
            string targetName;
            if (table.Rows.Count == 0)
            {
                originallyTargetLocalServer = this.TargetLocalServer = false;
            }
            // if the server is an msx then see if this job is targetted at local
            // servers
            else if (this.IsMsx)
            {
                for (int i = 0; i < table.Rows.Count; ++i)
                {
                    targetName = table.Rows[i]["ServerName"].ToString().ToLowerInvariant();

                    for (int ii = 0; ii < this.AvailableTargetServers.Length; ++ii)
                    {
                        if (targetName == this.AvailableTargetServers[ii].Name.ToLowerInvariant())
                        {
                            AvailableTargetServers[ii].IsJobAppliedToTarget = AvailableTargetServers[ii].WillJobBeAppliedToTarget = true;
                            originallyTargetLocalServer = this.TargetLocalServer = false;
                        }
                    }
                }
            }
        }

        private void CheckAndLoadGeneralData()
        {
            if (this.generalInfoLoaded || this.mode == ActionMode.Create)
            {
                return;
            }

            Job job = this.Job;

            this.currentName = this.originalName;
            this.description = job.Description;
            this.enabled = job.IsEnabled;
            // this.source = job.Source;
            this.created = job.DateCreated;
            this.lastModified = job.DateLastModified;
            this.lastExecution = job.LastRunDate;

            this.originatingServer = job.OriginatingServer;

            this.generalInfoLoaded = true;
            CheckAndLoadMsaInformation();
        }

        private void CheckAndLoadOwner()
        {
            if (this.owner != null)
            {
                return;
            }
            if (this.Mode == ActionMode.Edit)
            {
                this.owner = this.Job.OwnerLoginName;
            }
            else
            {
                this.owner = this.context.ConnectionInfo.UserName.TrimEnd();
            }
        }

        private void CheckAndLoadJobSteps()
        {
            if (this.jobSteps != null)
            {
                return;
            }

            this.jobSteps = new JobStepsData(context, this);
        }

        private void CheckAndLoadJobSchedules()
        {
            if (this.jobSchedules != null)
            {
                return;
            }
            // load schedules
            this.jobSchedules = new JobSchedulesData(context, this);
        }

        private void CheckAndLoadJobAlerts()
        {
            if (this.jobAlerts != null || (UserRole & UserRoles.SysAdmin) == 0)
            {
                return;
            }
            this.jobAlerts = new JobAlertsData(context, this);
        }

        // loads default values for a new job.
        private void SetDefaults()
        {
            this.description = string.Empty;
            this.enabled = true;
            this.source = null;

            // notifications
            this.operatorToEmail = string.Empty;
            this.emailLevel = CompletionAction.Never;

            this.operatorToPage = string.Empty;
            this.pageLevel = CompletionAction.Never;
            this.eventLogLevel = CompletionAction.Never;
            this.deleteLevel = CompletionAction.Never;

            this.generalInfoLoaded = true;
            this.notificationsLoaded = true;
            this.msaInformationLoaded = true;

            this.originatingServer = string.Empty;
        }

        private void CheckAndLoadNotifications()
        {
            if (this.notificationsLoaded || this.Mode == ActionMode.Create)
            {
                return;
            }

            Job job = this.Job;

            if (this.Version.Major >= 9 || ((UserRole & UserRoles.SysAdmin) > 0))
            {
                // notifications
                this.operatorToEmail = job.OperatorToEmail;
                this.emailLevel = job.EmailLevel;

                this.operatorToPage = job.OperatorToPage;
                this.pageLevel = job.PageLevel;
            }
            else
            {
                this.operatorToEmail = String.Empty;
                this.emailLevel = CompletionAction.Never;

                this.operatorToPage = String.Empty;
                this.pageLevel = CompletionAction.Never;
            }

            this.eventLogLevel = job.EventLogLevel;
            this.deleteLevel = job.DeleteLevel;
            this.notificationsLoaded = true;
        }

        private void CheckAndLoadCategory()
        {
            if (this.category != null)
            {
                return;
            }

            if (this.Mode == ActionMode.Edit)
            {
                this.category = ConvertStringToCategory(this.Job.Category);
            }
            else
            {
                this.category = Categories[0];
            }
        }


        /// <summary>
        /// Load all SMO categories.
        /// </summary>
        private void CheckAndLoadSmoCategories()
        {
            if (this.smoCategories != null)
            {
                return;
            }

            // force a preload of all of the category information. We cache this,
            // and if we are not careful SMO will end up executing a batch per category
            this.context.Server.SetDefaultInitFields(typeof(JobCategory), true);

            JobServer jobServer = this.context.Server.JobServer;
            List<LocalizableCategory> smoCategories = new List<LocalizableCategory>();
            for (int i = 0; i < jobServer.JobCategories.Count; ++i)
            {
                smoCategories.Add(new LocalizableCategory(jobServer.JobCategories[i]));
            }
            this.smoCategories = smoCategories.ToArray();
        }


        /// <summary>
        /// Load only those categories that will be displayed in the categories drop-down based on
        /// TSX and MSX job status.
        /// </summary>
        private void CheckAndLoadDisplayableCategories()
        {
            if (this.displayableCategories != null)
            {
                return;
            }
            this.displayableCategories = null;

            LocalizableCategory[] allCategories = this.SmoCategories;

            List<LocalizableCategory> displayableCategories = new List<LocalizableCategory>();

            bool targetsLocalServer = this.targetLocalServer || this.JobCategoryIsLocal;
            
	    
            // get all applicable categories
            for (int i = 0; i < allCategories.Length; ++i)
            {
                bool validCategoryForThisContext = false;
                CategoryType currentJobCategoryType = allCategories[i].SmoCategory.CategoryType;

                if (targetsLocalServer)
                {
                    // Only local jobs are valid, regardless of whether they are originated remotely or locally
                    // (Besides, it doesn't matter for TSX jobs as their category cannot be changed by TSX anyway)
                    validCategoryForThisContext = currentJobCategoryType == CategoryType.LocalJob;
                }
                else
                {
                    if (this.IsRemotelyOriginated)
                    {
                        if (!this.IsMsx)
                        {
                            validCategoryForThisContext = (currentJobCategoryType == CategoryType.LocalJob);
                        }
                    }
                    else
                    {
                        if (this.IsMsx)
                        {
                            validCategoryForThisContext = (currentJobCategoryType == CategoryType.MultiServerJob);
                        }
                    }
                }

                ///See if this category can be added.
                if (validCategoryForThisContext)
                {
                    displayableCategories.Add(allCategories[i]);
                }

            }
            this.displayableCategories = displayableCategories.ToArray();
        }

        private void LoadLogins()
        {
            // figure out what rights the user has.
            SqlServer.Management.Smo.Server server = this.context.Server;
            // see if the user is a sysadmin. At the moment sysadmins can assign
            // job ownership to any user. Non sysadmins cannot. Operators can see jobs owned by anyone
            if ((this.UserRole & UserRoles.SysAdmin) > 0 || (this.UserRole & UserRoles.AgentOperator) > 0)
            {
                System.Collections.Specialized.StringCollection validLoginNames = new System.Collections.Specialized.StringCollection();

                foreach (SMO.Login login in server.Logins)
                {
                    if (SMO.LoginType.WindowsGroup != login.LoginType)
                    {
                        //For Msx jobs, only add logins that are members of the sysadmin role.
                        if (!this.targetLocalServer)
                        {
                            if (login.IsMember("sysadmin"))
                            {
                                validLoginNames.Add(login.Name);
                            }

                        }
                        else
                        {
                            //Otherwise, if this is NOT an Msx jobs, just add it.
                            validLoginNames.Add(login.Name);
                        }

                    }
                }

                //validLoginNames will not include the current connection's trusted user therefore
                //add it to the owners string array.  This will allow the value to be seen (and selected) in 
                //the job properties drop down. 
                //Only add the name if it doesn't already exist in the collection
                if (!validLoginNames.Contains(TrueLogin))
                {
                    validLoginNames.Add(TrueLogin);
                }

                this.owners = new string[validLoginNames.Count];
                validLoginNames.CopyTo(this.owners, 0);
            }
            else
            {
                // the user is the only person allowed to own the job
                this.owners = new string[1] { server.ConnectionContext.TrueLogin };
            }
        }

        private void LoadTargetServers()
        {
            if ((UserRole & UserRoles.SysAdmin) > 0 || (UserRole & UserRoles.AgentOperator) > 0)
            {
                // load any target servers if this is an msx
                this.targetServers = new MsaJobTargetServer[this.context.Server.JobServer.TargetServers.Count];
                if (this.targetServers.Length > 0)
                {
                    for (int i = 0; i < this.targetServers.Length; ++i)
                    {
                        this.targetServers[i] = new MsaJobTargetServer(this.context.Server.JobServer.TargetServers[i].Name);
                    }
                }
            }
            else
            {
                this.targetServers = new MsaJobTargetServer[0];
            }
        }

        private void LoadOperators()
        {
            if (this.Version.Major >= 9 || ((UserRole & UserRoles.SysAdmin) > 0))
            {
                // load operators
                int operatorCount = this.context.Server.JobServer.Operators.Count;
                this.operators = new string[operatorCount];
                for (int i = 0; i < operatorCount; i++)
                {
                    this.operators[i] = this.context.Server.JobServer.Operators[i].Name;
                }
            }
            else
            {
                this.operators = new string[0];
            }
        }

        private void LoadTrueLogin()
        {
            this.trueLogin = this.context.Server.ConnectionContext.TrueLogin;
        }

        private void LoadVersion()
        {
            this.version = this.context.Server.Information.Version;
        }

        private void LoadUserRoles()
        {
            SqlServer.Management.Smo.Server server = this.context.Server;

            this.userRole = UserRoles.None;

            if ((server.ConnectionContext.UserProfile & ServerUserProfiles.SALogin) > 0)
            {
                this.userRole |= UserRoles.SysAdmin;
            }

            if (this.Version.Major >= 9)
            {
                Database msdb = server.Databases["msdb"];
                if (msdb != null)
                {
                    if (msdb.IsMember("SQLAgentOperatorRole"))
                    {
                        this.userRole |= UserRoles.AgentOperator;
                    }
                    if (msdb.IsMember("SQLAgentReaderRole"))
                    {
                        this.userRole |= UserRoles.AgentReader;
                    }
                }
            }

            this.userRole |= UserRoles.AgentUser;
        }
        #endregion

        #region saving
        public void ApplyChanges(bool creating)
        {
            Job job = null;
            bool scripting = this.context.Server.ConnectionContext.SqlExecutionModes == SqlExecutionModes.CaptureSql;
            bool targetServerSelected = false;

            this.mode = creating ? ActionMode.Create : ActionMode.Edit; 

            ///Before any job posting if donem make sure that if this is an MSX job that the user has selected at
            ///least one Target Server.
            if (!this.targetLocalServer)
            {
                for (int i = 0; i < this.AvailableTargetServers.Length; ++i)
                {
                    if (this.AvailableTargetServers[i].WillJobBeAppliedToTarget)
                    {
                        targetServerSelected = true;
                        break;
                    }
                }
                if (!targetServerSelected)
                {
                    ///Not target servers selected.  Throw error.
                    throw new ApplicationException(SR.TargetServerNotSelected);
                }
            }

            if (creating)
            {
                job = new Job(this.context.Server.JobServer, this.Name, this.Category.SmoCategory.ID);
            }
            else
            {
                // just lookup the original object
                job = this.Job;
            }

            if (!this.IsReadOnly)
            {
                if (creating || job.OwnerLoginName != this.owner)
                {
                    job.OwnerLoginName = this.owner;
                }

                if (creating || (this.category != null
                                && this.category.SmoCategory.Name != job.Category))
                {
                    job.Category = this.category.SmoCategory.Name;
                }

                if (creating || this.description != job.Description)
                {
                    job.Description = this.description;
                }

                SaveNotifications(job, creating);
            }

            if (this.AllowEnableDisable)
            {
                if (creating || this.enabled != job.IsEnabled)
                {
                    job.IsEnabled = this.enabled;
                }
            }

            // do the actual creation / alter
            if (creating)
            {
                ///Check to see if the job already exists
                JobExists(job.Name);
                job.Create();
                if (!scripting)
                {
                    this.urn = job.Urn;
                }
            }
            else
            {
                job.Alter();
            }

            if (!this.IsReadOnly && !scripting)
            {
                if (this.targetLocalServer)
                {
                    if (!OriginallyTargetLocalServer || creating)
                    {
                        foreach (MsaJobTargetServer targetServer in this.AvailableTargetServers)
                        {
                            if (targetServer.IsJobAppliedToTarget)
                            {
                                job.RemoveFromTargetServer(targetServer.Name.ToUpperInvariant());
                                targetServer.IsJobAppliedToTarget = false;
                            }
                        }

                        OriginallyTargetLocalServer = true;
                        job.ApplyToTargetServer(this.context.Server.ConnectionContext.TrueName.ToUpperInvariant());
                    }
                }
                else if (this.IsMsx)
                {

                    if (!creating && OriginallyTargetLocalServer)
                    {
                        // Remove from target server only if actually does target the local server
                        string thisServerName = this.context.Server.ConnectionContext.TrueName.ToUpperInvariant();
                        DataTable targetServers = job.EnumTargetServers();
                        foreach (DataRow row in targetServers.Rows)
                        {
                            string targetServerName = row["ServerName"] as string;
                            if (String.Compare(targetServerName, thisServerName, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                job.RemoveFromTargetServer(thisServerName);
                                break;
                            }
                        }

                        OriginallyTargetLocalServer = false;
                    }

                    // add and remove target servers
                    for (int i = 0; i < this.AvailableTargetServers.Length; ++i)
                    {
                        if (this.AvailableTargetServers[i].WillJobBeAppliedToTarget && !this.AvailableTargetServers[i].IsJobAppliedToTarget)
                        {
                            job.ApplyToTargetServer(this.AvailableTargetServers[i].Name.ToUpperInvariant());
                            this.AvailableTargetServers[i].IsJobAppliedToTarget = true;
                        }
                        else if (!this.AvailableTargetServers[i].WillJobBeAppliedToTarget && this.AvailableTargetServers[i].IsJobAppliedToTarget)
                        {
                            job.RemoveFromTargetServer(this.AvailableTargetServers[i].Name);
                            this.AvailableTargetServers[i].IsJobAppliedToTarget = false;
                        }
                    }
                }

            }

            // Because some of the SMO methods above can update Job's CategoryID affecting the Urn
            this.urn = job.Urn;

            bool stepsChanged = false;
            // save steps,schedules etc
            if (this.jobSteps != null)
            {
                stepsChanged = this.jobSteps.ApplyChanges(job);
            }
            bool schedulesChanged = false;
            if (this.jobSchedules != null)
            {
                schedulesChanged = this.jobSchedules.ApplyChanges(job);
            }

            if ((stepsChanged || schedulesChanged) && !this.TargetLocalServer && !creating)
            {
                // TODO: this seems wrong. Why do it here and not in SMO?
                this.context.Server.ConnectionContext.ExecuteNonQuery(
                    string.Format(CultureInfo.InvariantCulture, "EXECUTE msdb.dbo.sp_post_msx_operation  N'INSERT', N'JOB', @job_id = '{0}'"
                    , job.JobID));
            }

            //Do not attempt to save the job alert if we are in scripting mode, since the job id does not
            //yet exists.
            if (jobAlerts != null && !scripting)
            {
                this.jobAlerts.ApplyChanges(job);
            }

            // check the name if we are not creating
            if (!creating && this.Name != job.Name )
            {
                // new name = rename
                job.Rename(this.Name);

                // get the new urn if we aren't scripting
                if (!scripting)
                {
                    this.urn = job.Urn;
                }
            }
        }

        private void SaveNotifications(Job job, bool creating)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            // nothing to do if this information has not been loaded
            if (this.notificationsLoaded == false)
            {
                return;
            }

            // notifications panel
            if (this.emailLevel == CompletionAction.Never)
            {
                this.emailLevel = CompletionAction.OnFailure;
                this.operatorToEmail = String.Empty;
            }

            if (creating || this.emailLevel != job.EmailLevel)
            {
                job.EmailLevel = this.emailLevel;
            }

            if (creating || this.operatorToEmail != job.OperatorToEmail)
            {
                job.OperatorToEmail = this.operatorToEmail;
            }
            
            if (this.pageLevel == CompletionAction.Never)
            {
                this.pageLevel = CompletionAction.OnFailure;
                this.operatorToPage = String.Empty;
            }

            if (creating || this.pageLevel != job.PageLevel)
            {
                job.PageLevel = this.pageLevel;
            }

            if (creating || this.operatorToPage != job.OperatorToPage)
            {
                job.OperatorToPage = this.operatorToPage;
            }

            if (creating || this.eventLogLevel != job.EventLogLevel)
            {
                job.EventLogLevel = this.eventLogLevel;
            }

            if (creating || this.deleteLevel != job.DeleteLevel)
            {
                job.DeleteLevel = this.deleteLevel;
            }
        }
        #endregion

        #region implementation
        /// <summary>
        /// Convert a string into a Localizable job category
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal LocalizableCategory ConvertStringToCategory(string source)
        {
            if (this.SmoCategories == null || this.SmoCategories.Length == 0)
            {
                throw new InvalidOperationException();
            }

            LocalizableCategory category = null;

            for (int i = 0; i < this.SmoCategories.Length; ++i)
            {
                if (source == this.SmoCategories[i].SmoCategory.Name)
                {
                    category = this.SmoCategories[i];
                    break;
                }
            }

            return category;
        }

        /// <summary>
        /// Check SMO to see if job already exists.
        /// </summary>
        /// <param name="jobName"></param>
        private void JobExists(string jobName)
        {
            Microsoft.SqlServer.Management.Smo.Agent.JobCollection smoJobCollection = this.context.Server.JobServer.Jobs;
            try
            {
                if (smoJobCollection.Contains(jobName))
                {
                    throw new ApplicationException(SR.JobAlreadyExists(jobName));
                }
            }
            finally
            {
                smoJobCollection = null;
            }
        }
        #endregion

    }
}
