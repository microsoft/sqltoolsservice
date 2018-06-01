using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.SqlMgmt;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    /// <summary>
    /// Summary description for JobsRefrencingScheduleForm.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
        class JobsReferencingScheduleForm : System.Windows.Forms.Form
    {
        #region UI Variables

        private System.Windows.Forms.Panel panelContainer;
        private Microsoft.SqlServer.Management.Controls.Separator separatorContainerFromButtons;
        private System.Windows.Forms.Button buttonHelp;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        #endregion

        #region Other Variables

        private JobsReferencingScheduleControl m_innerControl = null;
        private IServiceProvider m_serviceProvider = null;

        #endregion

        #region Public

        public int NoOfSelectedJobs
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_innerControl != null);
                System.Diagnostics.Debug.Assert(this.DialogResult == DialogResult.OK,
                    "property meaningfull only if dialog dismised with OK");
                return m_innerControl.NoOfSelectedJobs;
            }
        }

        #endregion

        #region Constructors/Dispose

        /// <summary>
        /// constructor used so WinForms designer can work
        /// </summary>
        public JobsReferencingScheduleForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            InitializeInnerUserControl();
        }

        /// <summary>
        /// actual constuctor invoked from 'ManageSchedules' dialog
        /// </summary>
        /// <param name="context">context describing connection used, etc - similar with context in dbCommanders</param>
        /// <param name="scheduleId">shared schedule id (used to unique identify the shared schedule since duplicate names are possible)</param>
        /// <param name="scheduleName">shared schedule for which we should display the jobs (used for display purposes)</param>
        /// <param name="readOnlyMode">true if we dont allow user to modify data</param>
        /// <param name="svcProvider">provider used to show help, msg boxes, etc</param>
        public JobsReferencingScheduleForm(CDataContainer context, int scheduleId, string scheduleName,
            bool readOnlyMode, IServiceProvider svcProvider)
        {
            m_serviceProvider = svcProvider;

            InitializeComponent();
            InitializeInnerUserControl(context, scheduleId, scheduleName, readOnlyMode);
            InitializeButtonEvents();
        }

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

        #endregion

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources =
                new System.Resources.ResourceManager(typeof (JobsReferencingScheduleForm));
            this.panelContainer = new System.Windows.Forms.Panel();
            this.separatorContainerFromButtons = new Microsoft.SqlServer.Management.Controls.Separator();
            this.buttonHelp = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // panelContainer
            // 
            this.panelContainer.AccessibleDescription = resources.GetString("panelContainer.AccessibleDescription");
            this.panelContainer.AccessibleName = resources.GetString("panelContainer.AccessibleName");
            this.panelContainer.Anchor =
                ((System.Windows.Forms.AnchorStyles) (resources.GetObject("panelContainer.Anchor")));
            this.panelContainer.AutoScroll = ((bool) (resources.GetObject("panelContainer.AutoScroll")));
            this.panelContainer.AutoScrollMargin =
                ((System.Drawing.Size) (resources.GetObject("panelContainer.AutoScrollMargin")));
            this.panelContainer.AutoScrollMinSize =
                ((System.Drawing.Size) (resources.GetObject("panelContainer.AutoScrollMinSize")));
            this.panelContainer.BackgroundImage =
                ((System.Drawing.Image) (resources.GetObject("panelContainer.BackgroundImage")));
            this.panelContainer.Dock = ((System.Windows.Forms.DockStyle) (resources.GetObject("panelContainer.Dock")));
            this.panelContainer.Enabled = ((bool) (resources.GetObject("panelContainer.Enabled")));
            this.panelContainer.Font = ((System.Drawing.Font) (resources.GetObject("panelContainer.Font")));
            this.panelContainer.ImeMode =
                ((System.Windows.Forms.ImeMode) (resources.GetObject("panelContainer.ImeMode")));
            this.panelContainer.Location = ((System.Drawing.Point) (resources.GetObject("panelContainer.Location")));
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.RightToLeft =
                ((System.Windows.Forms.RightToLeft) (resources.GetObject("panelContainer.RightToLeft")));
            this.panelContainer.Size = ((System.Drawing.Size) (resources.GetObject("panelContainer.Size")));
            this.panelContainer.TabIndex = ((int) (resources.GetObject("panelContainer.TabIndex")));
            this.panelContainer.Text = resources.GetString("panelContainer.Text");
            this.panelContainer.Visible = ((bool) (resources.GetObject("panelContainer.Visible")));
            // 
            // separatorContainerFromButtons
            // 
            this.separatorContainerFromButtons.AccessibleDescription =
                resources.GetString("separatorContainerFromButtons.AccessibleDescription");
            this.separatorContainerFromButtons.AccessibleName =
                resources.GetString("separatorContainerFromButtons.AccessibleName");
            this.separatorContainerFromButtons.Anchor =
                ((System.Windows.Forms.AnchorStyles) (resources.GetObject("separatorContainerFromButtons.Anchor")));
            this.separatorContainerFromButtons.AutoSize =
                ((bool) (resources.GetObject("separatorContainerFromButtons.AutoSize")));
            this.separatorContainerFromButtons.Dock =
                ((System.Windows.Forms.DockStyle) (resources.GetObject("separatorContainerFromButtons.Dock")));
            this.separatorContainerFromButtons.Enabled =
                ((bool) (resources.GetObject("separatorContainerFromButtons.Enabled")));
            this.separatorContainerFromButtons.Font =
                ((System.Drawing.Font) (resources.GetObject("separatorContainerFromButtons.Font")));
            this.separatorContainerFromButtons.ImeMode =
                ((System.Windows.Forms.ImeMode) (resources.GetObject("separatorContainerFromButtons.ImeMode")));
            this.separatorContainerFromButtons.Location =
                ((System.Drawing.Point) (resources.GetObject("separatorContainerFromButtons.Location")));
            this.separatorContainerFromButtons.Name = "separatorContainerFromButtons";
            this.separatorContainerFromButtons.RightToLeft =
                ((System.Windows.Forms.RightToLeft) (resources.GetObject("separatorContainerFromButtons.RightToLeft")));
            this.separatorContainerFromButtons.Size =
                ((System.Drawing.Size) (resources.GetObject("separatorContainerFromButtons.Size")));
            this.separatorContainerFromButtons.TabIndex =
                ((int) (resources.GetObject("separatorContainerFromButtons.TabIndex")));
            this.separatorContainerFromButtons.Text = resources.GetString("separatorContainerFromButtons.Text");
            this.separatorContainerFromButtons.TextAlign =
                ((System.Drawing.ContentAlignment) (resources.GetObject("separatorContainerFromButtons.TextAlign")));
            this.separatorContainerFromButtons.Visible =
                ((bool) (resources.GetObject("separatorContainerFromButtons.Visible")));
            // 
            // buttonHelp
            // 
            this.buttonHelp.AccessibleDescription = resources.GetString("buttonHelp.AccessibleDescription");
            this.buttonHelp.AccessibleName = resources.GetString("buttonHelp.AccessibleName");
            this.buttonHelp.Anchor = ((System.Windows.Forms.AnchorStyles) (resources.GetObject("buttonHelp.Anchor")));
            this.buttonHelp.BackgroundImage =
                ((System.Drawing.Image) (resources.GetObject("buttonHelp.BackgroundImage")));
            this.buttonHelp.Dock = ((System.Windows.Forms.DockStyle) (resources.GetObject("buttonHelp.Dock")));
            this.buttonHelp.Enabled = ((bool) (resources.GetObject("buttonHelp.Enabled")));
            this.buttonHelp.FlatStyle = ((System.Windows.Forms.FlatStyle) (resources.GetObject("buttonHelp.FlatStyle")));
            this.buttonHelp.Font = ((System.Drawing.Font) (resources.GetObject("buttonHelp.Font")));
            this.buttonHelp.Image = ((System.Drawing.Image) (resources.GetObject("buttonHelp.Image")));
            this.buttonHelp.ImageAlign =
                ((System.Drawing.ContentAlignment) (resources.GetObject("buttonHelp.ImageAlign")));
            this.buttonHelp.ImageIndex = ((int) (resources.GetObject("buttonHelp.ImageIndex")));
            this.buttonHelp.ImeMode = ((System.Windows.Forms.ImeMode) (resources.GetObject("buttonHelp.ImeMode")));
            this.buttonHelp.Location = ((System.Drawing.Point) (resources.GetObject("buttonHelp.Location")));
            this.buttonHelp.Name = "buttonHelp";
            this.buttonHelp.RightToLeft =
                ((System.Windows.Forms.RightToLeft) (resources.GetObject("buttonHelp.RightToLeft")));
            this.buttonHelp.Size = ((System.Drawing.Size) (resources.GetObject("buttonHelp.Size")));
            this.buttonHelp.TabIndex = ((int) (resources.GetObject("buttonHelp.TabIndex")));
            this.buttonHelp.Text = resources.GetString("buttonHelp.Text");
            this.buttonHelp.TextAlign =
                ((System.Drawing.ContentAlignment) (resources.GetObject("buttonHelp.TextAlign")));
            this.buttonHelp.Visible = ((bool) (resources.GetObject("buttonHelp.Visible")));
            // 
            // buttonOK
            // 
            this.buttonOK.AccessibleDescription = resources.GetString("buttonOK.AccessibleDescription");
            this.buttonOK.AccessibleName = resources.GetString("buttonOK.AccessibleName");
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles) (resources.GetObject("buttonOK.Anchor")));
            this.buttonOK.BackgroundImage = ((System.Drawing.Image) (resources.GetObject("buttonOK.BackgroundImage")));
            this.buttonOK.Dock = ((System.Windows.Forms.DockStyle) (resources.GetObject("buttonOK.Dock")));
            this.buttonOK.Enabled = ((bool) (resources.GetObject("buttonOK.Enabled")));
            this.buttonOK.FlatStyle = ((System.Windows.Forms.FlatStyle) (resources.GetObject("buttonOK.FlatStyle")));
            this.buttonOK.Font = ((System.Drawing.Font) (resources.GetObject("buttonOK.Font")));
            this.buttonOK.Image = ((System.Drawing.Image) (resources.GetObject("buttonOK.Image")));
            this.buttonOK.ImageAlign = ((System.Drawing.ContentAlignment) (resources.GetObject("buttonOK.ImageAlign")));
            this.buttonOK.ImageIndex = ((int) (resources.GetObject("buttonOK.ImageIndex")));
            this.buttonOK.ImeMode = ((System.Windows.Forms.ImeMode) (resources.GetObject("buttonOK.ImeMode")));
            this.buttonOK.Location = ((System.Drawing.Point) (resources.GetObject("buttonOK.Location")));
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.RightToLeft =
                ((System.Windows.Forms.RightToLeft) (resources.GetObject("buttonOK.RightToLeft")));
            this.buttonOK.Size = ((System.Drawing.Size) (resources.GetObject("buttonOK.Size")));
            this.buttonOK.TabIndex = ((int) (resources.GetObject("buttonOK.TabIndex")));
            this.buttonOK.Text = resources.GetString("buttonOK.Text");
            this.buttonOK.TextAlign = ((System.Drawing.ContentAlignment) (resources.GetObject("buttonOK.TextAlign")));
            this.buttonOK.Visible = ((bool) (resources.GetObject("buttonOK.Visible")));
            // 
            // buttonCancel
            // 
            this.buttonCancel.AccessibleDescription = resources.GetString("buttonCancel.AccessibleDescription");
            this.buttonCancel.AccessibleName = resources.GetString("buttonCancel.AccessibleName");
            this.buttonCancel.Anchor =
                ((System.Windows.Forms.AnchorStyles) (resources.GetObject("buttonCancel.Anchor")));
            this.buttonCancel.BackgroundImage =
                ((System.Drawing.Image) (resources.GetObject("buttonCancel.BackgroundImage")));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Dock = ((System.Windows.Forms.DockStyle) (resources.GetObject("buttonCancel.Dock")));
            this.buttonCancel.Enabled = ((bool) (resources.GetObject("buttonCancel.Enabled")));
            this.buttonCancel.FlatStyle =
                ((System.Windows.Forms.FlatStyle) (resources.GetObject("buttonCancel.FlatStyle")));
            this.buttonCancel.Font = ((System.Drawing.Font) (resources.GetObject("buttonCancel.Font")));
            this.buttonCancel.Image = ((System.Drawing.Image) (resources.GetObject("buttonCancel.Image")));
            this.buttonCancel.ImageAlign =
                ((System.Drawing.ContentAlignment) (resources.GetObject("buttonCancel.ImageAlign")));
            this.buttonCancel.ImageIndex = ((int) (resources.GetObject("buttonCancel.ImageIndex")));
            this.buttonCancel.ImeMode = ((System.Windows.Forms.ImeMode) (resources.GetObject("buttonCancel.ImeMode")));
            this.buttonCancel.Location = ((System.Drawing.Point) (resources.GetObject("buttonCancel.Location")));
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.RightToLeft =
                ((System.Windows.Forms.RightToLeft) (resources.GetObject("buttonCancel.RightToLeft")));
            this.buttonCancel.Size = ((System.Drawing.Size) (resources.GetObject("buttonCancel.Size")));
            this.buttonCancel.TabIndex = ((int) (resources.GetObject("buttonCancel.TabIndex")));
            this.buttonCancel.Text = resources.GetString("buttonCancel.Text");
            this.buttonCancel.TextAlign =
                ((System.Drawing.ContentAlignment) (resources.GetObject("buttonCancel.TextAlign")));
            this.buttonCancel.Visible = ((bool) (resources.GetObject("buttonCancel.Visible")));
            // 
            // JobsReferencingScheduleForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
            this.AccessibleName = resources.GetString("$this.AccessibleName");
            this.AutoScaleBaseSize = ((System.Drawing.Size) (resources.GetObject("$this.AutoScaleBaseSize")));
            this.AutoScroll = ((bool) (resources.GetObject("$this.AutoScroll")));
            this.AutoScrollMargin = ((System.Drawing.Size) (resources.GetObject("$this.AutoScrollMargin")));
            this.AutoScrollMinSize = ((System.Drawing.Size) (resources.GetObject("$this.AutoScrollMinSize")));
            this.BackgroundImage = ((System.Drawing.Image) (resources.GetObject("$this.BackgroundImage")));
            this.CancelButton = this.buttonCancel;
            this.ClientSize = ((System.Drawing.Size) (resources.GetObject("$this.ClientSize")));
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonHelp);
            this.Controls.Add(this.separatorContainerFromButtons);
            this.Controls.Add(this.panelContainer);
            this.Enabled = ((bool) (resources.GetObject("$this.Enabled")));
            this.Font = ((System.Drawing.Font) (resources.GetObject("$this.Font")));
            this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
            this.ImeMode = ((System.Windows.Forms.ImeMode) (resources.GetObject("$this.ImeMode")));
            this.Location = ((System.Drawing.Point) (resources.GetObject("$this.Location")));
            this.MaximumSize = ((System.Drawing.Size) (resources.GetObject("$this.MaximumSize")));
            this.MinimumSize = ((System.Drawing.Size) (resources.GetObject("$this.MinimumSize")));
            this.Name = "JobsReferencingScheduleForm";
            this.RightToLeft = ((System.Windows.Forms.RightToLeft) (resources.GetObject("$this.RightToLeft")));
            this.StartPosition = ((System.Windows.Forms.FormStartPosition) (resources.GetObject("$this.StartPosition")));
            this.Text = resources.GetString("$this.Text");
            this.ResumeLayout(false);

        }

        #endregion

        #region Initialize Inner UserControl

        /// <summary>
        /// used for estitic purposes only
        /// </summary>
        private void InitializeInnerUserControl()
        {
            try
            {
                System.Diagnostics.Debug.Assert(m_innerControl == null, "inner control was already initialized");

                this.SuspendLayout();
                this.panelContainer.SuspendLayout();

                System.Diagnostics.Debug.Assert(this.Parent != null);
                if (this.Parent != null)
                {
                    this.BackColor = Parent.BackColor;
                    this.Font = Parent.Font;
                }

                m_innerControl = new JobsReferencingScheduleControl();
                m_innerControl.Dock = DockStyle.Fill;

                this.panelContainer.Controls.Clear();
                this.panelContainer.Controls.Add(m_innerControl);
            }
            finally
            {
                this.panelContainer.ResumeLayout();
                this.ResumeLayout();
            }
        }

        /// <summary>
        /// actual initialization
        /// </summary>
        private void InitializeInnerUserControl(CDataContainer context, int scheduleId, string scheduleName,
            bool readOnlyMode)
        {
            try
            {
                System.Diagnostics.Debug.Assert(m_innerControl == null, "inner control was already initialized");
                this.SuspendLayout();
                this.panelContainer.SuspendLayout();

                m_innerControl = new JobsReferencingScheduleControl(context, scheduleId, scheduleName, readOnlyMode);
                m_innerControl.Dock = DockStyle.Fill;

                this.panelContainer.Controls.Clear();
                this.panelContainer.Controls.Add(m_innerControl);
            }
            finally
            {
                this.panelContainer.ResumeLayout();
                this.ResumeLayout();
            }
        }

        #endregion

        #region Overrides - OnHelpRequested

        /// <summary>
        /// hook with standard help processing
        /// </summary>
        /// <param name="hevent"></param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            ShowHelp();

            hevent.Handled = true;
            base.OnHelpRequested(hevent);
        }

        #endregion

        #region Private Implementation - ShowHelp

        private void ShowHelp()
        {
            //F1 request might come in even when Help button is hidden
            if (m_serviceProvider == null)
            {
                return;
            }

            string key = AssemblyVersionInfo.VersionHelpKeywordPrefix + ".ag.job.jobsreferencingaschedule.f1";
                // pick schedule bol link
            ILaunchFormHost2 host2 = m_serviceProvider.GetService(typeof (ILaunchFormHost2)) as ILaunchFormHost2;
            System.Diagnostics.Debug.Assert(host2 != null,
                "Service Provider could not provide us the ILaunchFormHost2 service required for displaying books online");

            if (host2 == null)
            {
                return;
            }

            host2.ShowHelp(key);
        }

        #endregion

        #region Buttons Initialize / Events

        /// <summary>
        /// normaly this shouold be handled by WinForms designer
        /// but currently Rascal and Everett designers are unusuable so hooking them manually
        /// </summary>
        private void InitializeButtonEvents()
        {
            this.buttonOK.Click += new EventHandler(this.OnButtonOKClick);
            this.buttonCancel.Click += new EventHandler(this.OnButtonCancelClick);
            this.buttonHelp.Click += new EventHandler(this.OnButtonHelpClick);

            if (m_serviceProvider == null)
            {
                this.buttonHelp.Visible = false;
            }
        }

        private void OnButtonOKClick(object source, EventArgs args)
        {
            System.Diagnostics.Debug.Assert(m_innerControl != null);

            m_innerControl.ApplyChanges();

            this.DialogResult = m_innerControl.NoOfSelectedJobs != -1
                ? DialogResult.OK
                : DialogResult.None;
            this.Close();
        }

        private void OnButtonCancelClick(object source, EventArgs args)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnButtonHelpClick(object source, EventArgs args)
        {
            ShowHelp();
        }

        #endregion

    }
}
