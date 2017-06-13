//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Data.Tools.DataSets;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class BackupUtilities
    {
        private CDataContainer dataContainer;
        private ServerConnection serverConnection;
        private CommonUtilities backupRestoreUtil = null;
        private Backup bk = null;

        /// <summary>
        /// Constants
        /// </summary>
        private const int constDeviceTypeFile = 2;
        private const int constDeviceTypeTape = 5;
        private const int constDeviceTypeMediaSet = 3;

        /// <summary>
        /// UI input values
        /// </summary>
        private BackupInfo backupInfo;
        public BackupComponent backupComponent { get; set; }
        public BackupType backupType { get; set; } // 0 for Full, 1 for Differential, 2 for Log
        public BackupDeviceType backupDeviceType { get; set; }
        
        private BackupActionType backupActionType = BackupActionType.Database;
        private bool IsBackupIncremental = false;
        private bool isLocalPrimaryReplica;

        /// this is used when the backup dialog is launched in the context of a backup device
        /// The InitialBackupDestination will be loaded in LoadData
        private string initialBackupDestination = string.Empty;
        
        // Helps in populating the properties of an Azure blob given its URI
        private class BlobProperties
        {
            private string containerName;

            public string ContainerName
            {
                get { return this.containerName; }
            }

            private string fileName;

            public string FileName
            {
                get { return this.fileName; }
            }

            public BlobProperties(Uri blobUri)
            {
                // Extracts the container name and the filename from URI of the strict format https://<StorageAccount_Path>/<ContainerName>/<FileName>
                // The input URI should be well formed (Current used context - URI is read from msdb - well formed)

                this.containerName = string.Empty;
                this.fileName = string.Empty;
                if (blobUri == null)
                {
                    return;
                }
                string[] seg = blobUri.Segments;
                if (seg.Length >= 2)
                {
                    this.containerName = seg[1].Replace("/", "");
                }
                if (seg.Length >= 3)
                {
                    this.fileName = seg[2].Replace("/", "");
                }
            }
        };
        
        #region ctors
        
        /// <summary>
        /// Ctor
        /// </summary>
        public BackupUtilities()
        {               
        }

        /// <summary>
        /// Initialize variables
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="sqlConnection"></param>
        /// <param name="input"></param>
        public void Initialize(CDataContainer dataContainer, SqlConnection sqlConnection)
        {
            this.dataContainer = dataContainer;
            this.serverConnection = new ServerConnection(sqlConnection);
            this.backupRestoreUtil = new CommonUtilities(this.dataContainer, this.serverConnection);            
        }

        public void SetBackupInput(BackupInfo input)
        {
            this.backupInfo = input;

            // convert the types            
            this.backupComponent = (BackupComponent)input.BackupComponent;
            this.backupType = (BackupType)input.BackupType;
            this.backupDeviceType = (BackupDeviceType)input.BackupDeviceType;

            if (this.backupRestoreUtil.IsHADRDatabase(this.backupInfo.DatabaseName))
            {
                this.isLocalPrimaryReplica = this.backupRestoreUtil.IsLocalPrimaryReplica(this.backupInfo.DatabaseName);
            }
        }
        
        #endregion

        #region Methods for UI logic
                
        public BackupConfigInfo GetBackupConfigInfo(string databaseName)
        {
            BackupConfigInfo databaseInfo = new BackupConfigInfo();
            databaseInfo.RecoveryModel = this.GetRecoveryModel(databaseName);
            databaseInfo.DefaultBackupFolder = this.GetDefaultBackupFolder();
            databaseInfo.LatestBackups = this.GetLatestBackupLocations(databaseName);
            return databaseInfo;
        }

        /// <summary>
        /// Return recovery model of the database
        /// </summary>
        /// <returns></returns>
        public string GetRecoveryModel(string databaseName)
        {
            RecoveryModel recoveryModel = this.backupRestoreUtil.GetRecoveryModel(databaseName);
            return recoveryModel.ToString();
        }

        public string GetDefaultBackupFolder()
        {
            return this.backupRestoreUtil.GetDefaultBackupFolder();
        }

        /// <summary>
        /// Return the latest backup locations
        /// </summary>
        /// <returns></returns>
        public List<RestoreItemSource> GetLatestBackupLocations(string databaseName)
        {
            return this.backupRestoreUtil.GetLatestBackupLocations(databaseName);
        }

        /// <summary>
        /// Return true if backup to URL is supported in the current SQL Server version
        /// </summary>
        private bool BackupToUrlSupported()
        {
            return BackupRestoreBase.IsBackupUrlDeviceSupported(this.dataContainer.Server.PingSqlServerVersion(this.dataContainer.ServerName)); //@@ originally, DataContainer.Server.ServerVersion
        }

        #endregion

        private string GetDefaultBackupSetName()
        {
            string bkpsetName = this.backupInfo.DatabaseName + "-" 
                + this.backupType.ToString() + " " 
                + this.backupComponent.ToString() + " " 
                + BackupConstants.Backup;
            return bkpsetName;            
        }

        private void SetBackupProps()
        {
            try
            {
                switch (this.backupType)
                {
                    case BackupType.Full:
                        if (this.backupComponent == BackupComponent.Database) // define the value as const!!
                        {
                            this.backupActionType = BackupActionType.Database;
                        }
                        else if ((this.backupComponent == BackupComponent.Files) && (null != this.backupInfo.SelectedFileGroup) && (this.backupInfo.SelectedFileGroup.Count > 0))
                        {
                            this.backupActionType = BackupActionType.Files;
                        }
                        this.IsBackupIncremental = false;
                        break;
                    case BackupType.Differential:
                        if ((this.backupComponent == BackupComponent.Files) && (0 != this.backupInfo.SelectedFiles.Length))
                        {
                            this.backupActionType = BackupActionType.Files;
                            this.IsBackupIncremental = true;
                        }
                        else
                        {
                            this.backupActionType = BackupActionType.Database;
                            this.IsBackupIncremental = true;
                        }
                        break;
                    case BackupType.TransactionLog:
                        this.backupActionType = BackupActionType.Log;
                        this.IsBackupIncremental = false;
                        break;
                    default:
                        break;
                        //throw new Exception("Unexpected error");
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sets the backup properties from the general tab
        /// </summary>
        public void PerformBackup()
        {
            this.bk = new Backup();
            this.SetBackupProps();
            bk.Database = this.backupInfo.DatabaseName;
            bk.Action = this.backupActionType;
            bk.Incremental = this.IsBackupIncremental;
            if (bk.Action == BackupActionType.Files)
            {
                IDictionaryEnumerator IEnum = this.backupInfo.SelectedFileGroup.GetEnumerator();
                IEnum.Reset();
                while (IEnum.MoveNext())
                {
                    string CurrentKey = Convert.ToString(IEnum.Key,
                        System.Globalization.CultureInfo.InvariantCulture);
                    string CurrentValue = Convert.ToString(IEnum.Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                    if (CurrentKey.IndexOf(",", StringComparison.Ordinal) < 0)
                    {
                        // is a file group
                        bk.DatabaseFileGroups.Add(CurrentValue);
                    }
                    else
                    {
                        // is a file
                        int Idx = CurrentValue.IndexOf(".", StringComparison.Ordinal);
                        CurrentValue = CurrentValue.Substring(Idx + 1, CurrentValue.Length - Idx - 1);
                        bk.DatabaseFiles.Add(CurrentValue);
                    }
                }
            }

            bool bBackupToUrl = false;
            if (this.backupDeviceType == BackupDeviceType.Url)
            {
                bBackupToUrl = true;
            }

            bk.BackupSetName = this.backupInfo.BackupsetName;

            if (false == bBackupToUrl)
            {
                for (int i = 0; i < this.backupInfo.BackupPathList.Count; i++)
                {
                    string DestName = Convert.ToString(this.backupInfo.BackupPathList[i], System.Globalization.CultureInfo.InvariantCulture);
                    int deviceType = (int)(this.backupInfo.BackupPathDevices[DestName]);
                    switch (deviceType)
                    {
                        case (int)DeviceType.LogicalDevice:
                            int backupDeviceType =
                                GetDeviceType(Convert.ToString(DestName,
                                    System.Globalization.CultureInfo.InvariantCulture));

                            if ((this.backupDeviceType == BackupDeviceType.Disk && backupDeviceType == constDeviceTypeFile)
                                || (this.backupDeviceType == BackupDeviceType.Tape && backupDeviceType == constDeviceTypeTape))
                            {
                                bk.Devices.AddDevice(DestName, DeviceType.LogicalDevice);
                            }
                            break;
                        case (int)DeviceType.File:
                            if (this.backupDeviceType == BackupDeviceType.Disk)
                            {
                                bk.Devices.AddDevice(DestName, DeviceType.File);
                            }
                            break;
                        case (int)DeviceType.Tape:
                            if (this.backupDeviceType == BackupDeviceType.Tape)
                            {
                                bk.Devices.AddDevice(DestName, DeviceType.Tape);
                            }
                            break;
                    }
                }
            }
                
            //TODO: This should be changed to get user inputs
            bk.FormatMedia = false;
            bk.Initialize = false;
            bk.SkipTapeHeader = true;
            bk.Checksum = false;
            bk.ContinueAfterError = false;
            bk.LogTruncation = BackupTruncateLogType.Truncate;

            // Execute backup
            bk.SqlBackup(this.dataContainer.Server);
        }
     
        public void CancelBackup()
        {
            if (this.bk != null)
            {
                this.bk.Abort();
            }
        }

        private int GetDeviceType(string deviceName)
        {
            Enumerator en = new Enumerator();
            Request req = new Request();
            DataSet ds = new DataSet();
            ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
            int Result = -1;
            SqlExecutionModes execMode = this.serverConnection.SqlExecutionModes;
            this.serverConnection.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            try
            {
                req.Urn = "Server/BackupDevice[@Name='" + Urn.EscapeString(deviceName) + "']";
                req.Fields = new string[1];
                req.Fields[0] = "BackupDeviceType";
                ds = en.Process(this.serverConnection, req);
                int iCount = ds.Tables[0].Rows.Count;
                if (iCount > 0)
                {
                    Result = Convert.ToInt16(ds.Tables[0].Rows[0]["BackupDeviceType"],
                        System.Globalization.CultureInfo.InvariantCulture);
                    return Result;
                }
                else
                {
                    return constDeviceTypeMediaSet;
                }
            }
            catch
            {                
            }
            finally
            {
                this.serverConnection.SqlExecutionModes = execMode;
            }
            return Result;
        }
    }
}