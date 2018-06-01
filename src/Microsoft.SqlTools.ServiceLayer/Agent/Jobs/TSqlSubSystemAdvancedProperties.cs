//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for TSQLSubSystemAdvancedProperties.
    /// </summary>
    internal sealed class TSqlSubSystemAdvancedProperties : IJobStepPropertiesControl
    {
        private CDataContainer dataContainer = null;
        private bool userIsSysAdmin = false;
        private JobStepAdvancedLogging loggingControl = null;
        private JobStepData jobStepData;
        private Microsoft.SqlServer.Management.Controls.Separator seperator;
        
        public TSqlSubSystemAdvancedProperties()
        {           
        }

        public TSQLSubSystemAdvancedProperties(CDataContainer dataContainer, JobStepData jobStepData, IServiceProvider serviceProvider)
        {
            this.loggingControl = new JobStepAdvancedLogging(dataContainer, messageProvider, jobStepData);           
            this.dataContainer = dataContainer;
            this.jobStepData = jobStepData;
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
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TSQLSubSystemAdvancedProperties));
        //     this.runAsUser = new System.Windows.Forms.TextBox();
        //     this.runasUserLabel = new System.Windows.Forms.Label();
        //     this.buttonRunAsUser = new System.Windows.Forms.Button();
        //     this.seperator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.outputContainer = new System.Windows.Forms.Panel();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // runAsUser
        //     // 
        //     resources.ApplyResources(this.runAsUser, "runAsUser");
        //     this.runAsUser.Name = "runAsUser";
        //     // 
        //     // runasUserLabel
        //     // 
        //     resources.ApplyResources(this.runasUserLabel, "runasUserLabel");
        //     this.runasUserLabel.Name = "runasUserLabel";
        //     // 
        //     // buttonRunAsUser
        //     // 
        //     resources.ApplyResources(this.buttonRunAsUser, "buttonRunAsUser");
        //     this.buttonRunAsUser.Name = "buttonRunAsUser";
        //     this.buttonRunAsUser.Click += new System.EventHandler(this.buttonRunAsUser_Click);
        //     // 
        //     // seperator
        //     // 
        //     resources.ApplyResources(this.seperator, "seperator");
        //     this.seperator.Name = "seperator";
        //     // 
        //     // outputContainer
        //     // 
        //     resources.ApplyResources(this.outputContainer, "outputContainer");
        //     this.outputContainer.Margin = new System.Windows.Forms.Padding(0);
        //     this.outputContainer.Name = "outputContainer";
        //     // 
        //     // TSQLSubSystemAdvancedProperties
        //     // 
        //     this.Controls.Add(this.outputContainer);
        //     this.Controls.Add(this.seperator);
        //     this.Controls.Add(this.buttonRunAsUser);
        //     this.Controls.Add(this.runasUserLabel);
        //     this.Controls.Add(this.runAsUser);
        //     this.Name = "TSQLSubSystemAdvancedProperties";
        //     resources.ApplyResources(this, "$this");
        //     this.ResumeLayout(false);

        // }

#region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.userIsSysAdmin = (data.Parent.Parent.UserRole & UserRoles.SysAdmin) > 0;

            // this.runAsUser.Text = data.DatabaseUserName;

            // if (this.loggingControl != null)
            // {
            //     ((IJobStepPropertiesControl)this.loggingControl).Load(data);
            // }

            // UpdateControlStatus();
        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
            // data.DatabaseUserName = this.runAsUser.Text;

            // if (this.loggingControl != null)
            // {
            //     ((IJobStepPropertiesControl)this.loggingControl).Save(data, isSwitching);
            // }
        }
#endregion

        // private void buttonRunAsUser_Click(object sender, System.EventArgs e)
        // {
        //     // pop up the object picker to select a schema.
        //     using (SqlObjectSearch dlg = new SqlObjectSearch(this.Font,
        //                                                      iconSearchSchema,
        //                                                      this.helpProvider,
        //                                                      UserSR.TitleSearchUser,
        //                                                      this.dataContainer.ConnectionInfo,
        //                                                      this.jobStepData.DatabaseName,
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.User),
        //                                                      new SearchableObjectTypeCollection(SearchableObjectType.User)))
        //     {
        //         if (DialogResult.OK == dlg.ShowDialog(this.FindForm()))
        //         {
        //             //Replace the left and right brackets
        //             System.Text.StringBuilder sb = new System.Text.StringBuilder(dlg.SearchResults[0].ToString());
        //             sb.Replace("[", "");
        //             sb.Replace("]", "");
        //             this.runAsUser.Text = sb.ToString();
        //         }
        //     }            
        // }

        // private void UpdateControlStatus()
        // {
        //     this.runAsUser.Enabled = this.buttonRunAsUser.Enabled = this.userIsSysAdmin;
        // }

    }
}








