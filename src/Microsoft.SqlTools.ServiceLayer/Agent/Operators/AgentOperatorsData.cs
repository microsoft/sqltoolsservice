using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    #region internal structures
    /// <summary>
    /// Provides data to be consumed in the job notification grid
    /// </summary>
    internal struct AgentJobNotificationHelper
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">job name</param>
        /// <param name="notifyEmail"></param>
        /// <param name="notifyPager"></param>
        public AgentJobNotificationHelper(string name, CompletionAction notifyEmail, CompletionAction notifyPager)
        {
            this.Name = name;
            this.NotifyEmail = notifyEmail;
            this.NotifyPager = notifyPager;
        }
        /// <summary>
        /// Name of the job
        /// </summary>
        public string Name;
        /// <summary>
        /// job email notification action
        /// </summary>
        public CompletionAction NotifyEmail;
        /// <summary>
        /// job pager notification action
        /// </summary>
        public CompletionAction NotifyPager;
    }
    /// <summary>
    /// Provides data to be consumed in the alert notification grid
    /// </summary>
    internal struct AgentAlertNotificationHelper
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">Name of the alert</param>
        /// <param name="notifyEmail"></param>
        /// <param name="notifyPager"></param>
        /// <param name="alert">Alert object</param>
        public AgentAlertNotificationHelper(string name, bool notifyEmail, bool notifyPager, Alert alert)
        {
            this.Name = name;
            this.NotifyEmail = notifyEmail;
            this.NotifyPager = notifyPager;
            this.Alert = alert;
        }
        /// <summary>
        /// Alert name
        /// </summary>
        public string Name;
        /// <summary>
        /// Indicates whether the alert will notify the operator through email
        /// </summary>
        public bool NotifyEmail;
        /// <summary>
        /// Indicates whether the alert will notify the operator through pager
        /// </summary>
        public bool NotifyPager;
        /// <summary>
        /// Alert object. optimisation to stop us having to lookup the alert object when needed
        /// </summary>
        public Alert Alert;
    }
    #endregion
    /// <summary>
    /// Proxy class for the AgentOperators dialog and property pages.
    /// Performs lazy instantiation of groups of data based around the operators dialog property pages
    /// </summary>
    internal class AgentOperatorsData
    {
        #region members
        /// <summary>
        /// Data container
        /// </summary>
        CDataContainer dataContainer;
        /// <summary>
        /// Original operator name. Empty if we are creating a new operator
        /// </summary>
        string originalOperatorName = String.Empty;
        /// <summary>
        /// Indicates whether we are creating an operator or not
        /// </summary>
        bool createMode;

        /// <summary>
        /// Has then data for the general page been initialised
        /// </summary>
        bool generalInitialized = false;
        /// <summary>
        /// has the data for the history page been initialised
        /// </summary>
        bool historyInitialized = false;

        /// <summary>
        /// True if this operator cannot be modified
        /// </summary>
        bool readOnly = false;

        #region general items
        string name;
        bool enabled;
        string emailAddress;
        string pagerAddress;
        WeekDays pagerDays;
        DateTime weekdayStartTime;
        DateTime weekdayEndTime;
        DateTime saturdayStartTime;
        DateTime saturdayEndTime;
        DateTime sundayStartTime;
        DateTime sundayEndTime;
        #endregion

        #region notification items
        /// <summary>
        /// will be null if the alert notifications have not been initialised
        /// </summary>
        IList<AgentAlertNotificationHelper> alertNotifications;
        /// <summary>
        /// will be null if the job notifications have not been initialised
        /// </summary>
        IList<AgentJobNotificationHelper> jobNotifications;
        #endregion

        #region history items
        DateTime lastEmailDate;
        DateTime lastPagerDate;
        #endregion
        #endregion

        #region properties
        /// <summary>
        /// indicates if the data is in create mode
        /// </summary>
        public bool Creating
        {
            get
            {
                return this.createMode;
            }
        }
        /// <summary>
        /// name of the object
        /// </summary>
        public string Name
        {
            get
            {
                LoadGeneralData();
                return name;
            }
            set
            {
                LoadGeneralData();
                name = value;
            }
        }
        /// <summary>
        /// Indicates if the dataobject is readonly
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return this.readOnly;
            }
        }
        #region general items
        /// <summary>
        /// indicates whether or not the operator is enabled
        /// </summary>
        public bool Enabled
        {
            get
            {
                LoadGeneralData();
                return enabled;
            }
            set
            {
                LoadGeneralData();
                enabled = value;
            }
        }
        /// <summary>
        /// email address of this operator
        /// </summary>
        public string EmailAddress
        {
            get
            {
                LoadGeneralData();
                return this.emailAddress;
            }
            set
            {
                LoadGeneralData();
                this.emailAddress = value;
            }
        }
        /// <summary>
        /// pager address of this operator
        /// </summary>
        public string PagerAddress
        {
            get
            {
                LoadGeneralData();
                return this.pagerAddress;
            }
            set
            {
                LoadGeneralData();
                this.pagerAddress = value;
            }
        }
        
        /// <summary>
        /// the days of the week the operator is active
        /// </summary>
        public WeekDays PagerDays
        {
            get
            {
                LoadGeneralData();
                return this.pagerDays;
            }
            set
            {
                LoadGeneralData();
                this.pagerDays = value;
            }
        }
        /// <summary>
        /// Weekday start time for this operator to be active
        /// </summary>
        public DateTime WeekdayStartTime
        {
            get
            {
                LoadGeneralData();
                return this.weekdayStartTime;
            }
            set
            {
                LoadGeneralData();
                this.weekdayStartTime = value;
            }
        }
        /// <summary>
        /// Weekday end time for this operator to be active
        /// </summary>
        public DateTime WeekdayEndTime
        {
            get
            {
                LoadGeneralData();
                return this.weekdayEndTime;
            }
            set
            {
                LoadGeneralData();
                this.weekdayEndTime = value;
            }
        }
        /// <summary>
        /// Saturday start time for this operator to be active
        /// </summary>
        public DateTime SaturdayStartTime
        {
            get
            {
                LoadGeneralData();
                return this.saturdayStartTime;
            }
            set
            {
                LoadGeneralData();
                this.saturdayStartTime = value;
            }
        }
        /// <summary>
        /// Saturday end time for this operator to be active
        /// </summary>
        public DateTime SaturdayEndTime
        {
            get
            {
                LoadGeneralData();
                return this.saturdayEndTime;
            }
            set
            {
                LoadGeneralData();
                this.saturdayEndTime = value;
            }
        }
        /// <summary>
        /// Sunday start time for this operator to be active
        /// </summary>
        public DateTime SundayStartTime
        {
            get
            {
                LoadGeneralData();
                return this.sundayStartTime;
            }
            set
            {
                LoadGeneralData();
                this.sundayStartTime = value;
            }
        }
        /// <summary>
        /// Saturday end time for this operator to be active
        /// </summary>
        public DateTime SundayEndTime
        {
            get
            {
                LoadGeneralData();
                return this.sundayEndTime;
            }
            set
            {
                LoadGeneralData();
                this.sundayEndTime = value;
            }
        }
        #endregion

        #region notification items
        /// <summary>
        /// Alerts that notify this operator
        /// </summary>
        public IList<AgentAlertNotificationHelper> AlertNotifications
        {
            get
            {
                LoadAlertNotificationData();
                return this.alertNotifications;
            }
            set
            {
                this.alertNotifications = value;
            }
        }
        /// <summary>
        /// Jobs that notify this operator. This has to be set through the jobs dialog and is read only
        /// </summary>
        public IList<AgentJobNotificationHelper> JobNotifications
        {
            get
            {
                LoadJobNotificationData();
                return this.jobNotifications;
            }
        }
        #endregion

        #region history items
        /// <summary>
        /// Date this operator was last emailed
        /// </summary>
        public DateTime LastEmailDate
        {
            get
            {
                LoadHistoryData();
                return this.lastEmailDate;
            }
        }
        /// <summary>
        /// Date this operator was last paged
        /// </summary>
        public DateTime LastPagerDate
        {
            get
            {
                LoadHistoryData();
                return this.lastPagerDate;
            }
        }
        
        #endregion
        #endregion

        #region Constructors

        /// <summary>
        /// Default public constructor
        /// </summary>
        public AgentOperatorsData(CDataContainer dataContainer)
        {
            if(dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }
            this.dataContainer = dataContainer;

            this.createMode = true;
        }

        public AgentOperatorsData(CDataContainer dataContainer, string operatorName)
        {
            if(dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }
            if(operatorName == null)
            {
                throw new ArgumentNullException("operatorName");
            }

            this.dataContainer = dataContainer;

            this.readOnly = !this.dataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);
            this.originalOperatorName = operatorName;

            this.createMode = operatorName.Length == 0;
        }
        #endregion

        #region data loading
        /// <summary>
        /// load data for the general tab. This can be called multiple times but will only load the data once intially
        /// or after a reset
        /// </summary>
        private void LoadGeneralData()
        {
            if(this.generalInitialized)
                return;

            // load defaults if we're creating
            if(createMode)
            {
                LoadGeneralDefaults();
                return;
            }

            // lookup the operator this will throw if it has been deleted.
            Microsoft.SqlServer.Management.Smo.Agent.Operator currentOperator = GetCurrentOperator();

            // setup the members
            this.name = currentOperator.Name;
            this.enabled = currentOperator.Enabled;
            this.emailAddress = currentOperator.EmailAddress;
            this.pagerAddress = currentOperator.PagerAddress;

            this.pagerDays = currentOperator.PagerDays;

            this.weekdayStartTime = ConvertAgentTime(currentOperator.WeekdayPagerStartTime);
            this.weekdayEndTime = ConvertAgentTime(currentOperator.WeekdayPagerEndTime);

            this.saturdayStartTime = ConvertAgentTime(currentOperator.SaturdayPagerStartTime);
            this.saturdayEndTime = ConvertAgentTime(currentOperator.SaturdayPagerEndTime);

            this.sundayStartTime = ConvertAgentTime(currentOperator.SundayPagerStartTime);
            this.sundayEndTime = ConvertAgentTime(currentOperator.SundayPagerEndTime);

            this.generalInitialized = true;

        }
        /// <summary>
        /// Load the data for the jobs that notify this operator. Can be called multiple times and will
        /// only load the data initially, or after a reset.
        /// </summary>
        private void LoadJobNotificationData()
        {
            if(this.jobNotifications != null)
                return;

            // just set defaults if we're creating as no jobs will point to this operator yet.
            if(createMode)
            {
                LoadJobNotificationDefaults();
                return;
            }

            JobServer jobServer = GetJobServer();

            this.jobNotifications = new List<AgentJobNotificationHelper>();

            // we have to loop through each job and see if it notifies us.
            foreach(Job job in jobServer.Jobs)
            {
                bool emailOperator = (job.OperatorToEmail == this.originalOperatorName);
                bool pageOperator = (job.OperatorToPage == this.originalOperatorName);
                if(emailOperator || pageOperator )
                {
                    // only return jobs that notify this operator
                    AgentJobNotificationHelper notification = new AgentJobNotificationHelper(job.Name
                        , job.EmailLevel
                        , job.PageLevel
                        );
                    this.jobNotifications.Add(notification);
                }
            }
        }
        /// <summary>
        /// Load alerts that notify this operator
        /// </summary>
        private void LoadAlertNotificationData()
        {
            if(this.alertNotifications != null)
                return;

            // defaults in create ode
            if(createMode)
            {
                LoadAlertNotificationDefaults();
                return;
            }

            this.alertNotifications = new List<AgentAlertNotificationHelper>();

            Microsoft.SqlServer.Management.Smo.Agent.Operator agentOperator = GetCurrentOperator();
            JobServer jobServer = GetJobServer();

            // see all alerts that notifuy this operator
            DataTable notifications = agentOperator.EnumNotifications();
            DataRow alertRow;
            bool notifyEmail;
            bool notifyPager;
            AgentAlertNotificationHelper alertNotification;

            // Add every alert to the structure
            foreach(Alert alert in jobServer.Alerts)
            {
                alertRow = null;

                // see if the alert notifies us already
                foreach(DataRow row in notifications.Rows)
                {
                    if((string)row["AlertName"] == alert.Name)
                    {
                        alertRow = row;
                        break;
                    }
                }

                // set if the current alert notifies this operator
                // if so so how
                if(alertRow != null)
                {
                    notifyEmail = (bool)alertRow["UseEmail"];
                    notifyPager = (bool)alertRow["UsePager"];
                }
                else
                {   notifyEmail = false;
                    notifyPager = false;
                }

                alertNotification = new AgentAlertNotificationHelper(alert.Name
                    ,notifyEmail
                    ,notifyPager
                    ,alert);

                this.alertNotifications.Add(alertNotification);
            }
        }
        /// <summary>
        /// load the notifiaction history for the operator
        /// </summary>
        private void LoadHistoryData()
        {
            if(this.historyInitialized)
                return;

            if(this.createMode)
            {
                LoadHistoryDefaults();
                return;
            }

            Microsoft.SqlServer.Management.Smo.Agent.Operator currentOperator = GetCurrentOperator();

            this.lastEmailDate = currentOperator.LastEmailDate;
            this.lastPagerDate = currentOperator.LastPagerDate;            

        }
        #endregion

        #region saving
        /// <summary>
        /// apply any changes to the operator. If the operator does not exist create it.
        /// </summary>
        public void ApplyChanges()
        {
            // do nothing if we are read only
            if (this.readOnly)
            {
                return;
            }

            JobServer jobServer = GetJobServer();

            // get the operator. This will create a new one if it does not already exist
            Microsoft.SqlServer.Management.Smo.Agent.Operator currentOperator = GetCurrentOperator();

            // general tab
            currentOperator.Enabled = this.enabled;
            currentOperator.EmailAddress = this.emailAddress;
            currentOperator.PagerAddress = this.pagerAddress;

            currentOperator.PagerDays = this.pagerDays;

            if((this.pagerDays & WeekDays.WeekDays) > 0)
            {
                currentOperator.WeekdayPagerStartTime = ConvertAgentTime(this.weekdayStartTime);
                currentOperator.WeekdayPagerEndTime = ConvertAgentTime(this.weekdayEndTime);
            }
            if((this.pagerDays & WeekDays.Saturday) > 0)
            {
                currentOperator.SaturdayPagerStartTime = ConvertAgentTime(this.saturdayStartTime);
                currentOperator.SaturdayPagerEndTime = ConvertAgentTime(this.saturdayEndTime);
            }
            if((this.pagerDays & WeekDays.Sunday) > 0)
            {
                currentOperator.SundayPagerStartTime = ConvertAgentTime(this.sundayStartTime);
                currentOperator.SundayPagerEndTime = ConvertAgentTime(this.sundayEndTime);
            }

            if(this.createMode)
            {
                // create the object
                currentOperator.Create();
                this.originalOperatorName = this.name;
            }
            else
            {
                // alter the object
                currentOperator.Alter();
            }

            // only set this up if the notifications has been set
            if(this.alertNotifications != null)
            {
                NotifyMethods notifyMethods;
                for(int i = 0; i < alertNotifications.Count; ++i)
                {
                    notifyMethods = 0;

                    if(alertNotifications[i].NotifyEmail)
                    {
                        notifyMethods |= NotifyMethods.NotifyEmail;
                    }
                    if(alertNotifications[i].NotifyPager)
                    {
                        notifyMethods |= NotifyMethods.Pager;
                    }
                    
                    bool alertAlreadyNotifiesOperator = false;

                    // if we're not creating see if the current alert already notifies this operator
                    if(!createMode)
                    {
                        DataTable notifications = alertNotifications[i].Alert.EnumNotifications(this.originalOperatorName);
                        if(notifications.Rows.Count > 0)
                        {
                            alertAlreadyNotifiesOperator = true;
                        }
                    }

                    // either update or clear existing notifications
                    if(alertAlreadyNotifiesOperator)
                    {
                          if(notifyMethods != NotifyMethods.None)
                        {
                            alertNotifications[i].Alert.UpdateNotification(this.originalOperatorName, notifyMethods);
                        }
                        else
                        {
                            alertNotifications[i].Alert.RemoveNotification(this.originalOperatorName);
                        }
                    }
                    else if(notifyMethods != NotifyMethods.None)
                    {
                        // add a new notification
                        alertNotifications[i].Alert.AddNotification(this.originalOperatorName, notifyMethods);
                    }
                }
            }

            // see if we need to rename. This has to be done last otherwise any scripts generated will be incorrect.
            if(!this.createMode && currentOperator.Name != this.originalOperatorName)
            {
                currentOperator.Rename(this.name);
                if(this.dataContainer.Server.ConnectionContext.SqlExecutionModes != SqlExecutionModes.CaptureSql)
                {
                    this.originalOperatorName = this.name;
                }
            }
            // update state if we aren't scripting
            if(this.createMode && this.dataContainer.Server.ConnectionContext.SqlExecutionModes != SqlExecutionModes.CaptureSql)
            {
                this.createMode = false;
            }
        }
        #endregion

        #region reset
        /// <summary>
        /// Reset the object to it's original state / reload any data from the erver
        /// </summary>
        public void Reset()
        {
            JobServer jobServer = GetJobServer();
            this.generalInitialized = false;
            if(this.jobNotifications != null)
            {
                // ensure the individual jobs are reset also
                jobServer.Jobs.Refresh(true);
                this.jobNotifications = null;
            }
            if(this.alertNotifications != null)
            {
                // ensure the individual jobs are reset also
                jobServer.Alerts.Refresh(true);
                this.alertNotifications = null;
            }
            this.historyInitialized = false;
        }
        #endregion

        #region defaults
        /// <summary>
        /// set general tab defaults
        /// </summary>
        private void LoadGeneralDefaults()
        {
            name = String.Empty;
            this.emailAddress = String.Empty;
            this.pagerAddress = String.Empty;
            enabled = true;
            pagerDays = 0;

            weekdayStartTime = saturdayStartTime = sundayStartTime = new DateTime(2000, 1, 1, 8, 0, 0);
            weekdayEndTime = saturdayEndTime = sundayEndTime = new DateTime(2000, 1, 1, 18, 0, 0);

            this.generalInitialized = true;
        }
        /// <summary>
        /// Set job notification defaults. This is just an empty list
        /// </summary>
        private void LoadJobNotificationDefaults()
        {
            this.jobNotifications = new List<AgentJobNotificationHelper>();
        }
        /// <summary>
        /// set the alert notification defaults. This list will contain all of the alerts
        /// </summary>
        private void LoadAlertNotificationDefaults()
        {
            this.alertNotifications = new List<AgentAlertNotificationHelper>();

            JobServer jobServer = GetJobServer();

            AgentAlertNotificationHelper alertNotification;
            foreach(Alert alert in jobServer.Alerts)
            {
                alertNotification = new AgentAlertNotificationHelper(alert.Name, notifyEmail:false, notifyPager:false, alert: alert);
                this.alertNotifications.Add(alertNotification);
            }
        }
        /// <summary>
        /// load defaults for the history page
        /// </summary>
        private void LoadHistoryDefaults()
        {
            this.lastEmailDate = DateTime.MinValue;
            this.lastPagerDate = DateTime.MinValue;
            
            this.historyInitialized = true;
        }
        #endregion

        #region helpers
        /// <summary>
        /// Get the job server. Will throw if it is not available
        /// </summary>
        /// <returns>Job server object</returns>
        private JobServer GetJobServer()
        {
            JobServer jobServer = this.dataContainer.Server.JobServer;
            if(jobServer == null)
            {
                throw new ApplicationException("AgentOperatorsSR.JobServerIsNotAvailable");
            }
            return jobServer;
        }
        /// <summary> 
        /// Get the current operator. If we are creating this will be a new operator. If we are modifying
        /// an existing operator it will be the existing operator, and will throw if the operator has been 
        /// deleted.
        /// </summary>
        /// <returns>Operator object</returns>
        private Microsoft.SqlServer.Management.Smo.Agent.Operator GetCurrentOperator()
        {
            JobServer jobServer = GetJobServer();

            Microsoft.SqlServer.Management.Smo.Agent.Operator currentOperator;

            // new object in create mode
            if(this.createMode)
            {
                currentOperator = new Microsoft.SqlServer.Management.Smo.Agent.Operator(jobServer, this.name);
            }
            else
            {
                currentOperator = jobServer.Operators[this.originalOperatorName];
                // throw if the operator has been deleted already
                if(currentOperator == null)
                {
                    throw new ApplicationException("SRError.OperatorDoesNotExist(this.originalOperatorName)");
                }
            }
            return currentOperator;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public TimeSpan ConvertAgentTime(DateTime dateTime)
        {
            return new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public DateTime ConvertAgentTime(TimeSpan dateTime)
        {
            return new DateTime(2000, 1, 1, dateTime.Hours, dateTime.Minutes, dateTime.Seconds);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public int ConvertAgentTimeToInt(DateTime dateTime)
        {
            return dateTime.Hour * 10000 + dateTime.Minute * 100 + dateTime.Second;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public DateTime ConvertAgentTime(int dateTime)
        {
            return new DateTime(2000, 1, 1, (int)(dateTime / 10000), (int)((dateTime - (dateTime / 10000) * 10000) / 100), (int)(dateTime - (dateTime / 100) * 100));
        }
        #endregion
    }
}
