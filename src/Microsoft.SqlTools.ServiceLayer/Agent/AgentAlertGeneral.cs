using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;

#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Agent alert general page
    /// </summary>
    internal class AgentAlertGeneral : AgentControlBase
    {
        #region Members

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.TextBox name;
        // private System.Windows.Forms.Label nameLabel;
        // private System.Windows.Forms.CheckBox nameEnabled;
        // private System.Windows.Forms.Label typeLabel;
        // private System.Windows.Forms.ComboBox types;
        // private Microsoft.SqlServer.Management.Controls.Separator alertTypeSeparator;
        /// <summary>
        /// Agent alert being edited
        /// </summary>
        private string agentAlertName;
        /// <summary>
        /// Cached Event alert definition control. Do not access this field directly.
        /// Use EventAlertDefinitionControl property because we will create it on demand
        /// </summary>
        // private AgentAlertEvent eventAlertDefinitionControl = null;
        // /// <summary>
        // /// Cached Event alert definition control. Do not access this field directly.
        // /// Use PerformanceAlertDefinitionControl property because we will create it on demand
        // /// </summary>
        // private AgentAlertPerformance performanceAlertDefinitionControl = null;
        // /// <summary>
        // /// Cached WMI alert definition control. Do not access this field directly.
        // /// Use WmiAlertDefinitionControl property because we will create it on demand
        // /// </summary>
        // private AgentAlertWMI wmiAlertDefinitionControl = null;
        // /// <summary>
        // /// Reference to one of eventAlertDefinitionControl, performanceAlertDefinitionControl or wmiAlertDefinitionControl fields
        // /// </summary>
        // private UserControl activeAlertDefinitionControl = null;
        /// <summary>
        /// true if child controls have been initialized
        /// </summary>
        private bool controlInitialized = false;

        // true if the control should be read only
        private bool readOnly = false;

        #endregion

        #region Constructors

        private AgentAlertGeneral()
        {
            // This call is required by the Windows.Forms Form Designer.
            //InitializeComponent();

            // this.name.MaxLength = SqlLimits.SysName;
            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.alert.general.f1";
        }

        public AgentAlertGeneral(CDataContainer dataContainer, string agentAlertName)
            : this()
        {
            if (dataContainer == null)
                throw new ArgumentNullException("dataContainer");

            DataContainer = dataContainer;
            // //this.AllUIEnabled = false;
            // this.agentAlertName = agentAlertName; // agentAlert can be null if dialog runs in create new alert mode


            // Version version = dataContainer.Server.Information.Version;
            // var resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentAlertGeneral));
            // // SQL Server event alert
            // this.types.Items.Add(resources.GetString("types.Items"));
            // if (dataContainer.Server.HostPlatform == Microsoft.SqlServer.Management.Common.HostPlatformNames.Windows)
            // {
            //     // SQL Server Performance condition alert
            //     this.types.Items.Add(resources.GetString("types.Items1"));
            //     if (version.Major >= 9 && dataContainer.Server.Information.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
            //     {
            //         // WMI event alert
            //         this.types.Items.Add(resources.GetString("types.Items2"));
            //     }            
            // }
            // InitializeControls(false);
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
        /// Updates alert fields with data from this page
        /// </summary>
        /// <param name="alert"></param>
        public void UpdateAlert(Alert alert)
        {
            // if (alert == null)
            //     throw new ArgumentNullException("alert");

            // Version version = DataContainer.Server.Information.Version;

            // string alertName = this.AgentName.Trim();

            // if (this.agentAlertName != null)
            // {
            //     this.agentAlertName = alertName;
            // }

            // alert.IsEnabled = this.nameEnabled.Checked;
            // // alert.Description	= this.description.Text bug filed
            // switch (this.types.SelectedIndex)
            // {
            //     case 0: // server event
            //         if (alert.State != SqlSmoState.Creating)
            //         {
            //             AgentAlertPerformance.ClearAlert(alert);
            //             if (version.Major >= 9)
            //             {
            //                 AgentAlertWMI.ClearAlert(alert);
            //             }
            //         }

            //         this.eventAlertDefinitionControl.UpdateAlert(alert);
            //         break;
            //     case 1: // perf counter
            //         if (alert.State != SqlSmoState.Creating)
            //         {
            //             AgentAlertEvent.ClearAlert(alert);
            //             if (version.Major >= 9)
            //             {
            //                 AgentAlertWMI.ClearAlert(alert);
            //             }
            //         }

            //         this.performanceAlertDefinitionControl.UpdateAlert(alert);
            //         break;
            //     case 2: // wmi event
            //         if (alert.State != SqlSmoState.Creating)
            //         {
            //             AgentAlertEvent.ClearAlert(alert);
            //             AgentAlertPerformance.ClearAlert(alert);
            //         }
            //         this.wmiAlertDefinitionControl.UpdateAlert(alert);
            //         break;
            //     default:
            //         throw new ApplicationException(AgentAlertGeneralSR.UnknownAlertType);
            // }
            // if (alert.Name != alertName)
            // {
            //     alert.Rename(alertName);
            // }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Alert name
        /// </summary>
        // public string AgentName
        // {
        //     get { return this.name.Text; }
        // }

        #endregion

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentAlertGeneral));
        //     this.nameLabel = new System.Windows.Forms.Label();
        //     this.name = new System.Windows.Forms.TextBox();
        //     this.nameEnabled = new System.Windows.Forms.CheckBox();
        //     this.typeLabel = new System.Windows.Forms.Label();
        //     this.types = new System.Windows.Forms.ComboBox();
        //     this.alertTypeSeparator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // nameLabel
        //     // 
        //     resources.ApplyResources(this.nameLabel, "nameLabel");
        //     this.nameLabel.Name = "nameLabel";
        //     // 
        //     // name
        //     // 
        //     resources.ApplyResources(this.name, "name");
        //     this.name.Name = "name";
        //     this.name.TextChanged += new System.EventHandler(this.OnNameChanged);
        //     // 
        //     // nameEnabled
        //     // 
        //     resources.ApplyResources(this.nameEnabled, "nameEnabled");
        //     this.nameEnabled.Checked = true;
        //     this.nameEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
        //     this.nameEnabled.Name = "nameEnabled";
        //     // 
        //     // typeLabel
        //     // 
        //     resources.ApplyResources(this.typeLabel, "typeLabel");
        //     this.typeLabel.Name = "typeLabel";
        //     // 
        //     // types
        //     // 
        //     resources.ApplyResources(this.types, "types");
        //     this.types.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.types.FormattingEnabled = true;            
        //     this.types.Name = "types";
        //     this.types.SelectedIndexChanged += new System.EventHandler(this.OnTypesSelectedIndexChanged);
        //     // 
        //     // alertTypeSeparator
        //     // 
        //     resources.ApplyResources(this.alertTypeSeparator, "alertTypeSeparator");
        //     this.alertTypeSeparator.Name = "alertTypeSeparator";
        //     // 
        //     // AgentAlertGeneral
        //     // 
        //     this.Controls.Add(this.nameLabel);
        //     this.Controls.Add(this.name);
        //     this.Controls.Add(this.nameEnabled);
        //     this.Controls.Add(this.typeLabel);
        //     this.Controls.Add(this.types);
        //     this.Controls.Add(this.alertTypeSeparator);
        //     this.Name = "AgentAlertGeneral";
        //     resources.ApplyResources(this, "$this");
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region Event handlers

        private void OnTypesSelectedIndexChanged(object sender, System.EventArgs e)
        {
            // if (this.activeAlertDefinitionControl != null)
            // {
            //     this.activeAlertDefinitionControl.Visible = false;
            //     this.activeAlertDefinitionControl = null;
            // }

            // switch (this.types.SelectedIndex)
            // {
            //     case 0:
            //         this.alertTypeSeparator.Text = AgentAlertGeneralSR.EventAlertDefinition;
            //         this.activeAlertDefinitionControl = this.EventAlertDefinitionControl;
            //         break;
            //     case 1:
            //         this.alertTypeSeparator.Text = AgentAlertGeneralSR.PerformanceConditionAlertDefinition;
            //         this.activeAlertDefinitionControl = this.PerformanceAlertDefinitionControl;
            //         break;
            //     case 2:
            //         this.alertTypeSeparator.Text = AgentAlertGeneralSR.WmiEventAlertDefinition;
            //         this.activeAlertDefinitionControl = this.WmiAlertDefinitionControl;
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException("SelectedIndex", this.types.SelectedIndex, AgentAlertGeneralSR.UnknownAlertType);
            // }

            // this.activeAlertDefinitionControl.Visible = true;
        }

        // private void OnNameChanged(object sender, System.EventArgs e)
        // {
        //     this.AllUIEnabled = this.name.Text.Length != 0;
        // }

        #endregion

        #region Private Properties

        /// <summary>
        /// returns AgentAlertEvent control
        /// </summary>
        // private AgentAlertEvent EventAlertDefinitionControl
        // {
        //     get
        //     {
        //         if (this.eventAlertDefinitionControl == null)
        //         {
        //             this.eventAlertDefinitionControl = new AgentAlertEvent(DataContainer, this.agentAlertName, this.readOnly);
        //             InitializeAlertDefinitionControl(this.eventAlertDefinitionControl);
        //         }

        //         return this.eventAlertDefinitionControl;
        //     }
        // }

        // /// <summary>
        // /// returns AgentAlertPerformance control
        // /// </summary>
        // private AgentAlertPerformance PerformanceAlertDefinitionControl
        // {
        //     get
        //     {
        //         if (this.performanceAlertDefinitionControl == null)
        //         {
        //             this.performanceAlertDefinitionControl = new AgentAlertPerformance(DataContainer, this.agentAlertName, this.readOnly);
        //             InitializeAlertDefinitionControl(this.performanceAlertDefinitionControl);
        //         }

        //         return this.performanceAlertDefinitionControl;
        //     }
        // }

        // /// <summary>
        // /// returns AgentAlertWMI control
        // /// </summary>
        // private AgentAlertWMI WmiAlertDefinitionControl
        // {
        //     get
        //     {
        //         if (this.wmiAlertDefinitionControl == null)
        //         {
        //             this.wmiAlertDefinitionControl = new AgentAlertWMI(DataContainer, this.agentAlertName, this.readOnly);
        //             InitializeAlertDefinitionControl(this.wmiAlertDefinitionControl);
        //         }

        //         return this.wmiAlertDefinitionControl;
        //     }
        // }

        #endregion

        #region Private helpers

        /// <summary>
        /// Initializes controls with data from alert
        /// </summary>
        /// <param name="refresh">true if child controls need to be refreshed</param>
        void InitializeControls(bool refresh)
        {
            // if (refresh == false && this.controlInitialized)
            //     return;

            // if (this.agentAlertName == null)
            // {
            //     this.name.Text = "";
            //     this.nameEnabled.Checked = true;
            //     this.types.SelectedIndex = 0;
            // }
            // else
            // {
            //     Alert agentAlert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];

            //     this.readOnly = !this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);

            //     this.name.Text = agentAlert.Name;
            //     this.name.SelectionLength = 0;
            //     this.nameEnabled.Checked = agentAlert.IsEnabled;
            //     // this.description.Text		= this.agentAlert.Description; TODO SMO doesn't have Description property yet
            //     switch (agentAlert.AlertType)
            //     {
            //         // NonSqlServerEvent used to be invalid. However with Shiloh all alerts on a named instance return as
            //         // this.
            //         case AlertType.SqlServerEvent:
            //         case AlertType.NonSqlServerEvent:
            //             this.types.SelectedIndex = 0;
            //             if (refresh)
            //                 this.eventAlertDefinitionControl.Reset();
            //             break;
            //         case AlertType.SqlServerPerformanceCondition:
            //             this.types.SelectedIndex = 1;
            //             if (refresh)
            //                 this.performanceAlertDefinitionControl.Reset();
            //             break;
            //         case AlertType.WmiEvent:
            //             this.types.SelectedIndex = 2;
            //             if (refresh)
            //                 this.wmiAlertDefinitionControl.Reset();
            //             break;
            //         default: throw new ApplicationException(AgentAlertGeneralSR.UnknownAlertType);
            //     }

            //     if (this.readOnly)
            //     {
            //         SetDialogFieldsReadOnly(true,
            //             new Control[] {
            //                 this.name,
            //                 this.nameEnabled,
            //                 this.types
            //             }
            //         );
            //     }
            // }

            // this.controlInitialized = true;
        }

        /// <summary>
        /// Initialized alert difinition control and adds it to collection of child controls
        /// </summary>
        /// <param name="control"></param>
        // void InitializeAlertDefinitionControl(UserControl control)
        // {
        //     // if (control == null)
        //     //     throw new ArgumentNullException("control");

        //     // control.Location = new Point(0, this.alertTypeSeparator.Location.Y + alertTypeSeparator.Size.Height + 4);
        //     // control.Size = new Size(this.ClientSize.Width - 4, this.ClientSize.Height - this.alertTypeSeparator.Location.Y - alertTypeSeparator.Size.Height - 4);
        //     // control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

        //     // this.Controls.Add(control);
        // }

        #endregion

    }
}
