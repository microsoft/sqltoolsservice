//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

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
                    Assert.True(databaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.Database, "", objUrn);
                    await ObjectManagementTestUtils.SaveObject(parametersForUpdate, testDatabase);

                    // cleanup
                    await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn, throwIfNotExist: true);
                    Assert.False(databaseExists(testDatabase.Name!, server), $"Database '{testDatabase.Name}' was not dropped succesfully");
                }
                finally
                {
                    // Cleanup using SMO if Drop didn't work
                    dropDatabase(server, testDatabase.Name!);
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
                    Assert.True(!databaseExists(testDatabase.Name!, server), $"Database should not have been created for scripting operation");
                    Assert.True(script.ToLowerInvariant().Contains($"create database [{testDatabase.Name!.ToLowerInvariant()}]"));
                }
                finally
                {
                    // Cleanup database on the off-chance that scripting somehow created the database
                    dropDatabase(server, testDatabase.Name!);
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
                Assert.NotNull(ex.InnerException, "Expected inner exception was null.");
                Assert.True(ex.InnerException is MissingObjectException, $"Received unexpected inner exception type: {ex.InnerException!.GetType()}");
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
                    Assert.True(databaseExists(testDatabase.Name!, server), $"Expected database '{testDatabase.Name}' was not created succesfully");

                    await ObjectManagementTestUtils.SaveObject(parametersForCreation, testDatabase);
                    Assert.Fail("Did not throw an exception when trying to create database with same name.");
                }
                catch (FailedOperationException ex)
                {
                    Assert.NotNull(ex.InnerException, "Expected inner exception was null.");
                    Assert.True(ex.InnerException is ExecutionFailureException, $"Received unexpected inner exception type: {ex.InnerException!.GetType()}");
                    Assert.NotNull(ex.InnerException.InnerException, "Expected inner-inner exception was null.");
                    Assert.True(ex.InnerException.InnerException is SqlException, $"Received unexpected inner-inner exception type: {ex.InnerException.InnerException!.GetType()}");
                }
                finally
                {
                    dropDatabase(server, testDatabase.Name!);
                }
            }
        }

        private bool databaseExists(string dbName, Server server)
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

        private void dropDatabase(Server server, string databaseName)
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
