//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
using NUnit.Framework;

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
        ///[Test]
        public async Task GetBackupConfigInfoTest()
        {
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {

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
            }
        }

        /// <summary>
        /// Create simple backup test
        /// </summary>
        [Test]
        public void CreateBackupTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "SqlToolsService_TestBackup_" + new Random().Next(10000000, 99999999);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true))
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

                    BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                        BackupType.Full,
                        new List<string>() { backupPath },
                        new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
                    BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfo, helper.DataContainer, sqlConn);

                    // Backup the database
                    service.PerformBackup(backupOperation);

                    VerifyAndCleanBackup(sqlConn, backupPath);
                }
            }
        }

        [Test]
        public void ScriptBackupTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "SqlToolsService_TestBackup_" + new Random().Next(10000000, 99999999);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true))
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
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

                    VerifyAndCleanBackup(sqlConn, backupPath);
                }
            }
        }

        /// <summary>
        /// Test creating backup with advanced options set.
        /// </summary>
        [Test]
        public void CreateBackupWithAdvancedOptionsTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "SqlToolsService_TestBackup_" + new Random().Next(10000000, 99999999);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true))
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
                    string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

                    string certificateName = CreateCertificate(testDb);
                    string cleanupCertificateQuery = string.Format(CleanupCertificateQueryFormat, certificateName);

                    BackupInfo backupInfo = CreateDefaultBackupInfo(databaseName,
                        BackupType.Full,
                        new List<string>() { backupPath },
                        new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
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
                    VerifyAndCleanBackup(sqlConn, backupPath);

                    // Delete certificate and master key
                    Console.WriteLine("Remove certificate and master key..");
                    testDb.RunQuery(cleanupCertificateQuery);
                }
            }
        }

        /// <summary>
        /// Test creating backup with advanced options set.
        /// </summary>
        [Test]
        public void ScriptBackupWithAdvancedOptionsTest()
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            string databaseName = "SqlToolsService_TestBackup_" + new Random().Next(10000000, 99999999);

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true))
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo))
                {
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
                    VerifyAndCleanBackup(sqlConn, backupPath);

                    // Delete certificate and master key
                    Console.WriteLine("Remove certificate and master key..");
                    testDb.RunQuery(cleanupCertificateQuery);
                }
            }
        }

        /// <summary>
        /// Test the correct script generation for different backup action types
        /// </summary>
        [Test]
        public void ScriptBackupWithDifferentActionTypesTest()
        {
            string databaseName = "SqlToolsService_TestBackup_" + new Random().Next(10000000, 99999999);
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                // Create Full backup script
                string script = GenerateScriptForBackupType(BackupType.Full, databaseName);

                // Validate Full backup script 
                Assert.That(script, Does.Contain("BACKUP DATABASE").IgnoreCase);
                Assert.That(script, Does.Not.Contain("BACKUP LOG").IgnoreCase);
                Assert.That(script, Does.Not.Contain("DIFFERENTIAL").IgnoreCase);

                // Create log backup script
                script = GenerateScriptForBackupType(BackupType.TransactionLog, databaseName);

                // Validate Log backup script 
                Assert.That(script, Does.Contain("BACKUP LOG").IgnoreCase);
                Assert.That(script, Does.Not.Contain("BACKUP DATABASE").IgnoreCase);
                Assert.That(script, Does.Not.Contain("DIFFERENTIAL").IgnoreCase);

                // Create differential backup script
                script = GenerateScriptForBackupType(BackupType.Differential, databaseName);

                // Validate differential backup script 
                Assert.That(script, Does.Contain("BACKUP DATABASE").IgnoreCase);
                Assert.That(script, Does.Not.Contain("BACKUP LOG").IgnoreCase);
                Assert.That(script, Does.Contain("WITH  DIFFERENTIAL").IgnoreCase);
            }
        }

        //[Test]
        public async Task BackupFileBrowserTest()
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

        private void VerifyAndCleanBackup(SqlConnection sqlConn, string backupPath)
        {
            try
            {
                sqlConn.Open();
                using (SqlCommand sqlCmd = sqlConn.CreateCommand())
                {
                    sqlCmd.CommandText = @"
DECLARE @Files TABLE 
 (fileName nvarchar(max)
  ,depth int
  ,isFile int)

INSERT INTO @Files 
EXEC xp_dirtree @Path,1,1

SELECT CASE WHEN COUNT(*) > 0 THEN 'true' ELSE 'false' END FROM @Files where isFile=1 and fileName=@FileName";
                    sqlCmd.Parameters.AddWithValue("@Path", Path.GetDirectoryName(backupPath));
                    sqlCmd.Parameters.AddWithValue("@FileName", Path.GetFileName(backupPath));
                    var ret = bool.Parse(sqlCmd.ExecuteScalar().ToString());

                    // Verify it created backup
                    Assert.True(ret, $"Backup file {backupPath} was not created");
                }
            }
            finally
            {
                using (SqlCommand sqlCmd = sqlConn.CreateCommand())
                {
                    sqlCmd.CommandText = "EXECUTE master.dbo.xp_delete_file 0,@Path";
                    sqlCmd.Parameters.AddWithValue("@Path", backupPath);
                    sqlCmd.ExecuteNonQuery();
                }
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

        private string GenerateScriptForBackupType(BackupType backupType, string databaseName)
        {
            DisasterRecoveryService service = new DisasterRecoveryService();
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = ConnectionService.OpenSqlConnection(liveConnection.ConnectionInfo);
            string backupPath = GetDefaultBackupFullPath(service, databaseName, helper.DataContainer, sqlConn);

            BackupInfo backupInfoLog = CreateDefaultBackupInfo(databaseName,
                backupType,
                new List<string>() { backupPath },
                new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });
            backupInfoLog.FormatMedia = true;
            backupInfoLog.SkipTapeHeader = true;
            backupInfoLog.Initialize = true;
            backupInfoLog.MediaName = "backup test media";
            backupInfoLog.MediaDescription = "backup test";
            BackupOperation backupOperation = CreateBackupOperation(service, liveConnection.ConnectionInfo.OwnerUri, backupInfoLog, helper.DataContainer, sqlConn);

            // Generate Script
            Console.WriteLine("Generate script for backup operation..");
            service.ScriptBackup(backupOperation);
            string script = backupOperation.ScriptContent;

            // There shouldnt be any backup file created
            Assert.True(!File.Exists(backupPath), "Backup file is not expected to be created");

            sqlConn.Close();
            return script;
        }
        #endregion
    }
}