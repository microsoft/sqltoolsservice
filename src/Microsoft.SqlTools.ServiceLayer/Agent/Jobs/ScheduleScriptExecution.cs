//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    sealed class ScheduleScriptExecution : ManagementActionBase
    {
        SqlConnectionInfo ci;
        #region designer generated code
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button Help;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox scheduleName;
        private System.Windows.Forms.Label typeLabel;
        private Microsoft.SqlServer.Management.SqlManagerUI.Schedule.RecurrencePatternControl recurrancePattern;
        private System.Windows.Forms.ComboBox scheduleType;
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Panel scheduleControlPane;
        private System.Windows.Forms.Label jobNameLabel;
        private System.Windows.Forms.TextBox jobName;

        private JobScheduleData scheduleData = null;

        #endregion

        #region object construction
        /// <summary>
        ///  constructs an empty schedule dialog.
        /// </summary>
        public ScheduleScriptExecution()
        {
            InitializeComponent();
            this.scheduleData = new JobScheduleData();
            InitializeControls();
            InitializeData();

        }

        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo schedule. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleScriptExecution(JobSchedule source)
        {
            InitializeComponent();
            this.scheduleData = new JobScheduleData(source);
            InitializeControls();
            InitializeData();
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon an existing smo Job. Will
        /// automatically save changes on ok.
        /// </summary>
        /// <param name="source">source schedule</param>
        public ScheduleScriptExecution(Job source)
        {
            InitializeComponent();
            this.scheduleData = new JobScheduleData(source);
            InitializeControls();
            InitializeData();
        }
        /// <summary>
        /// Constructs a new ScheduleDialog based upon a JobScheduleData object.
        /// </summary>
        /// <param name="source"></param>
        public ScheduleScriptExecution(JobScheduleData source, SqlConnectionInfo ci)
        {
            STrace.Assert(ci != null);
            InitializeComponent();
            this.scheduleData = source;
            this.ci = ci;
            InitializeControls();
            InitializeData();
        }
        /// <summary>
        /// Constructs a new Schedule dialog based upon a SimpleJobSchedule structure
        /// </summary>
        /// <param name="source"></param>
        public ScheduleScriptExecution(SimpleJobSchedule source)
        {
            InitializeComponent();
            this.scheduleData = source.ToJobScheduleData();
            InitializeControls();
            InitializeData();
        }
        #endregion

        #region public properties
        /// <summary>
        /// Underlying JobScheduleData object
        /// </summary>
        public JobScheduleData Schedule
        {
            get
            {
                return this.scheduleData;
            }
        }

        /// <summary>
        /// SimpleJobSchedule structure
        /// </summary>
        public SimpleJobSchedule SimpleSchedule
        {
            get
            {
                STrace.Assert(this.scheduleData != null);
                SimpleJobSchedule s = SimpleJobSchedule.FromJobScheduleData(this.scheduleData);
                s.Description = this.ToString();
                return s;
            }
            set
            {
                this.scheduleData = value.ToJobScheduleData();
                InitializeData();
            }
        }

        public string JobName
        {
            get
            {
                return this.jobName.Text;
            }
        }

        /// <summary>
        /// Indicates whether or not the user can edit the type of the schedule.
        /// </summary>
        public bool AllowScheduleTypeChange
        {
            get
            {
                return this.scheduleType.Enabled;
            }
            set
            {
                this.scheduleType.Enabled = value;
            }
        }
        /// <summary>
        /// text description of the supplied schedule   
        /// </summary>
        public string Description
        {
            get
            {
                return this.ToString();
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Converts the schedule into a user friendly description
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Converts the schedule into a user friendly description using the provided IFormatProvider
        /// </summary>
        /// <returns></returns>
        public string ToString(IFormatProvider format)
        {
            return this.recurrancePattern.Description;
        }
        #endregion


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ScheduleScriptExecution));
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.Help = new System.Windows.Forms.Button();
            this.nameLabel = new System.Windows.Forms.Label();
            this.scheduleName = new System.Windows.Forms.TextBox();
            this.typeLabel = new System.Windows.Forms.Label();
            this.scheduleType = new System.Windows.Forms.ComboBox();
            this.scheduleControlPane = new System.Windows.Forms.Panel();
            this.jobNameLabel = new System.Windows.Forms.Label();
            this.jobName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // OK
            // 
            this.OK.AccessibleDescription = resources.GetString("OK.AccessibleDescription");
            this.OK.AccessibleName = resources.GetString("OK.AccessibleName");
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("OK.Anchor")));
            this.OK.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("OK.BackgroundImage")));
            this.OK.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("OK.Dock")));
            this.OK.Enabled = ((bool)(resources.GetObject("OK.Enabled")));
            this.OK.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("OK.FlatStyle")));
            this.OK.Font = ((System.Drawing.Font)(resources.GetObject("OK.Font")));
            this.OK.Image = ((System.Drawing.Image)(resources.GetObject("OK.Image")));
            this.OK.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("OK.ImageAlign")));
            this.OK.ImageIndex = ((int)(resources.GetObject("OK.ImageIndex")));
            this.OK.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("OK.ImeMode")));
            this.OK.Location = ((System.Drawing.Point)(resources.GetObject("OK.Location")));
            this.OK.Name = "OK";
            this.OK.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("OK.RightToLeft")));
            this.OK.Size = ((System.Drawing.Size)(resources.GetObject("OK.Size")));
            this.OK.TabIndex = ((int)(resources.GetObject("OK.TabIndex")));
            this.OK.Text = resources.GetString("OK.Text");
            this.OK.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("OK.TextAlign")));
            this.OK.Visible = ((bool)(resources.GetObject("OK.Visible")));
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // Cancel
            // 
            this.Cancel.AccessibleDescription = resources.GetString("Cancel.AccessibleDescription");
            this.Cancel.AccessibleName = resources.GetString("Cancel.AccessibleName");
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("Cancel.Anchor")));
            this.Cancel.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Cancel.BackgroundImage")));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("Cancel.Dock")));
            this.Cancel.Enabled = ((bool)(resources.GetObject("Cancel.Enabled")));
            this.Cancel.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("Cancel.FlatStyle")));
            this.Cancel.Font = ((System.Drawing.Font)(resources.GetObject("Cancel.Font")));
            this.Cancel.Image = ((System.Drawing.Image)(resources.GetObject("Cancel.Image")));
            this.Cancel.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("Cancel.ImageAlign")));
            this.Cancel.ImageIndex = ((int)(resources.GetObject("Cancel.ImageIndex")));
            this.Cancel.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("Cancel.ImeMode")));
            this.Cancel.Location = ((System.Drawing.Point)(resources.GetObject("Cancel.Location")));
            this.Cancel.Name = "Cancel";
            this.Cancel.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("Cancel.RightToLeft")));
            this.Cancel.Size = ((System.Drawing.Size)(resources.GetObject("Cancel.Size")));
            this.Cancel.TabIndex = ((int)(resources.GetObject("Cancel.TabIndex")));
            this.Cancel.Text = resources.GetString("Cancel.Text");
            this.Cancel.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("Cancel.TextAlign")));
            this.Cancel.Visible = ((bool)(resources.GetObject("Cancel.Visible")));
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // Help
            // 
            this.Help.AccessibleDescription = resources.GetString("Help.AccessibleDescription");
            this.Help.AccessibleName = resources.GetString("Help.AccessibleName");
            this.Help.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("Help.Anchor")));
            this.Help.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Help.BackgroundImage")));
            this.Help.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("Help.Dock")));
            this.Help.Enabled = ((bool)(resources.GetObject("Help.Enabled")));
            this.Help.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("Help.FlatStyle")));
            this.Help.Font = ((System.Drawing.Font)(resources.GetObject("Help.Font")));
            this.Help.Image = ((System.Drawing.Image)(resources.GetObject("Help.Image")));
            this.Help.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("Help.ImageAlign")));
            this.Help.ImageIndex = ((int)(resources.GetObject("Help.ImageIndex")));
            this.Help.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("Help.ImeMode")));
            this.Help.Location = ((System.Drawing.Point)(resources.GetObject("Help.Location")));
            this.Help.Name = "Help";
            this.Help.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("Help.RightToLeft")));
            this.Help.Size = ((System.Drawing.Size)(resources.GetObject("Help.Size")));
            this.Help.TabIndex = ((int)(resources.GetObject("Help.TabIndex")));
            this.Help.Text = resources.GetString("Help.Text");
            this.Help.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("Help.TextAlign")));
            this.Help.Visible = ((bool)(resources.GetObject("Help.Visible")));
            this.Help.Click += new System.EventHandler(this.Help_Click);
            // 
            // nameLabel
            // 
            this.nameLabel.AccessibleDescription = resources.GetString("nameLabel.AccessibleDescription");
            this.nameLabel.AccessibleName = resources.GetString("nameLabel.AccessibleName");
            this.nameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("nameLabel.Anchor")));
            this.nameLabel.AutoSize = ((bool)(resources.GetObject("nameLabel.AutoSize")));
            this.nameLabel.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("nameLabel.Dock")));
            this.nameLabel.Enabled = ((bool)(resources.GetObject("nameLabel.Enabled")));
            this.nameLabel.Font = ((System.Drawing.Font)(resources.GetObject("nameLabel.Font")));
            this.nameLabel.Image = ((System.Drawing.Image)(resources.GetObject("nameLabel.Image")));
            this.nameLabel.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("nameLabel.ImageAlign")));
            this.nameLabel.ImageIndex = ((int)(resources.GetObject("nameLabel.ImageIndex")));
            this.nameLabel.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("nameLabel.ImeMode")));
            this.nameLabel.Location = ((System.Drawing.Point)(resources.GetObject("nameLabel.Location")));
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("nameLabel.RightToLeft")));
            this.nameLabel.Size = ((System.Drawing.Size)(resources.GetObject("nameLabel.Size")));
            this.nameLabel.TabIndex = ((int)(resources.GetObject("nameLabel.TabIndex")));
            this.nameLabel.Text = resources.GetString("nameLabel.Text");
            this.nameLabel.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("nameLabel.TextAlign")));
            this.nameLabel.Visible = ((bool)(resources.GetObject("nameLabel.Visible")));
            // 
            // scheduleName
            // 
            this.scheduleName.AccessibleDescription = resources.GetString("scheduleName.AccessibleDescription");
            this.scheduleName.AccessibleName = resources.GetString("scheduleName.AccessibleName");
            this.scheduleName.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("scheduleName.Anchor")));
            this.scheduleName.AutoSize = ((bool)(resources.GetObject("scheduleName.AutoSize")));
            this.scheduleName.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("scheduleName.BackgroundImage")));
            this.scheduleName.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("scheduleName.Dock")));
            this.scheduleName.Enabled = ((bool)(resources.GetObject("scheduleName.Enabled")));
            this.scheduleName.Font = ((System.Drawing.Font)(resources.GetObject("scheduleName.Font")));
            this.scheduleName.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("scheduleName.ImeMode")));
            this.scheduleName.Location = ((System.Drawing.Point)(resources.GetObject("scheduleName.Location")));
            this.scheduleName.MaxLength = ((int)(resources.GetObject("scheduleName.MaxLength")));
            this.scheduleName.Multiline = ((bool)(resources.GetObject("scheduleName.Multiline")));
            this.scheduleName.Name = "scheduleName";
            this.scheduleName.PasswordChar = ((char)(resources.GetObject("scheduleName.PasswordChar")));
            this.scheduleName.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("scheduleName.RightToLeft")));
            this.scheduleName.ScrollBars = ((System.Windows.Forms.ScrollBars)(resources.GetObject("scheduleName.ScrollBars")));
            this.scheduleName.Size = ((System.Drawing.Size)(resources.GetObject("scheduleName.Size")));
            this.scheduleName.TabIndex = ((int)(resources.GetObject("scheduleName.TabIndex")));
            this.scheduleName.Text = resources.GetString("scheduleName.Text");
            this.scheduleName.TextAlign = ((System.Windows.Forms.HorizontalAlignment)(resources.GetObject("scheduleName.TextAlign")));
            this.scheduleName.Visible = ((bool)(resources.GetObject("scheduleName.Visible")));
            this.scheduleName.WordWrap = ((bool)(resources.GetObject("scheduleName.WordWrap")));
            this.scheduleName.TextChanged += new System.EventHandler(this.scheduleName_TextChanged);
            // 
            // typeLabel
            // 
            this.typeLabel.AccessibleDescription = resources.GetString("typeLabel.AccessibleDescription");
            this.typeLabel.AccessibleName = resources.GetString("typeLabel.AccessibleName");
            this.typeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("typeLabel.Anchor")));
            this.typeLabel.AutoSize = ((bool)(resources.GetObject("typeLabel.AutoSize")));
            this.typeLabel.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("typeLabel.Dock")));
            this.typeLabel.Enabled = ((bool)(resources.GetObject("typeLabel.Enabled")));
            this.typeLabel.Font = ((System.Drawing.Font)(resources.GetObject("typeLabel.Font")));
            this.typeLabel.Image = ((System.Drawing.Image)(resources.GetObject("typeLabel.Image")));
            this.typeLabel.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("typeLabel.ImageAlign")));
            this.typeLabel.ImageIndex = ((int)(resources.GetObject("typeLabel.ImageIndex")));
            this.typeLabel.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("typeLabel.ImeMode")));
            this.typeLabel.Location = ((System.Drawing.Point)(resources.GetObject("typeLabel.Location")));
            this.typeLabel.Name = "typeLabel";
            this.typeLabel.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("typeLabel.RightToLeft")));
            this.typeLabel.Size = ((System.Drawing.Size)(resources.GetObject("typeLabel.Size")));
            this.typeLabel.TabIndex = ((int)(resources.GetObject("typeLabel.TabIndex")));
            this.typeLabel.Text = resources.GetString("typeLabel.Text");
            this.typeLabel.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("typeLabel.TextAlign")));
            this.typeLabel.Visible = ((bool)(resources.GetObject("typeLabel.Visible")));
            // 
            // scheduleType
            // 
            this.scheduleType.AccessibleDescription = resources.GetString("scheduleType.AccessibleDescription");
            this.scheduleType.AccessibleName = resources.GetString("scheduleType.AccessibleName");
            this.scheduleType.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("scheduleType.Anchor")));
            this.scheduleType.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("scheduleType.BackgroundImage")));
            this.scheduleType.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("scheduleType.Dock")));
            this.scheduleType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.scheduleType.Enabled = ((bool)(resources.GetObject("scheduleType.Enabled")));
            this.scheduleType.Font = ((System.Drawing.Font)(resources.GetObject("scheduleType.Font")));
            this.scheduleType.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("scheduleType.ImeMode")));
            this.scheduleType.IntegralHeight = ((bool)(resources.GetObject("scheduleType.IntegralHeight")));
            this.scheduleType.ItemHeight = ((int)(resources.GetObject("scheduleType.ItemHeight")));
            this.scheduleType.Location = ((System.Drawing.Point)(resources.GetObject("scheduleType.Location")));
            this.scheduleType.MaxDropDownItems = ((int)(resources.GetObject("scheduleType.MaxDropDownItems")));
            this.scheduleType.MaxLength = ((int)(resources.GetObject("scheduleType.MaxLength")));
            this.scheduleType.Name = "scheduleType";
            this.scheduleType.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("scheduleType.RightToLeft")));
            this.scheduleType.Size = ((System.Drawing.Size)(resources.GetObject("scheduleType.Size")));
            this.scheduleType.TabIndex = ((int)(resources.GetObject("scheduleType.TabIndex")));
            this.scheduleType.Text = resources.GetString("scheduleType.Text");
            this.scheduleType.Visible = ((bool)(resources.GetObject("scheduleType.Visible")));
            this.scheduleType.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // scheduleControlPane
            // 
            this.scheduleControlPane.AccessibleDescription = resources.GetString("scheduleControlPane.AccessibleDescription");
            this.scheduleControlPane.AccessibleName = resources.GetString("scheduleControlPane.AccessibleName");
            this.scheduleControlPane.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("scheduleControlPane.Anchor")));
            this.scheduleControlPane.AutoScroll = ((bool)(resources.GetObject("scheduleControlPane.AutoScroll")));
            this.scheduleControlPane.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("scheduleControlPane.AutoScrollMargin")));
            this.scheduleControlPane.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("scheduleControlPane.AutoScrollMinSize")));
            this.scheduleControlPane.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("scheduleControlPane.BackgroundImage")));
            this.scheduleControlPane.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("scheduleControlPane.Dock")));
            this.scheduleControlPane.Enabled = ((bool)(resources.GetObject("scheduleControlPane.Enabled")));
            this.scheduleControlPane.Font = ((System.Drawing.Font)(resources.GetObject("scheduleControlPane.Font")));
            this.scheduleControlPane.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("scheduleControlPane.ImeMode")));
            this.scheduleControlPane.Location = ((System.Drawing.Point)(resources.GetObject("scheduleControlPane.Location")));
            this.scheduleControlPane.Name = "scheduleControlPane";
            this.scheduleControlPane.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("scheduleControlPane.RightToLeft")));
            this.scheduleControlPane.Size = ((System.Drawing.Size)(resources.GetObject("scheduleControlPane.Size")));
            this.scheduleControlPane.TabIndex = ((int)(resources.GetObject("scheduleControlPane.TabIndex")));
            this.scheduleControlPane.Text = resources.GetString("scheduleControlPane.Text");
            this.scheduleControlPane.Visible = ((bool)(resources.GetObject("scheduleControlPane.Visible")));
            // 
            // jobNameLabel
            // 
            this.jobNameLabel.AccessibleDescription = resources.GetString("jobNameLabel.AccessibleDescription");
            this.jobNameLabel.AccessibleName = resources.GetString("jobNameLabel.AccessibleName");
            this.jobNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("jobNameLabel.Anchor")));
            this.jobNameLabel.AutoSize = ((bool)(resources.GetObject("jobNameLabel.AutoSize")));
            this.jobNameLabel.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("jobNameLabel.Dock")));
            this.jobNameLabel.Enabled = ((bool)(resources.GetObject("jobNameLabel.Enabled")));
            this.jobNameLabel.Font = ((System.Drawing.Font)(resources.GetObject("jobNameLabel.Font")));
            this.jobNameLabel.Image = ((System.Drawing.Image)(resources.GetObject("jobNameLabel.Image")));
            this.jobNameLabel.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("jobNameLabel.ImageAlign")));
            this.jobNameLabel.ImageIndex = ((int)(resources.GetObject("jobNameLabel.ImageIndex")));
            this.jobNameLabel.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("jobNameLabel.ImeMode")));
            this.jobNameLabel.Location = ((System.Drawing.Point)(resources.GetObject("jobNameLabel.Location")));
            this.jobNameLabel.Name = "jobNameLabel";
            this.jobNameLabel.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("jobNameLabel.RightToLeft")));
            this.jobNameLabel.Size = ((System.Drawing.Size)(resources.GetObject("jobNameLabel.Size")));
            this.jobNameLabel.TabIndex = ((int)(resources.GetObject("jobNameLabel.TabIndex")));
            this.jobNameLabel.Text = resources.GetString("jobNameLabel.Text");
            this.jobNameLabel.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("jobNameLabel.TextAlign")));
            this.jobNameLabel.Visible = ((bool)(resources.GetObject("jobNameLabel.Visible")));
            // 
            // jobName
            // 
            this.jobName.AccessibleDescription = resources.GetString("jobName.AccessibleDescription");
            this.jobName.AccessibleName = resources.GetString("jobName.AccessibleName");
            this.jobName.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("jobName.Anchor")));
            this.jobName.AutoSize = ((bool)(resources.GetObject("jobName.AutoSize")));
            this.jobName.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("jobName.BackgroundImage")));
            this.jobName.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("jobName.Dock")));
            this.jobName.Enabled = ((bool)(resources.GetObject("jobName.Enabled")));
            this.jobName.Font = ((System.Drawing.Font)(resources.GetObject("jobName.Font")));
            this.jobName.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("jobName.ImeMode")));
            this.jobName.Location = ((System.Drawing.Point)(resources.GetObject("jobName.Location")));
            this.jobName.MaxLength = ((int)(resources.GetObject("jobName.MaxLength")));
            this.jobName.Multiline = ((bool)(resources.GetObject("jobName.Multiline")));
            this.jobName.Name = "jobName";
            this.jobName.PasswordChar = ((char)(resources.GetObject("jobName.PasswordChar")));
            this.jobName.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("jobName.RightToLeft")));
            this.jobName.ScrollBars = ((System.Windows.Forms.ScrollBars)(resources.GetObject("jobName.ScrollBars")));
            this.jobName.Size = ((System.Drawing.Size)(resources.GetObject("jobName.Size")));
            this.jobName.TabIndex = ((int)(resources.GetObject("jobName.TabIndex")));
            this.jobName.Text = resources.GetString("jobName.Text");
            this.jobName.TextAlign = ((System.Windows.Forms.HorizontalAlignment)(resources.GetObject("jobName.TextAlign")));
            this.jobName.Visible = ((bool)(resources.GetObject("jobName.Visible")));
            this.jobName.WordWrap = ((bool)(resources.GetObject("jobName.WordWrap")));
            this.jobName.TextChanged += new System.EventHandler(this.jobName_TextChanged);
            // 
            // ScheduleScriptExecution
            // 
            this.AcceptButton = this.OK;
            this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
            this.AccessibleName = resources.GetString("$this.AccessibleName");
            this.AutoScaleBaseSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScaleBaseSize")));
            this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
            this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
            this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.CancelButton = this.Cancel;
            this.ClientSize = ((System.Drawing.Size)(resources.GetObject("$this.ClientSize")));
            this.Controls.Add(this.jobName);
            this.Controls.Add(this.scheduleName);
            this.Controls.Add(this.jobNameLabel);
            this.Controls.Add(this.scheduleControlPane);
            this.Controls.Add(this.scheduleType);
            this.Controls.Add(this.typeLabel);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.Help);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
            this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
            this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
            this.MaximizeBox = false;
            this.MaximumSize = ((System.Drawing.Size)(resources.GetObject("$this.MaximumSize")));
            this.MinimizeBox = false;
            this.MinimumSize = ((System.Drawing.Size)(resources.GetObject("$this.MinimumSize")));
            this.Name = "ScheduleScriptExecution";
            this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = ((System.Windows.Forms.FormStartPosition)(resources.GetObject("$this.StartPosition")));
            this.Text = resources.GetString("$this.Text");
            this.ResumeLayout(false);

        }
        #endregion

        #region internal helpers
        private void UpdateOkCancelStatus()
        {
            //Only enable the OK button if both the Schedule Name and the JobName are not blank.
            if (this.scheduleName.Text.Trim().Length > 0 && this.jobName.Text.Trim().Length > 0)
            {
                this.OK.Enabled = true;
            }
            else
            {
                this.OK.Enabled = false;
            }

            this.Cancel.Enabled = true;
        }
        #endregion

        #region ui initalization
        /// <summary>
        /// load data controls.
        /// </summary>
        private void InitializeData()
        {
            this.scheduleName.Text = this.scheduleData.Name;
            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.job.scheduleproperties.f1";

            int index = idxRecurringSchedule;
            if (scheduleData.FrequencyTypes == FrequencyTypes.AutoStart)
            {
                index = idxAutoStartSchedule;
            }
            else if (scheduleData.FrequencyTypes == FrequencyTypes.OnIdle)
            {
                index = idxCpuIdleSchedule;
            }
            else if (scheduleData.FrequencyTypes == FrequencyTypes.OneTime)
            {
                index = idxOneTimeSchedule;
                this.recurrancePattern.OneTime = true;
                this.recurrancePattern.LoadData(scheduleData);
            }
            else if (this.scheduleData != null)
            {
                // only load data if there is something to load
                this.recurrancePattern.OneTime = false;
                this.recurrancePattern.LoadData(scheduleData);
            }

            this.scheduleType.SelectedIndex = index;

            UpdateOkCancelStatus();
        }

        private const int idxAutoStartSchedule = 0;
        private const int idxCpuIdleSchedule = 1;
        private const int idxRecurringSchedule = 2;
        private const int idxOneTimeSchedule = 3;
        private void InitializeControls()
        {
            //initialize recurrancePattern
            STrace.Assert(this.recurrancePattern == null);
            this.recurrancePattern = new Microsoft.SqlServer.Management.SqlManagerUI.Schedule.RecurrencePatternControl();
            this.recurrancePattern.Location = new Point(0, 0);
            this.recurrancePattern.Dock = DockStyle.Fill;
            this.recurrancePattern.AccessibleName = SR.ScheduleControlAaName;
            STrace.Assert(this.scheduleControlPane.Controls.Count == 0);
            this.scheduleControlPane.Controls.Add(this.recurrancePattern);
            this.scheduleControlPane.AccessibleRole = AccessibleRole.Pane;
            this.scheduleControlPane.AccessibleName = SR.ScheduleControlPaneAaName;

            //populate controls with resource strings
            this.scheduleType.Items.Add(SR.AutoStartSchedule);
            this.scheduleType.Items.Add(SR.CPUIdleSchedule);
            this.scheduleType.Items.Add(SR.RecurringSchedule);
            this.scheduleType.Items.Add(SR.OneTimeSchedule);

            // set the SqlManagementForm.HelpControl property
            this.HelpControl = this.Help;
        }
        #endregion

        #region ui event handlers

        private void OK_Click(System.Object sender, System.EventArgs e)
        {
            Microsoft.SqlServer.Management.Smo.Server smoServer;

            scheduleData.Name = this.scheduleName.Text;

            if (this.scheduleType.SelectedIndex == idxAutoStartSchedule)
            {
                scheduleData.FrequencyTypes = FrequencyTypes.AutoStart;
            }
            else if (this.scheduleType.SelectedIndex == idxCpuIdleSchedule)
            {
                scheduleData.FrequencyTypes = FrequencyTypes.OnIdle;
            }
            else
            {
                this.recurrancePattern.SaveData(this.scheduleData);
            }

            ///For methods which pass a connection object, connect to smo and get information about the job server and the
            ///job inventory to pass to the validate method
            try
            {
                if (ci != null)
                {
                    smoServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(ci));
                    System.Version version = smoServer.Information.Version;
                    ///This is the creation of a new job. We won't need to pass schedule information to Validate
                    ///because this a new job.  
                    ///But first make sure the job has not already been created
                    if (smoServer.JobServer.Jobs.Contains(this.jobName.Text.Trim()))
                    {
                        throw new ApplicationException(SRError.JobAlreadyExists(this.jobName.Text));
                    }
                    //If we have not failed.  The job doesn't exist.  Now check to make sure the schedule data
                    //is valid
                    ArrayList nullArrayList=null;
                    scheduleData.Validate(version, nullArrayList);
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (ApplicationException error)
            {
                DisplayExceptionMessage(error);
                this.DialogResult = DialogResult.None;

            }
            finally
            {
                smoServer = null;
            }

        }

        private void Cancel_Click(System.Object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void Help_Click(System.Object sender, System.EventArgs e)
        {

        }

        private void scheduleName_TextChanged(System.Object sender, System.EventArgs e)
        {
            UpdateOkCancelStatus();
        }


        private void comboBox1_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            this.recurrancePattern.Enabled =
                (this.scheduleType.SelectedIndex == idxRecurringSchedule) ||
                (this.scheduleType.SelectedIndex == idxOneTimeSchedule);
            this.recurrancePattern.OneTime = (this.scheduleType.SelectedIndex == idxOneTimeSchedule);

            //If the recurrence pattern control is One Time then  schedule type combo should be disabled.
            this.scheduleType.Enabled = !(this.recurrancePattern.OneTime);
        }

        private void jobName_TextChanged(object sender, System.EventArgs e)
        {
            UpdateOkCancelStatus();
        }

 
        #endregion
    }
}
