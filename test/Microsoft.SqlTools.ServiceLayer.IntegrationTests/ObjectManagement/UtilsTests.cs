//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    public class UtilsTests
    {
        [Test]
        public async Task GetDataFolderTest()
        {
            using (SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, nameof(GetDataFolderTest)))
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, serverType: TestServerType.OnPrem);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
                {
                    var serverConn = new ServerConnection(sqlConn);
                    var server = new Server(serverConn);
                    var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDb.DatabaseName);
                    var database = server.GetSmoObject(objUrn) as Database;

                    var dataFilePath = database.FileGroups[0].Files[0].FileName;
                    var expectedDataFolder = Path.GetDirectoryName(dataFilePath).ToString();

                    var actualDataFolder = CommonUtilities.GetDefaultDataFolder(serverConn);
                    actualDataFolder = Path.TrimEndingDirectorySeparator(actualDataFolder);
                    Assert.That(actualDataFolder, Is.EqualTo(expectedDataFolder).IgnoreCase, "Did not get expected data file folder path.");
                }
            }
        }

        [Test]
        public async Task GetAssociatedFilesTest()
        {
            using (SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, nameof(GetAssociatedFilesTest)))
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, serverType: TestServerType.OnPrem);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
                {
                    var serverConn = new ServerConnection(sqlConn);
                    var server = new Server(serverConn);
                    var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDb.DatabaseName);
                    var database = server.GetSmoObject(objUrn) as Database;

                    var expectedFilePaths = new List<string>();
                    DataFile primaryFile = null;
                    foreach (FileGroup group in database.FileGroups)
                    {
                        foreach (DataFile file in group.Files)
                        {
                            expectedFilePaths.Add(file.FileName);
                            if (file.IsPrimaryFile)
                            {
                                primaryFile = file;
                            }
                        }
                    }
                    foreach (LogFile file in database.LogFiles)
                    {
                        expectedFilePaths.Add(file.FileName);
                    }

                    // Detach database so that we don't throw an error when trying to access the primary data file
                    // Have to set database to single user mode to close active connections before detaching it.
                    database.DatabaseOptions.UserAccess = SqlServer.Management.Smo.DatabaseUserAccess.Single;
                    database.Alter(TerminationClause.RollbackTransactionsImmediately);
                    server.DetachDatabase(testDb.DatabaseName, false);
                    try
                    {
                        Assert.That(primaryFile, Is.Not.Null, "Could not find a primary file in the list of database files.");
                        var actualFilePaths = CommonUtilities.GetAssociatedFilePaths(serverConn, primaryFile.FileName);
                        Assert.That(actualFilePaths, Is.EqualTo(expectedFilePaths).IgnoreCase, "The list of associated files did not match the actual files for the database.");
                    }
                    finally
                    {
                        // Reattach database so it can get dropped during cleanup
                        var fileCollection = new StringCollection();
                        expectedFilePaths.ForEach(file => fileCollection.Add(file));
                        server.AttachDatabase(testDb.DatabaseName, fileCollection);
                    }
                }
            }
        }

        [Test]
        public async Task ThrowErrorWhenDatabaseExistsTest()
        {
            using (SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, nameof(ThrowErrorWhenDatabaseExistsTest)))
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, serverType: TestServerType.OnPrem);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
                {
                    var serverConn = new ServerConnection(sqlConn);
                    var server = new Server(serverConn);
                    var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDb.DatabaseName);
                    var database = server.GetSmoObject(objUrn) as Database;

                    DataFile primaryFile = null;
                    foreach (FileGroup group in database.FileGroups)
                    {
                        foreach (DataFile file in group.Files)
                        {
                            if (file.IsPrimaryFile)
                            {
                                primaryFile = file;
                            }
                        }
                    }

                    Assert.That(
                        () => CommonUtilities.GetAssociatedFilePaths(serverConn, primaryFile.FileName),
                        Throws.Exception,
                        "Should throw an error when trying to open a database file that's already in use."
                    );
                }
            }
        }
    }
}