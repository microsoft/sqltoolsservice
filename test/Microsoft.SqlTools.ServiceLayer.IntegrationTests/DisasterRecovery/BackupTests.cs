using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    public class BackupTests
    {
        // Query format to create master key and certificate for backup encryption
        private const string CreateCertificateQueryFormat = @"USE master;
IF NOT EXISTS(SELECT * FROM sys.symmetric_keys WHERE symmetric_key_id = 101)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'Yukon900';
IF NOT EXISTS(SELECT * FROM sys.certificates WHERE name = '{0}')
CREATE CERTIFICATE {0} WITH SUBJECT = 'Backup Encryption Certificate'; ";

        // Query format to clean up master key and certificate
        private const string CleanupCertificateQueryFormat = @"DROP CERTIFICATE {0}; DROP MASTER KEY";

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

            await DisasterRecoveryService.HandleBackupConfigInfoRequest(dbParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.Is<BackupConfigInfoResponse>
                (p => p.BackupConfigInfo.RecoveryModel != string.Empty
                && p.BackupConfigInfo.DefaultBackupFolder != string.Empty
                && p.BackupConfigInfo.DatabaseInfo != null)));
            
            testDb.Cleanup();
        }

        /// Test is failing in code coverage runs. Reenable when stable.
        ///[Fact]
        public void CreateBackupTest()
        {
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            
            // Initialize backup service
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = DisasterRecoveryService.GetSqlConnection(liveConnection.ConnectionInfo);
            
            // Get default backup path
            BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
            string backupPath = backupConfigInfo.DefaultBackupFolder + "\\" + databaseName + ".bak";

            BackupInfo backupInfo = createBackupInfo(databaseName,
                BackupType.Full,
                new List<string>(new string[] { backupPath }),
                new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } });

            var backupParams = new BackupParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                BackupInfo = backupInfo
            };

            // Backup the database
            BackupOperation backupOperation = DisasterRecoveryService.Instance.SetBackupInput(helper.DataContainer, sqlConn, backupParams.BackupInfo);
            DisasterRecoveryService.Instance.PerformBackup(backupOperation);
            
            // Remove the backup file
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            // Clean up the database
            testDb.Cleanup();
        }

        /// <summary>
        /// Test creating backup with advanced options set.
        /// </summary>
        [Fact]
        public void CreateBackupWithAdvancedOptionsTest()
        {
            string databaseName = "testbackup_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);
            string certificateName = "backupcertificate" + new Random().Next(10000000, 99999999);
            string createCertificateQuery = string.Format(CreateCertificateQueryFormat, certificateName);
            string cleanupCertificateQuery = string.Format(CleanupCertificateQueryFormat, certificateName);

            // create master key and certificate
            testDb.RunQuery(createCertificateQuery);
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);

            // Initialize backup service
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = DisasterRecoveryService.GetSqlConnection(liveConnection.ConnectionInfo);

            // Get default backup path
            BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
            string backupPath = backupConfigInfo.DefaultBackupFolder + "\\" + databaseName + ".bak";

            BackupInfo backupInfo = createBackupInfo(databaseName, 
                BackupType.Full, 
                new List<string>(new string[] { backupPath }), 
                new Dictionary<string, int>() {{ backupPath, (int)DeviceType.File }});
            
            // Set advanced options
            backupInfo.ContinueAfterError = true;
            backupInfo.FormatMedia = true;
            backupInfo.SkipTapeHeader = true;
            backupInfo.Initialize = true;
            backupInfo.MediaName = "backup test media";
            backupInfo.MediaDescription = "backup test";
            backupInfo.RetainDays = 90;            
            backupInfo.CompressionOption = (int)BackupCompressionOptions.On;

            // Set encryption
            backupInfo.EncryptionAlgorithm = (int)BackupEncryptionAlgorithm.Aes128;
            backupInfo.EncryptorType = (int)BackupEncryptorType.ServerCertificate;
            backupInfo.EncryptorName = certificateName;

            var backupParams = new BackupParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                BackupInfo = backupInfo
            };

            // Backup the database
            BackupOperation backupOperation = DisasterRecoveryService.Instance.SetBackupInput(helper.DataContainer, sqlConn, backupParams.BackupInfo);
            DisasterRecoveryService.Instance.PerformBackup(backupOperation);
            
            // Verify backup file is created
            Assert.True(File.Exists(backupPath));

            // Remove the backup file
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            
            // Delete certificate and master key
            testDb.RunQuery(cleanupCertificateQuery);
            
            // Clean up the database
            testDb.Cleanup();
        }

        private BackupInfo createBackupInfo(string databaseName, BackupType backupType, List<string> backupPathList, Dictionary<string, int> backupPathDevices)
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
    }
}