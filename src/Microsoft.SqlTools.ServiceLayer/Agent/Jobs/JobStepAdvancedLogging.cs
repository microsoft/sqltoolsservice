//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepAdvancedLogging.
    /// </summary>
    internal sealed class JobStepAdvancedLogging : IJobStepPropertiesControl
    {
        private CDataContainer dataContainer = null;
        // private IMessageBoxProvider messageProvider = null;
        // private System.Windows.Forms.Label fileLabel;
        // private System.Windows.Forms.TextBox outputFile;
        // private System.Windows.Forms.Button browse;
        // private System.Windows.Forms.CheckBox appendOutput;

        private bool userIsSysAdmin = false;
        private bool canViewFileLog = false;
        private bool canSetFileLog = false;

        private JobStepData jobStepData;
        // private CheckBox logToTable;
        // private CheckBox appendToFile;
        // private CheckBox appendToTable;
        // private Button viewFileLog;
        // private Button viewTableLog;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public JobStepAdvancedLogging()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitForm call

        }

        public JobStepAdvancedLogging(CDataContainer dataContainer, JobStepData jobStepData)
        {            
            this.dataContainer = dataContainer;
            this.jobStepData = jobStepData;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing)
        //     {
        //         if (components != null)
        //         {
        //             components.Dispose();
        //         }
        //     }
        //     base.Dispose(disposing);
        // }

#region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JobStepAdvancedLogging));
            // this.fileLabel = new System.Windows.Forms.Label();
            // this.outputFile = new System.Windows.Forms.TextBox();
            // this.browse = new System.Windows.Forms.Button();
            // this.appendOutput = new System.Windows.Forms.CheckBox();
            // this.logToTable = new System.Windows.Forms.CheckBox();
            // this.appendToFile = new System.Windows.Forms.CheckBox();
            // this.appendToTable = new System.Windows.Forms.CheckBox();
            // this.viewFileLog = new System.Windows.Forms.Button();
            // this.viewTableLog = new System.Windows.Forms.Button();
            // this.SuspendLayout();
            // this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            // this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // // 
            // // fileLabel
            // // 
            // resources.ApplyResources(this.fileLabel, "fileLabel");
            // this.fileLabel.Name = "fileLabel";
            // // 
            // // outputFile
            // // 
            // resources.ApplyResources(this.outputFile, "outputFile");
            // this.outputFile.Name = "outputFile";
            // this.outputFile.TextChanged += new System.EventHandler(this.outputFile_TextChanged);
            // // 
            // // browse
            // // 
            // resources.ApplyResources(this.browse, "browse");
            // this.browse.Name = "browse";
            // this.browse.Click += new System.EventHandler(this.browse_Click);
            // // 
            // // appendOutput
            // // 
            // resources.ApplyResources(this.appendOutput, "appendOutput");
            // this.appendOutput.Name = "appendOutput";
            // // 
            // // logToTable
            // // 
            // resources.ApplyResources(this.logToTable, "logToTable");
            // this.logToTable.Name = "logToTable";
            // this.logToTable.CheckedChanged += new System.EventHandler(this.logToTable_CheckedChanged);
            // // 
            // // appendToFile
            // // 
            // resources.ApplyResources(this.appendToFile, "appendToFile");
            // this.appendToFile.Name = "appendToFile";
            // // 
            // // appendToTable
            // // 
            // resources.ApplyResources(this.appendToTable, "appendToTable");
            // this.appendToTable.Name = "appendToTable";
            // // 
            // // viewFileLog
            // // 
            // resources.ApplyResources(this.viewFileLog, "viewFileLog");
            // this.viewFileLog.Name = "viewFileLog";
            // this.viewFileLog.Click += new System.EventHandler(this.viewFileLog_Click);
            // // 
            // // viewTableLog
            // // 
            // resources.ApplyResources(this.viewTableLog, "viewTableLog");
            // this.viewTableLog.Name = "viewTableLog";
            // this.viewTableLog.Click += new System.EventHandler(this.viewTableLog_Click);
            // // 
            // // JobStepAdvancedLogging
            // // 
            // this.Controls.Add(this.viewTableLog);
            // this.Controls.Add(this.viewFileLog);
            // this.Controls.Add(this.appendToTable);
            // this.Controls.Add(this.appendToFile);
            // this.Controls.Add(this.logToTable);
            // this.Controls.Add(this.appendOutput);
            // this.Controls.Add(this.browse);
            // this.Controls.Add(this.outputFile);
            // this.Controls.Add(this.fileLabel);
            // this.Name = "JobStepAdvancedLogging";
            // resources.ApplyResources(this, "$this");
            // this.ResumeLayout(false);
            // this.PerformLayout();
        }
#endregion

