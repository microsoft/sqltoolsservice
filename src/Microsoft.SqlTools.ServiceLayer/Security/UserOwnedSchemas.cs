//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.SqlManagerUI.UserData;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal partial class UserOwnedSchemas : SqlManagementUserControl, IPanelForm, ISupportValidation
    {
        private UserOwnedSchemasGrid schemasGrid = null;

        internal UserOwnedSchemas(CDataContainer context)
        {
            InitializeComponent();
            this.DataContainer = context;
            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.databaseuser.ownedschemas.f1";
            
            this.CreateOwnedSchemasGrid();
        }

        private void CreateOwnedSchemasGrid()
        {
            this.SuspendLayout();
            this.ownedSchemaPanel.SuspendLayout();
            
            string gridAccessibleName = this.ownedSchemaLabel.Text.Replace("&", String.Empty);

            this.schemasGrid = new UserOwnedSchemasGrid(
                                    UserPrototypeFactory.GetInstance(this.DataContainer).CurrentPrototype,
                                    gridAccessibleName);

            this.schemasGrid.TabIndex = 0;
            this.schemasGrid.Location = new Point(0, 0);
            this.schemasGrid.Size = new Size(this.ownedSchemaPanel.Width, this.ownedSchemaPanel.Height);
            this.schemasGrid.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Top |
                AnchorStyles.Right |
                AnchorStyles.Bottom;

            this.ownedSchemaPanel.Controls.Add(this.schemasGrid);
            this.ownedSchemaPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            UserPrototypeNew currentPrototype = UserPrototypeFactory.GetInstance(this.DataContainer).CurrentPrototype;

            //In case the UserGeneral/Membership pages are loaded,
            //those will takes care of applying schema ownership changes also.
            //Hence, we only need to apply changes in this method when those are not loaded.
            if (!currentPrototype.IsSchemaOwnershipChangesApplied)
            {
                base.OnRunNow(sender);

                User user = currentPrototype.ApplyChanges();

                this.ExecutionMode = ExecutionMode.Success;
                this.DataContainer.ObjectName = currentPrototype.Name;
                this.DataContainer.SqlDialogSubject = user;                
            }

            //setting back to original after changes are applied
            currentPrototype.IsSchemaOwnershipChangesApplied = false;
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
