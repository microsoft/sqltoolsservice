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
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// AppRoleGeneral - main app role page
    /// </summary>
    internal class AppRoleGeneral
    {
#region Members

        private IServiceProvider serviceProvider = null;

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


        /// <summary>
        /// execution mode by default for now is success
        /// </summary>
        private ExecutionMode m_executionMode = ExecutionMode.Success;

        /// <summary>
        /// should UI be enabled?
        /// </summary>
        private bool executeEnabled = true;

        /// <summary>
        /// should script buttons be enabled?
        /// </summary>
        private bool scriptEnabled = true;

        /// <summary>
        /// F1 keyword to be passed to books on-line
        /// </summary>
        private string helpF1Keyword = null;
        private RunType runType;

        //if derived class tries to call a protected method that relies on service provider,
        //and the service provider hasn't been set yet, we will cache the values and will
        //propagate them when we get the provider set
        private System.Drawing.Icon cachedIcon = null;
        private string cachedCaption = null;

		//whether or not try to auto resize grid columns inside OnLoad method
		private bool attemtGridAutoResize = true;

        private bool isPropertiesMode;
#endregion


#region Trace support
        private const string componentName = "AppRoleGeneral";

        public string ComponentName
        {
            get
            {
                return componentName;
            }
        }
#endregion

#region Constants - urn fields, etc...
        private const string ownerField = "Owner";
        private const string defaultSchemaField = "DefaultSchema";
        private const string schemaNameField = "Name";
        private const string schemaOwnerField = "Owner";
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
        private bool panelInitialized = false;


        // info extracted from context
        private string              serverName;
        private string              databaseName;
        private string              approleName;
        private bool passwordChanged = false;


        // initial values loaded from server
        private string initialDefaultSchema;


        private bool                isYukonOrLater;
#endregion


#region Properties: CreateNew/Properties mode
        private bool IsPropertiesMode
        {
            get
            {
                return isPropertiesMode;
            }
        }
#endregion

#region Constructors / Dispose
        public AppRoleGeneral()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();
        }

        public AppRoleGeneral(CDataContainer context, AppRoleInfo appRole, bool isNewObject)
        {
            // STrace.SetDefaultLevel(ComponentName , SQLToolsCommonTraceLvl.L1);

            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.approle.general.f1";

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

            this.isYukonOrLater = (9 <= context.Server.ConnectionContext.ServerVersion.Major);
            isPropertiesMode = !isNewObject;
            approleName = appRole.Name;
            serverName = dataContainer.ServerName;
            databaseName = "TriggerTest";
            serverConnection = dataContainer.Server.ConnectionContext;
            InitProp();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        // protected override void Dispose( bool disposing )
        // {
        //     if ( disposing )
        //     {
        //         if (components != null)
        //         {
        //             components.Dispose();
        //         }
        //     }
        //     base.Dispose( disposing );
        // }

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

            // STParameters                            param;
            // bool                                    bStatus;

            // param       = new STParameters();

            // param.SetDocument(doc);

            // bStatus         = param.GetParam("servername", ref this.serverName);
            // bStatus         = param.GetParam("database", ref this.databaseName);
            // bStatus         = param.GetParam("applicationrole", ref this.approleName);
        }

        /// <summary>
        ///  InitProp
        ///
        ///  talks with enumerator an retrieves info
        /// </summary>
        private void InitProp()
        {
            // STrace.Params(ComponentName, "InitProp", "", null);

            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.serverName), "serverName is empty");
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            passwordChanged = false;

            // InitializeBitmapAndIcons(); // bitmapMember

            if (this.IsPropertiesMode == true)
            {
                // initialize from enumerator in properties mode
                // STrace.Assert(!string.IsNullOrWhiteSpace(this.approleName), "approleName is empty");

                // this.textBoxRoleName.Text = this.approleName;

                if (this.isYukonOrLater)
                {
                    // get the default schema

                    // STrace.Assert(this.DataContainer.ObjectUrn.Length != 0, "object urn is empty");

                    Enumerator enumerator = new Enumerator();
                    Request request = new Request();
                    request.Urn = this.dataContainer.ObjectUrn;
                    request.Fields = new String[] { AppRoleGeneral.defaultSchemaField};

                    DataTable dataTable = enumerator.Process(serverConnection, request);
                    // STrace.Assert(dataTable != null, "dataTable is null");
                    // STrace.Assert(dataTable.Rows.Count == 1, "unexpected number of rows in dataTable");

                    if (dataTable.Rows.Count == 0)
                    {
                        throw new Exception("AppRoleSR.ErrorAppRoleNotFound");
                    }

                    DataRow dataRow = dataTable.Rows[0];
                    this.initialDefaultSchema = Convert.ToString(dataRow[AppRoleGeneral.defaultSchemaField],System.Globalization.CultureInfo.InvariantCulture);

                    // this.textBoxDefaultSchema.Text = this.initialDefaultSchema;
                }
            }
            else
            {
                // initialize with empty values in create new mode
                // this.textBoxRoleName.Text = String.Empty;
                // this.textBoxDefaultSchema.Text = this.initialDefaultSchema;

                // this.textBoxPasword.Text = String.Empty;
                // this.textBoxConfirmPassword.Text = String.Empty;
            }

            LoadSchemas();
            // InitializeSchemasGridColumns();
            // FillSchemasGrid();

            LoadMembership();
            // InitializeMembershipGridColumns();
            // FillMembershipGrid();

            // dont display the membership controls - app roles dont support members
            // HideMembership();

            // update UI enable/disable controls
            // EnableDisableControls();
        }

        // private void HideMembership()
        // {
        //     try
        //     {
        //         this.SuspendLayout();
        //         this.panelSchema.SuspendLayout();

        //         this.panelSchema.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;
        //         this.panelSchema.Size = new Size
        //                                 (
        //                                 this.panelSchema.Size.Width
        //                                 ,
        //                                 this.panelMembership.Location.Y + this.panelMembership.Size.Height -
        //                                 this.panelSchema.Location.Y
        //                                 );

        //         this.panelMembership.Visible = false;
        //     }
        //     finally
        //     {
        //         this.panelSchema.ResumeLayout();
        //         this.ResumeLayout();

        //         this.gridSchemasOwned.Refresh();
        //     }
        // }


        private string _selectedDefaultSchema;
        // private SqlSecureString _textBoxPaswordText;
        // private SqlSecureString _textBoxConfirmPasswordText;
        private string _textBoxRoleNameText;

        /// <summary>
        /// Called to validate all date in controls and save them in
        /// temproary storage to be used when OnRunNow is called
        /// </summary>
        // public override void OnGatherUiInformation(RunType runType)
        // {
        //     base.OnGatherUiInformation(runType);

        //     try
        //     {
        //         base.ExecutionMode = ExecutionMode.Success;

        //         _selectedDefaultSchema = this.textBoxDefaultSchema.Text;
        //         _textBoxPaswordText = textBoxPasword.Text;
        //         _textBoxConfirmPasswordText = textBoxConfirmPassword.Text;
        //         _textBoxRoleNameText = textBoxRoleName.Text;
        //     }
        //     catch (Exception exception)
        //     {
        //         DisplayExceptionMessage(exception);
        //         base.ExecutionMode = ExecutionMode.Failure;
        //     }
        // }

        /// <summary>
        /// SendDataToServer
        ///
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        public void SendDataToServer()
        {
            // STrace.Params(ComponentName, "SendDataToServer", "", null);

            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            Microsoft.SqlServer.Management.Smo.Server srv = this.dataContainer.Server;
            System.Diagnostics.Debug.Assert(srv!=null, "server object is null");

            Database db = srv.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(db!=null, "database object is null");

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.approleName), "approleName is empty");

                ApplicationRole approle = db.ApplicationRoles[this.approleName];
                System.Diagnostics.Debug.Assert(approle!=null, "approle object is null");

                bool alterRequired = false;

                if (this.isYukonOrLater && _selectedDefaultSchema != this.initialDefaultSchema)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                    alterRequired = true;
                }

                if (passwordChanged == true)
                {
                    approle.ChangePassword((string) "Random123456!");
                }

                if (alterRequired == true)
                {
                    approle.Alter();
                }

                SendToServerSchemaOwnershipChanges(db, approle);
                SendToServerMembershipChanges(db, approle);
            }
            else // not in properties mode -> create role
            {
                ApplicationRole approle = new ApplicationRole(db, approleName);
                if (this.isYukonOrLater && _selectedDefaultSchema!=null && _selectedDefaultSchema.Length > 0)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                }

                approle.Create((string) "Random123dafsa456!");

                SendToServerSchemaOwnershipChanges(db,approle);
                SendToServerMembershipChanges(db,approle);

                this.dataContainer.SqlDialogSubject = approle; // needed by extended properties page
            }

        }
