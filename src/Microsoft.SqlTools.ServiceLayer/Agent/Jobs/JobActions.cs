//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Globalization;
using System.Xml;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobPropertiesGeneral.
    /// </summary>
    internal class JobActions : ManagementActionBase
    {
        private JobData data;
        private IManagedConnection managedConnection = null;

        public JobActions(CDataContainer dataContainer, JobData data)
        {          
            this.DataContainer = dataContainer;
            this.data = data;

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

            // Dispose of the data member (a reference to JobProperties data).
            if (this.data != null)
            {
                //release the object
                this.data = null;
            }
        }

        

        // private void InitializeData()
        // {
        //     // if (this.data == null)
        //     // {
        //     //     return;
        //     // }
        //     // // job name
        //     // this.name.Text = this.data.Name;

        //     // this.owner.Text = this.data.Owner;

        //     // PopulateCategoryCombobox();

        //     // this.description.Text = this.data.Description;
        //     // this.enabled.Checked = this.data.Enabled;

        //     // if (this.data.Mode == JobData.DialogMode.Properties)
        //     // {
        //     //     this.source.Text = this.data.Source;
        //     //     this.created.Text = this.data.DateCreated.ToString(CultureInfo.CurrentCulture);
        //     //     this.lastModified.Text = this.data.DateLastModified.ToString(CultureInfo.CurrentCulture);
        //     //     if (this.data.LastRunDate != DateTime.MinValue)
        //     //     {
        //     //         this.lastExecuted.Text = this.data.LastRunDate.ToString(CultureInfo.CurrentCulture);
        //     //     }
        //     // }
        // }

        // Populate the category combo box
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
    }
}
