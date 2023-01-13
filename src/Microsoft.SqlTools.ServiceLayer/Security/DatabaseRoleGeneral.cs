using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Xml;
using System.Data;
using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.UI.Grid;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// DatabaseRoleGeneral - main panel for database role
    /// </summary>
    internal class DatabaseRoleGeneral : SqlManagementUserControl, IPanelForm, ISupportValidation
    {
#region Trace support
        private const string componentName = "DatabaseRoleGeneral";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
#endregion

        private class SchemaOwnership
        {
            public bool initiallyOwned;
            public bool currentlyOwned;

            public SchemaOwnership(bool initiallyOwned)
            {
                this.initiallyOwned = initiallyOwned;
                this.currentlyOwned = initiallyOwned;
            }
        }

        private class RoleMembership
        {
            public bool initiallyAMember;
            public bool currentlyAMember;

            public RoleMembership(bool initiallyAMember)
            {
                this.initiallyAMember = initiallyAMember;
                this.currentlyAMember = initiallyAMember;
            }

            public RoleMembership(bool initiallyAMember, bool currentlyAMember)
            {
                this.initiallyAMember = initiallyAMember;
                this.currentlyAMember = currentlyAMember;
            }
        }


#region Constants - urn fields, etc...
        private const string ownerField = "Owner";
        private const string schemaOwnerField = "Owner";
        private const string schemaNameField = "Name";
        private const string memberNameField = "Name";
        private const string memberUrnField = "Urn";
#endregion

#region Constants - grid columns positions, etc...
        private const int colSchemasChecked = 0;
        private const int colSchemasOwnedSchemas = 1;

        private const int colMembershipBitmap = 0;
        private const int colMembershipRoleMembers = 1;

        private const int sizeCheckboxColumn = 20;
        private const int sizeBitmapColumn = 20;
#endregion

#region UI Variables
        private System.Windows.Forms.Panel panelEntireUserControl;
        private System.Windows.Forms.Panel panelDbRoleGeneralInfo;
        private System.Windows.Forms.Panel panelMembership;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.Button buttonAdd;
		private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid gridRoleMembership;
        private System.Windows.Forms.Panel panelSchema;
		private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid gridSchemasOwned;
        private System.Windows.Forms.Label labelDbRoleName;
        private System.Windows.Forms.TextBox textBoxDbRoleName;
        private System.Windows.Forms.Label labelDbRoleOwner;
        private System.Windows.Forms.TextBox textBoxOwner;
        private System.Windows.Forms.Button buttonSearchOwner;
        private System.Windows.Forms.Label labelMembersOfDbRole;
        private System.Windows.Forms.Label labelSchemasOwnedByDbRole;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
#endregion

#region Non-UI variables

        private System.Xml.XmlDocument document = null;
        private bool panelInitialized = false;

        // info extracted from context
        private string              serverName;
        private string              databaseName;
        private string              dbroleName;
        private string              dbroleUrn;

        // initial values loaded from server
        private string              initialOwner;

        private string              ownerName       = String.Empty;
        private string              roleName        = String.Empty;
        private HybridDictionary    schemaOwnership = null;
        private HybridDictionary    roleMembers     = null;

#endregion

#region Properties: CreateNew/Properties mode
        private bool IsPropertiesMode
        {
            get
            {
                return(dbroleName!=null) && (dbroleName.Trim().Length != 0);
            }
        }
#endregion

#region Constructors / Dispose
        public DatabaseRoleGeneral()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
        }

        public DatabaseRoleGeneral(CDataContainer context)
        {
            

            this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.dbroleproperties.general.f1";


            InitializeComponent();
            DataContainer = context;

            if (DataContainer != null)
            {
                document = DataContainer.Document;
            }
            else
            {
                document = null;
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }
#endregion

#region IPanelForm interface implementation

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of Panel property
        /// </summary>
        UserControl IPanelForm.Panel
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnSelection
        /// </summary>
        void IPanelForm.OnSelection(TreeNode node)
        {
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnPanelLoseSelection
        /// </summary>
        /// <param name="node"></param>
        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
            System.Diagnostics.Debug.Assert(this.DataContainer != null);
            if (IsPropertiesMode == false)
            {
                this.DataContainer.ObjectName = this.textBoxDbRoleName.Text;
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnReset
        /// </summary>
        /// <param name="node"></param>
        public override void OnReset(object sender)
        {
            base.OnReset(sender);

            if (panelInitialized)
            {
                IPanelForm ipf = this as IPanelForm;
                if (ipf == null)
                {
                    return;
                }

                panelInitialized = false;
                ipf.OnInitialization();
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnInitialization
        /// </summary>
        void IPanelForm.OnInitialization()
        {
            if (panelInitialized == true)
            {
                return;
            }

            panelInitialized = true;
            try
            {
                LoadData(document);
                InitProp();
                IPanelForm panelform = this as IPanelForm;
                panelform.Panel.Enabled = true;
            }
            catch (Exception e)
            {
                IPanelForm panelform = this as IPanelForm;
                panelform.Panel.Enabled = false;

                System.Diagnostics.Trace.TraceError(e.Message);
                throw (e);
            }
        }

        /// <summary>
        /// interface IPanelForm
        /// 
        /// implementation of OnPanelRunNow
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow (object sender)
        {
            base.OnRunNow(sender);

            if (panelInitialized)
            {
                try
                {
                    SendDataToServer();
                    this.ExecutionMode = ExecutionMode.Success;
                }
                catch (Exception e)
                {
                    DisplayExceptionMessage(e);

                    this.ExecutionMode = ExecutionMode.Failure;
                    // throw;
                }
            }
        }

#endregion

#region Implementation: LoadData(), InitProp(), SendDataToServer()


        /// <summary>
        /// LoadData
        /// 
        /// loads connection parameters from an xml
        /// </summary>
        /// <param name="doc"></param>
        private void LoadData(XmlDocument doc)
        {
            

            STParameters                            param;
            bool                                    bStatus;

            param       = new STParameters();

            param.SetDocument(doc);

            bStatus         = param.GetParam("servername", ref this.serverName);
            bStatus         = param.GetParam("database", ref this.databaseName);

            bStatus         = param.GetParam("role", ref this.dbroleName);
            bStatus         = param.GetParam("urn", ref this.dbroleUrn);
        }


        /// <summary>
        ///  InitProp
        ///  
        ///  talks with enumerator an retrievives info
        /// </summary>
        private void InitProp()
        {
            

            System.Diagnostics.Debug.Assert(this.serverName!=null);
            System.Diagnostics.Debug.Assert((this.databaseName!=null) && (this.databaseName.Trim().Length!=0));

            InitializeBitmapAndIcons(); // bitmapMember

            InitializeSchemasGridColumns();
            if (this.DataContainer.Server.Information.Version.Major >= 9)
            {
                LoadSchemas();
                FillSchemasGrid();
            }
            else
            {
                panelSchema.Enabled = false;
            }

            LoadMembership();
            InitializeMembershipGridColumns();
            FillMembershipGrid();

            if (this.IsPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                System.Diagnostics.Debug.Assert(this.dbroleName!=null);
                System.Diagnostics.Debug.Assert(this.dbroleName.Trim().Length !=0);
                System.Diagnostics.Debug.Assert(this.dbroleUrn!=null);
                System.Diagnostics.Debug.Assert(this.dbroleUrn.Trim().Length != 0);

                this.textBoxDbRoleName.Text = this.dbroleName;

                Enumerator en = new Enumerator();
                Request req = new Request();
                req.Fields = new String [] {DatabaseRoleGeneral.ownerField};

                if ((this.dbroleUrn!=null) && (this.dbroleUrn.Trim().Length != 0))
                {
                    req.Urn = this.dbroleUrn;
                }
                else
                {
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Role[@Name='" + Urn.EscapeString(this.dbroleName) + "]";
                }

                DataTable dt = en.Process(ServerConnection,req);
                System.Diagnostics.Debug.Assert(dt!=null);
                System.Diagnostics.Debug.Assert(dt.Rows.Count==1);

                if (dt.Rows.Count==0)
                {
                    throw new Exception(DatabaseRoleSR.ErrorDbRoleNotFound);
                }

                DataRow dr = dt.Rows[0];
                this.initialOwner = Convert.ToString(dr[DatabaseRoleGeneral.ownerField],System.Globalization.CultureInfo.InvariantCulture);
                this.textBoxOwner.Text = this.initialOwner;
            }
            else
            {
                // initialize with empty values in create new mode
                this.textBoxDbRoleName.Text = String.Empty;
                this.textBoxOwner.Text = String.Empty;
            }

            // update UI enable/disable controls
            EnableDisableControls();
        }


        public override void OnGatherUiInformation(RunType runType)
        {
            base.OnGatherUiInformation(runType);

            this.ownerName  = this.textBoxOwner.Text;
            this.roleName   = this.textBoxDbRoleName.Text;
        }

        /// <summary>
        /// SendDataToServer
        /// 
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        private void SendDataToServer()
        {
            

            System.Diagnostics.Debug.Assert(this.databaseName != null && this.databaseName.Trim().Length != 0, "database name is empty");
            System.Diagnostics.Debug.Assert(this.DataContainer.Server != null, "server is null");

            Database database = this.DataContainer.Server.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(database!= null, "database is null");

            DatabaseRole role = null;

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(this.dbroleName != null && this.dbroleName.Trim().Length != 0, "role name is empty");

                role = database.Roles[this.dbroleName];
                System.Diagnostics.Debug.Assert(role != null, "role is null");

                if (0 != String.Compare(this.ownerName, this.initialOwner, StringComparison.Ordinal))
                {
                    role.Owner = this.ownerName;
                    role.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                role = new DatabaseRole(database, this.roleName);
                if (this.ownerName.Length != 0)
                {
                    role.Owner = this.ownerName;
                }

                role.Create();
            }

            SendToServerSchemaOwnershipChanges(database, role);
            SendToServerMembershipChanges(database, role);

            this.DataContainer.ObjectName       = role.Name;
            this.DataContainer.SqlDialogSubject = role; // needed by extended properties page
        }

#endregion

#region Update UI enable/disable controls
        private void EnableDisableControls()
        {
            if (this.DataContainer.Server.Information.Version.Major<9)
            {
                panelSchema.Enabled = false;
            }

            if (this.IsPropertiesMode == true)
            {
                this.textBoxDbRoleName.Enabled = false;

                this.AllUIEnabled = true;
            }
            else
            {
                this.textBoxDbRoleName.Enabled = true;

                this.AllUIEnabled = (this.textBoxDbRoleName.Text.Trim().Length!=0);
            }

            buttonRemove.Enabled = (gridRoleMembership.SelectedRow>=0);
        }
#endregion

#region ISupportValidation Members

        bool ISupportValidation.Validate()
        {
            if (IsPropertiesMode == false)
            {
                if (this.textBoxDbRoleName.Text.Trim().Length==0)
                {
                    System.Exception e = new System.Exception(DatabaseRoleSR.Error_SpecifyAName);
                    this.DisplayExceptionMessage(e);

                    return false;
                }
            }

            return true;
        }

#endregion

#region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DatabaseRoleGeneral));
            this.panelEntireUserControl = new System.Windows.Forms.Panel();
            this.panelSchema = new System.Windows.Forms.Panel();
            this.gridSchemasOwned = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            this.labelSchemasOwnedByDbRole = new System.Windows.Forms.Label();
            this.panelMembership = new System.Windows.Forms.Panel();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.gridRoleMembership = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            this.labelMembersOfDbRole = new System.Windows.Forms.Label();
            this.panelDbRoleGeneralInfo = new System.Windows.Forms.Panel();
            this.buttonSearchOwner = new System.Windows.Forms.Button();
            this.textBoxOwner = new System.Windows.Forms.TextBox();
            this.labelDbRoleOwner = new System.Windows.Forms.Label();
            this.textBoxDbRoleName = new System.Windows.Forms.TextBox();
            this.labelDbRoleName = new System.Windows.Forms.Label();
            this.panelEntireUserControl.SuspendLayout();
            this.panelSchema.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).BeginInit();
            this.panelMembership.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).BeginInit();
            this.panelDbRoleGeneralInfo.SuspendLayout();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // panelEntireUserControl
            // 
            this.panelEntireUserControl.Controls.Add(this.panelSchema);
            this.panelEntireUserControl.Controls.Add(this.panelMembership);
            this.panelEntireUserControl.Controls.Add(this.panelDbRoleGeneralInfo);
            resources.ApplyResources(this.panelEntireUserControl, "panelEntireUserControl");
            this.panelEntireUserControl.Name = "panelEntireUserControl";
            // 
            // panelSchema
            // 
            resources.ApplyResources(this.panelSchema, "panelSchema");
            this.panelSchema.Controls.Add(this.gridSchemasOwned);
            this.panelSchema.Controls.Add(this.labelSchemasOwnedByDbRole);
            this.panelSchema.Name = "panelSchema";
            // 
            // gridSchemasOwned
            // 
            resources.ApplyResources(this.gridSchemasOwned, "gridSchemasOwned");
            this.gridSchemasOwned.BackColor = System.Drawing.SystemColors.Window;
            this.gridSchemasOwned.ForceEnabled = false;
            this.gridSchemasOwned.Name = "gridSchemasOwned";
            this.gridSchemasOwned.MouseButtonClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventHandler(this.gridSchemasOwned_MouseButtonClicked);
            // 
            // labelSchemasOwnedByDbRole
            // 
            resources.ApplyResources(this.labelSchemasOwnedByDbRole, "labelSchemasOwnedByDbRole");
            this.labelSchemasOwnedByDbRole.Name = "labelSchemasOwnedByDbRole";
            // 
            // panelMembership
            // 
            resources.ApplyResources(this.panelMembership, "panelMembership");
            this.panelMembership.Controls.Add(this.buttonRemove);
            this.panelMembership.Controls.Add(this.buttonAdd);
            this.panelMembership.Controls.Add(this.gridRoleMembership);
            this.panelMembership.Controls.Add(this.labelMembersOfDbRole);
            this.panelMembership.Name = "panelMembership";
            // 
            // buttonRemove
            // 
            resources.ApplyResources(this.buttonRemove, "buttonRemove");
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
            // 
            // buttonAdd
            // 
            resources.ApplyResources(this.buttonAdd, "buttonAdd");
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // gridRoleMembership
            // 
            resources.ApplyResources(this.gridRoleMembership, "gridRoleMembership");
            this.gridRoleMembership.BackColor = System.Drawing.SystemColors.Window;
            this.gridRoleMembership.ForceEnabled = false;
            this.gridRoleMembership.Name = "gridRoleMembership";
            this.gridRoleMembership.SelectionChanged += new Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventHandler(this.gridRoleMembership_SelectionChanged);
            // 
            // labelMembersOfDbRole
            // 
            resources.ApplyResources(this.labelMembersOfDbRole, "labelMembersOfDbRole");
            this.labelMembersOfDbRole.Name = "labelMembersOfDbRole";
            // 
            // panelDbRoleGeneralInfo
            // 
            resources.ApplyResources(this.panelDbRoleGeneralInfo, "panelDbRoleGeneralInfo");
            this.panelDbRoleGeneralInfo.Controls.Add(this.buttonSearchOwner);
            this.panelDbRoleGeneralInfo.Controls.Add(this.textBoxOwner);
            this.panelDbRoleGeneralInfo.Controls.Add(this.labelDbRoleOwner);
            this.panelDbRoleGeneralInfo.Controls.Add(this.textBoxDbRoleName);
            this.panelDbRoleGeneralInfo.Controls.Add(this.labelDbRoleName);
            this.panelDbRoleGeneralInfo.Name = "panelDbRoleGeneralInfo";
            // 
            // buttonSearchOwner
            // 
            resources.ApplyResources(this.buttonSearchOwner, "buttonSearchOwner");
            this.buttonSearchOwner.Name = "buttonSearchOwner";
            this.buttonSearchOwner.Click += new System.EventHandler(this.buttonSearchOwner_Click);
            // 
            // textBoxOwner
            // 
            resources.ApplyResources(this.textBoxOwner, "textBoxOwner");
            this.textBoxOwner.Name = "textBoxOwner";
            // 
            // labelDbRoleOwner
            // 
            resources.ApplyResources(this.labelDbRoleOwner, "labelDbRoleOwner");
            this.labelDbRoleOwner.Name = "labelDbRoleOwner";
            // 
            // textBoxDbRoleName
            // 
            resources.ApplyResources(this.textBoxDbRoleName, "textBoxDbRoleName");
            this.textBoxDbRoleName.Name = "textBoxDbRoleName";
            // 
            // labelDbRoleName
            // 
            resources.ApplyResources(this.labelDbRoleName, "labelDbRoleName");
            this.labelDbRoleName.Name = "labelDbRoleName";
            // 
            // DatabaseRoleGeneral
            // 
            this.Controls.Add(this.panelEntireUserControl);
            this.Name = "DatabaseRoleGeneral";
            resources.ApplyResources(this, "$this");
            this.panelEntireUserControl.ResumeLayout(false);
            this.panelSchema.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).EndInit();
            this.panelMembership.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).EndInit();
            this.panelDbRoleGeneralInfo.ResumeLayout(false);
            this.panelDbRoleGeneralInfo.PerformLayout();
            this.ResumeLayout(false);

        }
#endregion

#region Schemas - general operations with ...
        /// <summary>
        /// loads initial schemas from server together with information about the schema owner
        /// </summary>
        private void LoadSchemas()
        {
            this.schemaOwnership = new HybridDictionary();

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String [] {DatabaseRoleGeneral.schemaNameField, DatabaseRoleGeneral.schemaOwnerField};
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";

            DataTable dt = en.Process(ServerConnection,req);
            System.Diagnostics.Debug.Assert((dt != null) && (0 < dt.Rows.Count), "enumerator did not return schemas");
            System.Diagnostics.Debug.Assert(!this.IsPropertiesMode || (this.dbroleName.Length != 0), "role name is not known");

            foreach (DataRow dr in dt.Rows)
            {
                string  schemaName      = Convert.ToString(dr[DatabaseRoleGeneral.schemaNameField],System.Globalization.CultureInfo.InvariantCulture);
                string  schemaOwner     = Convert.ToString(dr[DatabaseRoleGeneral.schemaOwnerField],System.Globalization.CultureInfo.InvariantCulture);
                bool    roleOwnsSchema  = 
                    this.IsPropertiesMode &&
                    (0 == String.Compare(this.dbroleName, schemaOwner, StringComparison.Ordinal));

                this.schemaOwnership[schemaName] = new SchemaOwnership(roleOwnsSchema);
            }
        }


        /// <summary>
        /// initializes the columns and headers of schema grid - but doesnt populate grid with any data
        /// </summary>
        private void InitializeSchemasGridColumns()
        {
            Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridSchemasOwned;

            if (grid.RowsNumber != 0)
            {
                grid.DeleteAllRows();
            }

            while (grid.ColumnsNumber != 0)
            {
                grid.DeleteColumn(0);
            }

            GridColumnInfo      colInfo     = null;

            // checkbox owned/not-owned
            colInfo             = new GridColumnInfo();
            colInfo.ColumnWidth = sizeCheckboxColumn;
            colInfo.WidthType   = GridColumnWidthType.InPixels;
            colInfo.ColumnType  = GridColumnType.Checkbox;
            grid.AddColumn(colInfo);

            // schema name
            colInfo             = new GridColumnInfo();
            colInfo.ColumnWidth = grid.Width - sizeCheckboxColumn - 2;
            colInfo.WidthType   = GridColumnWidthType.InPixels;
            grid.AddColumn(colInfo);

            grid.SetHeaderInfo(colSchemasOwnedSchemas,  DatabaseRoleSR.HeaderOwnedSchemas,      null);

            grid.SelectionType          = GridSelectionType.SingleRow;
            grid.UpdateGrid();
        }

        private void FillSchemasGrid()
        {
            Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridSchemasOwned;

            grid.BeginInit();
            grid.DeleteAllRows();

            IDictionaryEnumerator enumerator = this.schemaOwnership.GetEnumerator();
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                DictionaryEntry     entry   = enumerator.Entry;
                GridCellCollection  row     = new GridCellCollection();
                GridCell            cell    = null;

                string  schemaName              = entry.Key.ToString();
                bool    roleCurrentlyOwnsSchema = ((SchemaOwnership)entry.Value).currentlyOwned;

                // grid is filled either
                //      a) disabled-checked checkboxes: Indeterminate - if already owning schema - we cannot renounce ownership
                //      b) enabled-unchecked checkboxes: Unchecked - user can check / uncheck them and we read final state
                cell = new GridCell(roleCurrentlyOwnsSchema ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Unchecked); 
                row.Add(cell);

                cell = new GridCell(schemaName); 
                row.Add(cell);

                grid.AddRow(row);
            }

            grid.EndInit();

            if (grid.RowsNumber > 0)
            {
                grid.SelectedRow = 0;
            }

        }

        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void SendToServerSchemaOwnershipChanges(Database db, DatabaseRole dbrole)
        {
            if (9 <= this.DataContainer.Server.Information.Version.Major)
            {
                IDictionaryEnumerator enumerator = this.schemaOwnership.GetEnumerator();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    DictionaryEntry de          = enumerator.Entry;
                    string          schemaName  = de.Key.ToString();
                    SchemaOwnership ownership   = (SchemaOwnership)de.Value;

                    // If we are creating a new role, then no schema will have been initially owned by this role.
                    // If we are modifying an existing role, we can only take ownership of roles.  (Ownership can't
                    // be renounced, it can only be positively assigned to a principal.)
                    if (ownership.currentlyOwned && !ownership.initiallyOwned)
                    {
                        Schema schema = db.Schemas[schemaName];
                        schema.Owner = dbrole.Name;
                        schema.Alter();
                    }
                }
            }
        }

        private void gridSchemasOwned_MouseButtonClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventArgs args)
        {
            if ((args.Button == MouseButtons.Left) &&
                (colSchemasChecked == args.ColumnIndex))
            {
                int                 row         = (int) args.RowIndex;
                string              schemaName  = this.gridSchemasOwned.GetCellInfo(row, colSchemasOwnedSchemas).CellData.ToString();
                GridCheckBoxState   newState    = this.FlipCheckbox(this.gridSchemasOwned, row, colSchemasChecked);
                bool                nowOwned    = ((GridCheckBoxState.Checked == newState) || (GridCheckBoxState.Indeterminate == newState));

                ((SchemaOwnership) this.schemaOwnership[schemaName]).currentlyOwned = nowOwned;
            }
        }