#endregion


// #region Update UI enable/disable controls
//         private void EnableDisableControls()
//         {
//             if (!this.isYukonOrLater)
//             {
//                 panelSchema.Enabled = false;
//                 textBoxDefaultSchema.Enabled = false;
//                 buttonBrowseSchema.Enabled = false;
//             }

//             if (this.IsPropertiesMode == true)
//             {
//                 System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.textBoxRoleName.Text), "textBoxRoleName is empty");
//                 this.textBoxRoleName.Enabled = false;

//                 this.AllUIEnabled = true;
//             }
//             else
//             {
//                 this.textBoxRoleName.Enabled = true;

//                 this.AllUIEnabled = (this.textBoxRoleName.Text.Trim().Length != 0);
//             }

//             if ((passwordChanged==true) && (textBoxPasword.Text != textBoxConfirmPassword.Text))
//             {
//                 this.AllUIEnabled = false;
//             }

//             buttonRemove.Enabled = (gridRoleMembership.SelectedRow>=0);

//             panelMembership.Enabled = false; // app role currently doesnt support any members
//         }
// #endregion

// #region Component Designer generated code
//         /// <summary>
//         /// Required method for Designer support - do not modify
//         /// the contents of this method with the code editor.
//         /// </summary>
//         private void InitializeComponent()
//         {
//             System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppRoleGeneral));
//             this.panelEntireUserControl = new System.Windows.Forms.Panel();
//             this.panelGeneral = new System.Windows.Forms.Panel();
//             this.buttonBrowseSchema = new System.Windows.Forms.Button();
//             this.textBoxDefaultSchema = new System.Windows.Forms.TextBox();
//             this.textBoxPasword = new System.Windows.Forms.TextBox();
//             this.labelPassword = new System.Windows.Forms.Label();
//             this.labelDefaultSchema = new System.Windows.Forms.Label();
//             this.textBoxRoleName = new System.Windows.Forms.TextBox();
//             this.labelRoleName = new System.Windows.Forms.Label();
//             this.textBoxConfirmPassword = new System.Windows.Forms.TextBox();
//             this.labelConfirmPassword = new System.Windows.Forms.Label();
//             this.panelMembership = new System.Windows.Forms.Panel();
//             this.buttonRemove = new System.Windows.Forms.Button();
//             this.buttonAdd = new System.Windows.Forms.Button();
//             this.gridRoleMembership = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
//             this.labelMembersOfAppRole = new System.Windows.Forms.Label();
//             this.panelSchema = new System.Windows.Forms.Panel();
//             this.gridSchemasOwned = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
//             this.labelSchemasOwnedByAppRole = new System.Windows.Forms.Label();
//             this.panelEntireUserControl.SuspendLayout();
//             this.panelGeneral.SuspendLayout();
//             this.panelMembership.SuspendLayout();
//             ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).BeginInit();
//             this.panelSchema.SuspendLayout();
//             ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).BeginInit();
//             this.SuspendLayout();
//             this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//             this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//             //
//             // panelEntireUserControl
//             // Important: For narrator accessibility please make sure all Controls.Add are in order from top to bottom, left to right
//             //
//             this.panelEntireUserControl.Controls.Add(this.panelGeneral);
//             this.panelEntireUserControl.Controls.Add(this.panelMembership);
//             this.panelEntireUserControl.Controls.Add(this.panelSchema);
//             resources.ApplyResources(this.panelEntireUserControl, "panelEntireUserControl");
//             this.panelEntireUserControl.Name = "panelEntireUserControl";
//             //
//             // panelGeneral
//             // Important: For narrator accessibility please make sure all Controls.Add are in order from top to bottom, left to right
//             //
//             resources.ApplyResources(this.panelGeneral, "panelGeneral");
//             this.panelGeneral.Controls.Add(this.labelRoleName);
//             this.panelGeneral.Controls.Add(this.textBoxRoleName);
//             this.panelGeneral.Controls.Add(this.labelDefaultSchema);
//             this.panelGeneral.Controls.Add(this.textBoxDefaultSchema);
//             this.panelGeneral.Controls.Add(this.buttonBrowseSchema);
//             this.panelGeneral.Controls.Add(this.labelPassword);
//             this.panelGeneral.Controls.Add(this.textBoxPasword);
//             this.panelGeneral.Controls.Add(this.labelConfirmPassword);
//             this.panelGeneral.Controls.Add(this.textBoxConfirmPassword);
//             this.panelGeneral.Name = "panelGeneral";
//             //
//             // buttonBrowseSchema
//             //
//             resources.ApplyResources(this.buttonBrowseSchema, "buttonBrowseSchema");
//             this.buttonBrowseSchema.Name = "buttonBrowseSchema";
//             this.buttonBrowseSchema.Click += new System.EventHandler(this.buttonBrowseSchema_Click);
//             //
//             // textBoxDefaultSchema
//             //
//             resources.ApplyResources(this.textBoxDefaultSchema, "textBoxDefaultSchema");
//             this.textBoxDefaultSchema.Name = "textBoxDefaultSchema";
//             this.textBoxDefaultSchema.TextChanged += new System.EventHandler(this.textBoxRoleName_TextChanged);
//             //
//             // textBoxPasword
//             //
//             resources.ApplyResources(this.textBoxPasword, "textBoxPasword");
//             this.textBoxPasword.Name = "textBoxPasword";
//             this.textBoxPasword.TextChanged += new System.EventHandler(this.textBoxPasword_TextChanged);
//             //
//             // labelPassword
//             //
//             resources.ApplyResources(this.labelPassword, "labelPassword");
//             this.labelPassword.Name = "labelPassword";
//             //
//             // labelDefaultSchema
//             //
//             resources.ApplyResources(this.labelDefaultSchema, "labelDefaultSchema");
//             this.labelDefaultSchema.Name = "labelDefaultSchema";
//             //
//             // textBoxRoleName
//             //
//             resources.ApplyResources(this.textBoxRoleName, "textBoxRoleName");
//             this.textBoxRoleName.Name = "textBoxRoleName";
//             this.textBoxRoleName.TextChanged += new System.EventHandler(this.textBoxRoleName_TextChanged);
//             //
//             // labelRoleName
//             //
//             resources.ApplyResources(this.labelRoleName, "labelRoleName");
//             this.labelRoleName.Name = "labelRoleName";
//             //
//             // textBoxConfirmPassword
//             //
//             resources.ApplyResources(this.textBoxConfirmPassword, "textBoxConfirmPassword");
//             this.textBoxConfirmPassword.Name = "textBoxConfirmPassword";
//             this.textBoxConfirmPassword.TextChanged += new System.EventHandler(this.textBoxConfirmPassword_TextChanged);
//             //
//             // labelConfirmPassword
//             //
//             resources.ApplyResources(this.labelConfirmPassword, "labelConfirmPassword");
//             this.labelConfirmPassword.Name = "labelConfirmPassword";
//             //
//             // panelMembership
//             // Important: For narrator accessibility please make sure all Controls.Add are in order from top to bottom, left to right
//             //
//             resources.ApplyResources(this.panelMembership, "panelMembership");
//             this.panelMembership.Controls.Add(this.labelMembersOfAppRole);
//             this.panelMembership.Controls.Add(this.gridRoleMembership);
//             this.panelMembership.Controls.Add(this.buttonAdd);
//             this.panelMembership.Controls.Add(this.buttonRemove);
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
//             // labelMembersOfAppRole
//             //
//             resources.ApplyResources(this.labelMembersOfAppRole, "labelMembersOfAppRole");
//             this.labelMembersOfAppRole.Name = "labelMembersOfAppRole";
//             //
//             // panelSchema
//             // Important: For narrator accessibility please make sure all Controls.Add are in order from top to bottom, left to right
//             //
//             resources.ApplyResources(this.panelSchema, "panelSchema");
//             this.panelSchema.Controls.Add(this.labelSchemasOwnedByAppRole);
//             this.panelSchema.Controls.Add(this.gridSchemasOwned);
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
//             // labelSchemasOwnedByAppRole
//             //
//             resources.ApplyResources(this.labelSchemasOwnedByAppRole, "labelSchemasOwnedByAppRole");
//             this.labelSchemasOwnedByAppRole.Name = "labelSchemasOwnedByAppRole";
//             //
//             // AppRoleGeneral
//             //
//             this.Controls.Add(this.panelEntireUserControl);
//             this.Name = "AppRoleGeneral";
//             resources.ApplyResources(this, "$this");
//             this.panelEntireUserControl.ResumeLayout(false);
//             this.panelGeneral.ResumeLayout(false);
//             this.panelGeneral.PerformLayout();
//             this.panelMembership.ResumeLayout(false);
//             ((System.ComponentModel.ISupportInitialize)(this.gridRoleMembership)).EndInit();
//             this.panelSchema.ResumeLayout(false);
//             ((System.ComponentModel.ISupportInitialize)(this.gridSchemasOwned)).EndInit();
//             this.ResumeLayout(false);

