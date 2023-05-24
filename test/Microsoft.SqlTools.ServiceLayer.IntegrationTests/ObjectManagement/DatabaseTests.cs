//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Database management component
    /// </summary>
    public class DatabaseTests
    {
        /// <summary>
        /// Test the basic Create Database method handler by creating, updating, and then deleting a database.
        /// </summary>
        [Test]
        public async Task DatabaseCreateAndUpdateTest_OnPrem()
        {
            await RunDatabaseCreateAndUpdateTest(TestServerType.OnPrem);
        }

        // Disable Azure test by default since it's not supported for pipeline tests
        // [Test] 
        public async Task DatabaseCreateAndUpdateTest_Azure()
        {
            await RunDatabaseCreateAndUpdateTest(TestServerType.Azure);
        }

        private async Task RunDatabaseCreateAndUpdateTest(TestServerType serverType)
        {
            // setup, drop database if exists.
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", serverType: serverType);
            DbConnection connection;
            if (!connectionResult.ConnectionInfo.TryGetConnection(Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default, out connection))
            {
                throw new InvalidOperationException("Could not retrieve connection object.");
            }
            var server = new Server(new ServerConnection(new SqlConnection(connection.ConnectionString)));

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
                server.Databases.Refresh();
                foreach (Database db in server.Databases)
                {
                    if (db.Name == testDatabase.Name)
                    {
                        db.DropIfExists();
                        break;
                    }
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

        [Test]
        public async Task DatabaseAlreadyExistsErrorTest()
        {
            // setup, drop database if exists.
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master");
            DbConnection connection;
            if (!connectionResult.ConnectionInfo.TryGetConnection(Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default, out connection))
            {
                throw new InvalidOperationException("Could not retrieve connection object.");
            }
            var server = new Server(new ServerConnection(new SqlConnection(connection.ConnectionString)));

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
                server.Databases.Refresh();
                foreach (Database db in server.Databases)
                {
                    if (db.Name == testDatabase.Name)
                    {
                        db.DropIfExists();
                        break;
                    }
                }
            }
        }
    }
}
