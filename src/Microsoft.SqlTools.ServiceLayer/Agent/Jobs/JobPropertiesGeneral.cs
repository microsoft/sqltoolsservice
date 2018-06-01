//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Xml;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.Globalization;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobPropertiesGeneral.
    /// </summary>
    internal class JobPropertiesGeneral : ManagementActionBase
    {
        // private System.Windows.Forms.ComboBox category;
        // private System.Windows.Forms.Label categoryLabel;
        // private System.Windows.Forms.CheckBox enabled;
        // private System.Windows.Forms.Label descriptionLabel;
        // private System.Windows.Forms.TextBox description;
        // private System.Windows.Forms.Label ownerLabel;
        // private System.Windows.Forms.TextBox name;
        // private System.Windows.Forms.Label nameLabel;
        // private System.Windows.Forms.Panel createOnlyPanel;
        // private System.Windows.Forms.TextBox lastExecuted;
        // private System.Windows.Forms.Label lastExecutionLablel;
        // private System.Windows.Forms.TextBox lastModified;
        // private System.Windows.Forms.Label lastModifiedLabel;
        // private System.Windows.Forms.TextBox created;
        // private System.Windows.Forms.Label createdLabel;
        // private System.Windows.Forms.TextBox source;
        // private System.Windows.Forms.Label sourceLabel;
        // private System.Windows.Forms.Panel propertiesOnlyPanel;
        // private System.Windows.Forms.LinkLabel viewHistory;

        private JobData data;
       // private LogViewerForm historyDialog = null;
        private IManagedConnection managedConnection = null;

        // private System.Windows.Forms.Button viewOtherJobsInThisCategory;
        // private TextBox owner;
        // private Button browseForOwner;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;

        public JobPropertiesGeneral()
        {          
            data = null;
        }
        public JobPropertiesGeneral(CDataContainer dataContainer, JobData data)
        {          
            this.DataContainer = dataContainer;
            this.data = data;
            this.data.CategoriesChanged += OnCategoriesChangedListener;
            
            InitializeData();
            UpdateControlStatus();

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {               
            }
            base.Dispose(disposing);

            //Dispose of the data member (a reference to JobProperties data).
            if (this.data != null)
            {
                //remove the event handler
                this.data.CategoriesChanged -= OnCategoriesChangedListener;
                //release the object
                this.data = null;
            }

        }

       


        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobPropertiesGeneral));
        //     this.createOnlyPanel = new System.Windows.Forms.Panel();
        //     this.browseForOwner = new System.Windows.Forms.Button();
        //     this.owner = new System.Windows.Forms.TextBox();
        //     this.viewOtherJobsInThisCategory = new System.Windows.Forms.Button();
        //     this.category = new System.Windows.Forms.ComboBox();
        //     this.categoryLabel = new System.Windows.Forms.Label();
        //     this.enabled = new System.Windows.Forms.CheckBox();
        //     this.descriptionLabel = new System.Windows.Forms.Label();
        //     this.description = new System.Windows.Forms.TextBox();
        //     this.ownerLabel = new System.Windows.Forms.Label();
        //     this.name = new System.Windows.Forms.TextBox();
        //     this.nameLabel = new System.Windows.Forms.Label();
        //     this.lastExecuted = new System.Windows.Forms.TextBox();
        //     this.lastExecutionLablel = new System.Windows.Forms.Label();
        //     this.lastModified = new System.Windows.Forms.TextBox();
        //     this.lastModifiedLabel = new System.Windows.Forms.Label();
        //     this.created = new System.Windows.Forms.TextBox();
        //     this.createdLabel = new System.Windows.Forms.Label();
        //     this.source = new System.Windows.Forms.TextBox();
        //     this.sourceLabel = new System.Windows.Forms.Label();
        //     this.propertiesOnlyPanel = new System.Windows.Forms.Panel();
        //     this.viewHistory = new System.Windows.Forms.LinkLabel();
        //     this.createOnlyPanel.SuspendLayout();
        //     this.propertiesOnlyPanel.SuspendLayout();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // createOnlyPanel
        //     // 
        //     resources.ApplyResources(this.createOnlyPanel, "createOnlyPanel");
        //     this.createOnlyPanel.Controls.Add(this.browseForOwner);
        //     this.createOnlyPanel.Controls.Add(this.owner);
        //     this.createOnlyPanel.Controls.Add(this.viewOtherJobsInThisCategory);
        //     this.createOnlyPanel.Controls.Add(this.category);
        //     this.createOnlyPanel.Controls.Add(this.categoryLabel);
        //     this.createOnlyPanel.Controls.Add(this.enabled);
        //     this.createOnlyPanel.Controls.Add(this.descriptionLabel);
        //     this.createOnlyPanel.Controls.Add(this.description);
        //     this.createOnlyPanel.Controls.Add(this.ownerLabel);
        //     this.createOnlyPanel.Controls.Add(this.name);
        //     this.createOnlyPanel.Controls.Add(this.nameLabel);
        //     this.createOnlyPanel.Name = "createOnlyPanel";
        //     // 
        //     // browseForOwner
        //     // 
        //     resources.ApplyResources(this.browseForOwner, "browseForOwner");
        //     this.browseForOwner.Name = "browseForOwner";
        //     this.browseForOwner.Click += new System.EventHandler(this.browseForOwner_Click);
        //     // 
        //     // owner
        //     // 
        //     resources.ApplyResources(this.owner, "owner");
        //     this.owner.Name = "owner";
        //     this.owner.TextChanged += new System.EventHandler(this.owner_TextChanged);
        //     // 
        //     // viewOtherJobsInThisCategory
        //     // 
        //     resources.ApplyResources(this.viewOtherJobsInThisCategory, "viewOtherJobsInThisCategory");
        //     this.viewOtherJobsInThisCategory.Name = "viewOtherJobsInThisCategory";
        //     this.viewOtherJobsInThisCategory.Click += new System.EventHandler(this.viewOtherJobsInThisCategory_Click);
        //     // 
        //     // category
        //     // 
        //     resources.ApplyResources(this.category, "category");
        //     this.category.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.category.FormattingEnabled = true;
        //     this.category.Name = "category";
        //     this.category.SelectedIndexChanged += new System.EventHandler(this.category_SelectedIndexChanged);
        //     // 
        //     // categoryLabel
        //     // 
        //     resources.ApplyResources(this.categoryLabel, "categoryLabel");
        //     this.categoryLabel.Name = "categoryLabel";
        //     // 
        //     // enabled
        //     // 
        //     resources.ApplyResources(this.enabled, "enabled");
        //     this.enabled.Name = "enabled";
        //     this.enabled.CheckedChanged += new System.EventHandler(this.enabled_CheckedChanged);
        //     // 
        //     // descriptionLabel
        //     // 
        //     resources.ApplyResources(this.descriptionLabel, "descriptionLabel");
        //     this.descriptionLabel.Name = "descriptionLabel";
        //     // 
        //     // description
        //     // 
        //     resources.ApplyResources(this.description, "description");
        //     this.description.Name = "description";
        //     this.description.TextChanged += new System.EventHandler(this.description_TextChanged);
        //     // 
        //     // ownerLabel
        //     // 
        //     resources.ApplyResources(this.ownerLabel, "ownerLabel");
        //     this.ownerLabel.Name = "ownerLabel";
        //     // 
        //     // name
        //     // 
        //     resources.ApplyResources(this.name, "name");
        //     this.name.Name = "name";
        //     this.name.TextChanged += new System.EventHandler(this.name_TextChanged);
        //     // 
        //     // nameLabel
        //     // 
        //     resources.ApplyResources(this.nameLabel, "nameLabel");
        //     this.nameLabel.Name = "nameLabel";
        //     // 
        //     // lastExecuted
        //     // 
        //     resources.ApplyResources(this.lastExecuted, "lastExecuted");
        //     this.lastExecuted.Name = "lastExecuted";
        //     this.lastExecuted.ReadOnly = true;
        //     // 
        //     // lastExecutionLablel
        //     // 
        //     resources.ApplyResources(this.lastExecutionLablel, "lastExecutionLablel");
        //     this.lastExecutionLablel.Name = "lastExecutionLablel";
        //     // 
        //     // lastModified
        //     // 
        //     resources.ApplyResources(this.lastModified, "lastModified");
        //     this.lastModified.Name = "lastModified";
        //     this.lastModified.ReadOnly = true;
        //     // 
        //     // lastModifiedLabel
        //     // 
        //     resources.ApplyResources(this.lastModifiedLabel, "lastModifiedLabel");
        //     this.lastModifiedLabel.Name = "lastModifiedLabel";
        //     // 
        //     // created
        //     // 
        //     resources.ApplyResources(this.created, "created");
        //     this.created.Name = "created";
        //     this.created.ReadOnly = true;
        //     // 
        //     // createdLabel
        //     // 
        //     resources.ApplyResources(this.createdLabel, "createdLabel");
        //     this.createdLabel.Name = "createdLabel";
        //     // 
        //     // source
        //     // 
        //     resources.ApplyResources(this.source, "source");
        //     this.source.Name = "source";
        //     this.source.ReadOnly = true;
        //     // 
        //     // sourceLabel
        //     // 
        //     resources.ApplyResources(this.sourceLabel, "sourceLabel");
        //     this.sourceLabel.Name = "sourceLabel";
        //     // 
        //     // propertiesOnlyPanel
        //     // 
        //     resources.ApplyResources(this.propertiesOnlyPanel, "propertiesOnlyPanel");
        //     this.propertiesOnlyPanel.Controls.Add(this.lastExecuted);
        //     this.propertiesOnlyPanel.Controls.Add(this.lastExecutionLablel);
        //     this.propertiesOnlyPanel.Controls.Add(this.lastModified);
        //     this.propertiesOnlyPanel.Controls.Add(this.lastModifiedLabel);
        //     this.propertiesOnlyPanel.Controls.Add(this.created);
        //     this.propertiesOnlyPanel.Controls.Add(this.createdLabel);
        //     this.propertiesOnlyPanel.Controls.Add(this.source);
        //     this.propertiesOnlyPanel.Controls.Add(this.sourceLabel);
        //     this.propertiesOnlyPanel.Controls.Add(this.viewHistory);
        //     this.propertiesOnlyPanel.Name = "propertiesOnlyPanel";
        //     // 
        //     // viewHistory
        //     // 
        //     resources.ApplyResources(this.viewHistory, "viewHistory");
        //     this.viewHistory.Name = "viewHistory";
        //     this.viewHistory.TabStop = true;
        //     this.viewHistory.UseCompatibleTextRendering = true;
        //     this.viewHistory.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.viewHistory_LinkClicked);
        //     // 
        //     // JobPropertiesGeneral
        //     // 
        //     this.Controls.Add(this.propertiesOnlyPanel);
        //     this.Controls.Add(this.createOnlyPanel);
        //     this.Name = "JobPropertiesGeneral";
        //     resources.ApplyResources(this, "$this");
        //     this.createOnlyPanel.ResumeLayout(false);
        //     this.createOnlyPanel.PerformLayout();
        //     this.propertiesOnlyPanel.ResumeLayout(false);
        //     this.propertiesOnlyPanel.PerformLayout();
        //     this.ResumeLayout(false);

        // }
        #endregion


        #region ui work
        private void UpdateControlStatus()
        {
            // if (this.data == null)
            // {
            //     return;
            // }
            // if (this.data.Mode == JobData.DialogMode.Create)
            // {
            //     this.propertiesOnlyPanel.Visible = false;
            // }

            // Control [] enableDisableControls;
            
            // if (this.data.AllowEnableDisable)
            // {
            //     enableDisableControls = new Control[] {this.category,
            //         this.viewOtherJobsInThisCategory,
            //         this.description,
            //         this.owner,
            //         this.browseForOwner,
            //         this.name,
            //         this.viewHistory };
            // }
            // else
            // {
            //     enableDisableControls = new Control[] { this.category,
            //         this.viewOtherJobsInThisCategory,
            //         this.description,
            //         this.enabled,
            //         this.owner,
            //         this.browseForOwner,
            //         this.name,
            //         this.viewHistory};
            // }
            // SetDialogFieldsReadOnly(this.data.IsReadOnly,enableDisableControls);
            // this.owner.Enabled = this.data.IsUserAgentAdmin;
            // ///If this is an MSX job.  Do not allow the user to change the category.  If the job has already
            // ///been saved.
            // if (this.data.DateCreated.Ticks > 0 && (!this.data.TargetLocalServer || !this.data.IsLocalJob) && (!this.data.JobCategoryIsLocal))
            // {
            //     this.category.Enabled = false;
            // }
        }


        private void InitializeData()
        {
            // if (this.data == null)
            // {
            //     return;
            // }
            // // job name
            // this.name.Text = this.data.Name;

            // this.owner.Text = this.data.Owner;

            // PopulateCategoryCombobox();

            // this.description.Text = this.data.Description;
            // this.enabled.Checked = this.data.Enabled;

            // if (this.data.Mode == JobData.DialogMode.Properties)
            // {
            //     this.source.Text = this.data.Source;
            //     this.created.Text = this.data.DateCreated.ToString(CultureInfo.CurrentCulture);
            //     this.lastModified.Text = this.data.DateLastModified.ToString(CultureInfo.CurrentCulture);
            //     if (this.data.LastRunDate != DateTime.MinValue)
            //     {
            //         this.lastExecuted.Text = this.data.LastRunDate.ToString(CultureInfo.CurrentCulture);
            //     }
            // }
        }

        //Populate the category combo box
        // private void PopulateCategoryCombobox()
        // {
        //     string currentCategory;
        //     //Because this combo box will be populated with varying categories
        //     //make sure it is cleaned up before adding more entries.
        //     this.category.Items.Clear();

        //     //First set the data.Category to the appropriate category.  
        //     //We will scan the categories object collection for the correct object and set the
        //     //current data.Category to this object
        //     if (this.data.Category != null)
        //     {
        //         currentCategory = this.data.Category.ToString();
        //     }
        //     else
        //     {
        //         currentCategory = this.data.TargetLocalServer ? LocalizableCategorySR.CategoryLocal : LocalizableCategorySR.CategoryMultiServer;
        //     }
        //     bool categoryFound = false;

        //     for (int i = 0; i < this.data.Categories.Length; i++)
        //     {
        //         if (this.data.Categories[i].Name == currentCategory)
        //         {
        //             this.data.Category = this.data.Categories[i];
        //             categoryFound = true;
        //             break;
        //         }

        //     }
        //     //Could not find the category. Set the current category to null
        //     if (!categoryFound)
        //     {
        //         this.data.Category = null;
        //     }


        //     // populate the categories combo
        //     this.category.Items.AddRange(this.data.Categories);

        //     if (this.data.Category != null)
        //     {
        //         this.category.SelectedItem = this.data.Category;
        //     }
        //     else
        //     {
        //         this.category.SelectedIndex = 0;
        //     }
        //     this.category.Refresh();

        // }
        #endregion

        #region state changing
        // private void name_TextChanged(object sender, System.EventArgs e)
        // {
        //     this.data.Name = this.name.Text;
        // }
        // private void owner_TextChanged(object sender, System.EventArgs e)
        // {
        //     this.data.Owner = this.owner.Text;
        // }
        // private void category_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     this.data.Category = (LocalizableCategory)this.category.SelectedItem;
        // }
        // private void description_TextChanged(object sender, System.EventArgs e)
        // {
        //     this.data.Description = this.description.Text;
        // }
        // private void enabled_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     this.data.Enabled = this.enabled.Checked;
        // }
        // private void viewHistory_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        // {
        //     XmlDocument XmlDoc = new XmlDocument();

        //     if (this.historyDialog == null)
        //     {

        //         this.historyDialog = new LogViewerForm(this.CreateLogViewerXmlDocument(), this.ServiceProvider);
        //         this.historyDialog.Closed += new EventHandler(this.OnChildWindowClosed);
        //         this.historyDialog.ShowDialog();
        //     }
        //     else
        //     {
        //         // make sure existing dialog can be shown
        //         if (this.historyDialog.WindowState == FormWindowState.Minimized)
        //         {
        //             this.historyDialog.WindowState = FormWindowState.Normal;
        //         }
        //         // show the form
        //         this.historyDialog.Activate();
        //     }
        // }


        // /// <summary>
        // /// called when the history for this dialog is closed
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="args"></param>
        // private void OnChildWindowClosed(object sender, EventArgs args)
        // {
        //     // we should only be interested in events from our history dialog.
        //     STrace.Assert(sender == this.historyDialog);
        //     // remove the event handler
        //     this.historyDialog.Closed -= new EventHandler(this.OnChildWindowClosed);
        //     // remove our reference.
        //     this.historyDialog = null;
        // }
        #endregion

        #region Helpers

        //Create the XML document that will be used to pass to the LogViewerForm dialog.
        private XmlDocument CreateLogViewerXmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<params><sourcetype>jobhistory</sourcetype><nt>false</nt></params>");

            string jobName = this.data.Job.Name;

            XmlElement jobElement = doc.CreateElement("job");
            jobElement.InnerText = jobName;
            doc.DocumentElement.AppendChild(jobElement);

            XmlElement serverNameElement = doc.CreateElement("servername");
            serverNameElement.InnerText = this.managedConnection.Connection.ServerName;
            doc.DocumentElement.AppendChild(serverNameElement);

            XmlElement urnElement = doc.CreateElement("urn");
            urnElement.InnerText = "Server[@Name='" + this.managedConnection.Connection.ServerName;

            urnElement.InnerText += "']/JobServer/Job[@Name='" + Urn.EscapeString(jobName) + "' and @CategoryID='" + this.data.Job.CategoryID + "']";
            doc.DocumentElement.AppendChild(urnElement);

            return doc;
        }

        // protected override void OnHosted()
        // {
        //     base.OnHosted();
        //     this.managedConnection = (IManagedConnection)this.ServiceProvider.GetService(typeof(IManagedConnection));
        // }

        #endregion

        #region EventHandlers

        /// <summary>
        /// This event handler will be fired when the JobPropertiesData.Categories has changed.  This array is built dynamically
        /// based on the job action require (MultServer(MSX) or Local Job).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>       
        private void OnCategoriesChangedListener(object sender, System.EventArgs e)
        {
        //     //Changed  detected in the JobPropertiesData(JobData).Category array. Repopulate the caterory combo box.
        //     this.PopulateCategoryCombobox();
        }

        // private void viewOtherJobsInThisCategory_Click(object sender, System.EventArgs e)
        // {
        //     using (ViewJobsInCategory viewJobs = new ViewJobsInCategory(this.DataContainer,
        //                                                                 this.category.SelectedItem as LocalizableCategory))
        //     {
        //         viewJobs.ShowDialog();
        //     }
        // }
        // private void browseForOwner_Click(object sender, EventArgs e)
        // {
        //     CUtils utils = new CUtils();
        //     // pop up the object picker to select a schema.
        //     using (Icon iconSearchLogin = utils.LoadIcon("login.ico"))
        //     {
        //         using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
        //                                                          iconSearchLogin,
        //                                                          this.HelpProvider,
        //                                                          UserSR.TitleSearchLogin,
        //                                                          this.DataContainer.ConnectionInfo,
        //                                                          "msdb",
        //                                                          new SearchableObjectTypeCollection(SearchableObjectType.LoginOnly),
        //                                                          new SearchableObjectTypeCollection(SearchableObjectType.LoginOnly)))
        //         {
        //             if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
        //             {
        //                 //Replace the left and right brackets
        //                 System.Text.StringBuilder sb = new System.Text.StringBuilder(dlg.SearchResults[0].ToString());
        //                 sb.Replace("[", "");
        //                 sb.Replace("]", "");
        //                 this.owner.Text = sb.ToString();
        //             }
        //         }
        //     }
        // }
        #endregion

    }
}








