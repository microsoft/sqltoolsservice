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
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for CmdExecJobSubSystemDefinition.
    /// </summary>
    internal sealed class CmdExecJobSubSystemDefinition : ManagementActionBase, IJobStepPropertiesControl
    {
        // private System.Windows.Forms.Button selectAll;
        // private System.Windows.Forms.Button copy;
        // private System.Windows.Forms.Button paste;
        // private System.Windows.Forms.TextBox command;
        // private System.Windows.Forms.Label commandLabel;
        // private System.Windows.Forms.Label processExitCodeLabel;
        // private System.Windows.Forms.Button openFile;
        // private System.Windows.Forms.TextBox processExitCode;
        // private ImageList imgIcons;
        // private TextBox txtInfo;
        // private Panel infoPanel;
        // private PictureBox picInfo;
        // private IContainer components;

        public CmdExecJobSubSystemDefinition()
        {
        }

        public CmdExecJobSubSystemDefinition(CDataContainer dataContainer)
        {
            // This call is required by the Windows.Forms Form Designer.
            // InitializeComponent();

            // //Adjust Command.MaxLength depending on the version of SQL Server.
            // // Version >= 9 then MaxLength=SqlLimits.VarcharMax (nVarchar(max))
            // // Version <  9 then MaxLength=CommandDimensionMaxLength 
            // if (dataContainer.Server.Information.Version.Major >= 9)
            // {
            //     this.command.MaxLength = SqlLimits.VarcharMax;  //Ver >=9
            // }
            // else
            // {
            //     this.command.MaxLength = SqlLimits.CommandDimensionMaxLength;  //Ver < 9
            // }

            // this.picInfo.Image = this.imgIcons.Images[1];

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
        //     this.components = new System.ComponentModel.Container();
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CmdExecJobSubSystemDefinition));
        //     this.selectAll = new System.Windows.Forms.Button();
        //     this.copy = new System.Windows.Forms.Button();
        //     this.paste = new System.Windows.Forms.Button();
        //     this.command = new System.Windows.Forms.TextBox();
        //     this.commandLabel = new System.Windows.Forms.Label();
        //     this.processExitCodeLabel = new System.Windows.Forms.Label();
        //     this.openFile = new System.Windows.Forms.Button();
        //     this.processExitCode = new System.Windows.Forms.TextBox();
        //     this.imgIcons = new System.Windows.Forms.ImageList(this.components);
        //     this.txtInfo = new System.Windows.Forms.TextBox();
        //     this.infoPanel = new System.Windows.Forms.Panel();
        //     this.picInfo = new System.Windows.Forms.PictureBox();
        //     this.infoPanel.SuspendLayout();
        //     ((System.ComponentModel.ISupportInitialize)(this.picInfo)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // selectAll
        //     // 
        //     resources.ApplyResources(this.selectAll, "selectAll");
        //     this.selectAll.Name = "selectAll";
        //     this.selectAll.Click += new System.EventHandler(this.selectAll_Click);
        //     // 
        //     // copy
        //     // 
        //     resources.ApplyResources(this.copy, "copy");
        //     this.copy.Name = "copy";
        //     this.copy.Click += new System.EventHandler(this.copy_Click);
        //     // 
        //     // paste
        //     // 
        //     resources.ApplyResources(this.paste, "paste");
        //     this.paste.Name = "paste";
        //     this.paste.Click += new System.EventHandler(this.paste_Click);
        //     // 
        //     // command
        //     // 
        //     this.command.AcceptsReturn = true;
        //     this.command.AcceptsTab = true;
        //     resources.ApplyResources(this.command, "command");
        //     this.command.Name = "command";
        //     // 
        //     // commandLabel
        //     // 
        //     resources.ApplyResources(this.commandLabel, "commandLabel");
        //     this.commandLabel.Name = "commandLabel";
        //     // 
        //     // processExitCodeLabel
        //     // 
        //     resources.ApplyResources(this.processExitCodeLabel, "processExitCodeLabel");
        //     this.processExitCodeLabel.Name = "processExitCodeLabel";
        //     // 
        //     // openFile
        //     // 
        //     resources.ApplyResources(this.openFile, "openFile");
        //     this.openFile.Name = "openFile";
        //     this.openFile.Click += new System.EventHandler(this.openFile_Click);
        //     // 
        //     // processExitCode
        //     // 
        //     resources.ApplyResources(this.processExitCode, "processExitCode");
        //     this.processExitCode.Name = "processExitCode";
        //     this.processExitCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.processExitCode_KeyPress);
        //     // 
        //     // imgIcons
        //     // 
        //     this.imgIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgIcons.ImageStream")));
        //     this.imgIcons.Images.SetKeyName(0, "");
        //     this.imgIcons.Images.SetKeyName(1, "");
        //     this.imgIcons.Images.SetKeyName(2, "");
        //     this.imgIcons.Images.SetKeyName(3, "");
        //     // 
        //     // txtInfo
        //     // 
        //     resources.ApplyResources(this.txtInfo, "txtInfo");
        //     this.txtInfo.BackColor = System.Drawing.SystemColors.Info;
        //     this.txtInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
        //     this.txtInfo.Name = "txtInfo";
        //     // 
        //     // infoPanel
        //     // 
        //     resources.ApplyResources(this.infoPanel, "infoPanel");
        //     this.infoPanel.BackColor = System.Drawing.SystemColors.Info;
        //     this.infoPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        //     this.infoPanel.Controls.Add(this.txtInfo);
        //     this.infoPanel.Controls.Add(this.picInfo);
        //     this.infoPanel.Name = "infoPanel";
        //     // 
        //     // picInfo
        //     // 
        //     resources.ApplyResources(this.picInfo, "picInfo");
        //     this.picInfo.Name = "picInfo";
        //     this.picInfo.TabStop = false;
        //     // 
        //     // CmdExecJobSubSystemDefinition
        //     // 
        //     this.Controls.Add(this.processExitCode);
        //     this.Controls.Add(this.selectAll);
        //     this.Controls.Add(this.copy);
        //     this.Controls.Add(this.paste);
        //     this.Controls.Add(this.command);
        //     this.Controls.Add(this.commandLabel);
        //     this.Controls.Add(this.processExitCodeLabel);
        //     this.Controls.Add(this.openFile);
        //     this.Controls.Add(this.infoPanel);
        //     this.Name = "CmdExecJobSubSystemDefinition";
        //     resources.ApplyResources(this, "$this");
        //     this.infoPanel.ResumeLayout(false);
        //     ((System.ComponentModel.ISupportInitialize)(this.picInfo)).EndInit();
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.command.Text = data.Command;

            // if (data.CommandExecutionSuccessCode.ToString().Length > 0)
            // {
            //     this.processExitCode.Text = data.CommandExecutionSuccessCode.ToString();
            // }
            // else
            // {
            //     this.processExitCode.Text = "0";
            //     data.CommandExecutionSuccessCode = 0;
            // }

            // ///Set the state of the controls based on the job step read-only value
            // SetDialogFieldsReadOnly(data.IsReadOnly, new Control[] {
            //      selectAll,copy,paste,command,openFile,processExitCode}
            // );
        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
            // if (data.Command != this.command.Text)
            // {
            //     data.Command = this.command.Text;
            // }

            // if (this.processExitCode.Text.Length > 0)
            // {
            //     data.CommandExecutionSuccessCode = Convert.ToInt32(this.processExitCode.Text);
            // }
            // else
            // {
            //     data.CommandExecutionSuccessCode = 0;
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

        // private void processExitCode_KeyPress(object sender, KeyPressEventArgs e)
        // {

        //     ///Handle the backspace key
        //     if (e.KeyChar == '\b')
        //     {
        //         e.Handled = false;
        //         return;
        //     }

        //     ///Only allow digits and allow the backspace key to be used.    
        //     if (!char.IsNumber(e.KeyChar))
        //     {
        //         e.Handled = true;
        //         return;
        //     }

        //     ///If the key filtering has passed.  Check to see if the value is a valid integer
        //     try
        //     {
        //         ///Test only if the the string is not empty
        //         if (this.processExitCode.Text.Length > 0)
        //         {
        //             int testInt = Convert.ToInt32(this.processExitCode.Text);
        //         }
        //     }
        //     catch
        //     {
        //         ///Invalid int, cancel the user's last input
        //         e.Handled = true;
        //     }
        // }
    }
}
