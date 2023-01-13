using System;
using System.Drawing;
using System.Windows.Forms;
using System.Resources;

using Microsoft.SqlServer.Management.SqlMgmt;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginData;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// The Database Roles page of the Create Login/Login Properties dbCommander.
    /// </summary>
    internal class CreateLoginDatabaseAccess : SqlManagementUserControl, IPanelForm
    {
        private static int          permitColumn              = 0;
        private static int          databaseNameColumn        = 1;
        private static int          defaultSchemaColumn       = 3;
        private static int          defaultSchemaBrowseColumn = 4;
        private static int          userColumn                = 2;

        private System.Windows.Forms.Label databaseAccessLabel;
        private System.Windows.Forms.Label databaseRolesLabel;
        private System.Windows.Forms.CheckedListBox databaseRoles;
        private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid databases;
        private System.Windows.Forms.Panel databasePanel;
        private System.Windows.Forms.Panel rolesPanel;


        private LoginPrototype  prototype;
        private string          databaseRolesLabelFormat;
        private string          databaseGuestStatusFormat;
        private bool            initializing = false;
        private bool checkedForInaccessibleDatabases = false;
        private CheckBox checkBoxGuestStatus;
        private Icon            iconSearchSchema    = null;

        /// <summary>
        /// constructor
        /// </summary>
        public CreateLoginDatabaseAccess()
        {
            this.prototype                  = null;
            this.databaseRolesLabelFormat   = String.Empty;
            this.databaseGuestStatusFormat  = String.Empty;
            this.HelpF1Keyword              = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.databaseaccess.f1";

            CUtils utils = new CUtils();
            this.iconSearchSchema           = utils.LoadIcon("database_schema.ico");

            InitializeComponent();
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context">The database context for the dialog</param>
        /// <param name="prototype">The prototype login we are working with</param>
        public CreateLoginDatabaseAccess(CDataContainer context, LoginPrototype prototype)
        {
            DataContainer       = context;
            this.prototype      = prototype;
            this.HelpF1Keyword  = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.login.databaseaccess.f1";

            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
                                                                  typeof(CreateLoginDatabaseAccess).Assembly);

            this.databaseRolesLabelFormat = resourceManager.GetString("databaseAccess.rolesLabel");
            this.databaseGuestStatusFormat = resourceManager.GetString("databaseAccess.guestEnabled");

            CUtils utils = new CUtils();
            this.iconSearchSchema           = utils.LoadIcon("database_schema.ico");

            InitializeComponent();
        }


        /// <summary>
        /// Initialize the controls on the page
        /// </summary>
        private void    InitializeControlData()
        {
            InitializeDatabasesGrid();
            RefreshDatabasesGrid();
        }

        /// <summary>
        /// Initialize the grid control
        /// </summary>
        private void    InitializeDatabasesGrid()
        {
            GridColumnInfo  colInfo     = null;

            int buttonWidth = 20;

            // permit checkbox
            colInfo                 = new GridColumnInfo();
            colInfo.ColumnAlignment = HorizontalAlignment.Center;
            colInfo.ColumnType      = GridColumnType.Checkbox;  
            colInfo.ColumnWidth     = 50;
            colInfo.WidthType       = GridColumnWidthType.InPixels;
            databases.AddColumn(colInfo);

            int remainingColumnWidth = (databases.Width - colInfo.ColumnWidth - buttonWidth) / 3;

            // database column
            colInfo             = new GridColumnInfo();
            colInfo.ColumnWidth = remainingColumnWidth;
            colInfo.WidthType   = GridColumnWidthType.InPixels;
            databases.AddColumn(colInfo);

            // user column
            colInfo = new GridColumnInfo();
            colInfo.ColumnWidth = remainingColumnWidth;
            colInfo.WidthType = GridColumnWidthType.InPixels;
            databases.AddColumn(colInfo);

            // default schema
            colInfo                              = new GridColumnInfo();
            colInfo.ColumnWidth                  = remainingColumnWidth;
            colInfo.WidthType                    = GridColumnWidthType.InPixels;
            colInfo.IsWithRightGridLine          = false;
            colInfo.IsHeaderMergedWithRight      = true;
            colInfo.MergedHeaderResizeProportion = 1.0f;
            databases.AddColumn(colInfo);

            // default schema browse button
            colInfo                 = new GridColumnInfo();
            colInfo.ColumnWidth     = buttonWidth;
            colInfo.WidthType       = GridColumnWidthType.InPixels;
            colInfo.ColumnType      = GridColumnType.Button;
            colInfo.ColumnAlignment = HorizontalAlignment.Center;
            databases.AddColumn(colInfo);            

            ResourceManager resourceManager = 
                new ResourceManager("Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
                                    typeof(CreateLoginDatabaseAccess).Assembly);

            string permit           = resourceManager.GetString("databaseAccess.databases.permit");
            string database         = resourceManager.GetString("databaseAccess.databases.database");
            string defaultSchema    = resourceManager.GetString("databaseAccess.databases.defaultSchema");
            string user             = resourceManager.GetString("databaseAccess.dbAccessHeaderUser");

            databases.SetHeaderInfo(permitColumn,               permit,         null);
            databases.SetHeaderInfo(databaseNameColumn,         database,       null);
            databases.SetHeaderInfo(defaultSchemaBrowseColumn,  defaultSchema,  null);
            databases.SetHeaderInfo(userColumn,                 user,           null);

            databases.SelectionType = GridSelectionType.SingleRow;

            databases.SelectionChanged          += new SelectionChangedEventHandler(OnSelectionChanged);
            databases.MouseButtonClicked        += new MouseButtonClickedEventHandler(OnMouseButtonClicked);
            databases.SetCellDataFromControl    += new SetCellDataFromControlEventHandler(OnDatabaseGridDataChanged);
            databases.StandardKeyProcessing += new StandardKeyProcessingEventHandler(OnKeyPressed);

            databases.AlwaysHighlightSelection = false;

        }

        /// <summary>
        /// Clear and repopulate the databases grid
        /// </summary>
        private void    RefreshDatabasesGrid()
        {
            this.initializing = true;

            this.databases.BeginInit();
            this.databases.DeleteAllRows();

            bool isYukon  = (9 <= this.DataContainer.Server.Information.Version.Major);

            foreach (string databaseName in this.prototype.DatabaseNames)
            {
                // get the information for the database
                DatabaseRoles databaseRoles = this.prototype.GetDatabaseRoles(databaseName);

                // if the database is accessible, add a row for it in the grid
                if (databaseRoles.DatabaseIsAccessible)
                {
                    GridCellCollection  row         = new GridCellCollection();

                    // add checkbox column
                    GridCheckBoxState   checkState  = databaseRoles.PermitDatabaseAccess ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
                    GridCell            cell        = new GridCell(checkState);
                    row.Add(cell);

                    // add database name column
                    cell = new GridCell(EditableCellType.ReadOnly, databaseName);
                    row.Add(cell);

                    // add user name column
                    if (databaseRoles.PermitDatabaseAccess)
                    {
                        cell = new GridCell(EditableCellType.Editor, databaseRoles.UserName);
                    }
                    else
                    {
                        cell = new GridCell(EditableCellType.ReadOnly, String.Empty);
                    }
                    row.Add(cell);
                    
                    // add default schema/user name column
                    if (databaseRoles.PermitDatabaseAccess)
                    {
                        int cellType    = isYukon ? EditableCellType.Editor : EditableCellType.ReadOnly;
                        cell            = new GridCell(cellType, databaseRoles.DefaultSchema);
                    }
                    else
                    {
                        cell = new GridCell(EditableCellType.ReadOnly, String.Empty);
                    }
                    row.Add(cell);

                    // add schema browse button
                    cell = new GridCell(CreateLoginSR.ButtonText, null);

                    if (!isYukon || !databaseRoles.PermitDatabaseAccess)
                    {
                        // need to hide the browse button for now.  
                        // if they click on the permit checkbox it will be shown.
                        ((ButtonInfo)(cell.CellData)).State = ButtonCellState.Empty;
                    }

                    row.Add(cell);                    

                    // add the row to the grid
                    databases.AddRow(row);  
                }
            }

            this.databases.EndInit();

            // if there are any rows in the grid, select the first one
            if (0 < this.databases.RowsNumber)
            {
                this.databases.SelectedRow = 0;
                OnSelectionChanged(null,null);
            }

            this.initializing = false;
        }


        /// <summary>
        /// Clear and repopulate the database roles list box
        /// </summary>
        private void    RefreshDatabaseRolesList()
        {
            this.initializing = true;

            try
            {
                // clear the roles list box and, if there is a selected database, repopulate it         
                this.databaseRoles.Items.Clear();

                if (0 <= this.databases.SelectedRow)
                {
                    string databaseName = this.GetSelectedDatabaseName();
                    DatabaseRoles databaseRolesData = this.prototype.GetDatabaseRoles(databaseName);

                    System.Diagnostics.Debug.Assert(databaseRolesData != null, "didn't get database role data for the database");

                    foreach (string roleName in databaseRolesData.DatabaseRoleNames)
                    {
                        CheckState state = databaseRolesData.IsMember(roleName) ? CheckState.Checked : CheckState.Unchecked;
                        this.databaseRoles.Items.Add(roleName, state);
                    }

                    //enable list box if db access granted
                    this.databaseRoles.Enabled = databaseRolesData.PermitDatabaseAccess;
                    this.checkBoxGuestStatus.Checked = databaseRolesData.GuestStatus;
                }
            }
            catch (Exception ex)
            {
                this.ShowMessage(
                    ex, 
                    Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK, 
                    Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Warning);

            }

            this.initializing = false;
        }

        /// <summary>
        /// Determine whether the checkbox in a particular grid row is checked
        /// </summary>
        /// <param name="rowIndex">The grid row to check</param>
        /// <returns>True if the checkbox is checked, false otherwise</returns>
        private bool    IsPermittedChecked(int rowIndex)
        {
            GridCell cell = databases.GetCellInfo(rowIndex, permitColumn);
            return(((GridCheckBoxState) cell.CellData) == GridCheckBoxState.Checked);
        }

        /// <summary>
        /// Get the name of the database in the selected grid row
        /// </summary>
        /// <returns>The database name</returns>
        private string  GetSelectedDatabaseName()
        {
            int     row             = this.databases.SelectedRow;
            string  databaseName    = String.Empty;

            if (0 <= row)
            {
                databaseName = this.databases.GetCellInfo(row, databaseNameColumn).CellData.ToString();
            }

            return databaseName;
        }

        /// <summary>
        /// Handle changes to grid selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void    OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {            

            if (0 <= this.databases.SelectedRow)
            {
                string databaseName            = this.GetSelectedDatabaseName();
                this.databaseRolesLabel.Text    = String.Format(System.Globalization.CultureInfo.CurrentCulture, this.databaseRolesLabelFormat, databaseName);
                this.checkBoxGuestStatus.Visible = true;
                this.checkBoxGuestStatus.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, this.databaseGuestStatusFormat, databaseName);
            }
            else
            {
                this.databaseRolesLabel.Text    = String.Empty;
                this.checkBoxGuestStatus.Visible = false;
            }

            this.RefreshDatabaseRolesList();
                        
        }

        /// <summary>
        /// Handle changes to the check status in the database roles checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnRoleCheckChanged(object sender, System.Windows.Forms.ItemCheckEventArgs e)
        {
            if (!this.initializing)
            {
                if ((0 <= e.Index) && (e.Index < databaseRoles.Items.Count))
                {
                    string  roleName = this.databaseRoles.Items[e.Index].ToString();

                    if (0 == String.Compare(roleName, "public", StringComparison.Ordinal))
                    {
                        e.NewValue  = CheckState.Checked;
                    }
                    else
                    {
                        string  databaseName    = this.GetSelectedDatabaseName();
                        bool    isMember        = (CheckState.Checked == e.NewValue);

                        this.prototype.GetDatabaseRoles(databaseName).SetMember(roleName, isMember);
                    }
                }
            }
        }

        /// <summary>
        /// Handle mouse click events on the databases grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnMouseButtonClicked(object sender, MouseButtonClickedEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.ColumnIndex == defaultSchemaBrowseColumn)
                {
                    // show object picker for schemas...
                    string databaseName = this.databases.GetCellInfo((int) e.RowIndex, 
                                                                     databaseNameColumn).CellData.ToString();

                    using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
                                                                     iconSearchSchema,
                                                                     this.HelpProvider,
                                                                     UserSR.TitleSearchSchema,
                                                                     this.DataContainer.ConnectionInfo,
                                                                     databaseName,
                                                                     new SearchableObjectTypeCollection(SearchableObjectType.Schema),
                                                                     new SearchableObjectTypeCollection(SearchableObjectType.Schema)))
                    {
                        if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
                        {
                            DatabaseRoles databaseRolesData = this.prototype.GetDatabaseRoles(databaseName);
                            GridCellCollection row = databases.GetRowInfo((int) e.RowIndex);

                            databaseRolesData.DefaultSchema   = dlg.SearchResults[0].Name;
                            row[defaultSchemaColumn].CellData = dlg.SearchResults[0].Name;
                        }
                    }
                }

                else if (e.ColumnIndex == permitColumn)
                {
                    GridCellCollection  row                 = databases.GetRowInfo((int) e.RowIndex);
                    string              databaseName        = this.databases.GetCellInfo((int) e.RowIndex, databaseNameColumn).CellData.ToString();
                    DatabaseRoles       databaseRolesData   = this.prototype.GetDatabaseRoles(databaseName);
                    bool                isYukon             = (9 <= this.DataContainer.Server.Information.Version.Major);

                    if (databaseRolesData.PermitDatabaseAccess)
                    {
                        // uncheck the checkbox
                        GridCell cell = new GridCell(GridCheckBoxState.Unchecked);
                        this.databases.SetCellInfo((int) e.RowIndex, permitColumn, cell);

                        // set default schema to blank
                        row[defaultSchemaColumn].TextCellType   = EditableCellType.ReadOnly;
                        row[defaultSchemaColumn].CellData       = String.Empty;

                        // need to hide the browse button...
                        ((ButtonInfo)(row[defaultSchemaBrowseColumn].CellData)).State = ButtonCellState.Empty;

                        // set user to blank
                        row[userColumn].TextCellType    = EditableCellType.ReadOnly;
                        row[userColumn].CellData        = String.Empty;

                        // disable the roles grid
                        databaseRoles.Enabled                   = false;

                        // set the prototype data
                        databaseRolesData.PermitDatabaseAccess  = false;
                        databaseRolesData.DefaultSchema         = String.Empty;
                        databaseRolesData.UserName              = String.Empty;

                        this.prototype.DisjoinAllDatabaseRoles(databaseName);
                        this.RefreshDatabaseRolesList();
                    }
                    else
                    {
                        // set the prototype data
                        databaseRolesData.PermitDatabaseAccess  = true;
                        databaseRolesData.UserName              = this.prototype.LoginName;

                        if (isYukon)
                        {
                            // need to show the browse button...
                            ((ButtonInfo)(row[defaultSchemaBrowseColumn].CellData)).State = ButtonCellState.Normal;
                        }

                        // check the checkbox
                        GridCell cell = new GridCell(GridCheckBoxState.Checked);
                        this.databases.SetCellInfo((int) e.RowIndex, permitColumn, cell);

                        // set the default schema in the grid
                        row[defaultSchemaColumn].TextCellType   = isYukon ? EditableCellType.Editor : EditableCellType.ReadOnly;
                        row[defaultSchemaColumn].CellData       = isYukon ? databaseRolesData.DefaultSchema : String.Empty;

                        // set the user name to login name
                        row[userColumn].TextCellType            = EditableCellType.Editor;
                        row[userColumn].CellData                = this.prototype.LoginName;

                        // enable the roles grid
                        databaseRoles.Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Respond to changes to cell state in the grid
        /// </summary>
        /// <param name="sender">The grid control that changed</param>
        /// <param name="e">The event args describing which cell changed</param>
        private void OnDatabaseGridDataChanged(object sender, SetCellDataFromControlEventArgs e)
        {
            if (!this.initializing && (defaultSchemaColumn == e.ColumnIndex))
            {
                System.Diagnostics.Debug.Assert((this.DataContainer.Server != null) && (this.DataContainer.Server.Information.Version.Major>=9), "schema supported on 9.0+ servers only");

                string defaultSchemaName    = ((TextBox) e.Control).Text;
                string databaseName         = this.databases.GetCellInfo(e.RowIndex, databaseNameColumn).CellData.ToString();

                this.prototype.GetDatabaseRoles(databaseName).DefaultSchema = defaultSchemaName;        
            }
            else if (!this.initializing && (userColumn == e.ColumnIndex))
            {
                string userName = ((TextBox) e.Control).Text;
                string databaseName         = this.databases.GetCellInfo(e.RowIndex, databaseNameColumn).CellData.ToString();

                this.prototype.GetDatabaseRoles(databaseName).UserName = userName;
            }
        }

        /// <summary>
        /// Respond to pressing tab key
        /// </summary>
        /// <param name="sender">The grid control that changed</param>
        /// <param name="e">The event args describing which cell changed</param>
        private void OnKeyPressed(object sender, StandardKeyProcessingEventArgs e)
        {
            if (this.initializing || e.Key != Keys.Tab)
            {
                // do nothing
                return;
            }
            
            int row = -1;
            int col = -1;

            databases.GetSelectedCell(out row, out col);

            if (row == -1 || col == -1 || col != 0)
            {
                // The databases is empty or the it is not the first column (check box)
                //
                return; 
            }

            
            GridCell cell = databases.GetCellInfo(row, col);

            if (cell.CellData is GridCheckBoxState)
            {
                GridCheckBoxState state = (GridCheckBoxState)cell.CellData;
                if (state == GridCheckBoxState.Checked && databaseRoles.CanFocus)
                {
                    this.ActiveControl = databaseRoles;
                }
            }
            else
            {
                // if it is not checkedbox, let the base class decide
                //
                return;
            }
        }

        /// <summary>
        /// Apply changes to database access and role membership
        /// </summary>
        /// <param name="sender"></param>
        public override void OnRunNow(object sender)
        {
            try
            {
                this.ExecutionMode  = ExecutionMode.Success;
                this.prototype.ApplyDatabaseRoleChanges(DataContainer.Server);
            }
            catch (Exception e)
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
            this.InitializeControlData();
        }

        void IPanelForm.OnSelection(TreeNode node)
        {
            if (!this.checkedForInaccessibleDatabases)
            {
                this.checkedForInaccessibleDatabases = true;

                foreach (string databaseName in this.prototype.DatabaseNames)
                {
                    // get the information for the database
                    DatabaseRoles databaseRoles = this.prototype.GetDatabaseRoles(databaseName);

                    // if the database is accessible, add a row for it in the grid
                    if (!databaseRoles.DatabaseIsAccessible)
                    {
                        // if the database is inaccessible and we haven't warned already, warn the user
                        ResourceManager resourceManager = new ResourceManager(
                                                                             "Microsoft.SqlServer.Management.SqlManagerUI.CreateLoginStrings", 
                                                                             typeof(CreateLoginDatabaseAccess).Assembly);

                        string message = resourceManager.GetString("databaseAccess.error.inaccessibleDatabase");

                        this.DisplayExceptionInfoMessage(new Exception(message));
                        break;
                    }
                }
            }

            this.RefreshDatabasesGrid();
        }

        void IPanelForm.OnPanelLoseSelection(TreeNode node)
        {
        }

        public override void OnReset(object sender)
        {
            base.OnReset(sender);

            this.RefreshDatabasesGrid();
            this.RefreshDatabaseRolesList();
        }


#endregion  

#region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateLoginDatabaseAccess));
            this.databaseAccessLabel = new System.Windows.Forms.Label();
            this.databaseRolesLabel = new System.Windows.Forms.Label();
            this.databases = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            this.databaseRoles = new System.Windows.Forms.CheckedListBox();
            this.databasePanel = new System.Windows.Forms.Panel();
            this.rolesPanel = new System.Windows.Forms.Panel();
            this.checkBoxGuestStatus = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.databases)).BeginInit();
            this.databasePanel.SuspendLayout();
            this.rolesPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // databaseAccessLabel
            // 
            resources.ApplyResources(this.databaseAccessLabel, "databaseAccessLabel");
            this.databaseAccessLabel.Name = "databaseAccessLabel";
            // 
            // databaseRolesLabel
            // 
            resources.ApplyResources(this.databaseRolesLabel, "databaseRolesLabel");
            this.databaseRolesLabel.Name = "databaseRolesLabel";
            // 
            // databases
            // 
            resources.ApplyResources(this.databases, "databases");
            this.databases.BackColor = System.Drawing.SystemColors.Window;
            this.databases.ForceEnabled = false;
            this.databases.Name = "databases";
            // 
            // databaseRoles
            // 
            resources.ApplyResources(this.databaseRoles, "databaseRoles");
            this.databaseRoles.CheckOnClick = true;
            this.databaseRoles.FormattingEnabled = true;
            this.databaseRoles.Name = "databaseRoles";
            this.databaseRoles.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.OnRoleCheckChanged);
            // 
            // databasePanel
            // 
            this.databasePanel.Controls.Add(this.databases);
            this.databasePanel.Controls.Add(this.databaseAccessLabel);
            resources.ApplyResources(this.databasePanel, "databasePanel");
            this.databasePanel.Name = "databasePanel";
            // 
            // rolesPanel
            // 
            this.rolesPanel.Controls.Add(this.databaseRoles);
            this.rolesPanel.Controls.Add(this.checkBoxGuestStatus);
            this.rolesPanel.Controls.Add(this.databaseRolesLabel);
            resources.ApplyResources(this.rolesPanel, "rolesPanel");
            this.rolesPanel.Name = "rolesPanel";
            // 
            // checkBoxGuestStatus
            // 
            resources.ApplyResources(this.checkBoxGuestStatus, "checkBoxGuestStatus");
            this.checkBoxGuestStatus.Name = "checkBoxGuestStatus";
            // 
            // CreateLoginDatabaseAccess
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.databasePanel);
            this.Controls.Add(this.rolesPanel);
            this.Name = "CreateLoginDatabaseAccess";
            ((System.ComponentModel.ISupportInitialize)(this.databases)).EndInit();
            this.databasePanel.ResumeLayout(false);
            this.rolesPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
#endregion


    }
}








