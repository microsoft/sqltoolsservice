using System;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    /// <summary>
    /// Summary description for DbRoleCubePermissions.
    /// </summary>
    internal class DbRoleCubePermissions : System.Windows.Forms.UserControl
	{
        private EventArgs args = null;
        public event AccessChangedEventHandler AccessChanged = null;
        public delegate void AccessChangedEventHandler(object sender, EventArgs e);

        private string cubeName = "";
        private string cubeID = "";
        
        private System.Windows.Forms.GroupBox gboxPerm;
        private System.Windows.Forms.GroupBox groupDescr1;
        private System.Windows.Forms.TextBox txtDescr;
        private System.Windows.Forms.Button btAdvanced;
        private System.Windows.Forms.CheckBox cbSQLQueries;
        private System.Windows.Forms.CheckBox cbDrill;
        private System.Windows.Forms.CheckBox cbWrite;
        private System.Windows.Forms.CheckBox cbRead;
        private System.Windows.Forms.CheckBox cbProcess;
        private System.Windows.Forms.CheckBox cbAllowLinking;
        private System.Windows.Forms.CheckBox cbAccessDefinition;
        private System.Windows.Forms.Label lbClientPermissions;
        private System.Windows.Forms.Label lbManagementPermissions;
        private System.Windows.Forms.ComboBox comboAccess;
        private System.Windows.Forms.Label lbAccess;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public DbRoleCubePermissions()
		{			
			InitializeComponent();
		    //this.comboAccess.SelectedIndex = 1;
            this.args = new EventArgs();

		}

        public bool AccessDefinition
        {
            get { return this.cbAccessDefinition.Checked; }
            set { this.cbAccessDefinition.Checked = value; }
        }

        public bool AllowLinking
        {
            get { return this.cbAllowLinking.Checked; }
            set { this.cbAllowLinking.Checked = value; }
        }

        public bool Process
        {
            get { return this.cbProcess.Checked; }
            set { this.cbProcess.Checked = value; }
        }

        public bool Read
        {
            get { return this.cbRead.Checked; }
            set { this.cbRead.Checked = value; }
        }

        public bool Write
        {
            get { return this.cbWrite.Checked; }
            set { this.cbWrite.Checked = value; }
        }

        public bool AllowDrillThrough
        {
            get { return this.cbDrill.Checked; }
            set { this.cbDrill.Checked = value; }
        }

        public bool AllowSqlQueries
        {
            get { return this.cbSQLQueries.Checked; }
            set { this.cbSQLQueries.Checked = value; }
        }
        
        public int Access
        {
            get { return this.comboAccess.SelectedIndex; }
            set { this.comboAccess.SelectedIndex = value; }
        }        

        public string AccessType
        {
            get { return Convert.ToString(this.comboAccess.Items[this.comboAccess.SelectedIndex]); }            
        }
        
        public string CubeName
        {
            get { return this.cubeName; }
            set { this.cubeName = value; }
        }

        public string CubeID
        {
            get { return this.cubeID; }
            set { this.cubeID = value; }
        }
		
		private void OnPermChanged(object sender, System.EventArgs e)
		{
			string			szManagementPermission;
			string			szClientPermissions;
			bool			bState;


			szManagementPermission	= "Management:\t\t";
			szClientPermissions		= "Client:\t\t\t";

			bState		= false;
			if(this.cbAccessDefinition.Checked)
			{
				bState	= true;
				szManagementPermission	+= "Access definition";
			}

			
			if(this.cbAllowLinking.Checked)
			{
				if(bState)
				{
					szManagementPermission	+= " ,";
				}

				szManagementPermission	+= " Process";
			}
			
			if(this.cbProcess.Checked)
			{
				if(bState)
				{
					szManagementPermission	+= " ,";
				}

				szManagementPermission	+= " Process";
			}			

			bState		= false;
			if(this.cbRead.Checked)
			{
				bState	= true;
				szClientPermissions		+= "Read";
			}

			
			if(this.cbWrite.Checked)
			{
				if(bState)
				{
					szClientPermissions	+= ",";
				}

				bState	= true;
				szClientPermissions		+= " Write";
			}

			if(this.cbDrill.Checked)
			{
				if(bState)
				{
					szClientPermissions	+= ",";
				}

				szClientPermissions		+= " Allow Drill Through";
			}

			if(this.cbSQLQueries.Checked)
			{
				if(bState)
				{
					szClientPermissions	+= ",";
				}

				szClientPermissions		+= " Allow SQL Queries";
			}

			
			this.txtDescr.Text	= szManagementPermission + "\r\n" + szClientPermissions;
			

		}

        /// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DbRoleCubePermissions));
            this.gboxPerm = new System.Windows.Forms.GroupBox();
            this.groupDescr1 = new System.Windows.Forms.GroupBox();
            this.txtDescr = new System.Windows.Forms.TextBox();
            this.btAdvanced = new System.Windows.Forms.Button();
            this.cbSQLQueries = new System.Windows.Forms.CheckBox();
            this.cbDrill = new System.Windows.Forms.CheckBox();
            this.cbWrite = new System.Windows.Forms.CheckBox();
            this.cbRead = new System.Windows.Forms.CheckBox();
            this.cbProcess = new System.Windows.Forms.CheckBox();
            this.cbAllowLinking = new System.Windows.Forms.CheckBox();
            this.cbAccessDefinition = new System.Windows.Forms.CheckBox();
            this.lbClientPermissions = new System.Windows.Forms.Label();
            this.lbManagementPermissions = new System.Windows.Forms.Label();
            this.comboAccess = new System.Windows.Forms.ComboBox();
            this.lbAccess = new System.Windows.Forms.Label();
            this.gboxPerm.SuspendLayout();
            this.groupDescr1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gboxPerm
            // 
            resources.ApplyResources(this.gboxPerm, "gboxPerm");
            this.gboxPerm.Controls.Add(this.groupDescr1);
            this.gboxPerm.Controls.Add(this.btAdvanced);
            this.gboxPerm.Controls.Add(this.cbSQLQueries);
            this.gboxPerm.Controls.Add(this.cbDrill);
            this.gboxPerm.Controls.Add(this.cbWrite);
            this.gboxPerm.Controls.Add(this.cbRead);
            this.gboxPerm.Controls.Add(this.cbProcess);
            this.gboxPerm.Controls.Add(this.cbAllowLinking);
            this.gboxPerm.Controls.Add(this.cbAccessDefinition);
            this.gboxPerm.Controls.Add(this.lbClientPermissions);
            this.gboxPerm.Controls.Add(this.lbManagementPermissions);
            this.gboxPerm.Controls.Add(this.comboAccess);
            this.gboxPerm.Controls.Add(this.lbAccess);
            this.gboxPerm.Name = "gboxPerm";
            this.gboxPerm.TabStop = false;
            // 
            // groupDescr1
            // 
            resources.ApplyResources(this.groupDescr1, "groupDescr1");
            this.groupDescr1.Controls.Add(this.txtDescr);
            this.groupDescr1.Name = "groupDescr1";
            this.groupDescr1.TabStop = false;
            // 
            // txtDescr
            // 
            resources.ApplyResources(this.txtDescr, "txtDescr");
            this.txtDescr.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDescr.Name = "txtDescr";
            this.txtDescr.ReadOnly = true;
            // 
            // btAdvanced
            // 
            resources.ApplyResources(this.btAdvanced, "btAdvanced");
            this.btAdvanced.Name = "btAdvanced";
            this.btAdvanced.Click += new System.EventHandler(this.btAdvanced_Click);
            // 
            // cbSQLQueries
            // 
            resources.ApplyResources(this.cbSQLQueries, "cbSQLQueries");
            this.cbSQLQueries.Name = "cbSQLQueries";
            // 
            // cbDrill
            // 
            resources.ApplyResources(this.cbDrill, "cbDrill");
            this.cbDrill.Name = "cbDrill";
            // 
            // cbWrite
            // 
            resources.ApplyResources(this.cbWrite, "cbWrite");
            this.cbWrite.Name = "cbWrite";
            // 
            // cbRead
            // 
            resources.ApplyResources(this.cbRead, "cbRead");
            this.cbRead.Name = "cbRead";
            // 
            // cbProcess
            // 
            resources.ApplyResources(this.cbProcess, "cbProcess");
            this.cbProcess.Name = "cbProcess";
            // 
            // cbAllowLinking
            // 
            resources.ApplyResources(this.cbAllowLinking, "cbAllowLinking");
            this.cbAllowLinking.Name = "cbAllowLinking";
            // 
            // cbAccessDefinition
            // 
            resources.ApplyResources(this.cbAccessDefinition, "cbAccessDefinition");
            this.cbAccessDefinition.Name = "cbAccessDefinition";
            // 
            // lbClientPermissions
            // 
            resources.ApplyResources(this.lbClientPermissions, "lbClientPermissions");
            this.lbClientPermissions.Name = "lbClientPermissions";
            // 
            // lbManagementPermissions
            // 
            resources.ApplyResources(this.lbManagementPermissions, "lbManagementPermissions");
            this.lbManagementPermissions.Name = "lbManagementPermissions";
            // 
            // comboAccess
            // 
            resources.ApplyResources(this.comboAccess, "comboAccess");
            this.comboAccess.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAccess.FormattingEnabled = true;
            this.comboAccess.Items.AddRange(new object[] {
            resources.GetString("comboAccess.Items"),
            resources.GetString("comboAccess.Items1"),
            resources.GetString("comboAccess.Items2")});
            this.comboAccess.Name = "comboAccess";
            this.comboAccess.SelectedIndexChanged += new System.EventHandler(this.comboAccess_SelectedIndexChanged);
            // 
            // lbAccess
            // 
            resources.ApplyResources(this.lbAccess, "lbAccess");
            this.lbAccess.Name = "lbAccess";
            // 
            // DbRoleCubePermissions
            // 
            this.Controls.Add(this.gboxPerm);
            this.Name = "DbRoleCubePermissions";
            resources.ApplyResources(this, "$this");
            this.gboxPerm.ResumeLayout(false);
            this.groupDescr1.ResumeLayout(false);
            this.groupDescr1.PerformLayout();
            this.ResumeLayout(false);

        }
		#endregion

        private void comboAccess_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if(this.comboAccess.SelectedIndex == 0)
            {
                this.cbAccessDefinition.Checked = true;
                this.cbAccessDefinition.Enabled = false;
                this.cbAllowLinking.Checked     = true;
                this.cbAllowLinking.Enabled     = false;
                this.cbDrill.Checked            = true;
                this.cbDrill.Enabled            = false;
                this.cbProcess.Checked          = true;
                this.cbProcess.Enabled          = false;
                this.cbRead.Checked             = true;
                this.cbRead.Enabled             = false;
                this.cbWrite.Checked            = true;
                this.cbWrite.Enabled            = false;
                this.cbSQLQueries.Checked       = true;
                this.cbSQLQueries.Enabled       = false;
            }
            if(this.comboAccess.SelectedIndex == 1)
            {
                this.cbAccessDefinition.Checked = false;
                this.cbAccessDefinition.Enabled = false;
                this.cbAllowLinking.Checked     = false;
                this.cbAllowLinking.Enabled     = false;
                this.cbDrill.Checked            = false;
                this.cbDrill.Enabled            = false;
                this.cbProcess.Checked          = false;
                this.cbProcess.Enabled          = false;
                this.cbRead.Checked             = false;
                this.cbRead.Enabled             = false;
                this.cbWrite.Checked            = false;
                this.cbWrite.Enabled            = false;
                this.cbSQLQueries.Checked       = false;
                this.cbSQLQueries.Enabled       = false;
            }
            if(this.comboAccess.SelectedIndex == 2)
            {                
                this.cbAccessDefinition.Enabled = true;                
                this.cbAllowLinking.Enabled     = true;                
                this.cbDrill.Enabled            = true;                
                this.cbProcess.Enabled          = true;                
                this.cbRead.Enabled             = true;                
                this.cbWrite.Enabled            = true;                
                this.cbSQLQueries.Enabled       = true;
            }
            if(null != AccessChanged)
            {
                AccessChanged(this,e);
            }
			OnPermChanged(null,null);
        }

        private void btAdvanced_Click(object sender, System.EventArgs e)
        {
        
        }
	}
}








