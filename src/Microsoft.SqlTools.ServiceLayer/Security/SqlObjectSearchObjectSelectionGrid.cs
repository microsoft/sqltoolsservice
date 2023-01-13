using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Resources;

using Microsoft.SqlServer.Management.UI.Grid;



namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Summary description for SqlObjectSearchObjectSelectionGrid.
    /// </summary>
    internal class SqlObjectSearchObjectSelectionGrid : System.Windows.Forms.UserControl
    {
        private Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid grid;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        #region private classes
        
        /// <summary>
        /// Display class for Searchable Objects and whether they've been selected
        /// </summary>
        private class ObjectSelection
        {
            private bool                isChecked;
            private SearchableObject    searchableObject;

            /// <summary>
            /// Has the object been selected by the user?
            /// </summary>
            public bool                 IsChecked
            {
                get
                {
                    return this.isChecked;
                }

                set
                {
                    this.isChecked = value;
                }
            }
                
            /// <summary>
            /// The object type of the searchable object
            /// </summary>
            public SearchableObjectType ObjectType
            {
                get
                {
                    return this.searchableObject.SearchableObjectType;
                }
            }

            /// <summary>
            /// The display name for the object's type
            /// </summary>
            public string               ObjectTypeName
            {
                get
                {
                    return this.searchableObject.TypeName;
                }
            }

            /// <summary>
            /// The formatted name of the searchable object
            /// </summary>
            public string               DisplayName
            {
                get
                {
                    return this.searchableObject.ToString();
                }
            }

            /// <summary>
            /// The searchable object
            /// </summary>
            public SearchableObject     SearchableObject
            {
                get
                {
                    return this.searchableObject;
                }
            }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="searchableObject">The searchable object</param>
            public ObjectSelection(SearchableObject searchableObject)
            {
                this.isChecked          = false;
                this.searchableObject   = searchableObject;
            }
        }

        /// <summary>
        /// Class for ordering ObjectSelections
        /// </summary>
        private class ObjectSelectionComparer : IComparer
        {
            private bool    sortNameType;
            private bool    sortAscending;
            
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="sortNameType">True means sort name -> type, false means type -> name</param>
            /// <param name="sortAscending">Whether the sort should be in ascending order</param>
            public ObjectSelectionComparer(bool sortNameType, bool sortAscending)
            {
                this.sortNameType   = sortNameType;
                this.sortAscending  = sortAscending;
            }
            
            /// <summary>
            /// Compare two ObjectSelection objects
            /// </summary>
            /// <param name="x">The first object</param>
            /// <param name="y">The second object</param>
            /// <returns>-1 if x should precede y, 0 if x and y have the same value, 1 if x should follow y</returns>
            public int Compare(object x, object y)
            {
                ObjectSelection selection1;
                ObjectSelection selection2;

                if (this.sortAscending)
                {
                    selection1 = (ObjectSelection) x;
                    selection2 = (ObjectSelection) y;
                }
                else
                {
                    selection1 = (ObjectSelection) y;
                    selection2 = (ObjectSelection) x;
                }
                
                string criterion1 = this.sortNameType ? selection1.DisplayName : selection1.ObjectTypeName;
                string criterion2 = this.sortNameType ? selection2.DisplayName : selection2.ObjectTypeName;

                int result = String.Compare(criterion1, criterion2, StringComparison.Ordinal);

                if (0 == result)
                {
                    criterion1 = this.sortNameType ? selection1.ObjectTypeName : selection1.DisplayName;
                    criterion2 = this.sortNameType ? selection2.ObjectTypeName : selection2.DisplayName;

                    result = String.Compare(criterion1, criterion2, StringComparison.Ordinal);
                }

                return result;
            }

        }

        #endregion

        private bool                sortNameType    = true;
        private bool                sortAscending   = true;
        private HybridDictionary    searchableTypeToImage   = null;
        private ArrayList           foundObjects            = null;
        private ArrayList           actualTypes             = null;
        private int                 selectedObjectCount     = 0;
        private bool                allowNotifications      = true;
        private event EventHandler  observableChanged;
        
        private const int   checkboxColumn  = 0;
        private const int   iconColumn      = 1;
        private const int   nameColumn      = 2;
        private const int   typeColumn      = 3;

        private const int   iconColumnWidth     = 24;
        private const int   checkboxColumnWidth = 24;

        /// <summary>
        /// The set of searchable objects that have been selected by the user
        /// </summary>
        public SearchableObjectCollection   SelectedObjects
        {
            get
            {
                SearchableObjectCollection  result      = new SearchableObjectCollection();
                IEnumerator                 enumerator  = this.foundObjects.GetEnumerator();

                enumerator.Reset();

                while (enumerator.MoveNext())
                {
                    ObjectSelection selection = (ObjectSelection) enumerator.Current;
                    
                    if (selection.IsChecked)
                    {
                        result.Add(selection.SearchableObject);
                    }
                }

                return result;
            }
        }
        
        /// <summary>
        /// The number of checked objects in the grid
        /// </summary>
        public int                          SelectedObjectCount
        {
            get
            {
                return this.selectedObjectCount;
            }
        }
        


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="useSchemaNomenclature">If true, call schema "schema", otherwise call it "owner" in the grid</param>
        public SqlObjectSearchObjectSelectionGrid()
        {
            this.InitializeComponent();

            this.searchableTypeToImage      = new HybridDictionary();
            this.foundObjects               = new ArrayList();
            this.actualTypes                = new ArrayList();
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="foundObjects">The collection of found searchable objects to display</param>
        /// <param name="useSchemaNomenclature">If true, call schema "schema", otherwise call it "owner" in the grid</param>
        public SqlObjectSearchObjectSelectionGrid(SearchableObjectCollection foundObjects)
        {
            this.InitializeComponent();
            this.SetFoundObjects(foundObjects);
        }

        
        /// <summary>
        /// Set the collection of found objects displayed in the grid
        /// </summary>
        /// <param name="foundObjects">The collection of found searchable objects to display</param>
        public void     SetFoundObjects(SearchableObjectCollection foundObjects)
        {
            this.searchableTypeToImage  = new HybridDictionary();
            this.foundObjects           = new ArrayList(foundObjects.Count);
            this.actualTypes            = new ArrayList();
            Hashtable   seen            = new Hashtable();
            
            IEnumerator enumerator = foundObjects.GetEnumerator();
            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                SearchableObject    foundObject = (SearchableObject) enumerator.Current;
                ObjectSelection     selection   = new ObjectSelection(foundObject);

                seen[foundObject.SearchableObjectType] = true;
                this.foundObjects.Add(selection);
            }

            foreach (SearchableObjectType type in seen.Keys)
            {
                this.actualTypes.Add(type);
            }

            this.InitializeBitmaps();
        }

        /// <summary>
        /// Clear the grid and repopulate with freshly sorted ObjectSelections
        /// </summary>
        public void     RefreshGrid()
        {
            if (this.grid.ColumnsNumber != 0)
            {
                int index = 0;
                
                if (this.grid.RowsNumber != 0)
                {  
                    // get the selected object
                    index                       = this.grid.SelectedRow;
                    ObjectSelection selected    = (ObjectSelection) this.foundObjects[index];
                
                    // sort object selections
                    this.foundObjects.Sort(new ObjectSelectionComparer(this.sortNameType, this.sortAscending));
    
                    // get the index of the selected object
                    index = this.foundObjects.IndexOf(selected);
                }

                // clear the grid
                this.grid.BeginInit();
                this.grid.DeleteAllRows();

                // add each found object to the grid
                IEnumerator enumerator = this.foundObjects.GetEnumerator();
                enumerator.Reset();

                while (enumerator.MoveNext())
                {
                    ObjectSelection selection = (ObjectSelection) enumerator.Current;
                    this.AddObjectSelectionToGrid(selection);
                }

                // allow the grid to redraw
                this.grid.EndInit();

                // select the previously selected object
                if (this.grid.RowsNumber != 0)
                {
                    this.grid.SelectedRow = index;
                }
            }
        }

        /// <summary>
        /// Set the accessible name of the grid
        /// </summary>
        /// <param name="accessibleName">the new accessible name of the grid</param>
        public void     SetGridAccessibleName(string accessibleName)
        {
            this.grid.AccessibleName = accessibleName;
        }
        
        /// <summary>
        /// Initialize the map of bitmaps for the icon grid column
        /// </summary>
        private void    InitializeBitmaps()
        {
            Image defaultImage = SearchableObjectTypeDescription.GetDescription(SearchableObjectType.Database).Image;
            
            foreach (SearchableObjectType type in this.actualTypes)
            {
                Image image = SearchableObjectTypeDescription.GetDescription(type).Image;
                this.searchableTypeToImage[type] = (image != null) ? image : defaultImage;
            }
        }

        /// <summary>
        /// Initialize the grid so that items can be displayed in it
        /// </summary>
        private void    InitializeGrid()
        {
            this.CreateGridColumns();
			this.UpdateGridColumnWidths();
			this.SetColumnHeaderText();
            this.SetSortableColumns();
            this.InitializeGridEventHandlers();

            this.grid.SelectionType = GridSelectionType.SingleRow;

        }

        /// <summary>
        /// Create the columns in the grid
        /// </summary>
        private void    CreateGridColumns()
        {
			// calculate the width available for all columns, which is control width,
            // minus the width of the grid lines, minus the 3D border width
            int gridLineCount       = 3;
            int availableWidth      = this.grid.ClientRectangle.Width - gridLineCount - SystemInformation.Border3DSize.Width;
            
            // calculate column widths  
            int typeColumnWidth     = this.GetMaximumTypeNameWidth() + 8;
            int nameColumnWidth     = availableWidth - iconColumnWidth - checkboxColumnWidth - typeColumnWidth;

            // create the columns
            GridColumnInfo column = null;

            // checkbox column
            column                              = new GridColumnInfo();
            column.WidthType                    = GridColumnWidthType.InPixels;
            column.ColumnWidth                  = checkboxColumnWidth;
            column.IsUserResizable              = false;
            column.ColumnType                   = GridColumnType.Checkbox;
            column.ColumnAlignment              = HorizontalAlignment.Center;
            this.grid.AddColumn(column);
            
            // icon column
            column                              = new GridColumnInfo();
            column.WidthType                    = GridColumnWidthType.InPixels;
            column.ColumnWidth                  = iconColumnWidth;
            column.IsUserResizable              = false;
            column.ColumnType                   = GridColumnType.Bitmap;
            column.ColumnAlignment              = HorizontalAlignment.Center;
            this.grid.AddColumn(column);

            // name column
            column                  = new GridColumnInfo();
            column.WidthType        = GridColumnWidthType.InPixels;
            column.ColumnWidth      = nameColumnWidth;
            column.IsUserResizable  = true;
            this.grid.AddColumn(column);

            // type column
            column                  = new GridColumnInfo();
            column.WidthType        = GridColumnWidthType.InPixels;
            column.ColumnWidth      = typeColumnWidth;
            column.IsUserResizable  = true;
            this.grid.AddColumn(column);
        }
        
        /// <summary>
        /// Set the column header text
        /// </summary>
        private void    SetColumnHeaderText()
        {
            ResourceManager resourceManager = new ResourceManager(
                "Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", 
                this.GetType().Assembly);

            this.grid.SetHeaderInfo(nameColumn, resourceManager.GetString("selectionGrid.column.name"), null);
            this.grid.SetHeaderInfo(typeColumn, resourceManager.GetString("selectionGrid.column.type"), null);
        }

        /// <summary>
        /// Tell the grid which columns can be used to sort securables
        /// </summary>
        private void    SetSortableColumns()
        {
            this.grid.EnableSortingByColumn(nameColumn);
            this.grid.EnableSortingByColumn(typeColumn);
        }

        /// <summary>
        /// Tell the grid which methods should be called to handle grid events
        /// </summary>
        private void    InitializeGridEventHandlers()
        {
            this.grid.MouseButtonClicked    += new MouseButtonClickedEventHandler(this.OnMouseClick);
            this.grid.KeyPressedOnCell      += new KeyPressedOnCellEventHandler(this.OnKeyPress);
            this.grid.SortColumn            += new Microsoft.SqlServer.Management.SqlManagerUI.SortColumnEventHandler(this.OnSortGridColumn);
        }

        /// <summary>
        /// Add a securable to the grid
        /// </summary>
        /// <param name="securable">The securable object to add</param>
        private void    AddObjectSelectionToGrid(ObjectSelection selection)
        {
            GridCellCollection row = new GridCellCollection();

            // create checkbox cell
            GridCheckBoxState state = selection.IsChecked ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
            row.Add(new GridCell(state));
            
            // create icon cell
            row.Add(new GridCell((Bitmap) this.searchableTypeToImage[selection.ObjectType]));

             // create name cell
            row.Add(new GridCell(EditableCellType.ReadOnly, selection.DisplayName));

            // create type name cell
             row.Add(new GridCell(EditableCellType.ReadOnly, selection.ObjectTypeName));

            // Add the row
            this.grid.AddRow(row);
        }

        // <summary>
        /// Handle mouse click events in the grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnMouseClick(object sender, MouseButtonClickedEventArgs e)
        {
            if ((MouseButtons.Left == e.Button) && (checkboxColumn == e.ColumnIndex))
            {
                this.ToggleCheckState((int) e.RowIndex);
            }
        }

        /// <summary>
        /// Handle key press events in the grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnKeyPress(object sender, KeyPressedOnCellEventArgs e)
        {
            if ((Keys.Space == e.Key) && (checkboxColumn == e.ColumnIndex))
            {
                this.ToggleCheckState((int) e.RowIndex);
            }
        }

        /// <summary>
        /// Toggle the state of the selection checkbox for the input row
        /// </summary>
        /// <param name="row">The row whose checkbox is to be toggled</param>
        private void    ToggleCheckState(int row)
        {
            System.Diagnostics.Debug.Assert(row < this.foundObjects.Count, "unexpected selection index");

            // set object selection state
            ObjectSelection selection   = (ObjectSelection) this.foundObjects[row];
            selection.IsChecked         = !selection.IsChecked;

            // increment or decrement the selected object count
            this.selectedObjectCount    += ((selection.IsChecked) ? 1 : -1);

            // update grid UI
            GridCheckBoxState state = selection.IsChecked ? GridCheckBoxState.Checked : GridCheckBoxState.Unchecked;
            this.grid.GetCellInfo(row, checkboxColumn).CellData = state;

            // notify observers of the state change
            this.NotifyObservers();
        }
        
        /// <summary>
        /// Get the width of the widest localized type name in the type column
        /// </summary>
        private int     GetMaximumTypeNameWidth()
        {
            Graphics        g               = this.CreateGraphics();
            ResourceManager resourceManager = new ResourceManager(
                "Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", 
                this.GetType().Assembly);
            
            // minimum width is the width of the column header string
            int result = (int) g.MeasureString(resourceManager.GetString("selectionGrid.column.type"), this.Font).Width;

            foreach (SearchableObjectType type in this.actualTypes)
            {
                string  displayName = SearchableObjectTypeDescription.GetDescription(type).DisplayTypeNameSingular;
                int     length      = (int) g.MeasureString(displayName, this.Font).Width;

                result = (int) Math.Max(result, length);
            }

            return result;
        }

        /// <summary>
        /// Resize the grid column widths to take up the existing grid control width
        /// </summary>
        private void    UpdateGridColumnWidths()
        {
            // if we've created columns, resize them to eliminate blank space at the edge
            if (this.grid.ColumnsNumber != 0)
            {
                int gridLineCount   = 3;
                int availableWidth  = this.grid.ClientRectangle.Width - gridLineCount - SystemInformation.Border3DSize.Width;
                int typeWidth       = this.grid.GetColumnWidth(typeColumn);
                int nameWidth       = availableWidth - typeWidth - iconColumnWidth - checkboxColumnWidth;

                if (20 < nameWidth)
                {
                    this.grid.SetColumnWidth(nameColumn, GridColumnWidthType.InPixels, nameWidth);
                }
            }   
        }

        /// <summary>
        /// Handle click events in the column headers of the schema or name columns
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnSortGridColumn(object sender, Microsoft.SqlServer.Management.SqlManagerUI.SortColumnEventArgs e)
        {
            bool nameColumnClicked  = (nameColumn == e.ColumnIndex);
            bool typeColumnClicked  = (typeColumn == e.ColumnIndex);
            
            if (nameColumnClicked || typeColumnClicked)
            {
                e.DoDefaultSorting  = false;
                this.sortNameType   = nameColumnClicked;
                this.sortAscending  = (Microsoft.SqlServer.Management.SqlManagerUI.SortingColumnState.Descending != e.SortingState);

                this.RefreshGrid();
            }
        }


        /// <summary>
        /// When the Win32 control for this selector is created, initialize the grid
        /// </summary>
        /// <param name="e"></param>
        protected override void             OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            this.InitializeGrid();
            this.RefreshGrid();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void             Dispose( bool disposing )
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
    
        /// <summary>
        /// Property to access the observable event.
        /// </summary>
        internal event EventHandler         Changed
        {
            add     { this.observableChanged += value; }
            remove  { this.observableChanged -= value; }
        }

        /// <summary>
        /// Notify all observers that this object has changed.
        /// </summary>
        /// <param name="sender">The object that changed</param>
        /// <param name="e">Hint for the notification, usually null</param>
        private void                        NotifyObservers(object sender, EventArgs e)
        {
            if (this.allowNotifications && (this.observableChanged != null))
            {
                this.observableChanged(sender, e);
            }
        }

        /// <summary>
        /// Notify all observers that this object has changed.
        /// </summary>
        private void                        NotifyObservers()
        {
            this.NotifyObservers(this, new EventArgs());
        }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			this.grid.AutoResizeColumnsToFitClient();
		}


        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grid = new Microsoft.SqlServer.Management.SqlManagerUI.SqlManagerUIDlgGrid();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // grid
            // 
            this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.grid.BackColor = System.Drawing.SystemColors.Window;
            // TODO: Code generation for 'this.grid.FirstScrollableRow' failed because of Exception 'Invalid Primitive Type: System.UInt32. Only CLS compliant primitive types can be used. Consider using CodeObjectCreateExpression.'.
            this.grid.ForceEnabled = false;
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Name = "grid";
            this.grid.Size = new System.Drawing.Size(400, 200);
            this.grid.TabIndex = 0;
            this.grid.Text = "sqlManagerUIDlgGrid1";
            // 
            // SqlObjectSearchObjectSelectionGrid
            // 
            this.Controls.Add(this.grid);
            this.Name = "SqlObjectSearchObjectSelectionGrid";
            this.Size = new System.Drawing.Size(400, 200);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

    }
}