//         }
// #endregion

#region Schemas - general operations with ...
        HybridDictionary dictSchemas = null;
        StringCollection schemaNames = null;
        /// <summary>
        /// loads initial schemas from server together with information about the schema owner
        /// </summary>
        private void LoadSchemas()
        {
            if (this.isYukonOrLater)
            {
                this.dictSchemas = new HybridDictionary();
                this.schemaNames = new StringCollection();

                Enumerator en = new Enumerator();
                Request req = new Request();
                req.Fields = new String[] { AppRoleGeneral.schemaNameField, AppRoleGeneral.schemaOwnerField};
                req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";
                req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc)};

                DataTable dt = en.Process(serverConnection, req);
                // STrace.Assert((dt != null) && (dt.Rows.Count > 0), "No rows returned from schema enumerator");

                foreach (DataRow dr in dt.Rows)
                {
                    string name = Convert.ToString(dr[AppRoleGeneral.schemaNameField],System.Globalization.CultureInfo.InvariantCulture);
                    string owner = Convert.ToString(dr[AppRoleGeneral.schemaOwnerField],System.Globalization.CultureInfo.InvariantCulture);

                    dictSchemas.Add(name, owner);
                    schemaNames.Add(name);
                }
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

        //     GridColumnInfo colInfo = null;

        //     // checkbox owned/not-owned
        //     colInfo = new GridColumnInfo();
        //     colInfo.ColumnWidth = sizeCheckboxColumn;
        //     colInfo.WidthType = GridColumnWidthType.InPixels;
        //     colInfo.ColumnType = GridColumnType.Checkbox;
        //     grid.AddColumn(colInfo);

        //     // schema name
        //     colInfo = new GridColumnInfo();
        //     colInfo.ColumnWidth = grid.Width - sizeCheckboxColumn - 2;
        //     colInfo.WidthType = GridColumnWidthType.InPixels;
        //     grid.AddColumn(colInfo);

        //     grid.SetHeaderInfo(colSchemasOwnedSchemas, AppRoleSR.HeaderOwnedSchemas, null);

        //     grid.SelectionType = GridSelectionType.SingleRow;
        //     grid.UpdateGrid();

        // }

        // private void FillSchemasGrid()
        // {
        //     if (this.isYukonOrLater)
        //     {
        //         Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridSchemasOwned;

        //         grid.DeleteAllRows();
        //         foreach (string schemaName in this.schemaNames)
        //         {
        //             GridCellCollection row = new GridCellCollection();
        //             GridCell cell = null;

        //             STrace.Assert(!string.IsNullOrWhiteSpace(schemaName), "schemaName is empty");

        //             string owner = this.dictSchemas[schemaName].ToString();

        //             STrace.Assert(!string.IsNullOrWhiteSpace(owner), "owner is empty");

        //             bool owned = IsPropertiesMode ? (0 == String.Compare(owner, approleName, StringComparison.Ordinal)) : false;

        //             // grid is filled either
        //             //      a) disabled-checked checkboxes: Indeterminate - if already owning schema - we cannot renounce ownership
        //             //      b) enabled-unchecked checkboxes: Unchecked - user can check / uncheck them and we read final state
        //             cell = new GridCell(owned ? GridCheckBoxState.Indeterminate : GridCheckBoxState.Unchecked); row.Add(cell);
        //             cell = new GridCell(schemaName); row.Add(cell);

        //             grid.AddRow(row);
        //         }

        //         if (grid.RowsNumber > 0)
        //         {
        //             grid.SelectedRow = 0;
        //         }
        //     }
        // }

        /// <summary>
        /// sends to server changes related to schema ownership
        /// </summary>
        private void SendToServerSchemaOwnershipChanges(Database db, ApplicationRole approle)
        {
            if (this.isYukonOrLater)
            {
                // DlgGridControl grid = this.gridSchemasOwned;

                for (int i = 0; i < 1; ++i)
                {
                    string name = "grid.GetCellInfo(i, colSchemasOwnedSchemas).CellData.ToString()";
                    object o = dictSchemas[name];

                    System.Diagnostics.Debug.Assert(o != null, "schema object is null");

                    // bool currentlyOwned = IsEmbeededCheckboxChecked(grid, i, colSchemasChecked);
                    bool currentlyOwned = false;

                    if (IsPropertiesMode == true)
                    {
                        bool wasOwned = (o.ToString() == approleName);

                        if (currentlyOwned != wasOwned)
                        {
                            if (currentlyOwned == true)
                            {
                                Schema schema = db.Schemas[name];
                                schema.Owner = approle.Name;
                                schema.Alter();
                            }
                            else
                            {
                                /* we cannot not renounce ownership
                            Schema schema = db.Schemas[name];
                            schema.Owner = null;
                            schema.Alter();
                            */




                            }
                        }
                    }
                    else
                    {
                        if (currentlyOwned == true)
                        {
                            Schema schema = db.Schemas[name];
                            schema.Owner = approle.Name;
                            schema.Alter();
                        }
                    }
                }
            }
        }

        // private void gridSchemasOwned_MouseButtonClicked(object sender, Microsoft.SqlServer.Management.UI.Grid.MouseButtonClickedEventArgs args)
        // {
        //     if (args.Button != MouseButtons.Left)
        //     {
        //         return;
        //     }

        //     int rowno = Convert.ToInt32(args.RowIndex);
        //     int colno = Convert.ToInt32(args.ColumnIndex);

        //     switch (colno)
        //     {
        //         case colSchemasChecked:
        //             FlipCheckbox(gridSchemasOwned, rowno, colno);
        //             break;
        //         default: // else do default action: e.g. edit - open combo - etc ...
        //             break;
        //     }
        // }