#endregion

#region Membership - general operations with ...

        /// <summary>
        /// loads from server initial membership information
        /// </summary>
        private void LoadMembership()
        {
            this.roleMembers = new HybridDictionary();

            if (this.IsPropertiesMode)
            {
                Enumerator  enumerator  = new Enumerator();
                Urn         urn         = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
                                                        Urn.EscapeString(this.databaseName),
                                                        Urn.EscapeString(this.dbroleName));
                string[]    fields      = new string[] { DatabaseRoleGeneral.memberNameField};
                OrderBy[]   orderBy     = new OrderBy[] { new OrderBy(DatabaseRoleGeneral.memberNameField, OrderBy.Direction.Asc)};
                Request     request     = new Request(urn, fields, orderBy);
                DataTable   dt          = enumerator.Process(this.ServerConnection, request);

                foreach (DataRow dr in dt.Rows)
                {
                    string memberName = dr[DatabaseRoleGeneral.memberNameField].ToString();
                    this.roleMembers[memberName] = new RoleMembership(true);
                }
            }
        }

        /// <summary>
        /// initialize grid column headers, but not the content
        /// </summary>
        private void InitializeMembershipGridColumns()
        {
            Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridRoleMembership;

            if (grid.RowsNumber != 0)
            {
                grid.DeleteAllRows();
            }

            while (grid.ColumnsNumber != 0)
            {
                grid.DeleteColumn(0);
            }

            GridColumnInfo      colInfo     = null;

            // bitmap member type
            colInfo             = new GridColumnInfo();
            colInfo.ColumnWidth = sizeBitmapColumn;
            colInfo.WidthType   = GridColumnWidthType.InPixels;
            colInfo.ColumnType  = GridColumnType.Bitmap;
            grid.AddColumn(colInfo);

            // member name
            colInfo             = new GridColumnInfo();
            colInfo.ColumnWidth = grid.Width - sizeBitmapColumn - 2;
            colInfo.WidthType   = GridColumnWidthType.InPixels;
            grid.AddColumn(colInfo);

            grid.SetHeaderInfo(colMembershipRoleMembers, DatabaseRoleSR.HeaderRoleMembers,      null);

            grid.SelectionType          = GridSelectionType.SingleRow;
            grid.UpdateGrid();
        }

        /// <summary>
        /// fills the membership grid with data (bitmaps, names, etc)
        /// </summary>
        private void FillMembershipGrid()
        {
            Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridRoleMembership;

            grid.BeginInit();
            grid.DeleteAllRows();

            IDictionaryEnumerator enumerator = this.roleMembers.GetEnumerator();
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                DictionaryEntry entry       = enumerator.Entry;
                string          memberName  = entry.Key.ToString();
                RoleMembership  membership  = (RoleMembership) entry.Value;

                if (membership.currentlyAMember)
                {
                    GridCellCollection row = new GridCellCollection();
                    GridCell cell = null;

                    cell = new GridCell(bitmapMember); 
                    row.Add(cell);

                    cell = new GridCell(memberName); 
                    row.Add(cell);

                    grid.AddRow(row);
                }
            }

            grid.EndInit();

            if (grid.RowsNumber > 0)
            {
                grid.SelectedRow = 0;
            }
        }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        {
            IDictionaryEnumerator enumerator = this.roleMembers.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                DictionaryEntry entry       = enumerator.Entry;
                string          memberName  = entry.Key.ToString();
                RoleMembership  membership  = (RoleMembership) entry.Value;

                if (!membership.initiallyAMember && membership.currentlyAMember)
                {
                    dbrole.AddMember(memberName);
                }
                else if (membership.initiallyAMember && !membership.currentlyAMember)
                {
                    dbrole.DropMember(memberName);
                }
            }
        }

        private void gridRoleMembership_SelectionChanged(object sender, Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventArgs args)
        {
            EnableDisableControls();
        }

        private void buttonAdd_Click(object sender, System.EventArgs e)
        {

            using (SqlObjectSearch dlg = new SqlObjectSearch(
                                                            this.Font,
                                                            iconSearchRolesAndUsers,
                                                            this.HelpProvider,
                                                            DatabaseRoleSR.Add_DialogTitle,
                                                            this.DataContainer.ConnectionInfo,
                                                            this.databaseName,
                                                            new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.DatabaseRole),
                                                            new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.DatabaseRole),
                                                            false))
            {
                if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
                {
                    bool memberAdded = false;

                    this.gridRoleMembership.BeginInit();

                    foreach (SearchableObject principal in dlg.SearchResults)
                    {
                        if (!this.roleMembers.Contains(principal.Name))
                        {
                            this.roleMembers[principal.Name] = new RoleMembership(false, true);
                            memberAdded = true;
                        }
                        else
                        {
                            RoleMembership membership = (RoleMembership) this.roleMembers[principal.Name];

                            if (!membership.currentlyAMember)
                            {
                                membership.currentlyAMember = true;
                                memberAdded = true;
                            }
                        }

                        if (memberAdded)
                        {
                            GridCellCollection row = new GridCellCollection();
                            GridCell cell = null;

                            cell = new GridCell(bitmapMember); 
                            row.Add(cell); 

                            cell = new GridCell(principal.Name); 
                            row.Add(cell);

                            this.gridRoleMembership.AddRow(row);
                        }
                    }

                    this.gridRoleMembership.EndInit();

                    if (memberAdded)
                    {
                        this.gridRoleMembership.SelectedRow = this.gridRoleMembership.RowsNumber - 1;
                    }
                }
            }
        }

        private void buttonRemove_Click(object sender, System.EventArgs e)
        {
            DlgGridControl grid = this.gridRoleMembership;

            int row = this.gridRoleMembership.SelectedRow;
            System.Diagnostics.Debug.Assert(0 <= row, "unexpected row number");

            if (0 <= row)
            {
                string          memberName  = this.gridRoleMembership.GetCellInfo(row, colMembershipRoleMembers).CellData.ToString();
                RoleMembership  membership  = (RoleMembership) this.roleMembers[memberName];

                if (membership.initiallyAMember)
                {
                    membership.currentlyAMember = false;
                }
                else
                {
                    this.roleMembers.Remove(memberName);
                }

                this.gridRoleMembership.DeleteRow(row);
            }
        }
