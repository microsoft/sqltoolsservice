#if false
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
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepAdvancedLogging.
    /// </summary>
    internal sealed class JobStepAdvancedLogging
    {
        private CDataContainer dataContainer = null;
       
        private JobStepData jobStepData;


        public JobStepAdvancedLogging()
        {
        }

        public JobStepAdvancedLogging(CDataContainer dataContainer, JobStepData jobStepData)
        {            
            this.dataContainer = dataContainer;
            this.jobStepData = jobStepData;
        }


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
#endif