#endregion

#region Membership - general operations with ...
        System.Collections.Specialized.HybridDictionary dictMembership = null;

        /// <summary>
        /// loads from server initial membership information
        /// </summary>
        private void LoadMembership()
        {
            dictMembership = new System.Collections.Specialized.HybridDictionary();
            if (IsPropertiesMode == false)
            {
                return;
            }

            Enumerator en = new Enumerator();
            Request req = new Request();
            req.Fields = new String [] {AppRoleGeneral.memberNameField, AppRoleGeneral.memberUrnField};
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/ApplicationRole[@Name='" + Urn.EscapeString(this.approleName) + "']/Member";

            try
            {
                DataTable dt = en.Process(serverConnection,req);
                System.Diagnostics.Debug.Assert(dt!=null, "No results returned from membership query");

                foreach (DataRow dr in dt.Rows)
                {
                    string name = Convert.ToString(dr[AppRoleGeneral.memberNameField],System.Globalization.CultureInfo.InvariantCulture);
                    string urn  = Convert.ToString(dr[AppRoleGeneral.memberUrnField],System.Globalization.CultureInfo.InvariantCulture);

                    dictMembership.Add(name,urn);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
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

        //     grid.SetHeaderInfo(colMembershipRoleMembers, AppRoleSR.HeaderRoleMembers,       null);

        //     grid.SelectionType          = GridSelectionType.SingleRow;
        //     grid.UpdateGrid();
        // }

        /// <summary>
        /// fills the membership grid with data (bitmaps, names, etc)
        /// </summary>
        // private void FillMembershipGrid()
        // {
        //     Microsoft.SqlServer.Management.UI.Grid.DlgGridControl grid = this.gridRoleMembership;

        //     grid.DeleteAllRows();
        //     foreach (DictionaryEntry de in dictMembership)
        //     {
        //         GridCellCollection row = new GridCellCollection();
        //         GridCell cell = null;

        //         string name = de.Key.ToString();

        //         cell = new GridCell(bitmapMember); row.Add(cell); // compute type based on urn
        //         cell = new GridCell(name); row.Add(cell);

        //         // row.Tag = urn == de.Value.ToString();

        //         grid.AddRow(row);
        //     }

        //     if (grid.RowsNumber > 0)
        //     {
        //         grid.SelectedRow = 0;
        //     }
        // }

        /// <summary>
        /// sends to server user changes related to membership
        /// </summary>
        private void SendToServerMembershipChanges(Database db, ApplicationRole approle)
        {
            // DlgGridControl grid = this.gridRoleMembership;

            if (IsPropertiesMode == true)
            {
                // members to add
                for (int i=0; i<1; ++i)
                {
                    string name = "grid.GetCellInfo(i, colMembershipRoleMembers).CellData.ToString()";
                    bool nameExistedInitially = dictMembership.Contains(name);

                    if (nameExistedInitially == false)
                    {
                        // need SMO for: role.Members.Add();
                    }
                }
                // members to drop
                foreach (DictionaryEntry de in dictMembership)
                {
                    if (FoundInMembershipGrid(de.Key.ToString(), de.Value.ToString()) == false)
                    {
                        // need SMO for: role.Members.Remove();
                    }
                }
            }
            else
            {
                // add only
                for (int i=0; i<1; ++i)
                {
                    string name = "grid.GetCellInfo(i, colMembershipRoleMembers).CellData.ToString()";
                    // need SMO for: role.Members.Add();
                }
            }
        }

        /// <summary>
        /// lookup in membership grid to see if user added a name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="urn"></param>
        /// <returns></returns>
        private bool FoundInMembershipGrid(string name, string urn)
        {
            // DlgGridControl grid = this.gridRoleMembership;

            // for (int i = 0; i < grid.RowsNumber; ++i)
            // {
            //     string currentName = grid.GetCellInfo(i, colMembershipRoleMembers).CellData.ToString();
            //     if (name == currentName)
            //     {
            //         return true;
            //     }
            // }

            return false;
        }


        // private void gridRoleMembership_SelectionChanged(object sender, Microsoft.SqlServer.Management.UI.Grid.SelectionChangedEventArgs args)
        // {
        //     EnableDisableControls();
        // }

        // private void buttonAdd_Click(object sender, System.EventArgs e)
        // {
        //     DlgGridControl grid = this.gridRoleMembership;

        //     using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
        //                                                      iconSearchUsers,
        //                                                      this.HelpProvider,
        //                                                      AppRoleSR.SearchUsers,
        //                                                      this.DataContainer.ConnectionInfo,
        //                                                      this.databaseName,
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.User),
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.User)))
        //     {
        //         DialogResult dr = dlg.ShowDialog(this.FindForm());
        //         if (dr == DialogResult.OK)
        //         {
        //             foreach (SearchableObject principal in dlg.SearchResults)
        //             {
        //                 grid = this.gridRoleMembership;

        //                 GridCellCollection row = new GridCellCollection();
        //                 GridCell cell = null;

        //                 string name = principal.Name;

        //                 cell = new GridCell(bitmapMember); row.Add(cell); // compute type based on urn
        //                 cell = new GridCell(name); row.Add(cell);

        //                 // row.Tag = urn == de.Value.ToString();

        //                 grid.AddRow(row);
        //             }

        //             if (grid.RowsNumber > 0)
        //             {
        //                 grid.SelectedRow = grid.RowsNumber-1;
        //             }
        //         }
        //     }
        // }

        // private void buttonRemove_Click(object sender, System.EventArgs e)
        // {
        //     DlgGridControl grid = this.gridRoleMembership;

        //     int rowNo = grid.SelectedRow;

        //     System.Diagnostics.Debug.Assert(rowNo >= 0, "Invalid selected row");
        //     if (rowNo >= 0)
        //     {
        //         grid.DeleteRow(rowNo);
        //     }
        // }

