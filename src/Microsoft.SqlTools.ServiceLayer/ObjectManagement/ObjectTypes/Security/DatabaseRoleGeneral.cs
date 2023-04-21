//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// DatabaseRoleGeneral - main panel for database role
    /// </summary>
    internal class DatabaseRoleGeneral
    {
        #region Members
        /// <summary>
        /// data container member that contains data specific information like
        /// connection infor, SMO server object or an AMO server object as well
        /// as a hash table where one can manipulate custom data
        /// </summary>
        private CDataContainer dataContainer = null;

        //SMO Server connection that MUST be used for all enumerator calls
        //We'll get this object out of CDataContainer, that must be initialized
        //property by the initialization code
        private ServerConnection serverConnection;

        private bool isPropertiesMode;

        #endregion

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

        #region Non-UI variables

        private System.Xml.XmlDocument document = null;

        // info extracted from context
        private string serverName;
        private string databaseName;
        private string dbroleName;
        private string dbroleUrn;

        // initial values loaded from server
        private string initialOwner;

        private string ownerName = String.Empty;
        private string roleName = String.Empty;
        private HybridDictionary schemaOwnership = null;
        private HybridDictionary roleMembers = null;

        #endregion

        #region Properties: CreateNew/Properties mode
        private bool IsPropertiesMode
        {
            get
            {
                return (dbroleName != null) && (dbroleName.Trim().Length != 0);
            }
        }
        #endregion

        #region Constructors / Dispose
        public DatabaseRoleGeneral()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();
        }

        public DatabaseRoleGeneral(CDataContainer context, DatabaseRoleInfo dbRole, bool isNewObject)
        {
            // InitializeComponent();
            dataContainer = context;

            if (dataContainer != null)
            {
                document = dataContainer.Document;
            }
            else
            {
                document = null;
            }
            isPropertiesMode = !isNewObject;
            dbroleName = dbRole.Name;
            serverName = dataContainer.ServerName;
            databaseName = "TriggerTest";
            serverConnection = dataContainer.Server.ConnectionContext;
            InitProp();
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
            // STrace.Params(ComponentName, "LoadData", "XmlDocument doc=\"{0}\"", doc.OuterXml);

            // STParameters param;
            // bool bStatus;

            // param = new STParameters();

            // param.SetDocument(doc);

            // bStatus = param.GetParam("servername", ref this.serverName);
            // bStatus = param.GetParam("database", ref this.databaseName);

            // bStatus = param.GetParam("role", ref this.dbroleName);
            // bStatus = param.GetParam("urn", ref this.dbroleUrn);
        }


        /// <summary>
        ///  InitProp
        ///
        ///  talks with enumerator an retrievives info
        /// </summary>
        private void InitProp()
        {
            // STrace.Params(ComponentName, "InitProp", "", null);

            System.Diagnostics.Debug.Assert(this.serverName != null);
            System.Diagnostics.Debug.Assert((this.databaseName != null) && (this.databaseName.Trim().Length != 0));


            // InitializeSchemasGridColumns();
            if (this.dataContainer.Server.Information.Version.Major >= 9)
            {
                LoadSchemas();
                // FillSchemasGrid();
            }
            else
            {
                // panelSchema.Enabled = false;
            }

            LoadMembership();
            // InitializeMembershipGridColumns();
            // FillMembershipGrid();

            if (this.IsPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                System.Diagnostics.Debug.Assert(this.dbroleName != null);
                System.Diagnostics.Debug.Assert(this.dbroleName.Trim().Length != 0);
                System.Diagnostics.Debug.Assert(this.dbroleUrn != null);
                System.Diagnostics.Debug.Assert(this.dbroleUrn.Trim().Length != 0);

                // this.textBoxDbRoleName.Text = this.dbroleName;

                Enumerator en = new Enumerator();
                Request req = new Request();
                req.Fields = new String[] { DatabaseRoleGeneral.ownerField };

                if ((this.dbroleUrn != null) && (this.dbroleUrn.Trim().Length != 0))
                {
                    req.Urn = this.dbroleUrn;
                }
                else
                {
                    req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Role[@Name='" + Urn.EscapeString(this.dbroleName) + "]";
                }

                DataTable dt = en.Process(serverConnection, req);
                System.Diagnostics.Debug.Assert(dt != null);
                System.Diagnostics.Debug.Assert(dt.Rows.Count == 1);

                if (dt.Rows.Count == 0)
                {
                    throw new Exception("DatabaseRoleSR.ErrorDbRoleNotFound");
                }

                DataRow dr = dt.Rows[0];
                this.initialOwner = Convert.ToString(dr[DatabaseRoleGeneral.ownerField], System.Globalization.CultureInfo.InvariantCulture);
                // this.textBoxOwner.Text = this.initialOwner;
            }
            else
            {
                // initialize with empty values in create new mode
                // this.textBoxDbRoleName.Text = String.Empty;
                // this.textBoxOwner.Text = String.Empty;
            }

            // update UI enable/disable controls
            // EnableDisableControls();
        }


        // public override void OnGatherUiInformation(RunType runType)
        // {
        //     base.OnGatherUiInformation(runType);

        //     this.ownerName  = this.textBoxOwner.Text;
        //     this.roleName   = this.textBoxDbRoleName.Text;
        // }

        /// <summary>
        /// SendDataToServer
        ///
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        private void SendDataToServer()
        {
            // STrace.Params(ComponentName, "SendDataToServer", "", null);

            // STrace.Assert(this.databaseName != null && this.databaseName.Trim().Length != 0, "database name is empty");
            // STrace.Assert(this.DataContainer.Server != null, "server is null");

            Database database = this.dataContainer.Server.Databases[this.databaseName];
            // STrace.Assert(database!= null, "database is null");

            DatabaseRole role = null;

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                // STrace.Assert(this.dbroleName != null && this.dbroleName.Trim().Length != 0, "role name is empty");

                role = database.Roles[this.dbroleName];
                // STrace.Assert(role != null, "role is null");

                if (0 != String.Compare(this.ownerName, this.initialOwner, StringComparison.Ordinal))
                {
                    role.Owner = this.ownerName;
                    role.Alter();
                }
            }
            else // not in properties mode -> create role
            {
                role = new DatabaseRole(database, this.dbroleName);
                if (this.ownerName.Length != 0)
                {
                    role.Owner = this.ownerName;
                }

                role.Create();
            }

            SendToServerSchemaOwnershipChanges(database, role);
            SendToServerMembershipChanges(database, role);

            this.dataContainer.ObjectName = role.Name;
            this.dataContainer.SqlDialogSubject = role; // needed by extended properties page
        }

        #endregion

        // #region Update UI enable/disable controls
        //         private void EnableDisableControls()
        //         {
        //             if (this.DataContainer.Server.Information.Version.Major<9)
        //             {
        //                 panelSchema.Enabled = false;
        //             }

        //             if (this.IsPropertiesMode == true)
        //             {
        //                 this.textBoxDbRoleName.Enabled = false;

        //                 this.AllUIEnabled = true;
        //             }
        //             else
        //             {
        //                 this.textBoxDbRoleName.Enabled = true;

        //                 this.AllUIEnabled = (this.textBoxDbRoleName.Text.Trim().Length!=0);
        //             }

        //             buttonRemove.Enabled = (gridRoleMembership.SelectedRow>=0);
        //         }
        // #endregion

        // #region ISupportValidation Members

        //         bool ISupportValidation.Validate()
        //         {
        //             if (IsPropertiesMode == false)
        //             {
        //                 if (this.textBoxDbRoleName.Text.Trim().Length==0)
        //                 {
        //                     System.Exception e = new System.Exception(DatabaseRoleSR.Error_SpecifyAName);
        //                     this.DisplayExceptionMessage(e);

        //                     return false;
        //                 }
        //             }

        //             return true;
        //         }

        // #endregion

        // #region Component Designer generated code
        //         /// <summary>
        //         /// Required method for Designer support - do not modify
        //         /// the contents of this method with the code editor.
        //         /// </summary>
        //         private void InitializeComponent()
        //         {
        //             System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DatabaseRoleGeneral));
        //             this.panelEntireUserControl = new System.Windows.Forms.Panel();
        //             this.panelSchema = new System.Windows.Forms.Panel();
        //             this.gridSchemasOwned = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
        //             this.labelSchemasOwnedByDbRole = new System.Windows.Forms.Label();
        //             this.panelMembership = new System.Windows.Forms.Panel();
        //             this.buttonRemove = new System.Windows.Forms.Button();
        //             this.buttonAdd = new System.Windows.Forms.Button();
        //             this.gridRoleMembership = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
        //             this.labelMembersOfDbRole = new System.Windows.Forms.Label();
        //             this.panelDbRoleGeneralInfo = new System.Windows.Forms.Panel();
        //             this.buttonSearchOwner = new System.Windows.Forms.Button();
        //             this.textBoxOwner = new System.Windows.Forms.TextBox();
        //             this.labelDbRoleOwner = new System.Windows.Forms.Label();
        //             this.textBoxDbRoleName = new System.Windows.Forms.TextBox();
        //             this.labelDbRoleName = new System.Windows.Forms.Label();
        //             this.panelEntireUserControl.SuspendLayout();
        //             this.panelSchema.SuspendLayout();
        //             ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).BeginInit();
        //             this.panelMembership.SuspendLayout();
        //             ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).BeginInit();
        //             this.panelDbRoleGeneralInfo.SuspendLayout();
        //             this.SuspendLayout();
        //             this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //             this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //             //
        //             // panelEntireUserControl
        //             //
        //             this.panelEntireUserControl.Controls.Add(this.panelSchema);
        //             this.panelEntireUserControl.Controls.Add(this.panelMembership);
        //             this.panelEntireUserControl.Controls.Add(this.panelDbRoleGeneralInfo);
        //             resources.ApplyResources(this.panelEntireUserControl, "panelEntireUserControl");
        //             this.panelEntireUserControl.Name = "panelEntireUserControl";
        //             //
        //             // panelSchema
        //             //
        //             resources.ApplyResources(this.panelSchema, "panelSchema");
        //             this.panelSchema.Controls.Add(this.gridSchemasOwned);
        //             this.panelSchema.Controls.Add(this.labelSchemasOwnedByDbRole);
        //             this.panelSchema.Name = "panelSchema";
        //             //
        //             // gridSchemasOwned
        //             //
        //             resources.ApplyResources(this.gridSchemasOwned, "gridSchemasOwned");
        //             this.gridSchemasOwned.BackColor = System.Drawing.SystemColors.Window;
        //             this.gridSchemasOwned.ForceEnabled = false;
        //             this.gridSchemasOwned.Name = "gridSchemasOwned";
        //             this.gridSchemasOwned.MouseButtonClicked += new Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventHandler(this.gridSchemasOwned_MouseButtonClicked);
        //             //
        //             // labelSchemasOwnedByDbRole
        //             //
        //             resources.ApplyResources(this.labelSchemasOwnedByDbRole, "labelSchemasOwnedByDbRole");
        //             this.labelSchemasOwnedByDbRole.Name = "labelSchemasOwnedByDbRole";
        //             //
        //             // panelMembership
        //             //
        //             resources.ApplyResources(this.panelMembership, "panelMembership");
        //             this.panelMembership.Controls.Add(this.buttonRemove);
        //             this.panelMembership.Controls.Add(this.buttonAdd);
        //             this.panelMembership.Controls.Add(this.gridRoleMembership);
        //             this.panelMembership.Controls.Add(this.labelMembersOfDbRole);
        //             this.panelMembership.Name = "panelMembership";
        //             //
        //             // buttonRemove
        //             //
        //             resources.ApplyResources(this.buttonRemove, "buttonRemove");
        //             this.buttonRemove.Name = "buttonRemove";
        //             this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
        //             //
        //             // buttonAdd
        //             //
        //             resources.ApplyResources(this.buttonAdd, "buttonAdd");
        //             this.buttonAdd.Name = "buttonAdd";
        //             this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
        //             //
        //             // gridRoleMembership
        //             //
        //             resources.ApplyResources(this.gridRoleMembership, "gridRoleMembership");
        //             this.gridRoleMembership.BackColor = System.Drawing.SystemColors.Window;
        //             this.gridRoleMembership.ForceEnabled = false;
        //             this.gridRoleMembership.Name = "gridRoleMembership";
        //             this.gridRoleMembership.SelectionChanged += new Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventHandler(this.gridRoleMembership_SelectionChanged);
        //             //
        //             // labelMembersOfDbRole
        //             //
        //             resources.ApplyResources(this.labelMembersOfDbRole, "labelMembersOfDbRole");
        //             this.labelMembersOfDbRole.Name = "labelMembersOfDbRole";
        //             //
        //             // panelDbRoleGeneralInfo
        //             //
        //             resources.ApplyResources(this.panelDbRoleGeneralInfo, "panelDbRoleGeneralInfo");
        //             this.panelDbRoleGeneralInfo.Controls.Add(this.buttonSearchOwner);
        //             this.panelDbRoleGeneralInfo.Controls.Add(this.textBoxOwner);
        //             this.panelDbRoleGeneralInfo.Controls.Add(this.labelDbRoleOwner);
        //             this.panelDbRoleGeneralInfo.Controls.Add(this.textBoxDbRoleName);
        //             this.panelDbRoleGeneralInfo.Controls.Add(this.labelDbRoleName);
        //             this.panelDbRoleGeneralInfo.Name = "panelDbRoleGeneralInfo";
        //             //
        //             // buttonSearchOwner
        //             //
        //             resources.ApplyResources(this.buttonSearchOwner, "buttonSearchOwner");
        //             this.buttonSearchOwner.Name = "buttonSearchOwner";
        //             this.buttonSearchOwner.Click += new System.EventHandler(this.buttonSearchOwner_Click);
        //             //
        //             // textBoxOwner
        //             //
        //             resources.ApplyResources(this.textBoxOwner, "textBoxOwner");
        //             this.textBoxOwner.Name = "textBoxOwner";
        //             //
        //             // labelDbRoleOwner
        //             //
        //             resources.ApplyResources(this.labelDbRoleOwner, "labelDbRoleOwner");
        //             this.labelDbRoleOwner.Name = "labelDbRoleOwner";
        //             //
        //             // textBoxDbRoleName
        //             //
        //             resources.ApplyResources(this.textBoxDbRoleName, "textBoxDbRoleName");
        //             this.textBoxDbRoleName.Name = "textBoxDbRoleName";
        //             //
        //             // labelDbRoleName
        //             //
        //             resources.ApplyResources(this.labelDbRoleName, "labelDbRoleName");
        //             this.labelDbRoleName.Name = "labelDbRoleName";
        //             //
        //             // DatabaseRoleGeneral
        //             //
        //             this.Controls.Add(this.panelEntireUserControl);
        //             this.Name = "DatabaseRoleGeneral";
        //             resources.ApplyResources(this, "$this");
        //             this.panelEntireUserControl.ResumeLayout(false);
        //             this.panelSchema.ResumeLayout(false);
        //             ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).EndInit();
        //             this.panelMembership.ResumeLayout(false);
        //             ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).EndInit();
        //             this.panelDbRoleGeneralInfo.ResumeLayout(false);
        //             this.panelDbRoleGeneralInfo.PerformLayout();
        //             this.ResumeLayout(false);

        //         }
        // #endregion

        #region Schemas - general operations with ...
        /// <summary>
        /// loads initial schemas from server together with information about the schema owner
        /// </summary>
        private void LoadSchemas()
        {
            this.schemaOwnership = new HybridDictionary();

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String[] { DatabaseRoleGeneral.schemaNameField, DatabaseRoleGeneral.schemaOwnerField };
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";

            DataTable dt = en.Process(serverConnection, req);
            // STrace.Assert((dt != null) && (0 < dt.Rows.Count), "enumerator did not return schemas");
            // STrace.Assert(!this.IsPropertiesMode || (this.dbroleName.Length != 0), "role name is not known");

            foreach (DataRow dr in dt.Rows)
            {
                string schemaName = Convert.ToString(dr[DatabaseRoleGeneral.schemaNameField], System.Globalization.CultureInfo.InvariantCulture);
                string schemaOwner = Convert.ToString(dr[DatabaseRoleGeneral.schemaOwnerField], System.Globalization.CultureInfo.InvariantCulture);
                bool roleOwnsSchema =
                    this.IsPropertiesMode &&
                    (0 == String.Compare(this.dbroleName, schemaOwner, StringComparison.Ordinal));

                this.schemaOwnership[schemaName] = new SchemaOwnership(roleOwnsSchema);
            }
        }


        /// <summary>
        /// initializes the columns and headers of schema grid - but doesnt populate grid with any data
        /// </summary>
        // private void InitializeSchemasGridColumns()
        // {
        //     Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridSchemasOwned;

        //     if (grid.RowsNumber != 0)
        //     {
        //         grid.DeleteAllRows();
        //     }

        //     while (grid.ColumnsNumber != 0)
        //     {
        //         grid.DeleteColumn(0);
        //     }

        //     GridColumnInfo      colInfo     = null;

        //     // checkbox owned/not-owned
        //     colInfo             = new GridColumnInfo();
        //     colInfo.ColumnWidth = sizeCheckboxColumn;
        //     colInfo.WidthType   = GridColumnWidthType.InPixels;
        //     colInfo.ColumnType  = GridColumnType.Checkbox;
        //     grid.AddColumn(colInfo);

        //     // schema name
        //     colInfo             = new GridColumnInfo();
        //     colInfo.ColumnWidth = grid.Width - sizeCheckboxColumn - 2;
        //     colInfo.WidthType   = GridColumnWidthType.InPixels;
        //     grid.AddColumn(colInfo);

        //     grid.SetHeaderInfo(colSchemasOwnedSchemas,  DatabaseRoleSR.HeaderOwnedSchemas,      null);

        //     grid.SelectionType          = GridSelectionType.SingleRow;
        //     grid.UpdateGrid();
        // }

        // private void FillSchemasGrid()
        // {
        //     Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridSchemasOwned;

        //     grid.BeginInit();
        //     grid.DeleteAllRows();

        //     IDictionaryEnumerator enumerator = this.schemaOwnership.GetEnumerator();
        //     enumerator.Reset();
        //     while (enumerator.MoveNext())
        //     {
        //         DictionaryEntry     entry   = enumerator.Entry;
        //         GridCellCollection  row     = new GridCellCollection();
        //         GridCell            cell    = null;

        //         string  schemaName              = entry.Key.ToString();
        //         bool    roleCurrentlyOwnsSchema = ((SchemaOwnership)entry.Value).currentlyOwned;

        //         // grid is filled either
        //         //      a) disabled-checked checkboxes: Indeterminate - if already owning schema - we cannot renounce ownership
        //         //      b) enabled-unchecked checkboxes: Unchecked - user can check / uncheck them and we read final state
        //         cell = new GridCell(roleCurrentlyOwnsSchema ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Unchecked);
        //         row.Add(cell);

        //         cell = new GridCell(schemaName);
        //         row.Add(cell);

        //         grid.AddRow(row);
        //     }

        //     grid.EndInit();

        //     if (grid.RowsNumber > 0)
        //     {
        //         grid.SelectedRow = 0;
        //     }

        // }

        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void SendToServerSchemaOwnershipChanges(Database db, DatabaseRole dbrole)
        {
            if (9 <= this.dataContainer.Server.Information.Version.Major)
            {
                IDictionaryEnumerator enumerator = this.schemaOwnership.GetEnumerator();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    DictionaryEntry de = enumerator.Entry;
                    string schemaName = de.Key.ToString();
                    SchemaOwnership ownership = (SchemaOwnership)de.Value;

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

        // private void gridSchemasOwned_MouseButtonClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventArgs args)
        // {
        //     if ((args.Button == MouseButtons.Left) &&
        //         (colSchemasChecked == args.ColumnIndex))
        //     {
        //         int                 row         = (int) args.RowIndex;
        //         string              schemaName  = this.gridSchemasOwned.GetCellInfo(row, colSchemasOwnedSchemas).CellData.ToString();
        //         GridCheckBoxState   newState    = this.FlipCheckbox(this.gridSchemasOwned, row, colSchemasChecked);
        //         bool                nowOwned    = ((GridCheckBoxState.Checked == newState) || (GridCheckBoxState.Indeterminate == newState));

        //         ((SchemaOwnership) this.schemaOwnership[schemaName]).currentlyOwned = nowOwned;
        //     }
        // }

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
                Enumerator enumerator = new Enumerator();
                Urn urn = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "Server/Database[@Name='{0}']/Role[@Name='{1}']/Member",
                                                        Urn.EscapeString(this.databaseName),
                                                        Urn.EscapeString(this.dbroleName));
                string[] fields = new string[] { DatabaseRoleGeneral.memberNameField };
                OrderBy[] orderBy = new OrderBy[] { new OrderBy(DatabaseRoleGeneral.memberNameField, OrderBy.Direction.Asc) };
                Request request = new Request(urn, fields, orderBy);
                DataTable dt = enumerator.Process(this.serverConnection, request);

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
        // private void InitializeMembershipGridColumns()
        // {
        //     Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridRoleMembership;

        //     if (grid.RowsNumber != 0)
        //     {
        //         grid.DeleteAllRows();
        //     }

        //     while (grid.ColumnsNumber != 0)
        //     {
        //         grid.DeleteColumn(0);
        //     }

        //     GridColumnInfo      colInfo     = null;

        //     // bitmap member type
        //     colInfo             = new GridColumnInfo();
        //     colInfo.ColumnWidth = sizeBitmapColumn;
        //     colInfo.WidthType   = GridColumnWidthType.InPixels;
        //     colInfo.ColumnType  = GridColumnType.Bitmap;
        //     grid.AddColumn(colInfo);

        //     // member name
        //     colInfo             = new GridColumnInfo();
        //     colInfo.ColumnWidth = grid.Width - sizeBitmapColumn - 2;
        //     colInfo.WidthType   = GridColumnWidthType.InPixels;
        //     grid.AddColumn(colInfo);

        //     grid.SetHeaderInfo(colMembershipRoleMembers, DatabaseRoleSR.HeaderRoleMembers,      null);

        //     grid.SelectionType          = GridSelectionType.SingleRow;
        //     grid.UpdateGrid();
        // }

        /// <summary>
        /// fills the membership grid with data (bitmaps, names, etc)
        /// </summary>
        // private void FillMembershipGrid()
        // {
        //     Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridRoleMembership;

        //     grid.BeginInit();
        //     grid.DeleteAllRows();

        //     IDictionaryEnumerator enumerator = this.roleMembers.GetEnumerator();
        //     enumerator.Reset();
        //     while (enumerator.MoveNext())
        //     {
        //         DictionaryEntry entry       = enumerator.Entry;
        //         string          memberName  = entry.Key.ToString();
        //         RoleMembership  membership  = (RoleMembership) entry.Value;

        //         if (membership.currentlyAMember)
        //         {
        //             GridCellCollection row = new GridCellCollection();
        //             GridCell cell = null;

        //             cell = new GridCell(bitmapMember);
        //             row.Add(cell);

        //             cell = new GridCell(memberName);
        //             row.Add(cell);

        //             grid.AddRow(row);
        //         }
        //     }

        //     grid.EndInit();

        //     if (grid.RowsNumber > 0)
        //     {
        //         grid.SelectedRow = 0;
        //     }
        // }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void SendToServerMembershipChanges(Database db, DatabaseRole dbrole)
        {
            IDictionaryEnumerator enumerator = this.roleMembers.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                DictionaryEntry entry = enumerator.Entry;
                string memberName = entry.Key.ToString();
                RoleMembership membership = (RoleMembership)entry.Value;

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

        // private void gridRoleMembership_SelectionChanged(object sender, Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventArgs args)
        // {
        //     EnableDisableControls();
        // }

        //         private void buttonAdd_Click(object sender, System.EventArgs e)
        //         {

        //             using (SqlObjectSearch dlg = new SqlObjectSearch(
        //                                                             this.Font,
        //                                                             iconSearchRolesAndUsers,
        //                                                             this.HelpProvider,
        //                                                             DatabaseRoleSR.Add_DialogTitle,
        //                                                             this.DataContainer.ConnectionInfo,
        //                                                             this.databaseName,
        //                                                             new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.DatabaseRole),
        //                                                             new SearchableObjectTypeCollection(SearchableObjectType.User, SearchableObjectType.DatabaseRole),
        //                                                             false))
        //             {
        //                 if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
        //                 {
        //                     bool memberAdded = false;

        //                     this.gridRoleMembership.BeginInit();

        //                     foreach (SearchableObject principal in dlg.SearchResults)
        //                     {
        //                         if (!this.roleMembers.Contains(principal.Name))
        //                         {
        //                             this.roleMembers[principal.Name] = new RoleMembership(false, true);
        //                             memberAdded = true;
        //                         }
        //                         else
        //                         {
        //                             RoleMembership membership = (RoleMembership) this.roleMembers[principal.Name];

        //                             if (!membership.currentlyAMember)
        //                             {
        //                                 membership.currentlyAMember = true;
        //                                 memberAdded = true;
        //                             }
        //                         }

        //                         if (memberAdded)
        //                         {
        //                             GridCellCollection row = new GridCellCollection();
        //                             GridCell cell = null;

        //                             cell = new GridCell(bitmapMember);
        //                             row.Add(cell);

        //                             cell = new GridCell(principal.Name);
        //                             row.Add(cell);

        //                             this.gridRoleMembership.AddRow(row);
        //                         }
        //                     }

        //                     this.gridRoleMembership.EndInit();

        //                     if (memberAdded)
        //                     {
        //                         this.gridRoleMembership.SelectedRow = this.gridRoleMembership.RowsNumber - 1;
        //                     }
        //                 }
        //             }
        //         }

        //         private void buttonRemove_Click(object sender, System.EventArgs e)
        //         {
        //             DlgGridControl grid = this.gridRoleMembership;

        //             int row = this.gridRoleMembership.SelectedRow;
        //             STrace.Assert(0 <= row, "unexpected row number");

        //             if (0 <= row)
        //             {
        //                 string          memberName  = this.gridRoleMembership.GetCellInfo(row, colMembershipRoleMembers).CellData.ToString();
        //                 RoleMembership  membership  = (RoleMembership) this.roleMembers[memberName];

        //                 if (membership.initiallyAMember)
        //                 {
        //                     membership.currentlyAMember = false;
        //                 }
        //                 else
        //                 {
        //                     this.roleMembers.Remove(memberName);
        //                 }

        //                 this.gridRoleMembership.DeleteRow(row);
        //             }
        //         }
        #endregion

    }
}
