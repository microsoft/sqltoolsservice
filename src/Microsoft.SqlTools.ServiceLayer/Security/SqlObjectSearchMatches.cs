using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Resources;
using System.Windows.Forms;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Summary description for SqlObjectSearchMatches.
    /// </summary>
    internal class SqlObjectSearchMatches : System.Windows.Forms.Form
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

        private SqlObjectSearchObjectSelectionGrid  grid            = null;
        private IHelpProvider                       helpProvider    = null;
        private SearchableObjectCollection          selectedObjects = null;

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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog</param>
        /// <param name="icon">The icon for the dialog</param>
        /// <param name="like">The substring each of the matches match</param>
        /// <param name="matches">The names of the SQL objects containing the "like" string</param>
        public SqlObjectSearchMatches(
            Font                        font,
            Icon                        icon,
            IHelpProvider               helpProvider,
            ObjectName                  like,
            SearchableObjectCollection  matches)
        {
            InitializeComponent();

            this.Font           = font;
            this.Icon           = icon;
            this.helpProvider   = helpProvider;

            this.SetInstructionText(like.ToString());
            this.InitializeGrid(matches);

            // ok button is disabled until the user checks a checkbox
            this.ok.Enabled     = false;

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
        /// <param name="like">The substring each of the matches match</param>
        private void SetInstructionText(string like)
        {
            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", this.GetType().Assembly);
            string          displayName     = like;

            // If the entered name is more than 30 characters, truncate
            // it to 29 characters plus "..." in the "explaination"
            // and "remove" labels.
            if (30 < displayName.Length)
            {
                displayName = String.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    resourceManager.GetString("truncateObjectName"),
                    displayName.Substring(0, 29));
            }

            this.instructions.Text          = String.Format(System.Globalization.CultureInfo.InvariantCulture, resourceManager.GetString("matches.instructionsFormat"), displayName);
        }

        /// <summary>
        /// Put each of the matches in the ListView
        /// </summary>
        /// <param name="matches">The sql objects that were matched</param>
        private void InitializeGrid(SearchableObjectCollection matches)
        {
            this.SuspendLayout();
            this.foundObjectPanel.SuspendLayout();

            this.grid = new SqlObjectSearchObjectSelectionGrid(matches);

            this.grid.TabIndex          = 0;
            this.grid.Location          = new Point(0, 0);
            this.grid.Size              = this.foundObjectPanel.Size;
            this.grid.AccessibleName    = this.matchingObjectsLabel.Text;
            this.grid.Anchor            =
                AnchorStyles.Top    |
                AnchorStyles.Bottom |
                AnchorStyles.Left   |
                AnchorStyles.Right;

            this.grid.Changed += new EventHandler(this.OnChanged);

            this.foundObjectPanel.Controls.Add(this.grid);

            this.foundObjectPanel.ResumeLayout(false);
            this.ResumeLayout(false);
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
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.matchingobjects.f1");
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
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.matchingobjects.f1");
            }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlObjectSearchMatches));
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
            // SqlObjectSearchMatches
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
            this.Name = "SqlObjectSearchMatches";
            this.ResumeLayout(false);

        }
        #endregion

    }
}
