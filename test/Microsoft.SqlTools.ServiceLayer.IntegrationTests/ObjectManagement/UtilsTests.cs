//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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
        public async Task GetDataFolderTest() {
            using(SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "UtilsTest"))
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, serverType: TestServerType.OnPrem);
                using(SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
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
    }
}