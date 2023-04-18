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

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// AppRoleGeneral - main app role page
    /// </summary>
    internal class AppRoleGeneral
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

        // info extracted from context
        private string serverName;
        private string databaseName;
        private string approleName;
        private bool passwordChanged = false;


        // initial values loaded from server
        private string initialDefaultSchema;


        private bool isYukonOrLater;
        #endregion


        #region Properties: CreateNew/Properties mode
        private bool IsPropertiesMode
        {
            get
            {
                return (approleName != null) && (approleName.Trim().Length != 0);
            }
        }
        #endregion

        #region Constructors / Dispose
        public AppRoleGeneral()
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();
        }

        public AppRoleGeneral(CDataContainer context)
        {
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

            STParameters param;
            bool bStatus;

            param = new STParameters();

            param.SetDocument(doc);

            bStatus = param.GetParam("servername", ref this.serverName);
            bStatus = param.GetParam("database", ref this.databaseName);
            bStatus = param.GetParam("applicationrole", ref this.approleName);
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
                    request.Fields = new String[] { AppRoleGeneral.defaultSchemaField };

                    DataTable dataTable = enumerator.Process(serverConnection, request);
                    // STrace.Assert(dataTable != null, "dataTable is null");
                    // STrace.Assert(dataTable.Rows.Count == 1, "unexpected number of rows in dataTable");

                    if (dataTable.Rows.Count == 0)
                    {
                        throw new Exception("AppRoleSR.ErrorAppRoleNotFound");
                    }

                    DataRow dataRow = dataTable.Rows[0];
                    this.initialDefaultSchema = Convert.ToString(dataRow[AppRoleGeneral.defaultSchemaField], System.Globalization.CultureInfo.InvariantCulture);

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
        private void SendDataToServer()
        {
            // STrace.Params(ComponentName, "SendDataToServer", "", null);

            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.databaseName), "databaseName is empty");

            Microsoft.SqlServer.Management.Smo.Server srv = this.dataContainer.Server;
            System.Diagnostics.Debug.Assert(srv != null, "server object is null");

            Database db = srv.Databases[this.databaseName];
            System.Diagnostics.Debug.Assert(db != null, "database object is null");

            if (this.IsPropertiesMode == true) // in properties mode -> alter role
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(this.approleName), "approleName is empty");

                ApplicationRole approle = db.ApplicationRoles[this.approleName];
                System.Diagnostics.Debug.Assert(approle != null, "approle object is null");

                bool alterRequired = false;

                if (this.isYukonOrLater && _selectedDefaultSchema != this.initialDefaultSchema)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                    alterRequired = true;
                }

                if (passwordChanged == true)
                {
                    approle.ChangePassword((string)"_textBoxPaswordText");
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
                ApplicationRole approle = new ApplicationRole(db, _textBoxRoleNameText);
                if (this.isYukonOrLater && _selectedDefaultSchema.Length > 0)
                {
                    approle.DefaultSchema = _selectedDefaultSchema;
                }

                approle.Create((string)"_textBoxPaswordText");

                SendToServerSchemaOwnershipChanges(db, approle);
                SendToServerMembershipChanges(db, approle);

                this.dataContainer.SqlDialogSubject = approle; // needed by extended properties page
            }

        }
        #endregion

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
                req.Fields = new String[] { AppRoleGeneral.schemaNameField, AppRoleGeneral.schemaOwnerField };
                req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/Schema";
                req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };

                DataTable dt = en.Process(serverConnection, req);
                // STrace.Assert((dt != null) && (dt.Rows.Count > 0), "No rows returned from schema enumerator");

                foreach (DataRow dr in dt.Rows)
                {
                    string name = Convert.ToString(dr[AppRoleGeneral.schemaNameField], System.Globalization.CultureInfo.InvariantCulture);
                    string owner = Convert.ToString(dr[AppRoleGeneral.schemaOwnerField], System.Globalization.CultureInfo.InvariantCulture);

                    dictSchemas.Add(name, owner);
                    schemaNames.Add(name);
                }
            }
        }

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
            req.Fields = new String[] { AppRoleGeneral.memberNameField, AppRoleGeneral.memberUrnField };
            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(this.databaseName) + "']/ApplicationRole[@Name='" + Urn.EscapeString(this.approleName) + "']/Member";

            try
            {
                DataTable dt = en.Process(serverConnection, req);
                System.Diagnostics.Debug.Assert(dt != null, "No results returned from membership query");

                foreach (DataRow dr in dt.Rows)
                {
                    string name = Convert.ToString(dr[AppRoleGeneral.memberNameField], System.Globalization.CultureInfo.InvariantCulture);
                    string urn = Convert.ToString(dr[AppRoleGeneral.memberUrnField], System.Globalization.CultureInfo.InvariantCulture);

                    dictMembership.Add(name, urn);
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
                for (int i = 0; i < 1; ++i)
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
                for (int i = 0; i < 1; ++i)
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
        #endregion
    }

}