#endregion


// #region Bitmaps and Icons
//         private Bitmap bitmapMember = null;
//         private Icon iconSearchUsers = null;
//         private Icon iconSchema = null;
//         /// <summary>
//         /// initialize bitmaps used for membership grid
//         /// </summary>
//         private void InitializeBitmapAndIcons()
//         {
//             CUtils utils = new CUtils();
//             bitmapMember = utils.LoadIcon("member.ico").ToBitmap();

//             iconSearchUsers = utils.LoadIcon("search_users_roles.ico");
//             iconSchema = utils.LoadIcon("database_schema.ico");
//         }
// #endregion


// #region General Grid operations - helpers

//         /// <summary>
//         /// gets status of checkbox
//         /// </summary>
//         /// <param name="grid"></param>
//         /// <param name="rowno"></param>
//         /// <param name="colno"></param>
//         /// <returns></returns>
//         bool IsEmbeededCheckboxChecked(DlgGridControl grid, int rowno, int colno)
//         {
//             // get the storage for the cell
//             GridCell            cell    = grid.GetCellInfo(rowno, colno);
//             GridCheckBoxState   state   = (GridCheckBoxState) cell.CellData;

//             return(state == GridCheckBoxState.Checked);
//         }


//         /// <summary>
//         /// flips on/off checkboxes from grid
//         /// </summary>
//         /// <param name="rowsno"></param>
//         /// <param name="colno"></param>
//         void FlipCheckbox(DlgGridControl grid, int rowno, int colno)
//         {
//             // get the storage for the cell
//             GridCell            cell    = grid.GetCellInfo(rowno, colno);
//             GridCheckBoxState   state   = (GridCheckBoxState) cell.CellData;

