using System;
using System.Windows.Forms;
using System.Resources;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginData;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// The Server Roles page of the create/modify Logins dialog.
    /// </summary>
    internal class CreateLoginServerRoles : SqlManagementUserControl, IPanelForm
	{

		private LoginPrototype	prototype;
		private bool			initializing	= false;

		private System.Windows.Forms.Label serverRoleDescription;
		private System.Windows.Forms.Label serverRolesLabel;
		private System.Windows.Forms.CheckedListBox serverRoles;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		//private System.ComponentModel.Container components;

		/// <summary>
		/// constructor
		/// </summary>
		public CreateLoginServerRoles()
		{
			this.prototype		= null;
			this.HelpF1Keyword	= AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.serverroles.f1";

			InitializeComponent();
		}

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="context">The server context for this control</param>
		public CreateLoginServerRoles(CDataContainer context, LoginPrototype prototype)
		{
			DataContainer		= context;
			this.prototype		= prototype;
			this.HelpF1Keyword	= AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.serverroles.f1";

			InitializeComponent();
		}

		/// <summary>
		/// Initialize this control
		/// </summary>
		private void InitializeControlData()
		{
			this.initializing = true;
			serverRoles.Items.Clear();

			foreach (string roleName in this.prototype.ServerRoles.ServerRoleNames)
			{
				CheckState state = this.prototype.ServerRoles.IsMember(roleName) ? CheckState.Checked : CheckState.Unchecked;
				serverRoles.Items.Add(roleName, state);
			}

			this.initializing = false;
		}

		/// <summary>
		/// Handle changes to the check status of a server role
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCheckChanged(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			if (!this.initializing)
			{
				string roleName = serverRoles.Items[e.Index].ToString();

				if(0 == String.Compare(roleName, "public", StringComparison.Ordinal))
				{
					e.NewValue	= CheckState.Checked;
				}
				else
				{
					this.prototype.ServerRoles.SetMember(roleName, (e.NewValue == CheckState.Checked));
				}	
			}
		}


		/// <summary>
		/// Set server roles for the login
		/// </summary>
		/// <param name="sender"></param>
		public override void OnRunNow(object sender)
		{
			try
			{
				this.ExecutionMode	= ExecutionMode.Success;
				string loginName	= this.prototype.LoginName;
				
				if ((loginName == null) || (loginName.Length == 0))
				{
					ResourceManager resourceManager = new ResourceManager(
						"Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
						typeof(CreateLoginServerRoles).Assembly);

					string blankLogin = resourceManager.GetString("error.blankLogin");

					throw new SmoException(blankLogin);
				}

				this.prototype.ApplyServerRoleChanges(DataContainer.Server);
			}
			catch(Exception e)
			{
				DisplayExceptionMessage(e);

				this.ExecutionMode = ExecutionMode.Failure;
			}


		}


		#region IPanel implementation
		
		UserControl IPanelForm.Panel
		{
			get
			{
				return this;
			} 
			
		}

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

		void IPanelForm.OnSelection(TreeNode node)
		{
			this.InitializeControlData();
		}

		void IPanelForm.OnPanelLoseSelection(TreeNode node)
		{
		}


		
        public override void OnReset(object sender)
        {
            base.OnReset(sender);

            this.InitializeControlData();
		}



		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateLoginServerRoles));
            this.serverRoleDescription = new System.Windows.Forms.Label();
            this.serverRolesLabel = new System.Windows.Forms.Label();
            this.serverRoles = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // serverRoleDescription
            // 
            resources.ApplyResources(this.serverRoleDescription, "serverRoleDescription");
            this.serverRoleDescription.Name = "serverRoleDescription";
            // 
            // serverRolesLabel
            // 
            resources.ApplyResources(this.serverRolesLabel, "serverRolesLabel");
            this.serverRolesLabel.Name = "serverRolesLabel";
            // 
            // serverRoles
            // 
            resources.ApplyResources(this.serverRoles, "serverRoles");
            this.serverRoles.CheckOnClick = true;
            this.serverRoles.FormattingEnabled = true;
            this.serverRoles.Name = "serverRoles";
            this.serverRoles.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnCheckChanged);
            // 
            // CreateLoginServerRoles
            // 
            this.Controls.Add(this.serverRoles);
            this.Controls.Add(this.serverRolesLabel);
            this.Controls.Add(this.serverRoleDescription);
            resources.ApplyResources(this, "$this");
            this.Name = "CreateLoginServerRoles";
            this.ResumeLayout(false);

        }
		#endregion

	}
}








