using System;
using System.Drawing;
using System.Windows.Forms;
using System.Resources;



namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Dialog used to ask the user what should be done when no object matching
    /// an input name could be found.
    /// </summary>
    internal class SqlObjectSearchNameNotFound : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label explaination;
        private System.Windows.Forms.RadioButton searchAgain;
        private System.Windows.Forms.Label objectTypeLabel;
        private System.Windows.Forms.Button selectObjectTypes;
        private System.Windows.Forms.TextBox objectTypes;
        private System.Windows.Forms.Label objectNameLabel;
        private System.Windows.Forms.TextBox objectName;
        private System.Windows.Forms.RadioButton remove;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button help;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private IHelpProvider                   helpProvider    = null;
        private string                          newName         = String.Empty;
        private SearchableObjectTypeCollection  allowedTypes    = null;
        private SearchableObjectTypeCollection  selectedTypes   = null;
        private bool                            tryAgain        = true;

        /// <summary>
        /// Whether the search dialog should try again with new name or type information
        /// </summary>
        public bool             TryAgain
        {
            get
            {
                return this.tryAgain;
            }
        }

        /// <summary>
        /// The new name to try
        /// </summary>
        public string           NewName
        {
            get
            {
                return this.newName;
            }
        }

        /// <summary>
        /// SQL object types that the object might be
        /// </summary>
        public SearchableObjectTypeCollection   SelectedTypes
        {
            get
            {
                return this.selectedTypes;
            }
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">The font to use in the dialog</param>
        /// <param name="icon">The icon to use in the dialog</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="name">The object name that could not be found</param>
        /// <param name="allowedTypes">The types the user is allowed to search for</param>
        /// <param name="selectedTypes">The types that should be searched</param>
        public          SqlObjectSearchNameNotFound(
                                                   Font                            font,
                                                   Icon                            icon,
                                                   IHelpProvider                   helpProvider,
                                                   string                          name,
                                                   SearchableObjectTypeCollection  allowedTypes,
                                                   SearchableObjectTypeCollection  selectedTypes)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            this.Font           = font;
            this.Icon           = icon;
            this.helpProvider   = helpProvider;
            this.newName        = name;
            this.allowedTypes   = allowedTypes;
            this.selectedTypes  = selectedTypes;

            ResourceManager resourceManager = new ResourceManager("Microsoft.SqlServer.Management.SqlMgmt.SqlObjectSearchStrings", this.GetType().Assembly);
            string          displayName     = this.newName;

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

            this.explaination.Text  = String.Format(System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString("notFound.instructionsFormat"), displayName);
            this.remove.Text        = String.Format(System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString("notFound.removeFormat"), displayName);
            this.objectName.Text    = this.newName;

            var helpProvider2 = this.helpProvider as IHelpProvider2;
            if (helpProvider2 != null && !helpProvider2.ShouldDisplayHelp)
            {
                this.Controls.Remove(this.help);
            }
            else
            {
                this.help.Enabled = (this.helpProvider != null);
            }

            this.SetSelectedObjectTypeText();
            this.EnableControls();
        }

        /// <summary>
        /// Set the text of the object type textbox
        /// </summary>
        private void    SetSelectedObjectTypeText()
        {
            System.Diagnostics.Debug.Assert(0 < this.selectedTypes.Count, "no object type is selected");

            string text = SearchableObjectTypeDescription.GetDescription(this.selectedTypes[0]).DisplayTypeNamePlural;

            if (this.selectedTypes.Count != 1)
            {
                int index = 1;

                while (index < this.selectedTypes.Count)
                {
                    text = String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}, {1}", text, SearchableObjectTypeDescription.GetDescription(this.selectedTypes[index]).DisplayTypeNamePlural);
                    ++index;
                }
            }

            this.objectTypes.Text = text;
        }

        /// <summary>
        /// Enable or disable controls based on dialog state
        /// </summary>
        private void    EnableControls()
        {
            bool enableNewData              = this.searchAgain.Checked;

            this.objectName.Enabled         = enableNewData;
            this.objectNameLabel.Enabled    = enableNewData;
            this.objectTypes.Enabled        = enableNewData;
            this.objectTypeLabel.Enabled    = enableNewData;
            this.selectObjectTypes.Enabled  = enableNewData && (1 < this.allowedTypes.Count);
            this.ok.Enabled                 = (this.remove.Checked || (0 != this.objectName.Text.Length));
            this.help.Enabled               = (this.helpProvider != null);
        }

        /// <summary>
        /// Handle click events on the select object types button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnSelectObjectTypes(object sender, System.EventArgs e)
        {
            using (SqlObjectSearchSelectTypes dlg = new SqlObjectSearchSelectTypes(this.Font, this.Icon, this.helpProvider, this.allowedTypes, this.selectedTypes))
            {
                dlg.ShowDialog(this);

                if (DialogResult.OK == dlg.DialogResult)
                {
                    this.selectedTypes = dlg.SelectedTypes;
                }
            }
            this.SetSelectedObjectTypeText();
        }

        /// <summary>
        /// Handle click events on the Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnCancel(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Handle click events on the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnOK(object sender, System.EventArgs e)
        {
            this.tryAgain       = this.searchAgain.Checked;
            this.DialogResult   = DialogResult.OK;
            this.Close();

        }

        /// <summary>
        /// Handle click events on the help button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnHelp(object sender, System.EventArgs e)
        {
            if(this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.namenotfound.f1");
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
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.namenotfound.f1");
            }
        }

        /// <summary>
        /// Handle click events on the radio buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnRadioButtonChanged(object sender, System.EventArgs e)
        {
            this.EnableControls();
        }

        /// <summary>
        /// Handle text changed events on the name textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void    OnNameChanged(object sender, System.EventArgs e)
        {
            this.newName = this.objectName.Text;
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

#region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlObjectSearchNameNotFound));
            this.explaination = new System.Windows.Forms.Label();
            this.searchAgain = new System.Windows.Forms.RadioButton();
            this.objectTypeLabel = new System.Windows.Forms.Label();
            this.selectObjectTypes = new System.Windows.Forms.Button();
            this.objectTypes = new System.Windows.Forms.TextBox();
            this.objectNameLabel = new System.Windows.Forms.Label();
            this.objectName = new System.Windows.Forms.TextBox();
            this.remove = new System.Windows.Forms.RadioButton();
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.help = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // explaination
            //
            resources.ApplyResources(this.explaination, "explaination");
            this.explaination.Name = "explaination";
            //
            // searchAgain
            //
            resources.ApplyResources(this.searchAgain, "searchAgain");
            this.searchAgain.Checked = true;
            this.searchAgain.Name = "searchAgain";
            this.searchAgain.Click += new System.EventHandler(this.OnRadioButtonChanged);
            //
            // objectTypeLabel
            //
            resources.ApplyResources(this.objectTypeLabel, "objectTypeLabel");
            this.objectTypeLabel.Name = "objectTypeLabel";
            //
            // selectObjectTypes
            //
            resources.ApplyResources(this.selectObjectTypes, "selectObjectTypes");
            this.selectObjectTypes.Name = "selectObjectTypes";
            this.selectObjectTypes.Click += new System.EventHandler(this.OnSelectObjectTypes);
            //
            // objectTypes
            //
            resources.ApplyResources(this.objectTypes, "objectTypes");
            this.objectTypes.Name = "objectTypes";
            this.objectTypes.ReadOnly = true;
            //
            // objectNameLabel
            //
            resources.ApplyResources(this.objectNameLabel, "objectNameLabel");
            this.objectNameLabel.Name = "objectNameLabel";
            //
            // objectName
            //
            resources.ApplyResources(this.objectName, "objectName");
            this.objectName.Name = "objectName";
            this.objectName.TextChanged += new System.EventHandler(this.OnNameChanged);
            //
            // remove
            //
            resources.ApplyResources(this.remove, "remove");
            this.remove.Name = "remove";
            this.remove.Click += new System.EventHandler(this.OnRadioButtonChanged);
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
            // SqlObjectSearchNameNotFound
            //
            this.AcceptButton = this.ok;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.cancel;
            this.Controls.Add(this.help);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.remove);
            this.Controls.Add(this.objectName);
            this.Controls.Add(this.objectNameLabel);
            this.Controls.Add(this.objectTypes);
            this.Controls.Add(this.selectObjectTypes);
            this.Controls.Add(this.objectTypeLabel);
            this.Controls.Add(this.searchAgain);
            this.Controls.Add(this.explaination);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SqlObjectSearchNameNotFound";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
#endregion




    }
}
