//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    public class DatabaseLocksManagerTests
    {
        private const string server1 = "server1";
        private const string database1 = "database1";
       
        [Test]
        public void GainFullAccessShouldDisconnectTheConnections()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();
            connectionLock.Setup(x => x.CloseConnections(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.ConnectionService.RegisterConnectedQueue("test", connectionLock.Object);

                databaseLocksManager.GainFullAccessToDatabase(server1, database1);
                connectionLock.Verify(x => x.CloseConnections(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));
            }
        }

        [Test]
        public void ReleaseAccessShouldConnectTheConnections()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();
            connectionLock.Setup(x => x.OpenConnections(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.ConnectionService.RegisterConnectedQueue("test", connectionLock.Object);

                databaseLocksManager.ReleaseAccess(server1, database1);
                connectionLock.Verify(x => x.OpenConnections(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));
            }
        }

        //[Test]
        public void SecondProcessToGainAccessShouldWaitForTheFirstProcess()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.GainFullAccessToDatabase(server1, database1);
                bool secondTimeGettingAccessFails = false;
                try
                {
                    databaseLocksManager.GainFullAccessToDatabase(server1, database1);
                }
                catch (DatabaseFullAccessException)
                {
                    secondTimeGettingAccessFails = true;
                }
                Assert.AreEqual(true, secondTimeGettingAccessFails);
                databaseLocksManager.ReleaseAccess(server1, database1);
                Assert.AreEqual(true, databaseLocksManager.GainFullAccessToDatabase(server1, database1));
                databaseLocksManager.ReleaseAccess(server1, database1);
            }
        }

        private DatabaseLocksManager CreateManager()
        {
            DatabaseLocksManager databaseLocksManager = new DatabaseLocksManager(2000);
            var connectionLock1 = new Mock<IConnectedBindingQueue>();
            var connectionLock2 = new Mock<IConnectedBindingQueue>();
            connectionLock1.Setup(x => x.CloseConnections(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock2.Setup(x => x.OpenConnections(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock1.Setup(x => x.OpenConnections(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock2.Setup(x => x.CloseConnections(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            ConnectionService connectionService = new ConnectionService();

            databaseLocksManager.ConnectionService = connectionService;

            connectionService.RegisterConnectedQueue("1", connectionLock1.Object);
            connectionService.RegisterConnectedQueue("2", connectionLock2.Object);
            return databaseLocksManager;
        }
    }
}
