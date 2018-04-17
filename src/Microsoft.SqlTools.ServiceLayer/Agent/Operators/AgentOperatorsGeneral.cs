using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;
#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// General page on Agent Operators Dialog
    /// </summary>
    internal class AgentOperatorsGeneral : AgentControlBase
    {
        #region Members
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Label nameLabel;
        // private System.Windows.Forms.TextBox name;
        // private System.Windows.Forms.CheckBox nameEnabled;
        // private Microsoft.SqlServer.Management.Controls.Separator notificationOptionsSeparator;
        // private System.Windows.Forms.Label emailNameLabel;
        // private System.Windows.Forms.Label pagerEmailNameLabel;
        // private System.Windows.Forms.TextBox emailName;
        // private System.Windows.Forms.TextBox pagerEmailName;
        // private Microsoft.SqlServer.Management.Controls.Separator pagerOnDutySeparator;
        // private CheckBox onDutyMonday;
        // private CheckBox onDutyTuesday;
        // private CheckBox onDutyWednesday;
        // private CheckBox onDutyThursday;
        // private CheckBox onDutyFriday;
        // private CheckBox onDutySaturday;
        // private CheckBox onDutySunday;
        // private DateTimePicker weekdayBegin;
        // private DateTimePicker weekdayEnd;
        // private DateTimePicker saturdayBegin;
        // private DateTimePicker saturdayEnd;
        // private DateTimePicker sundayBegin;
        // private DateTimePicker sundayEnd;
        // private Microsoft.SqlServer.Management.Controls.Separator saturdaySeparator;
        // private Microsoft.SqlServer.Management.Controls.Separator sundaySeparator;
        // private Label beginLabel;
        // private Label endLabel;

        /// <summary>
        /// Agent operator that will be modified
        /// </summary>
        private AgentOperatorsData operatorsData = null;
        //private TableLayoutPanel tableLayoutPanel1;
        /// <summary>
        /// true if controls were initialized with agent operator properties or false otherwise
        /// </summary>
        private bool controlsInitialized = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default public constructor
        /// </summary>
        private AgentOperatorsGeneral()
        {
            // // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // // Put some limits
            // this.name.MaxLength = SqlLimits.SysName;
            // this.emailName.MaxLength = 256;
            // this.pagerEmailName.MaxLength = 256;

            // this.nameEnabled.Checked = true;
        }

        public AgentOperatorsGeneral(CDataContainer dataContainer, AgentOperatorsData operatorsData)
            : this()
        {
            if(dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }
            if(operatorsData == null)
            {
                throw new ArgumentNullException("operatorsData");
            }
            DataContainer = dataContainer;
            this.operatorsData = operatorsData;

            // this.AllUIEnabled = false;

            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.operator.general.f1";

            // InitializeControls(false);
        }

        #endregion

        #region Event handlers
        // private void OnNameChanged(object sender, System.EventArgs e)
        // {
        //     string operatorName = this.name.Text;
        //     this.AllUIEnabled = operatorName.Trim().Length != 0;
        // }
        // private void OnDutyCheckedChanged(object sender, EventArgs e)
        // {
        //     UpdateControlEnabledStatus();
        // }

        #endregion

        #region Overrides

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // if(components != null)
                // {
                //     components.Dispose();
                // }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region IPanelForm

        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }

        // public override void OnReset(object sender)
        // {
        //     if (!operatorsData.ReadOnly)
        //     {
        //         InitializeControls(true);
        //     }
        //     base.OnReset(sender);
        // }

        // public override void OnGatherUiInformation(RunType action)
        // {
        //     STrace.Assert(this.operatorsData != null);
        //     if(this.operatorsData == null)
        //     {
        //         throw new InvalidOperationException();
        //     }

        //     string operatorName = this.name.Text.Trim();
        //     if(operatorName == null || operatorName.Length == 0)
        //     {
        //         throw new Exception(SRError.OperatorNameCannotBeBlank);
        //     }

        //     this.operatorsData.Name = operatorName;
        //     this.operatorsData.Enabled = this.nameEnabled.Checked;
        //     this.operatorsData.EmailAddress = this.emailName.Text;
        //     this.operatorsData.PagerAddress = this.pagerEmailName.Text;

        //     WeekDays pagerDays = 0;

        //     if(this.onDutyMonday.Checked)
        //         pagerDays |= WeekDays.Monday;
        //     if(this.onDutyTuesday.Checked)
        //         pagerDays |= WeekDays.Tuesday;
        //     if(this.onDutyWednesday.Checked)
        //         pagerDays |= WeekDays.Wednesday;
        //     if(this.onDutyThursday.Checked)
        //         pagerDays |= WeekDays.Thursday;
        //     if(this.onDutyFriday.Checked)
        //         pagerDays |= WeekDays.Friday;
        //     if((pagerDays & WeekDays.WeekDays) > 0)
        //     {
        //         this.operatorsData.WeekdayStartTime = this.weekdayBegin.Value;
        //         this.operatorsData.WeekdayEndTime = this.weekdayEnd.Value;
        //     }
        //     if(this.onDutySaturday.Checked)
        //     {
        //         pagerDays |= WeekDays.Saturday;
        //         this.operatorsData.SaturdayStartTime = this.saturdayBegin.Value;
        //         this.operatorsData.SaturdayEndTime = this.saturdayEnd.Value;
        //     }
        //     if(this.onDutySunday.Checked)
        //     {
        //         pagerDays |= WeekDays.Sunday;
        //         this.operatorsData.SundayStartTime = this.sundayBegin.Value;
        //         this.operatorsData.SundayEndTime = this.sundayEnd.Value;
        //     }

        //     this.operatorsData.PagerDays = pagerDays;

        // }

        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        // }

        // void IPanelForm.OnInitialization()
        // {
        //     if (operatorsData.ReadOnly)
        //     {
        //         SetDialogFieldsReadOnly(true,
        //             new Control[] {
        //                 name,
        //                 nameEnabled,
        //                 emailName,
        //                 pagerEmailName,
        //                 onDutyMonday,
        //                 onDutyTuesday,
        //                 onDutyWednesday,
        //                 onDutyThursday,
        //                 onDutyFriday,
        //                 onDutySaturday,
        //                 onDutySunday,
        //                 weekdayBegin,
        //                 weekdayEnd,
        //                 saturdayBegin,
        //                 saturdayEnd,
        //                 sundayBegin,
        //                 sundayEnd
        //             });
        //     }

        // }

        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
        // }

        // #endregion

        // #region Component Designer generated code
        // /// <summary> 
        // /// Required method for Designer support - do not modify 
        // /// the contents of this method with the code editor.
        // /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgentOperatorsGeneral));
        //     this.nameLabel = new System.Windows.Forms.Label();
        //     this.name = new System.Windows.Forms.TextBox();
        //     this.nameEnabled = new System.Windows.Forms.CheckBox();
        //     this.notificationOptionsSeparator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.emailNameLabel = new System.Windows.Forms.Label();
        //     this.pagerEmailNameLabel = new System.Windows.Forms.Label();
        //     this.emailName = new System.Windows.Forms.TextBox();
        //     this.pagerEmailName = new System.Windows.Forms.TextBox();
        //     this.pagerOnDutySeparator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.onDutyMonday = new System.Windows.Forms.CheckBox();
        //     this.onDutyTuesday = new System.Windows.Forms.CheckBox();
        //     this.onDutyWednesday = new System.Windows.Forms.CheckBox();
        //     this.onDutyThursday = new System.Windows.Forms.CheckBox();
        //     this.onDutyFriday = new System.Windows.Forms.CheckBox();
        //     this.onDutySaturday = new System.Windows.Forms.CheckBox();
        //     this.onDutySunday = new System.Windows.Forms.CheckBox();
        //     this.weekdayBegin = new System.Windows.Forms.DateTimePicker();
        //     this.weekdayEnd = new System.Windows.Forms.DateTimePicker();
        //     this.saturdayBegin = new System.Windows.Forms.DateTimePicker();
        //     this.saturdayEnd = new System.Windows.Forms.DateTimePicker();
        //     this.sundayBegin = new System.Windows.Forms.DateTimePicker();
        //     this.sundayEnd = new System.Windows.Forms.DateTimePicker();
        //     this.saturdaySeparator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.sundaySeparator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.beginLabel = new System.Windows.Forms.Label();
        //     this.endLabel = new System.Windows.Forms.Label();
        //     this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
        //     this.tableLayoutPanel1.SuspendLayout();
        //     this.SuspendLayout();
        //     // 
        //     // nameLabel
        //     // 
        //     resources.ApplyResources(this.nameLabel, "nameLabel");
        //     this.nameLabel.Name = "nameLabel";
        //     // 
        //     // name
        //     // 
        //     resources.ApplyResources(this.name, "name");
        //     this.tableLayoutPanel1.SetColumnSpan(this.name, 2);
        //     this.name.Name = "name";
        //     // 
        //     // nameEnabled
        //     // 
        //     resources.ApplyResources(this.nameEnabled, "nameEnabled");
        //     this.nameEnabled.Name = "nameEnabled";
        //     // 
        //     // notificationOptionsSeparator
        //     // 
        //     resources.ApplyResources(this.notificationOptionsSeparator, "notificationOptionsSeparator");
        //     this.tableLayoutPanel1.SetColumnSpan(this.notificationOptionsSeparator, 5);
        //     this.notificationOptionsSeparator.Name = "notificationOptionsSeparator";
        //     // 
        //     // emailNameLabel
        //     // 
        //     resources.ApplyResources(this.emailNameLabel, "emailNameLabel");
        //     this.emailNameLabel.Name = "emailNameLabel";
        //     // 
        //     // pagerEmailNameLabel
        //     // 
        //     resources.ApplyResources(this.pagerEmailNameLabel, "pagerEmailNameLabel");
        //     this.pagerEmailNameLabel.Name = "pagerEmailNameLabel";
        //     // 
        //     // emailName
        //     // 
        //     resources.ApplyResources(this.emailName, "emailName");
        //     this.tableLayoutPanel1.SetColumnSpan(this.emailName, 2);
        //     this.emailName.Name = "emailName";
        //     // 
        //     // pagerEmailName
        //     // 
        //     resources.ApplyResources(this.pagerEmailName, "pagerEmailName");
        //     this.tableLayoutPanel1.SetColumnSpan(this.pagerEmailName, 2);
        //     this.pagerEmailName.Name = "pagerEmailName";
        //     // 
        //     // pagerOnDutySeparator
        //     // 
        //     resources.ApplyResources(this.pagerOnDutySeparator, "pagerOnDutySeparator");
        //     this.tableLayoutPanel1.SetColumnSpan(this.pagerOnDutySeparator, 5);
        //     this.pagerOnDutySeparator.Name = "pagerOnDutySeparator";
        //     // 
        //     // onDutyMonday
        //     // 
        //     resources.ApplyResources(this.onDutyMonday, "onDutyMonday");
        //     this.onDutyMonday.Name = "onDutyMonday";
        //     this.onDutyMonday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutyTuesday
        //     // 
        //     resources.ApplyResources(this.onDutyTuesday, "onDutyTuesday");
        //     this.onDutyTuesday.Name = "onDutyTuesday";
        //     this.onDutyTuesday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutyWednesday
        //     // 
        //     resources.ApplyResources(this.onDutyWednesday, "onDutyWednesday");
        //     this.onDutyWednesday.Name = "onDutyWednesday";
        //     this.onDutyWednesday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutyThursday
        //     // 
        //     resources.ApplyResources(this.onDutyThursday, "onDutyThursday");
        //     this.onDutyThursday.Name = "onDutyThursday";
        //     this.onDutyThursday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutyFriday
        //     // 
        //     resources.ApplyResources(this.onDutyFriday, "onDutyFriday");
        //     this.onDutyFriday.Name = "onDutyFriday";
        //     this.onDutyFriday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutySaturday
        //     // 
        //     resources.ApplyResources(this.onDutySaturday, "onDutySaturday");
        //     this.onDutySaturday.Name = "onDutySaturday";
        //     this.onDutySaturday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // onDutySunday
        //     // 
        //     resources.ApplyResources(this.onDutySunday, "onDutySunday");
        //     this.onDutySunday.Name = "onDutySunday";
        //     this.onDutySunday.CheckedChanged += new System.EventHandler(this.OnDutyCheckedChanged);
        //     // 
        //     // weekdayBegin
        //     // 
        //     resources.ApplyResources(this.weekdayBegin, "weekdayBegin");
        //     this.weekdayBegin.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.weekdayBegin.Name = "weekdayBegin";
        //     this.weekdayBegin.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.weekdayBegin.ShowUpDown = true;
        //     // 
        //     // weekdayEnd
        //     // 
        //     resources.ApplyResources(this.weekdayEnd, "weekdayEnd");
        //     this.weekdayEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.weekdayEnd.Name = "weekdayEnd";
        //     this.weekdayEnd.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.weekdayEnd.ShowUpDown = true;
        //     // 
        //     // saturdayBegin
        //     // 
        //     resources.ApplyResources(this.saturdayBegin, "saturdayBegin");
        //     this.saturdayBegin.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.saturdayBegin.Name = "saturdayBegin";
        //     this.saturdayBegin.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.saturdayBegin.ShowUpDown = true;
        //     // 
        //     // saturdayEnd
        //     // 
        //     resources.ApplyResources(this.saturdayEnd, "saturdayEnd");
        //     this.saturdayEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.saturdayEnd.Name = "saturdayEnd";
        //     this.saturdayEnd.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.saturdayEnd.ShowUpDown = true;
        //     // 
        //     // sundayBegin
        //     // 
        //     resources.ApplyResources(this.sundayBegin, "sundayBegin");
        //     this.sundayBegin.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.sundayBegin.Name = "sundayBegin";
        //     this.sundayBegin.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.sundayBegin.ShowUpDown = true;
        //     // 
        //     // sundayEnd
        //     // 
        //     resources.ApplyResources(this.sundayEnd, "sundayEnd");
        //     this.sundayEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
        //     this.sundayEnd.Name = "sundayEnd";
        //     this.sundayEnd.AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        //     this.sundayEnd.ShowUpDown = true;
        //     // 
        //     // saturdaySeparator
        //     // 
        //     resources.ApplyResources(this.saturdaySeparator, "saturdaySeparator");
        //     this.tableLayoutPanel1.SetColumnSpan(this.saturdaySeparator, 5);
        //     this.saturdaySeparator.Name = "saturdaySeparator";
        //     // 
        //     // sundaySeparator
        //     // 
        //     resources.ApplyResources(this.sundaySeparator, "sundaySeparator");
        //     this.tableLayoutPanel1.SetColumnSpan(this.sundaySeparator, 5);
        //     this.sundaySeparator.Name = "sundaySeparator";
        //     // 
        //     // beginLabel
        //     // 
        //     resources.ApplyResources(this.beginLabel, "beginLabel");
        //     this.beginLabel.Name = "beginLabel";
        //     // 
        //     // endLabel
        //     // 
        //     resources.ApplyResources(this.endLabel, "endLabel");
        //     this.endLabel.Name = "endLabel";
        //     // 
        //     // tableLayoutPanel1
        //     // 
        //     resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
        //     this.tableLayoutPanel1.Controls.Add(this.nameLabel, 1, 0);
        //     this.tableLayoutPanel1.Controls.Add(this.sundayEnd, 4, 13);
        //     this.tableLayoutPanel1.Controls.Add(this.sundaySeparator, 0, 12);
        //     this.tableLayoutPanel1.Controls.Add(this.sundayBegin, 3, 13);
        //     this.tableLayoutPanel1.Controls.Add(this.endLabel, 4, 8);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutySunday, 1, 13);
        //     this.tableLayoutPanel1.Controls.Add(this.saturdaySeparator, 0, 10);
        //     this.tableLayoutPanel1.Controls.Add(this.nameEnabled, 4, 0);
        //     this.tableLayoutPanel1.Controls.Add(this.saturdayEnd, 4, 11);
        //     this.tableLayoutPanel1.Controls.Add(this.beginLabel, 3, 8);
        //     this.tableLayoutPanel1.Controls.Add(this.saturdayBegin, 3, 11);
        //     this.tableLayoutPanel1.Controls.Add(this.name, 2, 0);
        //     this.tableLayoutPanel1.Controls.Add(this.notificationOptionsSeparator, 0, 1);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutySaturday, 1, 11);
        //     this.tableLayoutPanel1.Controls.Add(this.emailNameLabel, 1, 2);
        //     this.tableLayoutPanel1.Controls.Add(this.weekdayEnd, 4, 9);
        //     this.tableLayoutPanel1.Controls.Add(this.emailName, 3, 2);
        //     this.tableLayoutPanel1.Controls.Add(this.weekdayBegin, 3, 9);
        //     this.tableLayoutPanel1.Controls.Add(this.pagerEmailNameLabel, 1, 3);
        //     this.tableLayoutPanel1.Controls.Add(this.pagerEmailName, 3, 3);
        //     this.tableLayoutPanel1.Controls.Add(this.pagerOnDutySeparator, 0, 4);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutyFriday, 1, 9);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutyMonday, 1, 5);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutyTuesday, 1, 6);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutyWednesday, 1, 7);
        //     this.tableLayoutPanel1.Controls.Add(this.onDutyThursday, 1, 8);
        //     this.tableLayoutPanel1.Name = "tableLayoutPanel1";
        //     // 
        //     // AgentOperatorsGeneral
        //     // 
        //     resources.ApplyResources(this, "$this");
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     this.Controls.Add(this.tableLayoutPanel1);
        //     this.Name = "AgentOperatorsGeneral";
        //     this.tableLayoutPanel1.ResumeLayout(false);
        //     this.tableLayoutPanel1.PerformLayout();
        //     this.ResumeLayout(false);

        // }
        // #endregion

        // #region Private helpers
        // void InitializeControls(bool refresh)
        // {
        //     if(this.controlsInitialized == true && refresh == false)
        //         return; // All controls are already initialized and refresh isn't needed

        //     STrace.Assert(this.operatorsData != null);
        //     if(this.operatorsData == null)
        //     {
        //         throw new InvalidOperationException();
        //     }

        //     this.name.Text = this.operatorsData.Name;
        //     this.nameEnabled.Checked = this.operatorsData.Enabled;
        //     this.emailName.Text = this.operatorsData.EmailAddress;

        //     // Pager not supported for Managed Instances
        //     //
        //     if (this.ServerConnection != null && this.ServerConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
        //     {
        //         this.pagerEmailName.Text = String.Empty;
        //         this.pagerEmailName.Enabled = false;
        //     }
        //     else
        //     {
        //         this.pagerEmailName.Text = this.operatorsData.PagerAddress;
        //     }

        //     this.onDutyMonday.Checked = ((this.operatorsData.PagerDays & WeekDays.Monday) > 0);
        //     this.onDutyTuesday.Checked = ((this.operatorsData.PagerDays & WeekDays.Tuesday) > 0);
        //     this.onDutyWednesday.Checked = ((this.operatorsData.PagerDays & WeekDays.Wednesday) > 0);
        //     this.onDutyThursday.Checked = ((this.operatorsData.PagerDays & WeekDays.Thursday) > 0);
        //     this.onDutyFriday.Checked = ((this.operatorsData.PagerDays & WeekDays.Friday) > 0);
        //     this.onDutySaturday.Checked = ((this.operatorsData.PagerDays & WeekDays.Saturday) > 0);
        //     this.onDutySunday.Checked = ((this.operatorsData.PagerDays & WeekDays.Sunday) > 0);

        //     this.weekdayBegin.Value = this.operatorsData.WeekdayStartTime;
        //     this.weekdayEnd.Value = this.operatorsData.WeekdayEndTime;

        //     this.saturdayBegin.Value = this.operatorsData.SaturdayStartTime;
        //     this.saturdayEnd.Value = this.operatorsData.SaturdayEndTime;

        //     this.sundayBegin.Value = this.operatorsData.SundayStartTime;
        //     this.sundayEnd.Value = this.operatorsData.SundayEndTime;

        //     UpdateControlEnabledStatus();
        //     this.controlsInitialized = true;
        // }

        // private void UpdateControlEnabledStatus()
        // {
        //     this.weekdayBegin.Enabled = this.weekdayEnd.Enabled = (this.onDutyMonday.Checked
        //         || this.onDutyTuesday.Checked
        //         || this.onDutyWednesday.Checked
        //         || this.onDutyThursday.Checked
        //         || this.onDutyFriday.Checked);

        //     this.saturdayBegin.Enabled = this.saturdayEnd.Enabled = this.onDutySaturday.Checked;
            
        //     this.sundayBegin.Enabled = this.sundayEnd.Enabled = this.onDutySunday.Checked;
        // }

        #endregion

        #region Public properties

        /// <summary>
        /// Operator name
        /// </summary>
        // public string OperatorName
        // {
        //     get { return this.name.Text; }
        // }

        #endregion

        #region ISupportValidation

        public bool Validate()
        {
            // if(this.weekdayBegin.Enabled && this.weekdayEnd.Enabled)
            // {
            //     if(this.weekdayBegin.Value > this.weekdayEnd.Value)
            //     {
            //         // if (ShowMessage(AgentOperatorsGeneralSR.PagerScheduleMonFri, AgentOperatorsGeneralSR.PagerScheduleWarning, ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Warning) == DialogResult.No)
            //         // {
            //         //     this.weekdayBegin.Focus();
            //         //     return false;
            //         // }
            //     }
            // }
            // if(this.saturdayBegin.Enabled && this.saturdayEnd.Enabled)
            // {
            //     if(this.saturdayBegin.Value > this.saturdayEnd.Value)
            //     {
            //         // if (ShowMessage(AgentOperatorsGeneralSR.PagerScheduleSatSun, AgentOperatorsGeneralSR.PagerScheduleWarning, ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Warning) == DialogResult.No)
            //         // {
            //         //     this.saturdayBegin.Focus();
            //         //     return false;
            //         // }
            //     }
            // }
            // if(this.sundayBegin.Enabled && this.sundayEnd.Enabled)
            // {
            //     if(this.sundayBegin.Value > this.sundayEnd.Value)
            //     {
            //         // if (ShowMessage(AgentOperatorsGeneralSR.PagerScheduleSatSun, AgentOperatorsGeneralSR.PagerScheduleWarning, ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Warning) == DialogResult.No)
            //         // {
            //         //     this.sundayBegin.Focus();
            //         //     return false;
            //         // }
            //     }
            // }
            return true;
        }

        #endregion
    }
}









