//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#if LIVE_CONNECTION_TESTS

using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class StorageDataReaderTests
    {
        private StorageDataReader GetTestStorageDataReader(out DbDataReader reader, string query)
        {
            ScriptFile scriptFile;
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);

            var command = connInfo.SqlConnection.CreateCommand();
            command.CommandText = query;
            reader = command.ExecuteReader();

            return new StorageDataReader(reader);
        }

        /// <summary>
        /// Validate GetBytesWithMaxCapacity
        /// </summary>
        [Fact]
        public void GetBytesWithMaxCapacityTest()
        {
            DbDataReader reader;
            var storageReader = GetTestStorageDataReader(
                out reader,
                "SELECT CAST([name] as TEXT) As TextName FROM sys.all_columns");

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            byte[] bytes = storageReader.GetBytesWithMaxCapacity(0, 100);
            Assert.NotNull(bytes);
        }

        /// <summary>
        /// Validate GetCharsWithMaxCapacity
        /// </summary>
        [Fact]
        public void GetCharsWithMaxCapacityTest()
        {
            DbDataReader reader;
            var storageReader = GetTestStorageDataReader(
                out reader,
                "SELECT name FROM sys.all_columns");

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            string shortName = storageReader.GetCharsWithMaxCapacity(0, 2);
            Assert.True(shortName.Length == 2);
        }

        /// <summary>
        /// Validate GetXmlWithMaxCapacity
        /// </summary>
        [Fact]
        public void GetXmlWithMaxCapacityTest()
        {
            DbDataReader reader;
            var storageReader = GetTestStorageDataReader(
                out reader,
                "SELECT CAST('<xml>Test XML context</xml>' AS XML) As XmlColumn");

            reader.Read();
            Assert.False(storageReader.IsDBNull(0));

            string shortXml = storageReader.GetXmlWithMaxCapacity(0, 2);
            Assert.True(shortXml.Length == 3);
        }
    }
}

#endif