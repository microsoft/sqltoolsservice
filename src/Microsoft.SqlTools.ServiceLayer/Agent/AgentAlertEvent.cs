using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;

#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// AgentAlertEvent user control
    /// </summary>
    internal class AgentAlertEvent
    {
        #region Members

        
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        /// <summary>
        /// We do lazy initialization of database name drop down because we want dialog to open
        /// faster and not to enumerate thousands of databases if we don't need to
        /// </summary>
        private bool databaseNameInitialized = false;
        /// <summary>
        /// DataContainer
        /// </summary>
        private CDataContainer dataContainer;
        /// <summary>
        /// true if controls have been initialized
        /// </summary>
        private bool controlsInitialized = false;
        /// <summary>
        /// Agent alert being edited
        /// </summary>
        private string agentAlertName = null;
        /// <summary>
        /// Indicates if the controls should be read only
        /// </summary>
        private bool readOnly = false;
        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor is hidden
        /// </summary>
        private AgentAlertEvent()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataContainer"></param>
        public AgentAlertEvent(CDataContainer dataContainer, string agentAlertName, bool readOnly)
        {
            if (dataContainer == null)
                throw new ArgumentNullException("dataContainer");

            this.dataContainer = dataContainer;
            this.agentAlertName = agentAlertName;
            this.readOnly = readOnly;

            LoadSeverityCombo();
        }

        #endregion

        #region Overrides

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing)
        //     {
        //         if (components != null)
        //         {
        //             components.Dispose();
        //         }
        //     }
        //     base.Dispose(disposing);
        // }

        #endregion

        #region Public methods

        /// <summary>
        /// Resets child control values to default ones
        /// </summary>
        public void Reset()
        {
            InitializeControls(true);
        }

        /// <summary>
        /// Updates alert fields with data from this page
        /// </summary>
        /// <param name="alert"></param>
        public void UpdateAlert(Alert alert)
        {
            // if (alert == null)
            //     throw new ArgumentNullException("alert");

            // if (this.agentAlertName != null)
            //     this.agentAlertName = alert.Name;

            // if (this.databaseName.Text == AgentAlertEventSR.AllDatabases)
            //     alert.DatabaseName = "";
            // else
            //     alert.DatabaseName = this.databaseName.Text;

            // if (this.errorNumberRadio.Checked)
            // {
            //     alert.Severity = 0;
            //     alert.MessageID = Convert.ToInt32(this.errorNumber.Text, CultureInfo.CurrentCulture);
            // }
            // else
            // {
            //     alert.Severity = this.severity.SelectedIndex + 1;
            //     alert.MessageID = 0;
            // }

            // if (this.raiseAlertWhen.Checked)
            //     alert.EventDescriptionKeyword = this.messageText.Text;
            // else
            //     alert.EventDescriptionKeyword = "";
        }

        /// <summary>
        /// Clears fields of alert that this page is responsible for
        /// </summary>
        /// <param name="alert"></param>
        static public void ClearAlert(Alert alert)
        {
            if (alert == null)
                throw new ArgumentNullException("alert");

            alert.DatabaseName = "";
            alert.Severity = 0;
            alert.MessageID = 0;
            alert.EventDescriptionKeyword = "";
        }

        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {           
        }
        #endregion

        #region Event handlers

      

        #endregion

        #region Private

        private void LoadSeverityCombo()
        {
            // this.severity.Items.AddRange(new Object[] { 
            //          AgentAlertEventSR.Severity001
            //         ,AgentAlertEventSR.Severity002
            //         ,AgentAlertEventSR.Severity003
            //         ,AgentAlertEventSR.Severity004
            //         ,AgentAlertEventSR.Severity005
            //         ,AgentAlertEventSR.Severity006
            //         ,AgentAlertEventSR.Severity007
            //         ,AgentAlertEventSR.Severity008
            //         ,AgentAlertEventSR.Severity009
            //         ,AgentAlertEventSR.Severity010
            //         ,AgentAlertEventSR.Severity011
            //         ,AgentAlertEventSR.Severity012
            //         ,AgentAlertEventSR.Severity013
            //         ,AgentAlertEventSR.Severity014
            //         ,AgentAlertEventSR.Severity015
            //         ,AgentAlertEventSR.Severity016
            //         ,AgentAlertEventSR.Severity017
            //         ,AgentAlertEventSR.Severity018
            //         ,AgentAlertEventSR.Severity019
            //         ,AgentAlertEventSR.Severity020
            //         ,AgentAlertEventSR.Severity021
            //         ,AgentAlertEventSR.Severity022
            //         ,AgentAlertEventSR.Severity023
            //         ,AgentAlertEventSR.Severity024
            //         ,AgentAlertEventSR.Severity025 });
        }

        /// <summary>
        /// Initializes controls with data from alert if dialog in modify mode or resets controls values if
        /// dialog in new mode
        /// </summary>
        /// <param name="refresh"></param>
        void InitializeControls(bool refresh)
        {
            // if (refresh == false && this.controlsInitialized)
            //     return;

            // if (this.agentAlertName == null)
            // {
            //     int index = this.databaseName.FindStringExact(AgentAlertEventSR.AllDatabases, 0);
            //     if (index < 0)
            //         index = this.databaseName.Items.Add(AgentAlertEventSR.AllDatabases);
            //     this.databaseName.SelectedIndex = index;
            //     this.errorNumber.Text = "1";
            //     this.severityRadio.Checked = true;
            //     this.severity.SelectedIndex = 0;
            //     this.raiseAlertWhen.Checked = false;
            // }
            // else
            // {
            //     Alert agentAlert = this.dataContainer.Server.JobServer.Alerts[this.agentAlertName];

            //     if (this.databaseNameInitialized)
            //     {
            //         int index = 0;

            //         if (agentAlert.DatabaseName.Length == 0)
            //         {
            //             index = this.databaseName.FindStringExact(AgentAlertEventSR.AllDatabases, 0);
            //             if (index < 0)
            //                 index = this.databaseName.Items.Add(AgentAlertEventSR.AllDatabases);
            //         }
            //         else
            //         {
            //             index = this.databaseName.FindStringExact(agentAlert.DatabaseName, 0);
            //             if (index < 0)
            //                 index = this.databaseName.Items.Add(agentAlert.DatabaseName);
            //         }
            //         this.databaseName.SelectedIndex = index;
            //     }
            //     else
            //     {
            //         int index = 0;
            //         if (agentAlert.DatabaseName.Length == 0)
            //             index = this.databaseName.Items.Add(AgentAlertEventSR.AllDatabases);
            //         else
            //             index = this.databaseName.Items.Add(agentAlert.DatabaseName);
            //         this.databaseName.SelectedIndex = index;
            //     }
            //     if (agentAlert.MessageID == 0)
            //     {
            //         this.severityRadio.Checked = true;
            //         if (agentAlert.Severity < 0 || agentAlert.Severity > this.severity.Items.Count)
            //             throw new ApplicationException(AgentAlertEventSR.UnknownSeverity(agentAlert.Severity));
            //         this.severity.SelectedIndex = agentAlert.Severity - 1;
            //     }
            //     else
            //     {
            //         this.errorNumberRadio.Checked = true;
            //         this.errorNumber.Text = agentAlert.MessageID.ToString(CultureInfo.CurrentCulture);
            //     }
            //     if (agentAlert.EventDescriptionKeyword.Length == 0)
            //     {
            //         this.raiseAlertWhen.Checked = false;
            //     }
            //     else
            //     {
            //         this.raiseAlertWhen.Checked = true;
            //         this.messageText.Text = agentAlert.EventDescriptionKeyword;
            //     }
            // }

            // if (this.readOnly)
            // {
            //     this.databaseName.Enabled = false;
            //     this.errorNumber.ReadOnly = true;
            //     this.errorNumberRadio.Enabled = false;
            //     this.severityRadio.Enabled = false;
            //     this.severity.Enabled = false;
            //     this.raiseAlertWhen.Enabled = false;
            // }

            // this.controlsInitialized = true;
        }

        #endregion
    }
}
