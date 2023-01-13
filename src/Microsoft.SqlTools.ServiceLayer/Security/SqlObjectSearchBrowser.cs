//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Windows.Forms;

namespace Microsoft.SqlServer.Management.SqlMgmt
{
    /// <summary>
    /// Dialog to browse for objects.
    /// </summary>
    internal class SqlObjectSearchBrowser : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button help;
        private System.Windows.Forms.Label instructions;
        private System.Windows.Forms.Label matchingObjectsLabel;
        private System.Windows.Forms.Panel foundObjectPanel;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components  = null;

        private SqlObjectSearchObjectSelectionGrid  grid                    = null;
        private IHelpProvider                       helpProvider            = null;
        private SearchableObjectCollection          selectedObjects         = null;
        private SearchableObjectTypeCollection      searchableTypes         = null;
        private int                                 matchCount              = 0;
        private object                              connectionInfo          = null;
        private string                              databaseName            = String.Empty;
        private bool[]                              includeSystemObjects    = new bool[] { true };
        private SearchableObjectCollection          objectExclusionList     = new SearchableObjectCollection();

        /// <summary>
        /// After the OK button has been pressed, this property returns the set of
        /// selected SQL objects
        /// </summary>
        public  SearchableObjectCollection SelectedObjects
        {
            get
            {
                return this.selectedObjects;
            }
        }

        // <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog</param>
        /// <param name="icon">The icon for the dialog</param>
        /// <param name="helpProvider">Help provider</param>
        /// <param name="types">The types of sql objects to browse</param>
        /// <param name="connectionInfo">Connection info used to enumerate objects</param>
        /// <param name="databaseName">The name of the relevent database, or Empty if no database is relevent</param>
        /// <param name="includeSystemObjects">Whether to include system or built-in objects of the types in the search results</param>
        /// <param name="objectExclusionList">Specific objects to be excluded from the grid.</param>
        public SqlObjectSearchBrowser(
            Font font,
            Icon icon,
            IHelpProvider helpProvider,
            SearchableObjectTypeCollection types,
            object connectionInfo,
            string databaseName,
            bool[] includeSystemObjects,
            SearchableObjectCollection objectExclusionList)
        {
            InitializeComponent();

            this.Font = font;
            this.Icon = icon;
            this.helpProvider = helpProvider;
            this.searchableTypes = types;
            this.connectionInfo = connectionInfo;
            this.databaseName = databaseName;
            this.includeSystemObjects = includeSystemObjects;

            if (objectExclusionList != null)
            {
                this.objectExclusionList = objectExclusionList;
            }

            CustomInitialization();
        }


        // <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog</param>
        /// <param name="icon">The icon for the dialog</param>
        /// <param name="helpProvider">Help provider</param>
        /// <param name="types">The types of sql objects to browse</param>
        /// <param name="connectionInfo">Connection info used to enumerate objects</param>
        /// <param name="databaseName">The name of the relevent database, or Empty if no database is relevent</param>
        /// <param name="includeSystemObjects">Whether to include system or built-in objects of the types in the search results</param>
        public SqlObjectSearchBrowser(
            Font font,
            Icon icon,
            IHelpProvider helpProvider,
            SearchableObjectTypeCollection types,
            object connectionInfo,
            string databaseName,
            bool[] includeSystemObjects)
        {
            InitializeComponent();

            this.Font = font;
            this.Icon = icon;
            this.helpProvider = helpProvider;
            this.searchableTypes = types;
            this.connectionInfo = connectionInfo;
            this.databaseName = databaseName;
            this.includeSystemObjects = includeSystemObjects;

            CustomInitialization();
        }

        // <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog</param>
        /// <param name="icon">The icon for the dialog</param>
        /// <param name="helpProvider">Help provider</param>
        /// <param name="types">The types of sql objects to browse</param>
        /// <param name="connectionInfo">Connection info used to enumerate objects</param>
        /// <param name="databaseName">The name of the relevent database, or Empty if no database is relevent</param>
        /// <param name="includeSystemObjects">Whether to include system or built-in objects in the search results</param>
        public SqlObjectSearchBrowser(
            Font                            font,
            Icon                            icon,
            IHelpProvider                   helpProvider,
            SearchableObjectTypeCollection  types,
            object                          connectionInfo,
            string                          databaseName,
            bool                            includeSystemObjects)
        {
            InitializeComponent();

            this.Font                   = font;
            this.Icon                   = icon;
            this.helpProvider           = helpProvider;
            this.searchableTypes        = types;
            this.connectionInfo         = connectionInfo;
            this.databaseName           = databaseName;
            this.includeSystemObjects   = new bool[] { includeSystemObjects };

            this.SetInstructionText();
            this.InitializeGrid();

            // ok button is disabled until the user checks a checkbox
            this.ok.Enabled     = false;
            this.help.Enabled = (this.helpProvider != null);

            CustomInitialization();
        }

