//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// AgentAlert class
    /// </summary>
    internal class AgentAlertActions : ManagementActionBase
    {
        /// <summary>
        /// Agent alert info instance
        /// </summary>
        private AgentAlertInfo alertInfo = null;

        private ConfigAction configAction;

        private string originalAlertName = null;

        private JobData jobData = null;

        /// <summary>
        /// Default constructor that will be used to create dialog
        /// </summary>
        /// <param name="dataContainer"></param>
        public AgentAlertActions(
            CDataContainer dataContainer, string originalAlertName, 
            AgentAlertInfo alertInfo, ConfigAction configAction,
            JobData jobData = null)
        {
            this.originalAlertName = originalAlertName;
            this.alertInfo = alertInfo;
            this.DataContainer = dataContainer;
            this.configAction = configAction;
            if (jobData != null)
            {
                this.jobData = jobData;
            }
        }

        private static string GetAlertName(CDataContainer container)
        {
            string alertName = null;
            STParameters parameters = new STParameters();
            parameters.SetDocument(container.Document);
            if (parameters.GetParam("alert", ref alertName) == false || string.IsNullOrWhiteSpace(alertName))
            {
                throw new Exception(SR.AlertNameCannotBeBlank);
            }
            return alertName.Trim();
        }

        /// <summary>
        /// called by ManagementActionBase.PreProcessExecution
        /// </summary>        
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);
            if (this.configAction == ConfigAction.Drop)
            {
                Drop();
            }
            else
            {
                CreateOrUpdate();
            }

            // regular execution always takes place
            return true;
        }   

        public bool Drop()
        {     
            // fail if the user is not in the sysadmin role
            if (!this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin))
            {
                return false;
            }

            string alertName = GetAlertName(this.DataContainer);
            if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName))
            {
                this.DataContainer.Server.JobServer.Alerts.Refresh();
                if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName))
                {
                    Alert alert = this.DataContainer.Server.JobServer.Alerts[alertName];
                    if (alert != null)
                    {
                        alert.DropIfExists();
                        if (this.jobData != null)
                        {
                            JobAlertData alertData = new JobAlertData(alert);
                            this.jobData.JobAlerts.DeleteAlert(alertData);
                        }
                    }
                }
            }
            return true;
        }

        public bool CreateOrUpdate()
        {          
            Alert alert = null;            
            string alertName = GetAlertName(this.DataContainer);
            bool createNewAlert = true;

            try
            {
                // check if alert already exists
                if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName))
                {
                    this.DataContainer.Server.JobServer.Alerts.Refresh(); // Try to recover
                    if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName)) // If still no luck
                    {
                        // use the existing alert
                        alert = this.DataContainer.Server.JobServer.Alerts[alertName];
                        createNewAlert = false;
                    }
                }

                // create a new alert
                if (createNewAlert)
                {
                    alert = new Alert(this.DataContainer.Server.JobServer, alertName);
                }

                // apply changes from input parameter to SMO alert object
                UpdateAlertProperties(alert);

                if (createNewAlert)
                {
                    alert.Create();
                    if (this.jobData != null)
                    {
                        JobAlertData alertData = new JobAlertData(alert);
                        this.jobData.JobAlerts.AddAlert(alertData);
                    }
                }
                else
                {
                    // don't bother trying to update the alert unless they are sysadmin.   
                    if (!this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin))
                    {
                        return false;
                    }
                    alert.Alter();

                    if (!string.IsNullOrWhiteSpace(this.alertInfo.Name) && !string.Equals(alert.Name, this.alertInfo.Name)) 
                    {
                        alert.Rename(this.alertInfo.Name);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                ApplicationException applicationException;
                if (createNewAlert)
                {
                    applicationException = new ApplicationException(SR.CannotCreateNewAlert, e);
                }
                else
                {
                    applicationException = new ApplicationException(SR.CannotAlterAlert, e);
                }
                throw applicationException;
            }
        }

        private void UpdateAlertProperties(Alert alert)
        {
            if (alert == null)
            {
                throw new ArgumentNullException("alert");
            }
            if (this.alertInfo.JobId != null)
            {
                alert.JobID = new Guid(this.alertInfo.JobId);
            }

            alert.DatabaseName = !string.IsNullOrWhiteSpace(this.alertInfo.DatabaseName)
                ? this.alertInfo.DatabaseName :  string.Empty;

            if (!string.IsNullOrWhiteSpace(this.alertInfo.CategoryName))
            {
                alert.CategoryName = this.alertInfo.CategoryName;
            }

            alert.IsEnabled = this.alertInfo.IsEnabled;
            
            if (alertInfo.AlertType == Contracts.AlertType.SqlServerEvent)
            {
                if (this.alertInfo.MessageId.HasValue && this.alertInfo.MessageId > 0)
                {
                    alert.Severity = 0;
                    alert.MessageID = this.alertInfo.MessageId.Value;
                }
                else if (this.alertInfo.Severity.HasValue && this.alertInfo.Severity > 0)
                {
                    alert.Severity = this.alertInfo.Severity.Value;
                    alert.MessageID = 0;
                }

                if (!string.IsNullOrWhiteSpace(this.alertInfo.EventDescriptionKeyword))
                {
                    alert.EventDescriptionKeyword = this.alertInfo.EventDescriptionKeyword;
                }

                // clear out other alert type fields
                alert.PerformanceCondition	= string.Empty;
                alert.WmiEventNamespace = string.Empty;
                alert.WmiEventQuery = string.Empty;
            }
        }
    }
}