#region IJobStepPropertiesControl implementation
        void IJobStepPropertiesControl.Load(JobStepData data)
        {
            // this.outputFile.Text = data.OutputFileName;
            // this.appendToFile.Checked = data.AppendToLogFile;
            // this.appendOutput.Checked = data.AppendToStepHistory;

            // this.logToTable.Checked = data.WriteLogToTable;
            // this.logToTable.Enabled = data.CanLogToTable;

            // this.appendToTable.Checked = data.AppendLogToTable;

            this.userIsSysAdmin = (data.Parent.Parent.UserRole & UserRoles.SysAdmin) > 0;
            this.canViewFileLog = this.userIsSysAdmin && data.Version.Major <= 8;
            // must be sysadmin to set log in yukon
            this.canSetFileLog = (data.Version.Major <= 8 || this.userIsSysAdmin);

            if (this.canSetFileLog)
            {
                // Managed Instance doesn't allow setting this path.
                //
                if (this.dataContainer != null &&
                    this.dataContainer.Server != null &&
                    this.dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                {
                    this.canSetFileLog = false;
                }
            }

            UpdateControlStatus();

        }
        void IJobStepPropertiesControl.Save(JobStepData data, bool isSwitching)
        {
        //     if (this.appendToFile.Checked && this.outputFile.Text.Trim().Length == 0)
        //     {
        //         throw new ApplicationException(SRError.MissingOutputLogFileName);
        //     }

        //     data.OutputFileName = this.outputFile.Text;
        //     data.AppendToLogFile = this.appendToFile.Checked;
        //     data.AppendToStepHistory = this.appendOutput.Checked;

        //     data.WriteLogToTable = this.logToTable.Checked;
        //     if (this.logToTable.Checked)
        //     {
        //         data.AppendLogToTable = this.appendToTable.Checked;
        //     }
        //     else
        //     {
        //         data.AppendLogToTable = false;
        //    } 
        }
#endregion

#region event handlers
        /// <summary>
        /// Called when the user clicks on the browse for file button. Will allow the
        /// user to either enter a new file, or pick an existing one for logging on the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void browse_Click(object sender, System.EventArgs e)
        {
            // using (BrowseFolder browse = new BrowseFolder(this.dataContainer.Server.ConnectionContext, 
            //                                               this.messageProvider))
            // {
            //     browse.Font = this.Font;
            //     browse.BrowseForFiles = true;

            //     if (browse.ShowDialog() == DialogResult.OK)
            //     {
            //         this.outputFile.Text = browse.SelectedFullFileName;
            //     }
            // }           
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void outputFile_TextChanged(object sender, EventArgs e)
        {
            UpdateControlStatus();

        }
        /// <summary>
        /// User wishes to view the file log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewFileLog_Click(object sender, EventArgs e)
        {
            // Cursor originalCursor = Cursor.Current;
            // try
            // {
            //     Cursor.Current = Cursors.WaitCursor;
            //     try
            //     {
            //         string tempFileName = String.Empty;
            //         if (CheckFileExistsAndIsValid(this.outputFile.Text))
            //         {
            //             tempFileName = ReadLogToFile(this.outputFile.Text);
            //         }

            //         ViewLog(tempFileName);
            //     }
            //     catch (Exception ex)
            //     {
            //         messageProvider.ShowMessage(
            //                                    ex
            //                                    , SRError.SQLWorkbench
            //                                    , Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK
            //                                    , Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Error
            //                                    , this);
            //     }
            // }
            // finally
            // {
            //     Cursor.Current = originalCursor;
            // }
        }
        /// <summary>
        /// user wishes to view the table log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logToTable_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControlStatus();
        }
        private void viewTableLog_Click(object sender, EventArgs e)
        {
            // Cursor originalCursor = Cursor.Current;
            // try
            // {
            //     Cursor.Current = Cursors.WaitCursor;
            //     try
            //     {
            //         JobStep step = this.jobStepData.JobStep;
            //         String tempFileName = String.Empty;

            //         if (step != null)
            //         {
            //             tempFileName = ReadStepLogToFile(step);
            //         }
            //         // Note that ViewLog deletes the temp file after showing it.
            //         ViewLog(tempFileName);
            //     }
            //     catch (Exception ex)
            //     {
            //         messageProvider.ShowMessage(
            //                                    ex
            //                                    , SRError.SQLWorkbench
            //                                    , Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK
            //                                    , Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Error
            //                                    , this);
            //     }

            // }
            // finally
            // {
            //     Cursor.Current = originalCursor;
            // }
        }

        private void ViewLog(string tempFileName)
        {
            // if (tempFileName == null || tempFileName.Length == 0)
            // {
            //     messageProvider.ShowMessage(
            //                                SRError.LogNotYetCreated
            //                                , SRError.SQLWorkbench
            //                                , Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK
            //                                , Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Information
            //                                , this);
            // }
            // else
            // {
            //     try
            //     {
            //         String notepadProcess = String.Format(CultureInfo.InvariantCulture
            //                                               , "{0}\\notepad.exe"
            //                                               , System.Environment.SystemDirectory);

            //         System.Diagnostics.Process.Start(notepadProcess, tempFileName);
            //         System.Threading.Thread.Sleep(1000);
            //     }
            //     finally
            //     {
            //         System.IO.File.Delete(tempFileName);
            //     }
            // }
        }
#endregion

#region internal helpers
        /// <summary>
        /// Update the enabled/disabled status of controls
        /// </summary>
        private void UpdateControlStatus()
        {
            // this.appendToTable.Enabled = this.logToTable.Checked;
            // this.viewTableLog.Enabled = this.logToTable.Checked;
            // this.viewFileLog.Enabled = this.canViewFileLog
            //                            && this.outputFile.Text.Length > 0
            //                            && this.outputFile.Text[this.outputFile.Text.Length - 1] != '\\';
            // this.outputFile.Enabled = this.canSetFileLog;
            // this.browse.Enabled = this.canSetFileLog;
			// this.appendToFile.Enabled = this.canSetFileLog
			// 						   && this.outputFile.Text.Length > 0
			// 						   && this.outputFile.Text[this.outputFile.Text.Length - 1] != '\\';
			// if (!this.appendToFile.Enabled) this.appendToFile.Checked = false;
        }
        /// <summary>
        /// Check that a file exists on the server, and validates the path.
        /// It will throw if the file is either a directoty, or it's parent
        /// path is invalid.
        /// </summary>
        /// <param name="file">File name</param>
        /// <returns>true if the file already exists</returns>
        private bool CheckFileExistsAndIsValid(string file)
        {
            bool fileExists = false;
            // check to see that the file exists.
            // ServerConnection connection = this.dataContainer.ServerConnection;

            // string query = String.Format(CultureInfo.InvariantCulture
            //                              , "EXECUTE master.dbo.xp_fileexist {0}"
            //                              , SqlSmoObject.MakeSqlString(file));

            // DataSet data = connection.ExecuteWithResults(query);

            // STrace.Assert(data.Tables.Count == 1, "Unexpected number of result sets returned from query");

            // if (data.Tables.Count > 0)
            // {
            //     DataTable table = data.Tables[0];

            //     STrace.Assert(table.Rows.Count == 1, "Unexpected number of rows returned");
            //     STrace.Assert(table.Columns.Count == 3, "Unexpected number of columns returned");

            //     if (table.Rows.Count > 0 && table.Columns.Count > 2)
            //     {
            //         DataRow row = table.Rows[0];

            //         fileExists = ((byte)row[0] == 1);
            //         bool fileIsDirectory = ((byte)row[1] == 1);

            //         if (fileIsDirectory)
            //         {
            //             throw new ApplicationException(SRError.FileIsDirectory);
            //         }

            //         bool parentDiectoryExists = ((byte)row[2] == 1);
            //         if (!parentDiectoryExists)
            //         {
            //             throw new ApplicationException(SRError.FileLocationInvalid);
            //         }
            //     }
            // }

            return fileExists;
        }
        /// <summary>
        /// read a log on the server to a local file. This method is only supported on
        /// pre 9.0 servers
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string ReadLogToFile(string fileName)
        {
            ServerConnection connection = this.dataContainer.ServerConnection;

            string query = string.Format(CultureInfo.InvariantCulture
                                         , "EXECUTE master.dbo.xp_readerrorlog -1, {0}"
                                         , SqlSmoObject.MakeSqlString(fileName));

            DataSet data = connection.ExecuteWithResults(query);

            if (data.Tables.Count > 0)
            {
                DataTable table = data.Tables[0];

                string tempFileName = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                    "{0}{1}", Path.GetTempPath(), "JobSR.StepOutput(Path.GetFileName(fileName))");

                StreamWriter writer = new StreamWriter(tempFileName, false, Encoding.Unicode);

                foreach(DataRow row in table.Rows)
                {
                    writer.Write(row[0].ToString());
                    if ((byte)row[1] == 0)
                    {
                        writer.WriteLine();
                    }
                }

                writer.Close();

                return tempFileName;
            }
            return string.Empty;
        }

        /// <summary>
        /// Read the step log to a file. This is only supported on a 9.0 Server
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private string ReadStepLogToFile(JobStep step)
        {
            DataTable table = step.EnumLogs();

            String tempFileName = Path.GetTempFileName();

            StreamWriter writer = new StreamWriter(tempFileName, false, Encoding.Unicode);

            foreach(DataRow row in table.Rows)
            {
                writer.WriteLine(row["Log"].ToString());
            }

            writer.Close();

            return tempFileName;
        }
#endregion
    }
}








