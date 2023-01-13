using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.SqlManagerUI.UserData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    public partial class UserMembership : SqlManagementUserControl, IPanelForm, ISupportValidation
    {
        private UserRoleMembersGrid membershipGrid = null;

        public UserMembership(CDataContainer context)
        {
            InitializeComponent();
            this.DataContainer = context;
            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.databaseuser.membership.f1";
            
            this.CreateMembershipGrid();
        }

        private void CreateMembershipGrid()
        {
            this.SuspendLayout();
            this.userMembershipPanel.SuspendLayout();

            string gridAccessibleName = this.userMembershipLabel.Text.Replace("&", String.Empty);

            this.membershipGrid = new UserRoleMembersGrid(
                                        UserPrototypeFactory.GetInstance(this.DataContainer).CurrentPrototype,
                                        gridAccessibleName);

            this.membershipGrid.TabIndex = 0;
            this.membershipGrid.Location = new Point(0, 0);
            this.membershipGrid.Size = new Size(this.userMembershipPanel.Width, this.userMembershipPanel.Height);
            this.membershipGrid.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Top |
                AnchorStyles.Right |
                AnchorStyles.Bottom;

            this.userMembershipPanel.Controls.Add(this.membershipGrid);
            this.userMembershipPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            UserPrototypeNew currentPrototype = UserPrototypeFactory.GetInstance(this.DataContainer).CurrentPrototype;

            //In case the UserGeneral/OwnedSchemas pages are loaded,
            //those will takes care of applying membership changes also.
            //Hence, we only need to apply changes in this method when those are not loaded.
            if (!currentPrototype.IsRoleMembershipChangesApplied)
            {
                base.OnRunNow(sender);

                User user = currentPrototype.ApplyChanges();

                this.ExecutionMode = ExecutionMode.Success;
                this.DataContainer.ObjectName = currentPrototype.Name;
                this.DataContainer.SqlDialogSubject = user;
            }

            //setting back to original after changes are applied
            currentPrototype.IsRoleMembershipChangesApplied = false;
        }

        #region IPanelForm Members

        void IPanelForm.OnInitialization()
        {
        }

        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
        }

        void IPanelForm.OnSelection(TreeNode node)
        {
        }

        UserControl IPanelForm.Panel
        {
            get { return this; }
        }

        #endregion
    }
}
