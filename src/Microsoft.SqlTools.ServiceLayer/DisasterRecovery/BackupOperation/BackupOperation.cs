//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// This class implements backup operations
    /// </summary>
    public class BackupOperation :  SmoScriptableTaskOperation, IBackupOperation
    {
        private CDataContainer dataContainer;
        private ServerConnection serverConnection;
        private CommonUtilities backupRestoreUtil = null;
        private Backup backup = null;

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
        private BackupComponent backupComponent;
        private BackupType backupType;
        private BackupDeviceType backupDeviceType;
        
        private BackupActionType backupActionType = BackupActionType.Database;
        private bool isBackupIncremental = false;
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
        public BackupOperation()
        {               
        }

        #endregion

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

        /// <summary>
        /// Set backup input properties
        /// </summary>
        /// <param name="input"></param>
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

        /// <summary>
        /// Return backup configuration data
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public BackupConfigInfo CreateBackupConfigInfo(string databaseName)
        {
            BackupConfigInfo configInfo = new BackupConfigInfo();
            configInfo.RecoveryModel = GetRecoveryModel(databaseName);
            configInfo.DefaultBackupFolder = CommonUtilities.GetDefaultBackupFolder(this.serverConnection);
            configInfo.BackupEncryptors = GetBackupEncryptors();
            return configInfo;
        }

        /// <summary>
        /// The error occurred during backup operation
        /// </summary>
        public override string ErrorMessage
        {
            get
            {
                return string.Empty;
            }
        }

        public override Server Server
        {
            get
            {
                return this.dataContainer.Server;
            }
        }

        /// <summary>
        /// Execute backup
        /// </summary>
        public override void Execute()
        {
            // set the operation properties before using them to create backup obejct
            this.SetBackupProps();
            this.backup = new Backup();
            this.backup.Database = this.backupInfo.DatabaseName;
            this.backup.Action = this.backupActionType;
            this.backup.Incremental = this.isBackupIncremental;

            try
            {
                if (this.backup.Action == BackupActionType.Files)
                {
                    IDictionaryEnumerator filegroupEnumerator = this.backupInfo.SelectedFileGroup.GetEnumerator();
                    filegroupEnumerator.Reset();
                    while (filegroupEnumerator.MoveNext())
                    {
                        string currentKey = Convert.ToString(filegroupEnumerator.Key,
                            System.Globalization.CultureInfo.InvariantCulture);
                        string currentValue = Convert.ToString(filegroupEnumerator.Value,
                            System.Globalization.CultureInfo.InvariantCulture);
                        if (currentKey.IndexOf(",", StringComparison.Ordinal) < 0)
                        {
                            // is a file group
                            this.backup.DatabaseFileGroups.Add(currentValue);
                        }
                        else
                        {
                            // is a file
                            int idx = currentValue.IndexOf(".", StringComparison.Ordinal);
                            currentValue = currentValue.Substring(idx + 1, currentValue.Length - idx - 1);
                            this.backup.DatabaseFiles.Add(currentValue);
                        }
                    }
                }

                this.backup.BackupSetName = this.backupInfo.BackupsetName;

                for (int i = 0; i < this.backupInfo.BackupPathList.Count; i++)
                {
                    string destName = Convert.ToString(this.backupInfo.BackupPathList[i], System.Globalization.CultureInfo.InvariantCulture);
                    int deviceType = (int)(this.backupInfo.BackupPathDevices[destName]);
                    switch (deviceType)
                    {
                        case (int)DeviceType.LogicalDevice:
                            int backupDeviceType =
                                GetDeviceType(Convert.ToString(destName,
                                    System.Globalization.CultureInfo.InvariantCulture));

                            if (this.backupDeviceType == BackupDeviceType.Disk && backupDeviceType == constDeviceTypeFile)
                            {
                                this.backup.Devices.AddDevice(destName, DeviceType.LogicalDevice);
                            }
                            break;
                        case (int)DeviceType.File:
                            if (this.backupDeviceType == BackupDeviceType.Disk)
                            {
                                this.backup.Devices.AddDevice(destName, DeviceType.File);
                            }
                            break;
                    }
                }

                this.backup.CopyOnly = this.backupInfo.IsCopyOnly;
                this.backup.FormatMedia = this.backupInfo.FormatMedia;
                this.backup.Initialize = this.backupInfo.Initialize;
                this.backup.SkipTapeHeader = this.backupInfo.SkipTapeHeader;
                this.backup.Checksum = this.backupInfo.Checksum;
                this.backup.ContinueAfterError = this.backupInfo.ContinueAfterError;

                if (!string.IsNullOrEmpty(this.backupInfo.MediaName))
                {
                    this.backup.MediaName = this.backupInfo.MediaName;
                }

                if (!string.IsNullOrEmpty(this.backupInfo.MediaDescription))
                {
                    this.backup.MediaDescription = this.backupInfo.MediaDescription;
                }

                if (this.backupInfo.TailLogBackup
                    && !this.backupRestoreUtil.IsHADRDatabase(this.backupInfo.DatabaseName)
                    && !this.backupRestoreUtil.IsMirroringEnabled(this.backupInfo.DatabaseName))
                {
                    this.backup.NoRecovery = true;
                }

                if (this.backupInfo.LogTruncation)
                {
                    this.backup.LogTruncation = BackupTruncateLogType.Truncate;
                }
                else
                {
                    this.backup.LogTruncation = BackupTruncateLogType.NoTruncate;
                }

                if (!string.IsNullOrEmpty(this.backupInfo.BackupSetDescription))
                {
                    this.backup.BackupSetDescription = this.backupInfo.BackupSetDescription;
                }

                if (this.backupInfo.RetainDays >= 0)
                {
                    this.backup.RetainDays = this.backupInfo.RetainDays;
                }
                else
                {
                    this.backup.ExpirationDate = this.backupInfo.ExpirationDate;
                }

                this.backup.CompressionOption = (BackupCompressionOptions)this.backupInfo.CompressionOption;

                if (!string.IsNullOrEmpty(this.backupInfo.EncryptorName))
                {
                    this.backup.EncryptionOption = new BackupEncryptionOptions((BackupEncryptionAlgorithm)this.backupInfo.EncryptionAlgorithm,
                        (BackupEncryptorType)this.backupInfo.EncryptorType,
                        this.backupInfo.EncryptorName);
                }

                if (this.dataContainer.Server.ConnectionContext != null)
                {
                    // Execute backup
                    this.backup.SqlBackup(this.dataContainer.Server);

                    // Verify backup if required
                    if (this.backupInfo.VerifyBackupRequired)
                    {
                        Restore restore = new Restore();
                        restore.Devices.AddRange(this.backup.Devices);
                        restore.Database = this.backup.Database;

                        string errorMessage = null;
                        restore.SqlVerifyLatest(this.dataContainer.Server, out errorMessage);
                        if (errorMessage != null)
                        {
                            throw new DisasterRecoveryException(errorMessage);
                        }
                    }
                }
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                if (this.serverConnection != null)
                {
                    this.serverConnection.Disconnect();
                }
                if(this.dataContainer != null)
                {
                    this.dataContainer.Dispose();
                }
            }
        }

        /// <summary>
        /// Cancel backup
        /// </summary>
        public override void Cancel()
        {
            if (this.backup != null)
            {
                this.backup.Abort();
            }
        }

        #region Methods for UI logic
        
        /// <summary>
        /// Return recovery model of the database
        /// </summary>
        /// <returns></returns>
        public string GetRecoveryModel(string databaseName)
        {
            RecoveryModel recoveryModel = this.backupRestoreUtil.GetRecoveryModel(databaseName);
            return recoveryModel.ToString();
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
        /// Returns the certificates and asymmetric keys from master for encryption
        /// </summary>
        public List<BackupEncryptor> GetBackupEncryptors()
        {
            List<BackupEncryptor> encryptors = new List<BackupEncryptor>();
            if (this.dataContainer.Server.Databases.Contains("master"))
            {
                CertificateCollection certificates = this.dataContainer.Server.Databases["master"].Certificates;
                DateTime currentUtcDateTime = DateTime.UtcNow;
                foreach (Certificate item in certificates)
                {
                    if ((item.Name.StartsWith("##", StringComparison.InvariantCulture) && item.Name.EndsWith("##", StringComparison.InvariantCulture)) ||
                        DateTime.Compare(item.ExpirationDate, currentUtcDateTime) < 0)
                    {
                        continue;
                    }
                    encryptors.Add(new BackupEncryptor((int)BackupEncryptorType.ServerCertificate, item.Name));
                }
                AsymmetricKeyCollection keys = this.dataContainer.Server.Databases["master"].AsymmetricKeys;
                foreach (AsymmetricKey item in keys)
                {
                    if (item.KeyEncryptionAlgorithm == AsymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined)
                    {
                        encryptors.Add(new BackupEncryptor((int)BackupEncryptorType.ServerAsymmetricKey, item.Name));
                    }
                }
            }
            return encryptors;
        }

        #endregion

        private void SetBackupProps()
        {
            switch (this.backupType)
            {
                case BackupType.Full:
                    if (this.backupComponent == BackupComponent.Database)
                    {
                        this.backupActionType = BackupActionType.Database;
                    }
                    else if ((this.backupComponent == BackupComponent.Files) && (this.backupInfo.SelectedFileGroup != null) && (this.backupInfo.SelectedFileGroup.Count > 0))
                    {
                        this.backupActionType = BackupActionType.Files;
                    }
                    this.isBackupIncremental = false;
                    break;
                case BackupType.Differential:
                    if ((this.backupComponent == BackupComponent.Files) && (this.backupInfo.SelectedFiles != null) && (this.backupInfo.SelectedFiles.Length > 0))
                    {
                        this.backupActionType = BackupActionType.Files;
                        this.isBackupIncremental = true;
                    }
                    else
                    {
                        this.backupActionType = BackupActionType.Database;
                        this.isBackupIncremental = true;
                    }
                    break;
                case BackupType.TransactionLog:
                    this.backupActionType = BackupActionType.Log;
                    this.isBackupIncremental = false;
                    break;
                default:
                    break;
            }
        }
        
        private int GetDeviceType(string deviceName)
        {
            Enumerator enumerator = new Enumerator();
            Request request = new Request();
            DataSet dataset = new DataSet();
            dataset.Locale = System.Globalization.CultureInfo.InvariantCulture;
            int result = -1;
            SqlExecutionModes executionMode = this.serverConnection.SqlExecutionModes;
            this.serverConnection.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            try
            {
                request.Urn = "Server/BackupDevice[@Name='" + Urn.EscapeString(deviceName) + "']";
                request.Fields = new string[1];
                request.Fields[0] = "BackupDeviceType";
                dataset = enumerator.Process(this.serverConnection, request);
                if (dataset.Tables[0].Rows.Count > 0)
                {
                    result = Convert.ToInt16(dataset.Tables[0].Rows[0]["BackupDeviceType"],
                        System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    result = constDeviceTypeMediaSet;
                }
            }
            finally
            {
                this.serverConnection.SqlExecutionModes = executionMode;
            }

            return result;
        }
    }
}