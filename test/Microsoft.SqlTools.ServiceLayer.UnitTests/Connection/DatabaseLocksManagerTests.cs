//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    public class DatabaseLocksManagerTests
    {
        private const string server1 = "server1";
        private const string database1 = "database1";
       
        [Test]
        public async Task GainFullAccessShouldDisconnectTheConnections()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();
            connectionLock.Setup(x => x.CloseConnectionsAsync(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.ConnectionService.RegisterConnectedQueue("test", connectionLock.Object);

                await databaseLocksManager.GainFullAccessToDatabaseAsync(server1, database1);
                connectionLock.Verify(x => x.CloseConnectionsAsync(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));
            }
        }

        [Test]
        public async Task ReleaseAccessShouldConnectTheConnections()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();
            connectionLock.Setup(x => x.OpenConnectionsAsync(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.ConnectionService.RegisterConnectedQueue("test", connectionLock.Object);

                await databaseLocksManager.ReleaseAccessAsync(server1, database1);
                connectionLock.Verify(x => x.OpenConnectionsAsync(server1, database1, DatabaseLocksManager.DefaultWaitToGetFullAccess));
            }
        }

        //[Test]
        public async Task SecondProcessToGainAccessShouldWaitForTheFirstProcess()
        {
            var connectionLock = new Mock<IConnectedBindingQueue>();

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                await databaseLocksManager.GainFullAccessToDatabaseAsync(server1, database1);
                bool secondTimeGettingAccessFails = false;
                try
                {
                    await databaseLocksManager.GainFullAccessToDatabaseAsync(server1, database1);
                }
                catch (DatabaseFullAccessException)
                {
                    secondTimeGettingAccessFails = true;
                }
                Assert.AreEqual(true, secondTimeGettingAccessFails);
                await databaseLocksManager.ReleaseAccessAsync(server1, database1);
                Assert.AreEqual(true, await databaseLocksManager.GainFullAccessToDatabaseAsync(server1, database1));
                await databaseLocksManager.ReleaseAccessAsync(server1, database1);
            }
        }

        private DatabaseLocksManager CreateManager()
        {
            DatabaseLocksManager databaseLocksManager = new DatabaseLocksManager(2000);
            var connectionLock1 = new Mock<IConnectedBindingQueue>();
            var connectionLock2 = new Mock<IConnectedBindingQueue>();
            connectionLock1.Setup(x => x.CloseConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock2.Setup(x => x.OpenConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock1.Setup(x => x.OpenConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            connectionLock2.Setup(x => x.CloseConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));
            ConnectionService connectionService = new ConnectionService();

            databaseLocksManager.ConnectionService = connectionService;

            connectionService.RegisterConnectedQueue("1", connectionLock1.Object);
            connectionService.RegisterConnectedQueue("2", connectionLock2.Object);
            return databaseLocksManager;
        }
    }
}