//             // explicitly invert the cell state
//             switch (state)
//             {
//                 case GridCheckBoxState.Checked:
//                     cell.CellData   = GridCheckBoxState.Unchecked;
//                     break;
//                 case GridCheckBoxState.Unchecked:
//                     cell.CellData   = GridCheckBoxState.Checked;
//                     break;
//                 case GridCheckBoxState.Indeterminate:
//                     // do nothing if Indeterminate - this means that entry is checked and r/o (e.g. schemas already owned)
//                     break;

//                 case GridCheckBoxState.None:
//                     break;
//                 default:
//                     System.Diagnostics.Debug.Assert(false,"unknown checkbox state");
//                     break;
//             }
//         }
// #endregion

// #region Non-Grid related Events
//         private void textBoxRoleName_TextChanged(object sender, System.EventArgs e)
//         {
//             EnableDisableControls();
//         }

//         bool passwordChanged = false;
//         private void textBoxPasword_TextChanged(object sender, System.EventArgs e)
//         {
//             passwordChanged = true;
//             EnableDisableControls();
//         }

//         private void textBoxConfirmPassword_TextChanged(object sender, System.EventArgs e)
//         {
//             passwordChanged = true;
//             EnableDisableControls();
//         }
// #endregion

// #region ISupportValidation Members

