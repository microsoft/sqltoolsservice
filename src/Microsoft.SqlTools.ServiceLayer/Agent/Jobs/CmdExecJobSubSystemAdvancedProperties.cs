//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for CmdExecJobSubSystemAdvancedProperties.
    /// </summary>
    internal sealed class CmdExecJobSubSystemAdvancedProperties : IJobStepPropertiesControl
    {
        private CDataContainer dataContainer = null;
        // private IMessageBoxProvider messageProvider = null;
        // private System.Windows.Forms.Button browse;
        // private System.Windows.Forms.TextBox outputFile;
        // private System.Windows.Forms.Label fileLabel;
        // private System.Windows.Forms.RadioButton append;
        // private System.Windows.Forms.RadioButton overwrite;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;

        public CmdExecJobSubSystemAdvancedProperties()
        {

        }
        public CmdExecJobSubSystemAdvancedProperties(CDataContainer dataContainer)
        {            
            this.dataContainer = dataContainer;

        }

#region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CmdExecJobSubSystemAdvancedProperties));
        //     this.browse = new System.Windows.Forms.Button();
        //     this.outputFile = new System.Windows.Forms.TextBox();
        //     this.fileLabel = new System.Windows.Forms.Label();
        //     this.append = new System.Windows.Forms.RadioButton();
        //     this.overwrite = new System.Windows.Forms.RadioButton();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // browse
        //     // 
        //     resources.ApplyResources(this.browse, "browse");
        //     this.browse.Name = "browse";
        //     this.browse.Click += new System.EventHandler(this.browse_Click);
        //     // 
        //     // outputFile
        //     // 
        //     resources.ApplyResources(this.outputFile, "outputFile");
        //     this.outputFile.Name = "outputFile";
        //     // 
        //     // fileLabel
        //     // 
        //     resources.ApplyResources(this.fileLabel, "fileLabel");
        //     this.fileLabel.Name = "fileLabel";
        //     // 
        //     // append
        //     // 
        //     resources.ApplyResources(this.append, "append");
        //     this.append.Name = "append";
        //     this.append.CheckedChanged += new System.EventHandler(this.CheckedChanged);
        //     // 
        //     // overwrite
        //     // 
        //     resources.ApplyResources(this.overwrite, "overwrite");
        //     this.overwrite.Name = "overwrite";
        //     this.overwrite.CheckedChanged += new System.EventHandler(this.CheckedChanged);
        //     // 
        //     // CmdExecJobSubSystemAdvancedProperties
        //     // 
        //     this.Controls.Add(this.append);
        //     this.Controls.Add(this.overwrite);
        //     this.Controls.Add(this.browse);
        //     this.Controls.Add(this.outputFile);
        //     this.Controls.Add(this.fileLabel);
        //     this.Name = "CmdExecJobSubSystemAdvancedProperties";
        //     resources.ApplyResources(this, "$this");
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
#endregion

#region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.outputFile.Text = data.OutputFileName;
            // this.append.Checked = data.AppendToLogFile;
            // this.overwrite.Checked = !data.AppendToLogFile;
        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
            // data.OutputFileName = this.outputFile.Text;
            // data.AppendToLogFile = this.append.Checked;
        }
#endregion

        // private void browse_Click(object sender, System.EventArgs e)
        // {
        //     using (BrowseFolder browse = new BrowseFolder(this.dataContainer.Server.ConnectionContext, 
        //                                                   this.messageProvider))
        //     {
        //         browse.Font = this.Font;
        //         browse.BrowseForFiles = true;

        //         if (browse.ShowDialog() == DialogResult.OK)
        //         {
        //             this.outputFile.Text = browse.SelectedFullFileName;
        //         }
        //     }
        // }

        // private void CheckedChanged(object sender, System.EventArgs e)
        // {

        // }
    }
}









