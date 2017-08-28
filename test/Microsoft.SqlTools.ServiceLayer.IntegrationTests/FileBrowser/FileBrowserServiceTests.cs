//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.FileBrowser
{
    /// <summary>
    /// Unit tests for file browser
    /// </summary>
    public class FileBrowserServiceTests
    {
        [Fact]
        public void OpenBackupFileBrowser()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            SqlConnection sqlConn = DisasterRecoveryService.GetSqlConnection(liveConnection.ConnectionInfo);
            ServerConnection serverConnection = new ServerConnection(sqlConn);
            FileBrowserOperation browser = new FileBrowserOperation(serverConnection, true, new[] { new FileFilter("Backup Files", "*.bak;*.trn") }, 0);

            browser.Init();
            //browse.SetFileValidityChecker(Validate);

            //if (null != txtFilePath && txtFilePath.Text.Length > 0)
            //{
            //    string path = txtFilePath.Text;

            //    browse.StartPath = PathWrapper.GetDirectoryName(path);
            //}

            //browser.StartPath = "";

            //if (browser.Init())
            //{
            //    // validate path
            //}

        }
        /*
        private void btBrowse_Click(object sender, EventArgs e)
        {
            using (FileBrowser browse =
                new FileBrowser(sqlConnection,
                    true,
                    mbProvider,
                    new[] { new FileFilter(SelBakDestSR.BackupFilesFilter, "*.bak;*.trn") },
                    0)
                )
            {
                browse.SetFileValidityChecker(Validate);

                if (null != txtFilePath && txtFilePath.Text.Length > 0)
                {
                    string path = txtFilePath.Text;

                    browse.StartPath = PathWrapper.GetDirectoryName(path);
                }

                if (browse.Init())
                {
                    // path is not validated
                    pathValidated = false;

                    // show browse dialog
                    browse.ShowDialog(this);

                    if (DialogResult.OK == browse.DialogResult)
                    {
                        string filePath = browse.SelectedFullFileName;

                        // Path vas validated in browse dialog
                        pathValidated = true;

                        txtFilePath.Text = filePath;
                    }
                }
            }
        }*/


    }
}