#endregion

#region Bitmaps and Icons
        private Bitmap bitmapMember = null;
        private Icon iconSearchRolesAndUsers = null;
        /// <summary>
        /// initialize bitmaps used for membership grid
        /// </summary>
        private void InitializeBitmapAndIcons()
        {
            CUtils utils = new CUtils();
            bitmapMember = utils.LoadIcon("member.ico").ToBitmap();

            iconSearchRolesAndUsers = utils.LoadIcon("search_users_roles.ico");
        }
#endregion

#region General Grid operations - helpers

        /// <summary>
        /// gets status of checkbox
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="rowno"></param>
        /// <param name="colno"></param>
        /// <returns></returns>
        bool IsEmbeededCheckboxChecked(DlgGridControl grid, int rowno, int colno)
        {
            // get the storage for the cell
            GridCell            cell    = grid.GetCellInfo(rowno, colno);
            GridCheckBoxState   state   = (GridCheckBoxState) cell.CellData;

            return(state == GridCheckBoxState.Checked);
        }


        /// <summary>
        /// flips on/off checkboxes from grid
        /// </summary>
        /// <param name="rowsno"></param>
        /// <param name="colno"></param>
        /// <returns>The checkstate of the checkbox after the click was processed</returns>
        GridCheckBoxState FlipCheckbox(DlgGridControl grid, int rowno, int colno)
        {
            // get the storage for the cell
            GridCell            cell        = grid.GetCellInfo(rowno, colno);
            GridCheckBoxState   oldState    = (GridCheckBoxState) cell.CellData;
            GridCheckBoxState   newState    = GridCheckBoxState.None;


            // explicitly invert the cell state
            switch (oldState)
            {
                case GridCheckBoxState.Checked:
                    newState        = GridCheckBoxState.Unchecked;
                    cell.CellData   = newState;
                    break;
                case GridCheckBoxState.Unchecked:
                    newState        = GridCheckBoxState.Checked;
                    cell.CellData   = newState;
                    break;
                case GridCheckBoxState.Indeterminate:
                    newState = oldState;
                    // do nothing if Indeterminate - this means that entry is checked and r/o (e.g. schemas already owned)
                    break;
                case GridCheckBoxState.None:
                    newState = oldState;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false,"unknown checkbox state");
                    break;
            }

            return newState;
        }


#endregion

#region Non-Grid related events
        private void buttonSearchOwner_Click(object sender, System.EventArgs e)
        {
            DialogResult dr = DialogResult.OK;

            using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
                                                             iconSearchRolesAndUsers,
                                                             this.HelpProvider,
                                                             DatabaseRoleSR.Search_DialogTitle,
                                                             this.DataContainer.ConnectionInfo,
                                                             this.databaseName,
                                                             new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.ApplicationRole, SearchableObjectType.DatabaseRole),
                                                             new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.ApplicationRole, SearchableObjectType.DatabaseRole)))
            {
                dr = dlg.ShowDialog(this.FindForm());

                if (dr == DialogResult.OK)
                {
                    SearchableObject principal = dlg.SearchResults[0];
                    textBoxOwner.Text = principal.Name;
                }
            }
        }

#endregion
    }
}








