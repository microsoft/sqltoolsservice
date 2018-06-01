//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for PowerShellJobSubSystemDefinition.
    /// </summary>
    internal sealed class PowerShellJobSubSystemDefinition : ManagementActionBase, IJobStepPropertiesControl
    {
    //     private System.Windows.Forms.Button selectAll;
    //     private System.Windows.Forms.Button copy;
    //     private System.Windows.Forms.Button paste;
    //     private System.Windows.Forms.TextBox command;
    //     private System.Windows.Forms.Label commandLabel;
    //     private System.Windows.Forms.Button openFile;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
       // private System.ComponentModel.Container components = null;

        public PowerShellJobSubSystemDefinition()
        {
        }

        public PowerShellJobSubSystemDefinition(CDataContainer dataContainer)
        {
            //Adjust Command.MaxLength depending on the version of SQL Server.
            // Version >= 9 then MaxLength=SqlLimits.VarcharMax (nVarchar(max))
            // Version <  9 then MaxLength=CommandDimensionMaxLength 
            // if (dataContainer.Server.Version.Major >= 9)
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
        private void InitializeComponent()
        {
            // System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PowerShellJobSubSystemDefinition));
            // this.selectAll = new System.Windows.Forms.Button();
            // this.copy = new System.Windows.Forms.Button();
            // this.paste = new System.Windows.Forms.Button();
            // this.command = new System.Windows.Forms.TextBox();
            // this.commandLabel = new System.Windows.Forms.Label();
            // this.openFile = new System.Windows.Forms.Button();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // // 
            // // selectAll
            // // 
            // resources.ApplyResources(this.selectAll, "selectAll");
            // this.selectAll.Name = "selectAll";
            // this.selectAll.Click += new System.EventHandler(this.selectAll_Click);
            // // 
            // // copy
            // // 
            // resources.ApplyResources(this.copy, "copy");
            // this.copy.Name = "copy";
            // this.copy.Click += new System.EventHandler(this.copy_Click);
            // // 
            // // paste
            // // 
            // resources.ApplyResources(this.paste, "paste");
            // this.paste.Name = "paste";
            // this.paste.Click += new System.EventHandler(this.paste_Click);
            // // 
            // // command
            // // 
            // this.command.AcceptsReturn = true;
            // this.command.AcceptsTab = true;
            // resources.ApplyResources(this.command, "command");
            // this.command.Name = "command";
            // // 
            // // commandLabel
            // // 
            // resources.ApplyResources(this.commandLabel, "commandLabel");
            // this.commandLabel.Name = "commandLabel";
            // // 
            // // openFile
            // // 
            // resources.ApplyResources(this.openFile, "openFile");
            // this.openFile.Name = "openFile";
            // this.openFile.Click += new System.EventHandler(this.openFile_Click);
            // // 
            // // PowerShellJobSubSystemDefinition
            // // 
            // this.Controls.Add(this.selectAll);
            // this.Controls.Add(this.copy);
            // this.Controls.Add(this.paste);
            // this.Controls.Add(this.command);
            // this.Controls.Add(this.commandLabel);
            // this.Controls.Add(this.openFile);
            // this.Name = "PowerShellJobSubSystemDefinition";
            // resources.ApplyResources(this, "$this");
            // this.ResumeLayout(false);

        }
#endregion

#region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.command.Text = data.Command;
            
            // ///Set the state of the controls based on the job step read-only value
            // SetDialogFieldsReadOnly(data.IsReadOnly, new Control[] {
            //    selectAll,copy,paste,command,openFile}
            // );
        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
            // if (data.Command != this.command.Text)
            // {
            //     data.Command = this.command.Text;
            // }
        }
#endregion

        // private void openFile_Click(object sender, System.EventArgs e)
        // {
        //     using (OpenFileDialog dlg = new OpenFileDialog())
        //     {
        //         dlg.Filter = JobSR.AllFiles;
        //         dlg.FilterIndex = 0;
        //         if (dlg.ShowDialog(this) == DialogResult.OK)
        //         {
        //             FileStream file = null;
        //             try
        //             {
        //                 file = File.Open(dlg.FileName, FileMode.Open);
        //                 StreamReader reader = new StreamReader(file);

        //                 this.command.Text = reader.ReadToEnd();
        //             }
        //             finally
        //             {
        //                 if (file != null)
        //                     file.Close();
        //             }
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
    }
}