        /// <summary>
        /// Custom initialization tasks that are ran after the form has been initialized
        /// </summary>
        private void CustomInitialization()
        {
            this.SetInstructionText();
            this.InitializeGrid();

            // ok button is disabled until the user checks a checkbox
            this.ok.Enabled = false;

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

        /// <summary>
        /// Enable controls appropriately based on dialog state
        /// </summary>
        /// <param name="newCheck">Whether this call is caused by a checkbox becoming checked</param>
        private void EnableControls()
        {
            this.ok.Enabled = (this.grid.SelectedObjectCount != 0);
        }

        /// <summary>
        /// Set the instruction label text
        /// </summary>
        private void SetInstructionText()
        {
            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", this.GetType().Assembly);

            this.instructions.Text          = String.Format(System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString("browse.instructions"), this.matchCount);
        }

        /// <summary>
        /// Put each of the matches in the ListView
        /// </summary>
        private void InitializeGrid()
        {
            this.SuspendLayout();
            this.foundObjectPanel.SuspendLayout();

            this.grid = new SqlObjectSearchObjectSelectionGrid();

            this.grid.TabIndex          = 0;
            this.grid.Location          = new Point(0, 0);
            this.grid.Size              = this.foundObjectPanel.Size;
            this.grid.Anchor            =
                AnchorStyles.Top    |
                AnchorStyles.Bottom |
                AnchorStyles.Left   |
                AnchorStyles.Right;

            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", this.GetType().Assembly);
            this.grid.AccessibleName = resourceManager.GetString("selectionGrid.accessibleName");
            this.grid.SetGridAccessibleName(this.matchingObjectsLabel.Text);

            this.grid.Changed += new EventHandler(this.OnChanged);

            this.foundObjectPanel.Controls.Add(this.grid);

            this.foundObjectPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Search for objects and put them in the grid
        /// </summary>
        private void PopulateGrid()
        {
            Cursor cursor = Cursor.Current;

            try
            {
                Cursor.Current                              = Cursors.WaitCursor;
                SearchableObjectCollection  foundObjects    = new SearchableObjectCollection();

                for(int i=0; i<this.searchableTypes.Count; i++)
                {
                    SearchableObjectType type = this.searchableTypes[i];

                    Version serverVersion = PermissionsData.Securable.GetServerVersion(this.connectionInfo);

                    SearchableObjectTypeDescription typeDescription = SearchableObjectTypeDescription.GetDescription(this.connectionInfo, type);

                    if (typeDescription.IsServerObject)
                    {
                        // Seach all the SearchableObjectTypes that are applicable to this server version
                        // We iterate over the the possible SearchableObjectTypes to account for Securable Objects that may
                        // may to multiple
                        foreach (var searchableObjectType in type.GetMultiSearchObjectSecurableInfo(serverVersion).Select(x => x.SearchableObjectType))
                        {
                            SearchableObject.Search(
                                foundObjects,
                                searchableObjectType,
                                this.connectionInfo,
                                this.includeSystemObjects.Length > 1
                                    ? this.includeSystemObjects[i]
                                    : this.includeSystemObjects[0]
                                );
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(
                            !type.GetMultiSearchObjectSecurableInfo(serverVersion: null, synthetizeInfoWhenNotFound: false).Any(),
                            "MultiSearchObject NYI for non-Server objects. Add necessary code, similarly to the Server objects!");

                        SearchableObject.Search(
                            foundObjects,
                            type,
                            this.connectionInfo,
                            this.databaseName,
                            this.includeSystemObjects.Length > 1
                                ? this.includeSystemObjects[i]
                                : this.includeSystemObjects[0]
                            );
                    }
                }
                this.RemoveExcludedObjects(foundObjects);

                this.matchCount = foundObjects.Count;
                this.SetInstructionText();
                this.grid.SetFoundObjects(foundObjects);
                this.grid.RefreshGrid();
            }
            finally
            {
                Cursor.Current = cursor;
            }
        }

        /// <summary>
        /// Remove objects in objectExclusionList from foundObjects.
        /// </summary>
        /// <param name="foundObjects"></param>
        private void RemoveExcludedObjects(SearchableObjectCollection foundObjects)
        {
            if (this.objectExclusionList == null)
            {
                return;
            }

            foreach (SearchableObject obj in this.objectExclusionList)
            {
                if (foundObjects.Contains(obj))
                {
                    foundObjects.Remove(obj);
                }
            }
        }

        /// <summary>
        /// Handle click events on the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOk(object sender, System.EventArgs e)
        {
            this.selectedObjects    = this.grid.SelectedObjects;
            this.DialogResult       = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Handle click events on the Cancel button
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
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.browseobjects.f1");
            }
        }

        /// <summary>
        /// Handle F1 help
        /// </summary>
        /// <param name="hevent"></param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            base.OnHelpRequested (hevent);

            hevent.Handled  = true;

            if (this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.browseobjects.f1");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated (e);
            this.PopulateGrid();
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

        /// <summary>
        /// Handle check changes in the grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e)
        {
            this.EnableControls();
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlObjectSearchBrowser));
            this.instructions = new System.Windows.Forms.Label();
            this.matchingObjectsLabel = new System.Windows.Forms.Label();
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.help = new System.Windows.Forms.Button();
            this.foundObjectPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // instructions
            //
            resources.ApplyResources(this.instructions, "instructions");
            this.instructions.Name = "instructions";
            //
            // matchingObjectsLabel
            //
            resources.ApplyResources(this.matchingObjectsLabel, "matchingObjectsLabel");
            this.matchingObjectsLabel.Name = "matchingObjectsLabel";
            //
            // ok
            //
            resources.ApplyResources(this.ok, "ok");
            this.ok.Name = "ok";
            this.ok.Click += new System.EventHandler(this.OnOk);
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
            // foundObjectPanel
            //
            resources.ApplyResources(this.foundObjectPanel, "foundObjectPanel");
            this.foundObjectPanel.Name = "foundObjectPanel";
            //
            // SqlObjectSearchBrowser
            //
            this.AcceptButton = this.ok;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.cancel;
            this.Controls.Add(this.foundObjectPanel);
            this.Controls.Add(this.help);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.matchingObjectsLabel);
            this.Controls.Add(this.instructions);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SqlObjectSearchBrowser";
            this.ResumeLayout(false);

        }
        #endregion


    }
}
