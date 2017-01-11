//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution.DataStorage
{
    public class StorageDataReaderTests
    {
        private async Task<StorageDataReader> GetTestStorageDataReader(string query)
        {
            var result = await TestObjects.InitLiveConnectionInfo();
            DbConnection connection;
            result.ConnectionInfo.TryGetConnection(ConnectionType.Default, out connection);

            var command = connection.CreateCommand();
            command.CommandText = query;
            DbDataReader reader = command.ExecuteReader();

            return new StorageDataReader(reader);
        }

        /// <summary>
        /// Validate GetBytesWithMaxCapacity
        /// </summary>
        [Fact]
        public async Task GetBytesWithMaxCapacityTest()
        {
            var storageReader = await GetTestStorageDataReader(
                "SELECT CAST([name] as TEXT) As TextName FROM sys.all_columns");
            DbDataReader reader = storageReader.DbDataReader;

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            byte[] bytes = storageReader.GetBytesWithMaxCapacity(0, 100);
            Assert.NotNull(bytes);
        }

        /// <summary>
        /// Validate GetCharsWithMaxCapacity
        /// </summary>
        [Fact]
        public async Task GetCharsWithMaxCapacityTest()
        {
            var storageReader = await GetTestStorageDataReader(
                "SELECT name FROM sys.all_columns");
            DbDataReader reader = storageReader.DbDataReader;

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            Assert.NotNull(storageReader.GetValue(0));

            string shortName = storageReader.GetCharsWithMaxCapacity(0, 2);
            Assert.True(shortName.Length == 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => storageReader.GetBytesWithMaxCapacity(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => storageReader.GetCharsWithMaxCapacity(0, 0));     
        }

        /// <summary>
        /// Validate GetXmlWithMaxCapacity
        /// </summary>
        [Fact]
        public async Task GetXmlWithMaxCapacityTest()
        {
            var storageReader = await GetTestStorageDataReader(
                "SELECT CAST('<xml>Test XML context</xml>' AS XML) As XmlColumn");
            DbDataReader reader = storageReader.DbDataReader;

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            string shortXml = storageReader.GetXmlWithMaxCapacity(0, 2);
            Assert.True(shortXml.Length == 3);
        }

        /// <summary>
        /// Validate StringWriterWithMaxCapacity Write test
        /// </summary>
        [Fact]
        public void StringWriterWithMaxCapacityTest()
        {
            var writer = new StorageDataReader.StringWriterWithMaxCapacity(null, 4);
            string output = "...";
            writer.Write(output);
            Assert.True(writer.ToString().Equals(output));
            writer.Write('.');
            Assert.True(writer.ToString().Equals(output + '.'));       
            writer.Write(output);
            writer.Write('.');
            Assert.True(writer.ToString().Equals(output + '.'));
        }       
    }
}