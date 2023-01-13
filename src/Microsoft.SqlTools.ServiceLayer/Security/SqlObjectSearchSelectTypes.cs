using System;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;


using Microsoft.SqlServer.Management.UI.Grid;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Summary description for SqlObjectSearchSelectTypes.
    /// </summary>
    internal class SqlObjectSearchSelectTypes : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label objectTypesLabel;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button help;
        private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid = null;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components		= null;

        private SearchableObjectTypeCollection	allowedTypes	= null;
        private SearchableObjectTypeCollection	selectedTypes	= null;
        private IHelpProvider					helpProvider	= null;

        private	int checkedItemCount = 0;

        private const int	checkBoxColumn	= 0;
        private const int	iconColumn		= 1;
        private const int	typeNameColumn	= 2;

        private const int	checkColumnWidth	= 24;
        private const int	iconColumnWidth		= 24;

        /// <summary>
        /// Array of types selected in the dialog
        /// </summary>
        public SearchableObjectTypeCollection SelectedTypes
        {
            get
            {
                return this.selectedTypes;
            }
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">Font for the dialog, should be same as parent dialog</param>
        /// <param name="icon">Icon for the dialog, should be same as parent dialog</param>
        /// <param name="helpProvider">Provider for displaying BOL</param>
        /// <param name="title">Title text for the dialog</param>
        /// <param name="allowedTypes">SQL types for which checkboxes should be shown</param>
        /// <param name="selectedTypes">SQL types whose checkboxes should be checked</param>
        public			SqlObjectSearchSelectTypes(
            Font							font,
            Icon							icon,
            IHelpProvider					helpProvider,
            string							title,
            SearchableObjectTypeCollection	allowedTypes,
            SearchableObjectTypeCollection	selectedTypes)
        {
            InitializeComponent();

            this.Font					= font;
            this.Icon					= icon;
            this.helpProvider			= helpProvider;
            this.Text					= title;
            this.allowedTypes			= allowedTypes;
            this.selectedTypes			= selectedTypes;

            CustomInitialization();
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">Font for the dialog, should be same as parent dialog</param>
        /// <param name="icon">Icon for the dialog, should be same as parent dialog</param>
        /// <param name="helpProvider">Provider for displaying BOL</param>
        /// <param name="allowedTypes">SQL types for which checkboxes should be shown</param>
        /// <param name="selectedTypes">SQL types whose checkboxes should be checked</param>
        public			SqlObjectSearchSelectTypes(
            Font							font,
            Icon							icon,
            IHelpProvider					helpProvider,
            SearchableObjectTypeCollection	allowedTypes,
            SearchableObjectTypeCollection	selectedTypes)
        {
            InitializeComponent();

            this.Font					= font;
            this.Icon					= icon;
            this.helpProvider			= helpProvider;
            this.allowedTypes			= allowedTypes;
            this.selectedTypes			= selectedTypes;

            CustomInitialization();
        }


        /// <summary>
        /// Create the grid control
        /// </summary>
        private void CreateGrid()
        {
            this.SuspendLayout();

            this.grid = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid()
            {
                TabIndex = 1,
                Location = new Point(12, this.objectTypesLabel.Bottom),
                Size = new Size(this.ClientSize.Width - 24, this.ok.Top - 8 - this.objectTypesLabel.Bottom),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                AccessibleName = this.objectTypesLabel.Text.Replace("&", String.Empty),
                GridLineType = GridLineType.None,
                SelectionType = GridSelectionType.SingleRow
            };

            this.grid.MouseButtonClicked += new MouseButtonClickedEventHandler(OnMouseButtonClicked);

            this.Controls.Add(this.grid);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Create the grid control's columns
        /// </summary>
        private void CreateGridColumns()
        {
            System.Diagnostics.Debug.Assert(this.grid != null, "grid doesn't exist!");

            int clientWidth = this.grid.ClientSize.Width - 2 - SystemInformation.Border3DSize.Width;
            int nameWidth = clientWidth - checkColumnWidth - iconColumnWidth;

            // create the columns
            GridColumnInfo column = null;

            // checkbox column
            column = new GridColumnInfo();
            column.WidthType = GridColumnWidthType.InPixels;
            column.ColumnWidth = checkColumnWidth;
            column.IsUserResizable = false;
            column.ColumnType = GridColumnType.Checkbox;
            column.ColumnAlignment = HorizontalAlignment.Center;
            column.IsHeaderMergedWithRight = true;
            column.MergedHeaderResizeProportion = 0f;
            this.grid.AddColumn(column);

            // icon column
            column = new GridColumnInfo();
            column.WidthType = GridColumnWidthType.InPixels;
            column.ColumnWidth = iconColumnWidth;
            column.IsUserResizable = false;
            column.ColumnType = GridColumnType.Bitmap;
            column.ColumnAlignment = HorizontalAlignment.Center;
            column.IsHeaderMergedWithRight = true;
            column.MergedHeaderResizeProportion = 0f;
            this.grid.AddColumn(column);

            // name column
            column = new GridColumnInfo();
            column.WidthType = GridColumnWidthType.InPixels;
            column.ColumnWidth = nameWidth;
            column.IsUserResizable = false;
            column.ColumnType = GridColumnType.Text;
            column.ColumnAlignment = HorizontalAlignment.Left;
            column.MergedHeaderResizeProportion = 1f;
            this.grid.AddColumn(column);

            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", this.GetType().Assembly);
            string			headerText		= resourceManager.GetString("selectTypes.gridHeader");

            this.grid.SetHeaderInfo(typeNameColumn, headerText, null);

            // Allow the 'Name' column to be sortable, meaning that the UI is going to allow
            // the sorting ASC or DESC by clicking on the column header.
            this.grid.EnableSortingByColumn(typeNameColumn);
        }

        /// <summary>
        /// Populate the grid's rows
        /// </summary>
        private void PopulateGrid()
        {
            System.Diagnostics.Debug.Assert(this.grid != null, "grid doesn't exist!");

            this.grid.BeginInit();
            this.grid.DeleteAllRows();

            this.checkedItemCount = 0;

            System.Diagnostics.Debug.Assert(
                ((allowedTypes != null) && (allowedTypes.Count != 0)),
                "unexpected empty allowedTypes array");

            // add list items for each selectable type
            foreach (SearchableObjectType objectType in allowedTypes)
            {
                GridCellCollection				row			= new GridCellCollection();
                GridCell						cell		= null;

                // gather row data
                bool							isSelected	= selectedTypes.Contains(objectType);
                SearchableObjectTypeDescription	description = SearchableObjectTypeDescription.GetDescription(objectType);
                string							displayName	= description.DisplayTypeNamePlural;
                Image							image		= description.Image;

                // create checkbox cell
                GridCheckBoxState checkState = isSelected ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
                cell		= new GridCell(checkState);
                cell.Tag	= objectType;
                row.Add(cell);

                // create icon cell
                cell = new GridCell(new Bitmap(image));
                row.Add(cell);

                // create type name cell
                cell		= new GridCell(EditableCellType.ReadOnly, displayName);
                row.Add(cell);

                // add the row
                this.grid.AddRow(row);

                if (isSelected)
                {
                    ++(this.checkedItemCount);
                }
            }

            // Sort the 'Name' column, so the types show up sorted nicely...
            this.grid.SortByColumn(typeNameColumn, SqlManagerUI.SortingColumnState.Ascending);

            this.grid.EndInit();
        }

        /// <summary>
        /// Enable or disable controls as required based on dialog state
        /// </summary>
        private void EnableControls()
        {
            this.ok.Enabled = (0 < this.checkedItemCount);
        }

        /// <summary>
        /// Handle click events on the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOK(object sender, System.EventArgs e)
        {
            SearchableObjectTypeCollection selections = new SearchableObjectTypeCollection();

            int count = this.grid.RowsNumber;

            for (int index = 0; index < count; ++index)
            {
                GridCell			gridCell	= this.grid.GetCellInfo(index, checkBoxColumn);
                GridCheckBoxState	checkState	= (GridCheckBoxState) gridCell.CellData;
                bool				isSelected	= (GridCheckBoxState.Checked == checkState);

                if (isSelected)
                {
                    selections.Add((SearchableObjectType) gridCell.Tag);
                }
            }

            this.selectedTypes = selections;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Handle click events on the cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCancel(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Handle click events on the Help button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHelp(object sender, System.EventArgs e)
        {
            if(this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.selectobjecttypes.f1");
            }
        }

        /// <summary>
        /// Handle F1 help
        /// </summary>
        /// <param name="hevent"></param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            base.OnHelpRequested(hevent);

            hevent.Handled = true;

            if (this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.selectobjecttypes.f1");
            }
        }

        /// <summary>
        /// Perform processing that should happen when the form is loaded
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.CreateGrid();
            this.CreateGridColumns();
            this.PopulateGrid();

            this.grid.AutoResizeColumnsToFitClient();
        }


        /// <summary>
        /// Handle click events on the checkbox column
        /// </summary>
        private void OnMouseButtonClicked(object sender, MouseButtonClickedEventArgs args)
        {
            if ((MouseButtons.Left == args.Button) && (checkBoxColumn == args.ColumnIndex))
            {
                GridCell			cell			= this.grid.GetCellInfo((int) args.RowIndex, checkBoxColumn);
                bool				shouldBeChecked	= (GridCheckBoxState.Unchecked == (GridCheckBoxState) cell.CellData);
                GridCheckBoxState	newState		= shouldBeChecked ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;

                cell.CellData = newState;

                if (shouldBeChecked)
                {
                    ++(this.checkedItemCount);
                }
                else
                {
                    --(this.checkedItemCount);
                }

                this.EnableControls();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Custom initialization tasks that are ran after the form has been initialized
        /// </summary>
        private void CustomInitialization()
        {
            var helpProvider2 = this.helpProvider as IHelpProvider2;
            if (helpProvider2 != null && !helpProvider2.ShouldDisplayHelp)
            {
                this.Controls.Remove(this.help);
            }
            else
            {
                this.help.Enabled = (this.helpProvider != null);
            }
            StartPosition = FormStartPosition.CenterParent;
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlObjectSearchSelectTypes));
            this.objectTypesLabel = new System.Windows.Forms.Label();
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.help = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // objectTypesLabel
            //
            resources.ApplyResources(this.objectTypesLabel, "objectTypesLabel");
            this.objectTypesLabel.Name = "objectTypesLabel";
            //
            // ok
            //
            resources.ApplyResources(this.ok, "ok");
            this.ok.Name = "ok";
            this.ok.Click += new System.EventHandler(this.OnOK);
            //
            // cancel
            //
            resources.ApplyResources(this.cancel, "cancel");
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Name = "cancel";
            this.cancel.Click += new System.EventHandler(this.OnCancel);
            //
            // help
            //
            resources.ApplyResources(this.help, "help");
            this.help.Name = "help";
            this.help.Click += new System.EventHandler(this.OnHelp);
            //
            // SqlObjectSearchSelectTypes
            //
            this.AcceptButton = this.ok;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.cancel;
            this.Controls.Add(this.help);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.objectTypesLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SqlObjectSearchSelectTypes";
            this.ResumeLayout(false);

        }
        #endregion


    }
}
