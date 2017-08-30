//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.FileValidator;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Moq;
using Microsoft.SqlTools.Hosting.Protocol;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.FileBrowser
{
    /// <summary>
    /// File browser service tests
    /// </summary>
    public class FileBrowserServiceTests
    {
        [Fact]
        public async void OpenAndCloseFileBrowser()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var openRequestContext = new Mock<RequestContext<FileBrowserOpenResponse>>();
            openRequestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserOpenResponse>()))
                .Returns(Task.FromResult(new object()));
            var closeRequestContext = new Mock<RequestContext<FileBrowserCloseResponse>>();
            closeRequestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserCloseResponse>()))
                .Returns(Task.FromResult(new object()));

            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = "",
                FileFilters = new string[1] {"*"}
            };
            
            // Open file browser
            await service.HandleFileBrowserOpenRequest(openParams, openRequestContext.Object);

            // Verify result
            openRequestContext.Verify(x => x.SendResult(It.Is<FileBrowserOpenResponse>
                (p => p.FileTree  != null
                && p.FileTree.RootNode != null
                && p.FileTree.RootNode.Children != null
                && p.FileTree.RootNode.Children.Count > 0)));

            var closeParams = new FileBrowserCloseParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri
            };

            // Close file browser
            await service.HandleFileBrowserCloseRequest(closeParams, closeRequestContext.Object);

            // Verify result
            closeRequestContext.Verify(x => x.SendResult(It.Is<FileBrowserCloseResponse>
                (p => p.Succeeded == true)));
        }

        [Fact]
        public async void ValidateSelectedFilesWithNullValidator()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var requestContext = new Mock<RequestContext<FileBrowserValidateResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserValidateResponse>()))
                .Returns(Task.FromResult(new object()));
            
            var validateParams = new FileBrowserValidateParams
            {
                // Do not pass any service so that the file validator will be null
                ServiceType = "",
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                SelectedFiles = new string[] { "" }
            };

            // Validate files with null file validator
            await service.HandleFileBrowserValidateRequest(validateParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.Is<FileBrowserValidateResponse>
                (p => p.Succeeded == true)));
        }

        [Fact]
        public async void UseFileBrowserForBackupService()
        {
            string databaseName = "testfilebrowser_" + new Random().Next(10000000, 99999999);
            SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName);

            // Initialize backup service
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(liveConnection.ConnectionInfo, databaseExists: true);
            SqlConnection sqlConn = DisasterRecoveryService.GetSqlConnection(liveConnection.ConnectionInfo);
            DisasterRecoveryService disasterRecoveryService = new DisasterRecoveryService();
            BackupConfigInfo backupConfigInfo = disasterRecoveryService.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);

            // Create backup file
            string backupPath = Path.Combine(backupConfigInfo.DefaultBackupFolder, databaseName + ".bak");
            string query = $"BACKUP DATABASE [{databaseName}] TO  DISK = N'{backupPath}' WITH NOFORMAT, NOINIT, NAME = N'{databaseName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD,  STATS = 10";
            await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", query);

            FileBrowserService service = new FileBrowserService();
            var openRequestContext = new Mock<RequestContext<FileBrowserOpenResponse>>();
            openRequestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserOpenResponse>()))
                .Returns(Task.FromResult(new object()));
            string[] backupFilters = new string[2] { "*.bak", "*.trn" };
            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = backupConfigInfo.DefaultBackupFolder,
                FileFilters = backupFilters
            };

            // Open file browser
            await service.HandleFileBrowserOpenRequest(openParams, openRequestContext.Object);

            // Verify result
            openRequestContext.Verify(x => x.SendResult(It.Is<FileBrowserOpenResponse>
                (p => p.FileTree != null
                && p.FileTree.RootNode != null
                && p.FileTree.RootNode.Children != null
                && p.FileTree.RootNode.Children.Count > 0
                && p.FileTree.SelectedNode.FullPath == backupConfigInfo.DefaultBackupFolder
                && p.FileTree.SelectedNode.Children.Count > 0
                && ContainsFileInTheFolder(p.FileTree.SelectedNode, backupPath)
                )));

            var expandRequestContext = new Mock<RequestContext<FileBrowserExpandResponse>>();
            expandRequestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserExpandResponse>()))
                .Returns(Task.FromResult(new object()));
            var expandParams = new FileBrowserExpandParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = backupConfigInfo.DefaultBackupFolder
            };

            // Expand the node in file browser
            await service.HandleFileBrowserExpandRequest(expandParams, expandRequestContext.Object);

            // Verify result
            expandRequestContext.Verify(x => x.SendResult(It.Is<FileBrowserExpandResponse>
                (p => p.Succeeded == true
                && p.ExpandedNode.FullPath == backupConfigInfo.DefaultBackupFolder
                )));

            var validateRequestContext = new Mock<RequestContext<FileBrowserValidateResponse>>();
            validateRequestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserValidateResponse>()))
                .Returns(Task.FromResult(new object()));
            var validateParams = new FileBrowserValidateParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ServiceType = ServiceConstants.Backup,
                SelectedFiles = new string[] { backupPath }
            };

            // Validate selected files in the browser
            await service.HandleFileBrowserValidateRequest(validateParams, validateRequestContext.Object);

            // Validate result
            validateRequestContext.Verify(x => x.SendResult(It.Is<FileBrowserValidateResponse>
                (p => p.Succeeded == true)));

            // Remove the backup file
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        #region private methods

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