//         bool ISupportValidation.Validate()
//         {
//             if (this.textBoxRoleName.Text.Trim().Length == 0)
//             {
//                 System.Exception e = new System.Exception(AppRoleSR.ErrorApplicationRoleNameMustBeSpecified);
//                 this.DisplayExceptionMessage(e);

//                 return false;
//             }
//             if (this.textBoxPasword.Text.Trim().Length == 0)
//             {
//                 System.Exception e = new System.Exception(AppRoleSR.ErrorPasswordIsBlank);
//                 this.DisplayExceptionMessage(e);

//                 return false;
//             }
//             if (this.textBoxPasword.Text != this.textBoxConfirmPassword.Text)
//             {
//                 System.Exception e = new System.Exception(AppRoleSR.ErrorPasswordMismatch);
//                 this.DisplayExceptionMessage(e);

//                 return false;
//             }
//             return true;
//         }

// #endregion

//         private void buttonBrowseSchema_Click(object sender, System.EventArgs e)
//         {
//             //
//             // pop up object picker
//             //
//             using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
//                                                              this.iconSchema,
//                                                              this.HelpProvider,
//                                                              AppRoleSR.BrowseSchemaTitle,
//                                                              this.DataContainer.ConnectionInfo,
//                                                              this.databaseName,
//                                                              new SearchableObjectTypeCollection(SearchableObjectType.Schema),
//                                                              new SearchableObjectTypeCollection(SearchableObjectType.Schema)))
//             {
//                 if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
//                 {
//                     this.textBoxDefaultSchema.Text = dlg.SearchResults[0].Name;
//                 }
//             }
//         }
    }

}









