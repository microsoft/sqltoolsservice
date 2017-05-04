//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());
        private static ConnectionService connectionService = null;

        // Backup instance to perform execution on
        private Backup backup;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DisasterRecoveryService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DisasterRecoveryService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(BackupRequest.Type, HandleBackupRequest);
        }

        /// <summary>
        /// Handles a backup request
        /// </summary>
        internal static async Task HandleBackupRequest(
            BackupParams backupParams,
            RequestContext<BackupResponse> requestContext)
        {            
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    backupParams.OwnerUri,
                    out connInfo);
                        
            if (connInfo != null)
            {
                SqlConnection sqlConn = GetSqlConnection(connInfo);
                if (sqlConn != null)
                {
                    Server server = new Server(new ServerConnection(sqlConn));
                    DisasterRecoveryService.Instance.InitializeBackup(backupParams.BackupInfo);
                    DisasterRecoveryService.Instance.PerformBackup(server);
                }
            }
         
            await requestContext.SendResult(new BackupResponse());
        }

        internal static SqlConnection GetSqlConnection(ConnectionInfo connInfo)
        {
            try
            {
                // increase the connection timeout to at least 30 seconds and and build connection string
                // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                connInfo.ConnectionDetails.PersistSecurityInfo = true;
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                // open a dedicated binding server connection
                SqlConnection sqlConn = new SqlConnection(connectionString);
                sqlConn.Open();
                return sqlConn;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private void GetBkProps()
        {
            /*this.propBackupExpireAfter = -1;
            this.propBackupExpireOn = DateTime.MinValue;*/
            try
            {
                //this.propBackupSetName = this.txtBakName.Text;
                //this.propBackupSetDescription = this.txtBakDescr.Text;
                /*if (true == this.rbOn.Checked)
                {
                    this.propBackupExpireOn = this.dataPick.Value;
                }
                if ((true == this.rbAfter.Checked) && (this.numDays.Value > 0))
                {
                    this.propBackupExpireAfter = (int)(this.numDays.Value);
                }*/
            }
            catch
            {                
            }
        }

        /// <summary>
        /// Create backup instance and set properties
        /// </summary>
        public void InitializeBackup(BackupInfo backupInfo)
        {
            this.backup = new Backup();
            try
            {
                this.backup.Database = backupInfo.DatabaseName;

                // backup component: Database/Files/Log
                switch(backupInfo.BackupComponent)
                {
                    case "Database":
                        this.backup.Action = BackupActionType.Database;
                        break;
                    case "Files":
                        this.backup.Action = BackupActionType.Files;
                        break;
                    case "Log":
                        this.backup.Action = BackupActionType.Log;
                        break;
                }
                
                // this is set to true if backup type is 'differential'
                this.backup.Incremental = backupInfo.BackupType == "Differential";

                //if (bk.Action == BackupActionType.Files)
                //{
                //    IDictionaryEnumerator IEnum = this.selectedFileGroup.GetEnumerator();
                //    IEnum.Reset();
                //    while (IEnum.MoveNext())
                //    {
                //        string CurrentKey = Convert.ToString(IEnum.Key,
                //            System.Globalization.CultureInfo.InvariantCulture);
                //        string CurrentValue = Convert.ToString(IEnum.Value,
                //            System.Globalization.CultureInfo.InvariantCulture);
                //        if (CurrentKey.IndexOf(",", StringComparison.Ordinal) < 0)
                //        {
                //            /// is a file group
                //            bk.DatabaseFileGroups.Add(CurrentValue);
                //        }
                //        else
                //        {
                //            /// is a file
                //            int Idx = CurrentValue.IndexOf(".", StringComparison.Ordinal);
                //            CurrentValue = CurrentValue.Substring(Idx + 1, CurrentValue.Length - Idx - 1);
                //            bk.DatabaseFiles.Add(CurrentValue);
                //        }
                //    }
                //}

                //string bakDest = this.cbBakDest.SelectedItem.ToString();
                //bool bBackupToUrl = false;
                //if (0 ==
                //    string.Compare(bakDest, BackupPropGeneralSR.BackupDestUrl, false,
                //        System.Globalization.CultureInfo.CurrentUICulture))
                //{
                //    bBackupToUrl = true;
                //}
                //bk.CopyOnly = this.cbCopyOnly.Checked;

                //string bakType = this.cBBakType.SelectedItem.ToString();
                //if (0 ==
                //    string.Compare(bakType, BackupPropGeneralSR.BackupTypeDiff, false,
                //        System.Globalization.CultureInfo.CurrentUICulture))
                //{
                //    bk.CopyOnly = false;
                //}

                //STParameters param;
                //bool bStatus;
                //param = new STParameters();
                //param.SetDocument(DataContainer.Document);
                //string backupSetName = string.Empty;
                //bStatus = param.GetParam("backupname", ref backupSetName);

                // !!!
                this.backup.BackupSetName = "test backup " + backupInfo.DatabaseName;

                // There is only 1 destination for Url which is retreived from this.urlControl
                //if (false == bBackupToUrl)
                //{
                                
                foreach (KeyValuePair<string, int> entry in backupInfo.BackupPathList)
                {
                    string destinationPath = entry.Key;
                    int deviceType = entry.Value;

                    switch (deviceType)
                    {
                        case (int)DeviceType.LogicalDevice:
                            this.backup.Devices.AddDevice(destinationPath, DeviceType.LogicalDevice);
                            break;
                        case (int)DeviceType.File:
                            this.backup.Devices.AddDevice(destinationPath, DeviceType.File);
                            break;
                    }

                    // !!The following check should be done at the UI level to make sure users don't mix it up!!
                    //switch (TypeOfDevice)
                    //{
                    //    case (int)DeviceType.LogicalDevice:
                    //        int deviceType = GetDeviceType(DestName);

                    //        if ((0 ==
                    //                string.Compare(bakDest, BackupPropGeneralSR.BackupDestDisk, false,
                    //                    System.Globalization.CultureInfo.CurrentUICulture))
                    //                && (deviceType == constDeviceTypeFile))
                    //        {
                    //            this.backup.Devices.AddDevice(DestName, DeviceType.LogicalDevice);
                    //        }
                    //        break;
                    //    case (int)DeviceType.File:
                    //        if (0 ==
                    //            string.Compare(bakDest, BackupPropGeneralSR.BackupDestDisk, false,
                    //                System.Globalization.CultureInfo.CurrentUICulture))
                    //        {
                    //            this.backup.Devices.AddDevice(DestName, DeviceType.File);
                    //        }
                    //        break;                            
                    //}
                }
                //}
                //else
                //{
                //    if (this.UrlControl130.ListBakDestUrls.Count > 0)
                //    {
                //        // Append the URL filename to the URL prefix
                //        foreach (string urlPath in this.UrlControl130.ListBakDestUrls.ToArray())
                //        {
                //            if (!String.IsNullOrWhiteSpace(urlPath))
                //            {
                //                bk.Devices.AddDevice(urlPath, DeviceType.Url);
                //            }
                //        }
                //    }
                //}

                //??? do i need this ????
                //if (DataContainer.HashTable.ContainsKey(bk.BackupSetName))
                //{
                //    DataContainer.HashTable.Remove(bk.BackupSetName);
                //}
                //DataContainer.HashTable.Add(bk.BackupSetName, bk);
            }
            catch
            {
                //ShowMessage(e,
                //    ExceptionMessageBoxButtons.OK,
                //    ExceptionMessageBoxSymbol.Error);
                //this.txFiles.Focus();
            }

            /**
             * The following is from 'backuppropmediaoptions.cs - onRunNow()'
             **/
            //STParameters param = null;
            //bool bStatus = false;
            //param = new STParameters();
            //param.SetDocument(DataContainer.Document);
            //bStatus = param.GetParam("backupname", ref this.backupName);
            //this.bk = (Backup)(DataContainer.HashTable[this.backupName]);

            //if (this.rbExistMedia.Checked)
            //{
                //this.bk.FormatMedia = false;
                /*
                if (!DatabaseEngineTypeExtension.IsMatrix(this.DataContainer.Server.DatabaseEngineType))
                {
                    param.SetParam("encryptEnabled", "false");
                    if (this.rbAppend.Checked)
                    {
                        //Append to Existing backupSets is selected. Create a t-sql with NOINIT option
                        this.bk.Initialize = false;
                    }
                    if (this.rbOverwrite.Checked)
                    {
                        //Overrite existing backupsets is selected.Create a t-sql with INIT option
                        this.bk.Initialize = true;
                    }
                }*/
                /*
                if (this.checkbCheckMedia.Checked == true)
                {
                    this.bk.SkipTapeHeader = false;
                    this.bk.MediaName = this.txtMediaSetName.Text;
                }*/
                //else
                //{
                    this.backup.SkipTapeHeader = true;
                //}
            //}
            /*if (this.rbNewMediaSet.Checked == true)
            {
                if (!DatabaseEngineTypeExtension.IsMatrix(this.DataContainer.Server.DatabaseEngineType))
                {
                    this.bk.Initialize = true;
                }
                this.bk.SkipTapeHeader = true;
                this.bk.FormatMedia = true;
                this.bk.MediaName = this.txtInitializeMediaName.Text;
                this.bk.MediaDescription = this.txtMediaSetDescr.Text;
                param.SetParam("encryptEnabled", "true");
            }*/
            //if (this.checkbPerformCHK.Checked == true)
            //{
            //    this.bk.Checksum = true;
            //}
            //else
            //{
                //this.bk.Checksum = false;
            //}
            //if (this.checkbContinueCHK.Checked)
            //{
            //    this.bk.ContinueAfterError = true;
            //}
            //else
            //{
            //    this.bk.ContinueAfterError = false;
            //}
            //bool bTransLog = param.GetParam("translog", ref this.transLog);
            //if (true == bTransLog && this.transLog == "yes" && (this.cbRemove.Checked == false && this.checkbTail.Checked == false))
            //{
            //    this.bk.LogTruncation = BackupTruncateLogType.Truncate;
            //}
            //else
            //{
            //    if (this.cbRemove.Checked == true)
            //    {
            //        this.bk.LogTruncation = BackupTruncateLogType.Truncate;
            //    }
            //    else
            //    {
            //        this.bk.LogTruncation = BackupTruncateLogType.NoTruncate;
            //    }
            //    bool bHadr = param.GetParam("hadr", ref this.hadr);
            //    bool bMirrored = param.GetParam("mirrored", ref this.mirrored);
            //    // Set the TLogWithNoRecovery only if database is not mirrored and is not a Hadron enabled
            //    bool TLogWithNoRecovery = ((true == bHadr) && (!this.hadr)) && ((true == bMirrored) && (!this.mirrored));
            //    if (this.checkbTail.Checked == true && TLogWithNoRecovery)
            //    {
            //        this.bk.NoRecovery = true;
            //    }
            //}
            
        }

        /// <summary>
		/// Performs the backup operation
		/// </summary>
		public void PerformBackup(Server server)
        {            
            //this.ExecutionMode = ExecutionMode.Success;
            //this.cancelRequested = false;
            try
            {           
                //STParameters param = null;
                //bool bStatus = false;
                //param = new STParameters();
                //param.SetDocument(DataContainer.Document);
                //bStatus = param.GetParam("backupname", ref this.BackupName);
                //this.backup = (Backup)(DataContainer.HashTable[this.BackupName]);
                //this.GetBkProps();

                /*
                if (-1 != this.propBackupExpireAfter)
                {
                    this.bk.RetainDays = this.propBackupExpireAfter;
                }
                else
                {
                    if (DateTime.MinValue != this.propBackupExpireOn)
                    {
                        this.bk.ExpirationDate = this.propBackupExpireOn;
                    }
                }*/
                
                /*
                this.bk.BackupSetDescription = this.propBackupSetDescription;
                if (DataContainer.Server.VersionMajor >= 10)
                {
                    // set the compression option based upon selected index on comboxbox control
                    this.bk.CompressionOption = this.backupCompressOption;
                }

                string encryptEnabled = string.Empty;
                param.GetParam("encryptEnabled", ref encryptEnabled);
                if (encryptEnabled.Equals("true") && this.checkBoxEncryption.Checked)
                {
                    this.bk.EncryptionOption = new BackupEncryptionOptions(this.backEncryptionControl.BackupEncryptionAlgorithm, this.backEncryptionControl.BackupEncryptorType, this.backEncryptionControl.BackupEncryptorName);
                }
                string verifyRequired = string.Empty;
                string unloadTapeAfter = string.Empty;
                string rewind = string.Empty;
                param.GetParam("verifyRequired", ref verifyRequired);
                param.GetParam("unloadTapeAfter", ref unloadTapeAfter);
                param.GetParam("rewind", ref rewind);
                this.bk.PercentComplete += new PercentCompleteEventHandler(bk_PercentComplete);
                this.bk.NextMedia += new ServerMessageEventHandler(bk_NextMedia);
                */

                // Run backup operation
                this.backup.SqlBackup(server);

                /*
                this.bk.PercentComplete -= new PercentCompleteEventHandler(bk_PercentComplete);
                this.bk.NextMedia -= new ServerMessageEventHandler(bk_NextMedia);
                if (verifyRequired.Equals("true"))
                {
                    if (true == this.cancelRequested)
                    {
                        this.ExecutionMode = ExecutionMode.Cancel;
                        return;
                    }
                    this.ReportUpdate("", 0);
                    this.rst = new Restore();
                    this.rst.PercentComplete += new PercentCompleteEventHandler(rst_PercentComplete);
                    this.rst.Devices.AddRange(this.bk.Devices);
                    this.rst.Database = this.bk.Database;
                    if (BackupRestoreBase.IsBackupUrlDeviceSupported(DataContainer.Server.ServerVersion)
                        && this.ServerConnection.ServerVersion.Major < 13) // BackupToUrl: SQL16 will use SAS credential by default
                    {
                        this.rst.CredentialName = this.bk.CredentialName;
                    }
                    if (!DatabaseEngineTypeExtension.IsMatrix(this.DataContainer.Server.DatabaseEngineType))
                    {
                        this.rst.UnloadTapeAfter = (unloadTapeAfter.Equals("true")) ? true : false;
                        this.rst.NoRewind = (rewind.Equals("true")) ? false : true;
                    }
                    string errorMessage = null;
                    this.rst.SqlVerifyLatest(DataContainer.Server, out errorMessage);
                    if (errorMessage != null)
                    {
                        errorMessage = String.Format(System.Globalization.CultureInfo.CurrentCulture, BackupPropGeneralSR.VerifyBackUpError, errorMessage);
                        ShowMessage(errorMessage, SRError.SQLWorkbench, ExceptionMessageBoxButtons.OK, ExceptionMessageBoxSymbol.Error);
                        this.ExecutionMode = ExecutionMode.Failure;

                    }
                    this.rst.PercentComplete -= new PercentCompleteEventHandler(rst_PercentComplete);
                }*/
            }
            catch //SmoException e
            {
                /*
                ShowMessage(e,
                    ExceptionMessageBoxButtons.OK,
                    ExceptionMessageBoxSymbol.Error);
                if (this.ExecutionMode != ExecutionMode.Cancel)
                {
                    this.ExecutionMode = ExecutionMode.Failure;
                }*/
            }
        }

        /*
        /// <summary>
        /// Display popup 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="executionMode"></param>
        /// <param name="executionType"></param>
        public override void OnTaskCompleted(object sender, ExecutionMode executionMode, RunType executionType)
        {
            base.OnTaskCompleted(sender, executionMode, executionType);
            //we need to disable all UI options if we ran at least once, and only if we did the real run (not scripting)
            if (executionMode == ExecutionMode.Success && !IsScripting(executionType))
            {
                STParameters param = null;
                bool bStatus = false;
                param = new STParameters();
                param.SetDocument(DataContainer.Document);
                bStatus = param.GetParam("backupname", ref this.BackupName);
                Backup bk = (Backup)(DataContainer.HashTable[this.BackupName]);
                this.MessageBoxProvider.ShowMessage(BackupPropGeneralSR.MessageSuccess(bk.Database), SRError.SQLWorkbench, ExceptionMessageBoxButtons.OK, ExceptionMessageBoxSymbol.Information, (this as Control).Parent);
            }
        }*/

    }
}
