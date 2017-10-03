//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    public class DatabaseLocksManagerTests
    {
        private string server1 = "server1";
        private string database1 = "database1";
        private string server2 = "server2";
        private string database2 = "database2";

        [Fact]
        public void RemoveConnectionShouldRemoveTheGivenConnection()
        {
            var connectionLock = new Mock<IDatabaseLockConnection>();
            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                int count = databaseLocksManager.GetLocks(server1, database1).Count;
                databaseLocksManager.AddConnection(server1, database1, connectionLock.Object);

                int actual = databaseLocksManager.GetLocks(server1, database1).Count;
                int expected = count + 1;
                Assert.Equal(actual, expected);

                databaseLocksManager.RemoveConnection(server1, database1, connectionLock.Object);
                actual = databaseLocksManager.GetLocks(server1, database1).Count;
                expected = count;
                Assert.Equal(actual, expected);
            }
        }

        [Fact]
        public void RemoveConnectionShouldNotFailGivenInvalidConnection()
        {
            var connectionLock = new Mock<IDatabaseLockConnection>();
            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                int count = databaseLocksManager.GetLocks(server1, database1).Count;

                databaseLocksManager.RemoveConnection(server1, database1, connectionLock.Object);
                int actual = databaseLocksManager.GetLocks(server1, database1).Count;
                int expected = count;
                Assert.Equal(actual, expected);
            }
        }

        [Fact]
        public void GainFullAccessShouldDisconnectTheConnections()
        {
            var connectionLock = new Mock<IDatabaseLockConnection>();
            connectionLock.Setup(x => x.Disconnect());
            connectionLock.Setup(x => x.IsConnctionOpen).Returns(true);
            connectionLock.Setup(x => x.CanTemporaryClose).Returns(true);

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.AddConnection(server1, database1, connectionLock.Object);


                databaseLocksManager.GainFullAccessToDatabase(server1, database1);
                connectionLock.Verify(x => x.Disconnect());
            }
        }

        [Fact]
        public void ReleaseAccessShouldConnectTheConnections()
        {
            var connectionLock = new Mock<IDatabaseLockConnection>();
            connectionLock.Setup(x => x.Connect());
            connectionLock.Setup(x => x.IsConnctionOpen).Returns(false);

            using (DatabaseLocksManager databaseLocksManager = CreateManager())
            {
                databaseLocksManager.AddConnection(server1, database1, connectionLock.Object);


                databaseLocksManager.ReleaseAccess(server1, database1);
                connectionLock.Verify(x => x.Connect());
            }
        }

        [Fact]
        public void SecondProcessToGainAccessShouldWaitForTheFirstProcess()
        {
            var connectionLock = new Mock<IDatabaseLockConnection>();

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
                Assert.Equal(secondTimeGettingAccessFails, true);
                databaseLocksManager.ReleaseAccess(server1, database1);
                Assert.Equal(databaseLocksManager.GainFullAccessToDatabase(server1, database1), true);
                databaseLocksManager.ReleaseAccess(server1, database1);
            }
        }

        private DatabaseLocksManager CreateManager()
        {
            DatabaseLocksManager databaseLocksManager = new DatabaseLocksManager(2000);
            var connectionLock1 = new Mock<IDatabaseLockConnection>();
            var connectionLock2 = new Mock<IDatabaseLockConnection>();
            connectionLock1.Setup(x => x.Disconnect());
            connectionLock2.Setup(x => x.Disconnect());
            connectionLock1.Setup(x => x.Connect());
            connectionLock2.Setup(x => x.Connect());
            connectionLock1.Setup(x => x.IsConnctionOpen).Returns(true);
            connectionLock2.Setup(x => x.IsConnctionOpen).Returns(true);
            connectionLock1.Setup(x => x.CanTemporaryClose).Returns(true);
            connectionLock2.Setup(x => x.CanTemporaryClose).Returns(true);


            databaseLocksManager.AddConnection(server1, database1, connectionLock1.Object);
            databaseLocksManager.AddConnection(server2, database2, connectionLock2.Object);
            return databaseLocksManager;
        }
    }
}
