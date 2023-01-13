using System;
using System.Windows.Forms;

using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginData;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    internal partial class CreateLoginStatus : SqlManagementUserControl, IPanelForm
    {
        private bool initializing = false;
        private LoginPrototype prototype = null;
        private bool lockedOutCanBeDisabled = true;

        public CreateLoginStatus()
        {
            InitializeComponent();
        }

        public CreateLoginStatus(CDataContainer context, LoginPrototype prototype)
        {
            this.InitializeComponent();
            
            this.DataContainer  = context;
            this.prototype      = prototype;
            this.HelpF1Keyword  = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.status.f1";
        }

        private void OnPermissionChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.WindowsGrantAccess = this.grantConnect.Checked;
            }
        }

        private void OnLoginEnabledChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.IsDisabled = this.disabled.Checked;
            }
        }

        private void OnLoginLockedOutChanged(object sender, EventArgs e)
        {
            if (!this.initializing)
            {
                this.prototype.IsLockedOut = this.lockedOut.Checked;                
            }
        }

        private void InitializeControlData()
        {
            this.initializing = true;
            
            if (this.prototype.WindowsGrantAccess)
            {
                this.grantConnect.Checked = true;
            }
            else
            {
                this.denyConnect.Checked = true;
            }

            if (this.prototype.IsDisabled)
            {
                this.disabled.Checked = true;
            }
            else
            {
                this.enabled.Checked = true;
            }
            this.lockedOut.Checked = this.prototype.IsLockedOut;
            if (this.lockedOutCanBeDisabled) //The lockedOut checkbox can be disabled only when the dialog is launched first time
            {
                this.lockedOut.Enabled = this.prototype.Exists && this.prototype.IsLockedOut;
                this.lockedOutCanBeDisabled = false;
            }

            this.initializing = false;
        }

        #region IPanel implementation

        /// <summary>
        /// IPanelForm.OnInitialization
        /// 
        /// TODO - in order to reduce IPanelForm container load time
        /// and to improve performance, IPanelForm-s should be able
        /// to lazy-initialize themself when IPanelForm.OnInitialization
        /// is called (a continer like TreePanelForm calls the
        /// OnInitialization() method before first OnSelection())
        /// </summary>
        void IPanelForm.OnInitialization()
        {
        }

        public override void OnReset(object sender)
        {
            this.InitializeControlData();
            base.OnReset(sender);
        }

        void IPanelForm.OnSelection(TreeNode node)
        {
            this.InitializeControlData();            
        }

        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
        }

        UserControl IPanelForm.Panel
        {
            get
            {
                return this;
            }
        }

        #endregion
    }
}








