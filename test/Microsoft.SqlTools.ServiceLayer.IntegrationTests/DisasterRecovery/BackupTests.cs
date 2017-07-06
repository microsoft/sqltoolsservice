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
        /// <summary>
        /// Get backup configuration info
        /// </summary>
        /// Test is failing in code coverage runs. Reenable when stable.
        /// [Fact]
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
            
            var backupInfo = new BackupInfo();
            backupInfo.BackupComponent = (int)BackupComponent.Database;
            backupInfo.BackupDeviceType = (int)BackupDeviceType.Disk;
            backupInfo.BackupPathDevices = new Dictionary<string, int>() { { backupPath, (int)DeviceType.File } };
            backupInfo.BackupPathList = new List<string>(new string[] { backupPath });
            backupInfo.BackupsetName = "default_backup";
            backupInfo.BackupType = (int)BackupType.Full;
            backupInfo.DatabaseName = databaseName;
            backupInfo.SelectedFileGroup = null;
            backupInfo.SelectedFiles = "";

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
    }
}