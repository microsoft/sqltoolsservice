//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    public class BackupServiceTests
    {
        // Query format to create master key and certificate for backup encryption
        private const string CreateCertificateQueryFormat = @"USE master;
IF NOT EXISTS(SELECT * FROM sys.symmetric_keys WHERE symmetric_key_id = 101)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = '{0}';
IF NOT EXISTS(SELECT * FROM sys.certificates WHERE name = '{1}')
CREATE CERTIFICATE {1} WITH SUBJECT = 'Backup Encryption Certificate'; ";

        // Query format to clean up master key and certificate
        private const string CleanupCertificateQueryFormat = @"USE master; DROP CERTIFICATE {0}; DROP MASTER KEY";

        /// <summary>
        /// Get backup configuration info
        /// </summary>
        /// Test is failing in code coverage runs. Reenable when stable.
        ///[Fact]
        public async void GetBackupConfigInfoTest()
        {
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999); 
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);

            var requestContext = new Mock<RequestContext<BackupConfigInfoResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<BackupConfigInfoResponse>()))
                .Returns(Task.FromResult(new object()));
            
            var dbParams = new DefaultDatabaseInfoParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri
            };

            DisasterRecoveryService service = new DisasterRecoveryService();
            await service.HandleBackupConfigInfoRequest(dbParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.Is<BackupConfigInfoResponse>
                (p => p.BackupConfigInfo.RecoveryModel != string.Empty
                && p.BackupConfigInfo.DefaultBackupFolder != string.Empty)));
            
            testDb.Cleanup();
        }

        /// <summary>
        /// Create simple backup test
        /// </summary>
        //[Fact]
        public void CreateBackupTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);

            string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

            BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                BackupType.Full,
                new List<string>() { backupPath },
                new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
            BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

            // Backup the database
            service.PerformBackup(backupOperation);

            VerifyAndCleanBackup(backupPath);
            testDb.Cleanup();
        }

        //[Fact]
        public void ScriptBackupTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

            BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                BackupType.Full,
                new List<string>() { backupPath },
                new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
            BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

            // Generate script for backup
            service.ScriptBackup(backupOperation);
            string script = backupOperation.ScriptContent;
            Assert.True(!string.IsNullOrEmpty(script));

            // Execute the script
            testDb.RunQuery(script);

            VerifyAndCleanBackup(backupPath);
            testDb.Cleanup();
        }


        /// <summary>
        /// Test creating backup with advanced options set.
        /// </summary>
        //[Fact]
        public void CreateBackupWithAdvancedOptionsTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

            string certificateName = CreateCertificate(testDb);
            string cleanupCertificateQuery = string.Format(CleanupCertificateQueryFormat, certificateName);

            BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName, 
                BackupType.Full, 
                new List<string>(){ backupPath }, 
                new Dictionary<string, int>(){{ backupPath, (int)DeviceType.File }});
            backupInfo.ContinueAfterError = true;
            backupInfo.FormatMedia = true;
            backupInfo.SkipTapeHeader = true;
            backupInfo.Initialize = true;
            backupInfo.MediaName = "backup test media";
            backupInfo.MediaDescription = "backup test";
            backupInfo.RetainDays = 90;
            backupInfo.CompressionOption = (int)BackupCompressionOptions.On;
            backupInfo.EncryptionAlgorithm = (int)BackupEncryptionAlgorithm.Aes128;
            backupInfo.EncryptorType = (int)BackupEncryptorType.ServerCertificate;
            backupInfo.EncryptorName = certificateName;

            BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);
            
            // Backup the database
            Console.WriteLine("Perform backup operation..");
            service.PerformBackup(backupOperation);

            // Remove the backup file
            Console.WriteLine("Verify the backup file exists and remove..");
            VerifyAndCleanBackup(backupPath);

            // Delete certificate and master key
            Console.WriteLine("Remove certificate and master key..");
            testDb.RunQuery(cleanupCertificateQuery);
            
            // Clean up the database
            Console.WriteLine("Clean up database..");
            testDb.Cleanup();
        }

        /// <summary>
        /// Test creating backup with advanced options set.
        /// </summary>
        //[Fact]
        public void ScriptBackupWithAdvancedOptionsTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

            string certificateName = CreateCertificate(testDb);
            string cleanupCertificateQuery = string.Format(CleanupCertificateQueryFormat, certificateName);

            BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                BackupType.Full,
                new List<string>() { backupPath },
                new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
            backupInfo.FormatMedia = true;
            backupInfo.SkipTapeHeader = true;
            backupInfo.Initialize = true;
            backupInfo.MediaName = "backup test media";
            backupInfo.MediaDescription = "backup test";
            backupInfo.EncryptionAlgorithm = (int)BackupEncryptionAlgorithm.Aes128;
            backupInfo.EncryptorType = (int)BackupEncryptorType.ServerCertificate;
            backupInfo.EncryptorName = certificateName;

            BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

            // Backup the database
            Console.WriteLine("Generate script for backup operation..");
            service.ScriptBackup(backupOperation);
            string script = backupOperation.ScriptContent;

            // Run the script
            Console.WriteLine("Execute the script..");
            testDb.RunQuery(script);

            // Remove the backup file
            Console.WriteLine("Verify the backup file exists and remove..");
            VerifyAndCleanBackup(backupPath);

            // Delete certificate and master key
            Console.WriteLine("Remove certificate and master key..");
            testDb.RunQuery(cleanupCertificateQuery);

            // Clean up the database
            Console.WriteLine("Clean up database..");
            testDb.Cleanup();
        }

        //[Fact]
        public async void BackupFileBrowserTest()
        {
            string databaseName = "testfilebrowser_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);

            // Initialize backup service
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);
            DisasterRecoveryService disasterRecoveryService = new DisasterRecoveryService();
            BackupConfigInfo backupConfigInfo = disasterRecoveryService.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);

            // Create backup file
            string backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + ".bak");
            string query = $"BACKUP DATABASE [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
            await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);

            FileBrowserService service = new FileBrowserService();

            string[] backupFilters = new string[2] { "*.bak", "*.trn" };
            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = backupConfigInfo.DefaultBackupFolder,
                FileFilters = backupFilters
            };

            var openBrowserEventFlowValidator = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserOpenedNotification.Type, eventParams =>
                {
                    Assert.True(eventParams.Succeeded);
                    Assert.NotNull(eventParams.FileTree);
                    Assert.NotNull(eventParams.FileTree.RootNode);
                    Assert.NotNull(eventParams.FileTree.RootNode.Children);
                    Assert.True(eventParams.FileTree.RootNode.Children.Count > 0);
                    Assert.True(ContainsFileInTheFolder(eventParams.FileTree.SelectedNode, backupPath));
                })
                .Complete();

            await service.RunFileBrowserOpenTask(openParams, openBrowserEventFlowValidator.Object);

            // Verify complete notification event was fired and the result
            openBrowserEventFlowValidator.Validate();

            var expandParams = new FileBrowserExpandParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = backupConfigInfo.DefaultBackupFolder
            };


            var expandEventFlowValidator = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserExpandedNotification.Type, eventParams =>
                {
                    Assert.True(eventParams.Succeeded);
                    Assert.NotNull(eventParams.Children);
                    Assert.True(eventParams.Children.Length > 0);
                })
                .Complete();
            
            // Expand the node in file browser
            await service.RunFileBrowserExpandTask(expandParams, expandEventFlowValidator.Object);

            // Verify result
            expandEventFlowValidator.Validate();

            var validateParams = new FileBrowserValidateParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ServiceType = FileValidationServiceConstants.Backup,
                SelectedFiles = new[] { backupPath }
            };

            var validateEventFlowValidator = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserValidatedNotification.Type, eventParams => Assert.True(eventParams.Succeeded))
                .Complete();
            
            // Validate selected files in the browser
            await service.RunFileBrowserValidateTask(validateParams, validateEventFlowValidator.Object);

            // Verify complete notification event was fired and the result
            validateEventFlowValidator.Validate();

            // Remove the backup file
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        #region private methods

        private string CreateCertificate(SqlTestDb testDb)
        {
            string certificateName = "backupcertificate" + new Random().Next(10000000, 99999999);
            string masterkeyPassword = Guid.NewGuid().ToString();
            string createCertificateQuery = string.Format(CreateCertificateQueryFormat, masterkeyPassword, certificateName);
            
            // create master key and certificate
            Console.WriteLine("Create master key and certificate..");
            testDb.RunQuery(createCertificateQuery);

            return certificateName;
        }

        private BackupInfo CreateDefaultBackupInfo(string databaseName, BackupType backupType, List<string> backupPathList, Dictionary<string, int> backupPathDevices)
        {
            BackupInfo backupInfo = new BackupInfo();
            backupInfo.BackupComponent = (int)BackupComponent.Database;
            backupInfo.BackupDeviceType = (int)BackupDeviceType.Disk;
            backupInfo.BackupPathDevices = backupPathDevices;
            backupInfo.BackupPathList = backupPathList;
            backupInfo.BackupsetName = "default_backup";
            backupInfo.BackupType = (int)backupType;
            backupInfo.DatabaseName = databaseName;
            backupInfo.SelectedFileGroup = null;
            backupInfo.SelectedFiles = "";
            return backupInfo;
        }

        private string GetDefaultBackupFullPath(DisasterRecoveryService service, string databaseName, CDataContainer dataContainer, SqlConnection sqlConn)
        {
            BackupConfigInfo backupConfigInfo = service.GetBackupConfigInfo(dataContainer, sqlConn, sqlConn.Database);
            return Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + ".bak");
        }

        private BackupOperation CreateBackupOperation(DisasterRecoveryService service, string uri, BackupInfo backupInfo, CDataContainer dataContainer, SqlConnection sqlConn)
        {
            var backupParams = new BackupParams
            {
                OwnerUri = uri,
                BackupInfo = backupInfo,
            };

            return service.CreateBackupOperation(dataContainer, sqlConn, backupParams.BackupInfo);
        }

        private void VerifyAndCleanBackup(string backupPath)
        {
            // Verify it created backup
            Assert.True(File.Exists(backupPath));

            // Remove the backup file
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        private bool ContainsFileInTheFolder(FileTreeNode folderNode, string filePath)
        {
            foreach (FileTreeNode node in folderNode.Children)
            {
                if (node.FullPath == filePath)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}