//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Database management component
    /// </summary>
    public class DatabaseHandlerTests
    {
        /// <summary>
        /// Test the basic Create Database method handler by creating, updating, and then deleting a database.
        /// </summary>
        [Test]
        public async Task DatabaseCreateAndUpdateTest_OnPrem()
        {
            await RunDatabaseCreateAndUpdateTest(TestServerType.OnPrem);
        }

        /// <summary>
        /// Test the Create Database method handler functionality against an Azure SQL database.
        /// </summary>
        [Test]
        [Ignore("Test is not supported in the integration test pipeline.")]
        public async Task DatabaseCreateAndUpdateTest_Azure()
        {
            await RunDatabaseCreateAndUpdateTest(TestServerType.Azure);
        }

        private async Task RunDatabaseCreateAndUpdateTest(TestServerType serverType)
        {
            // setup, drop database if exists.
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", serverType: serverType);
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    // create and update
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.Database, "", objUrn);
                    await ObjectManagementTestUtils.SaveObject(parametersForUpdate, testDatabase);

                    // cleanup
                    await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn, throwIfNotExist: true);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), Is.False, $"Database '{testDatabase.Name}' was not dropped succesfully");
                }
                finally
                {
                    // Cleanup using SMO if Drop didn't work
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        /// <summary>
        /// Test that the handler can export the Create Database operation to a SQL script.
        /// </summary>
        [Test]
        public async Task DatabaseScriptTest()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    var script = await ObjectManagementTestUtils.ScriptObject(parametersForCreation, testDatabase);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), Is.False, $"Database should not have been created for scripting operation");
                    Assert.That(script.ToLowerInvariant(), Does.Contain($"create database [{testDatabase.Name!.ToLowerInvariant()}]"));
                }
                finally
                {
                    // Cleanup database on the off-chance that scripting somehow created the database
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        /// <summary>
        /// Test that the handler correctly throws an error when trying to drop a database that doesn't exist.
        /// </summary>
        [Test]
        public async Task DatabaseNotExistsErrorTest()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
            var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
            try
            {
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn, throwIfNotExist: true);
                Assert.Fail("Did not throw an exception when trying to drop non-existent database.");
            }
            catch (FailedOperationException ex)
            {
                Assert.That(ex.InnerException, Is.Not.Null, "Expected inner exception was null.");
                Assert.That(ex.InnerException, Is.InstanceOf<MissingObjectException>(), $"Received unexpected inner exception type: {ex.InnerException!.GetType()}");
            }
        }

        /// <summary>
        /// Test that the handler correctly throws an error when trying to create a database with the same name as an existing database.
        /// </summary>
        [Test]
        public async Task DatabaseAlreadyExistsErrorTest()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.Fail("Did not throw an exception when trying to create database with same name.");
                }
                catch (FailedOperationException ex)
                {
                    Assert.That(ex.InnerException, Is.Not.Null, "Expected inner exception was null.");
                    Assert.That(ex.InnerException, Is.InstanceOf<ExecutionFailureException>(), $"Received unexpected inner exception type: {ex.InnerException!.GetType()}");
                    Assert.That(ex.InnerException.InnerException, Is.Not.Null, "Expected inner-inner exception was null.");
                    Assert.That(ex.InnerException.InnerException, Is.InstanceOf<SqlException>(), $"Received unexpected inner-inner exception type: {ex.InnerException.InnerException!.GetType()}");
                }
                finally
                {
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        [Test]
        public void GetAzureEditionsTest()
        {
            var actualEditionNames = DatabaseHandler.AzureEditionNames;
            var expectedEditionNames = AzureSqlDbHelper.GetValidAzureEditionOptions().Select(edition => edition.DisplayName);
            Assert.That(actualEditionNames, Is.EquivalentTo(expectedEditionNames));
        }

        [Test]
        public void GetAzureBackupRedundancyLevelsTest()
        {
            var actualLevels = DatabaseHandler.AzureBackupLevels;
            var expectedLevels = new string[] { "Geo", "Local", "Zone" };
            Assert.That(actualLevels, Is.EquivalentTo(expectedLevels));
        }

        [Test]
        public void GetAzureServiceLevelObjectivesTest()
        {
            var actualLevelsMap = new Dictionary<string, OptionsCollection>();
            foreach (AzureEditionDetails serviceDetails in DatabaseHandler.AzureServiceLevels)
            {
                actualLevelsMap.Add(serviceDetails.EditionDisplayName, serviceDetails.EditionOptions);
            }

            var expectedDefaults = new Dictionary<AzureEdition, string>()
            {
                { AzureEdition.Basic, "Basic" },
                { AzureEdition.Standard, "S0" },
                { AzureEdition.Premium, "P1" },
                { AzureEdition.BusinessCritical, "BC_Gen5_2" },
                { AzureEdition.GeneralPurpose, "GP_Gen5_2" },
                { AzureEdition.Hyperscale, "HS_Gen5_2" }
            };
            Assert.That(actualLevelsMap.Count, Is.EqualTo(expectedDefaults.Count), "Did not get expected number of editions for DatabaseHandler's service levels");
            foreach (AzureEdition edition in expectedDefaults.Keys)
            {
                if (AzureSqlDbHelper.TryGetServiceObjectiveInfo(edition, out var expectedLevelInfo))
                {
                    var expectedServiceLevels = expectedLevelInfo.Value;
                    var actualServiceLevels = actualLevelsMap[edition.DisplayName];
                    Assert.That(actualServiceLevels.Options, Is.EquivalentTo(expectedServiceLevels), "Did not get expected SLO list for edition '{0}'", edition.DisplayName);

                    var expectedDefaultIndex = expectedLevelInfo.Key;
                    var expectedDefault = expectedServiceLevels[expectedDefaultIndex];
                    var actualDefault = actualServiceLevels.Options[actualServiceLevels.DefaultValueIndex];
                    Assert.That(actualDefault, Is.EqualTo(expectedDefault), "Did not get expected default SLO for edition '{0}'", edition.DisplayName);
                }
                else
                {
                    Assert.Fail("Could not retrieve SLO info for Azure edition '{0}'", edition.DisplayName);
                }
            }
        }

        [Test]
        public void GetAzureMaxSizesTest()
        {
            var actualSizesMap = new Dictionary<string, OptionsCollection>();
            foreach (AzureEditionDetails sizeDetails in DatabaseHandler.AzureMaxSizes)
            {
                actualSizesMap.Add(sizeDetails.EditionDisplayName, sizeDetails.EditionOptions);
            }

            var expectedDefaults = new Dictionary<AzureEdition, string>()
            {
                { AzureEdition.Basic, "2GB" },
                { AzureEdition.Standard, "250GB" },
                { AzureEdition.Premium, "500GB" },
                { AzureEdition.BusinessCritical, "32GB" },
                { AzureEdition.GeneralPurpose, "32GB" },
                { AzureEdition.Hyperscale, "0MB" }
            };
            Assert.That(actualSizesMap.Count, Is.EqualTo(expectedDefaults.Count), "Did not get expected number of editions for DatabaseHandler's max sizes");
            foreach (AzureEdition edition in expectedDefaults.Keys)
            {
                if (AzureSqlDbHelper.TryGetDatabaseSizeInfo(edition, out var expectedSizeInfo))
                {
                    var expectedSizes = expectedSizeInfo.Value.Select(size => size.ToString()).ToArray();
                    var actualSizes = actualSizesMap[edition.DisplayName];
                    Assert.That(actualSizes.Options, Is.EquivalentTo(expectedSizes), "Did not get expected size list for edition '{0}'", edition.DisplayName);

                    var expectedDefaultIndex = expectedSizeInfo.Key;
                    var expectedDefault = expectedSizes[expectedDefaultIndex];
                    var actualDefault = actualSizes.Options[actualSizes.DefaultValueIndex];
                    Assert.That(actualDefault, Is.EqualTo(expectedDefault.ToString()), "Did not get expected default size for edition '{0}'", edition.DisplayName);
                }
                else
                {
                    Assert.Fail("Could not retrieve max size info for Azure edition '{0}'", edition.DisplayName);
                }
            }
        }

        [Test]
        /// This test validates the newly created database properties and verifies with some default values
        public async Task VerifyDatabasePropertiesTest()
        {
            // setup, drop database if exists.
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", serverType: TestServerType.OnPrem);
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    // create database
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    // Get database properties and verify
                    var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, testDatabase.Name, false, SqlObjectType.Database, "", objUrn);
                    DatabaseViewInfo databaseViewInfo = await ObjectManagementTestUtils.GetDatabaseObject(parametersForUpdate, testDatabase);
                    Assert.That(databaseViewInfo.ObjectInfo, Is.Not.Null, $"Expected result should not be empty");
                    Assert.That(databaseViewInfo.ObjectInfo.Name, Is.EqualTo(testDatabase.Name), $"database name should be matched");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).DateCreated, Is.Not.Null, $"database name should be matched");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).NumberOfUsers, Is.GreaterThan(0), $"Default database users count should not be zero");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).LastDatabaseBackup, Is.EqualTo(testDatabase.LastDatabaseBackup), $"Should have no database last backup date");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).LastDatabaseLogBackup, Is.EqualTo(testDatabase.LastDatabaseLogBackup), $"Should have no database backup log date");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).SizeInMb, Is.GreaterThan(0), $"Should have default database size when created");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).AutoCreateIncrementalStatistics, Is.True, $"AutoCreateIncrementalStatistics match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).AutoCreateStatistics, Is.True, $"AutoCreateStatistics should match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).AutoShrink, Is.False, $"AutoShrink should match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).AutoUpdateStatistics, Is.True, $"AutoUpdateStatistics should match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).AutoUpdateStatisticsAsynchronously, Is.False, $"AutoUpdateStatisticsAsynchronously should match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).PageVerify, Is.EqualTo(testDatabase.PageVerify), $"PageVerify should match with testdata");
                    Assert.That(((DatabaseInfo)databaseViewInfo.ObjectInfo).RestrictAccess, Is.EqualTo(testDatabase.RestrictAccess), $"RestrictAccess should match with testdata");

                    // cleanup
                    await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn, throwIfNotExist: true);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), Is.False, $"Database '{testDatabase.Name}' was not dropped succesfully");
                }
                finally
                {
                    // Cleanup using SMO if Drop didn't work
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        [Test]
        public async Task DetachDatabaseTest()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    // Create database to test with
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.That(DatabaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    var handler = new DatabaseHandler(ConnectionService.Instance);
                    var connectionUri = connectionResult.ConnectionInfo.OwnerUri;

                    var detachParams = new DetachDatabaseRequestParams()
                    {
                        ConnectionUri = connectionUri,
                        ObjectUrn = objUrn,
                        DropConnections = true,
                        UpdateStatistics = true,
                        GenerateScript = false
                    };

                    // Get databases's files so we can reattach it later before dropping it
                    var fileCollection = new StringCollection();
                    var smoDatabase = server.GetSmoObject(objUrn) as Database;
                    foreach (FileGroup fileGroup in smoDatabase!.FileGroups)
                    {
                        foreach (DataFile file in fileGroup.Files)
                        {
                            fileCollection.Add(file.FileName);
                        }
                    }
                    foreach (LogFile file in smoDatabase.LogFiles)
                    {
                        fileCollection.Add(file.FileName);
                    }

                    var script = handler.Detach(detachParams);
                    Assert.That(script, Is.Empty, "Should only return an empty string if GenerateScript is false");

                    server.Databases.Refresh();
                    Assert.That(DatabaseExists(testDatabase.Name!, server), Is.False, $"Expected database '{testDatabase.Name}' was not detached succesfully");

                    server.AttachDatabase(testDatabase.Name, fileCollection);

                    server.Databases.Refresh();
                    Assert.That(DatabaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not re-attached succesfully");
                }
                finally
                {
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        [Test]
        public async Task DetachDatabaseScriptTest()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));

                var testDatabase = ObjectManagementTestUtils.GetTestDatabaseInfo();
                var objUrn = ObjectManagementTestUtils.GetDatabaseURN(testDatabase.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                try
                {
                    // Create database to test with
                    var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Database, "", "");
                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);

                    var handler = new DatabaseHandler(ConnectionService.Instance);
                    var connectionUri = connectionResult.ConnectionInfo.OwnerUri;

                    // Default use case
                    var detachParams = new DetachDatabaseRequestParams()
                    {
                        ConnectionUri = connectionUri,
                        ObjectUrn = objUrn,
                        DropConnections = false,
                        UpdateStatistics = false,
                        GenerateScript = true
                    };
                    var expectedDetachScript = $"EXEC master.dbo.sp_detach_db @dbname = N'{testDatabase.Name}'";
                    var expectedAlterScript = $"ALTER DATABASE [{testDatabase.Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                    var expectedStatsScript = $"EXEC master.dbo.sp_detach_db @dbname = N'{testDatabase.Name}', @skipchecks = 'false'";

                    var actualScript = handler.Detach(detachParams);
                    Assert.That(actualScript, Does.Contain(expectedDetachScript).IgnoreCase);

                    // Drop connections only
                    detachParams.DropConnections = true;
                    actualScript = handler.Detach(detachParams);
                    Assert.That(actualScript, Does.Contain(expectedDetachScript).IgnoreCase);
                    Assert.That(actualScript, Does.Contain(expectedAlterScript).IgnoreCase);

                    // Update statistics only
                    detachParams.DropConnections = false;
                    detachParams.UpdateStatistics = true;
                    actualScript = handler.Detach(detachParams);
                    Assert.That(actualScript, Does.Contain(expectedStatsScript).IgnoreCase);

                    // Both drop and update
                    detachParams.DropConnections = true;
                    actualScript = handler.Detach(detachParams);
                    Assert.That(actualScript, Does.Contain(expectedAlterScript).IgnoreCase);
                    Assert.That(actualScript, Does.Contain(expectedStatsScript).IgnoreCase);
                }
                finally
                {
                    DropDatabase(server, testDatabase.Name!);
                }
            }
        }

        private bool DatabaseExists(string dbName, Server server)
        {
            server.Databases.Refresh();
            bool dbFound = false;
            foreach (Database db in server.Databases)
            {
                if (db.Name == dbName)
                {
                    dbFound = true;
                    break;
                }
            }
            return dbFound;
        }

        private void DropDatabase(Server server, string databaseName)
        {
            server.Databases.Refresh();
            foreach (Database db in server.Databases)
            {
                if (db.Name == databaseName)
                {
                    db.DropIfExists();
                    break;
                }
            }
        }
    }
}
