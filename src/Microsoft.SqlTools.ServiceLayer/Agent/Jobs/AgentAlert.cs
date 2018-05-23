//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// AgentAlert class
	/// </summary>
	internal class AgentAlert : AgentConfigurationBase
	{
        /// <summary>
        /// Agent alert info instance
        /// </summary>
        private AgentAlertInfo alertInfo = null;

        /// <summary>
        /// Default constructor that will be used to create dialog
        /// </summary>
        /// <param name="dataContainer"></param>
        public AgentAlert(CDataContainer dataContainer, AgentAlertInfo alertInfo)
        {
            this.alertInfo = alertInfo;
            this.DataContainer = dataContainer;
        }

        private static string GetAlertName(CDataContainer container)
        {
            string alertName = null;
            STParameters parameters = new STParameters();
            parameters.SetDocument(container.Document);
            if (parameters.GetParam("alert", ref alertName) == false || string.IsNullOrWhiteSpace(alertName))
            {
                throw new Exception("SRError.AlertNameCannotBeBlank");
            }
            return alertName.Trim();
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
                }
                else
                {
                    // don't bother trying to update the alert unless they are sysadmin.   
                    if (!this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin))
                    {
                        return false;
                    }
                    alert.Alter();
                }

                return true;
            }
            catch (Exception e)
            {
                ApplicationException applicationException;
                if (createNewAlert)
                {
                    applicationException = new ApplicationException("AgentAlertSR.CannotCreateNewAlert", e);
                }
                else
                {
                    applicationException = new ApplicationException("AgentAlertSR.CannotAlterAlert", e);
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

            if (!string.IsNullOrWhiteSpace(this.DataContainer.ConnectionInfo.DatabaseName))
            {
                alert.DatabaseName = this.DataContainer.ConnectionInfo.DatabaseName;
            }

            if (!string.IsNullOrWhiteSpace(this.alertInfo.CategoryName))
            {
                alert.CategoryName = this.alertInfo.CategoryName;
            }

            alert.IsEnabled = this.alertInfo.IsEnabled;
            
            if (alertInfo.AlertType == Contracts.AlertType.SqlServerEvent)
            {
                alert.Severity = this.alertInfo.Severity;
                alert.MessageID = this.alertInfo.MessageId;
                if (!string.IsNullOrWhiteSpace(this.alertInfo.EventDescriptionKeyword))
                {
                    alert.EventDescriptionKeyword = this.alertInfo.EventDescriptionKeyword;
                }
            }
        }
    }
}
