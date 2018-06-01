//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlManagerUI;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for TSQLJobSubSystemDefinition.
    /// </summary>
    internal class TSqlJobSubSystemDefinition : ManagementActionBase, IJobStepPropertiesControl
    {
        // private System.Windows.Forms.Button openFile;
        // private System.Windows.Forms.Label databaseLabel;
        // private System.Windows.Forms.ComboBox databaseList;
        // private System.Windows.Forms.Label commandLabel;
        // private System.Windows.Forms.Button paste;
        // private System.Windows.Forms.Button copy;
        // private System.Windows.Forms.Button selectAll;
        // private System.Windows.Forms.Button parse;
        // private System.Windows.Forms.TextBox command;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;
        private CDataContainer dataContainer = null;
        //private IMessageBoxProvider messageProvider = null;
        private JobStepData jobStepData;

        public TSqlJobSubSystemDefinition()
        {
        }

        public TSqlJobSubSystemDefinition(CDataContainer dataContainer, JobStepData jobStepData)
        {
            this.dataContainer = dataContainer;
            this.jobStepData = jobStepData;

            //Adjust Command.MaxLength depending on the version of SQL Server.
            // Version >= 9 then MaxLength=SqlLimits.VarcharMax (nVarchar(max))
            // Version <  9 then MaxLength=CommandDimensionMaxLength 
            // if (dataContainer.Server.Information.Version.Major >= 9)
            // {
            //     this.command.MaxLength = SqlLimits.VarcharMax;  //Ver >=9
            // }
            // else
            // {
            //     this.command.MaxLength = SqlLimits.CommandDimensionMaxLength;  //Ver < 9
            // }
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
        }

#region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TSQLJobSubSystemDefinition));
        //     this.openFile = new System.Windows.Forms.Button();
        //     this.databaseLabel = new System.Windows.Forms.Label();
        //     this.databaseList = new System.Windows.Forms.ComboBox();
        //     this.commandLabel = new System.Windows.Forms.Label();
        //     this.command = new System.Windows.Forms.TextBox();
        //     this.paste = new System.Windows.Forms.Button();
        //     this.copy = new System.Windows.Forms.Button();
        //     this.selectAll = new System.Windows.Forms.Button();
        //     this.parse = new System.Windows.Forms.Button();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // openFile
        //     // 
        //     resources.ApplyResources(this.openFile, "openFile");
        //     this.openFile.Name = "openFile";
        //     this.openFile.Click += new System.EventHandler(this.openFile_Click);
        //     // 
        //     // databaseLabel
        //     // 
        //     resources.ApplyResources(this.databaseLabel, "databaseLabel");
        //     this.databaseLabel.Name = "databaseLabel";
        //     // 
        //     // databaseList
        //     // 
        //     resources.ApplyResources(this.databaseList, "databaseList");
        //     this.databaseList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.databaseList.FormattingEnabled = true;
        //     this.databaseList.Name = "databaseList";
        //     this.databaseList.SelectedIndexChanged += new System.EventHandler(this.databaseList_SelectedIndexChanged);
        //     // 
        //     // commandLabel
        //     // 
        //     resources.ApplyResources(this.commandLabel, "commandLabel");
        //     this.commandLabel.Name = "commandLabel";
        //     // 
        //     // command
        //     // 
        //     this.command.AcceptsReturn = true;
        //     this.command.AcceptsTab = true;
        //     resources.ApplyResources(this.command, "command");
        //     this.command.Name = "command";
        //     // 
        //     // paste
        //     // 
        //     resources.ApplyResources(this.paste, "paste");
        //     this.paste.Name = "paste";
        //     this.paste.Click += new System.EventHandler(this.paste_Click);
        //     // 
        //     // copy
        //     // 
        //     resources.ApplyResources(this.copy, "copy");
        //     this.copy.Name = "copy";
        //     this.copy.Click += new System.EventHandler(this.copy_Click);
        //     // 
        //     // selectAll
        //     // 
        //     resources.ApplyResources(this.selectAll, "selectAll");
        //     this.selectAll.Name = "selectAll";
        //     this.selectAll.Click += new System.EventHandler(this.selectAll_Click);
        //     // 
        //     // parse
        //     // 
        //     resources.ApplyResources(this.parse, "parse");
        //     this.parse.Name = "parse";
        //     this.parse.Click += new System.EventHandler(this.parse_Click);
        //     // 
        //     // TSQLJobSubSystemDefinition
        //     // 
        //     this.Controls.Add(this.parse);
        //     this.Controls.Add(this.selectAll);
        //     this.Controls.Add(this.copy);
        //     this.Controls.Add(this.paste);
        //     this.Controls.Add(this.command);
        //     this.Controls.Add(this.commandLabel);
        //     this.Controls.Add(this.databaseList);
        //     this.Controls.Add(this.databaseLabel);
        //     this.Controls.Add(this.openFile);
        //     this.Name = "TSQLJobSubSystemDefinition";
        //     resources.ApplyResources(this, "$this");
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
#endregion

#region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.jobStepData = data;
            // this.command.Text = data.Command;
            // this.databaseList.Items.Clear();
            // this.databaseList.Items.AddRange(data.Databases);
            // this.databaseList.Text = data.DatabaseName;

            // ///Set the state of the controls based on the job step read-only value
            // SetDialogFieldsReadOnly(data.IsReadOnly, new Control[] {
            //  openFile,databaseList,paste,copy,selectAll,parse,command}
            // );

        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
            // if (data.Command != this.command.Text)
            // {
            //     data.Command = this.command.Text;
            // }

            // if (this.databaseList.SelectedItem != null)
            // {
            //     data.DatabaseName = this.databaseList.SelectedItem.ToString();
                
                
            // }
        }
#endregion

        // private void openFile_Click(object sender, System.EventArgs e)
        // {
        //     using (OpenFileDialog dlg = new OpenFileDialog())
        //     {
        //         dlg.Filter = JobSR.TSQLFilter;
        //         dlg.FilterIndex = 0;
        //         if (dlg.ShowDialog(this) == DialogResult.OK)
        //         {
        //             this.command.Text = File.ReadAllText(dlg.FileName, Encoding.Default);
        //         }
        //     }
        // }

        // private void selectAll_Click(object sender, System.EventArgs e)
        // {
        //     this.command.SelectAll();
        //     this.command.Focus();
        // }

        // private void copy_Click(object sender, System.EventArgs e)
        // {
        //     this.command.Copy();
        // }

        // private void paste_Click(object sender, System.EventArgs e)
        // {
        //     this.command.Paste();
        // }

        // private void parse_Click(object sender, System.EventArgs e)
        // {
        //     ServerConnection conn = this.dataContainer.Server.ConnectionContext;
        //     try
        //     {
        //         if (this.dataContainer == null)
        //         {
        //             return;
        //         }
        //         // get the connection

        //         // use the selected database
        //         conn.ExecuteNonQuery(String.Format(System.Globalization.CultureInfo.InvariantCulture
        //                                            , "use [{0}]", this.databaseList.SelectedItem != null
        //                                            ? Urn.EscapeString(this.databaseList.SelectedItem.ToString())
        //                                            : "master"));

        //         conn.ExecuteNonQuery("set noexec on");

        //         conn.ExecuteNonQuery(this.command.Text);

        //         if (this.messageProvider != null)
        //         {

        //             this.messageProvider.ShowMessage(
        //                                             JobSR.ParseSuccess
        //                                             , JobSR.ParseTitle
        //                                             , Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK
        //                                             , Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Information
        //                                             , this);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         if (this.messageProvider != null)
        //         {

        //             this.messageProvider.ShowMessage(
        //                                             new Exception(JobSR.ParseFailure, ex)
        //                                             , JobSR.ParseTitle
        //                                             , Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK
        //                                             , Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Error
        //                                             , this);
        //         }
        //     }
        //     finally
        //     {
        //         try
        //         {
        //             conn.ExecuteNonQuery("set noexec off");
        //             conn.ExecuteNonQuery("use [master]");
        //         }
        //         catch (Exception)
        //         {
        //         }
        //     }
        // }

        // private void databaseList_SelectedIndexChanged(object sender, System.EventArgs e)
        // {
        //     if( this.databaseList.SelectedItem != null )
        //     {
        //         this.jobStepData.DatabaseName = this.databaseList.SelectedItem.ToString();
        //     }
        // }
    }
}
