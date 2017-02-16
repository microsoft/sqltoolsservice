//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.SaveResults
{
    public class ResultSetTests
    {
        [Fact]
        public void SaveAsNullParams()
        {
            // If: I attempt to save with a null set of params
            // Then: I should get a null argument exception
            ResultSet rs = new ResultSet(
                GetReader(null, false, Common.NoOpQuery), Common.Ordinal, Common.Ordinal, 
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            Assert.Throws<ArgumentNullException>(() => rs.SaveAs(
                null,
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()),
                null, null));
        }

        [Fact]
        public void SaveAsNullFactory()
        {
            // If: I attempt to save with a null set of params
            // Then: I should get a null argument exception
            ResultSet rs = new ResultSet(
                GetReader(null, false, Common.NoOpQuery), Common.Ordinal, Common.Ordinal, 
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            Assert.Throws<ArgumentNullException>(() => rs.SaveAs(
                new SaveResultsRequestParams(),
                null, null, null));
        }

        [Fact]
        public void SaveAsFailedIncomplete()
        {
            // If: I attempt to save a result set that hasn't completed execution
            // Then: I should get an invalid operation exception
            ResultSet rs = new ResultSet(
                GetReader(null, false, Common.NoOpQuery), Common.Ordinal, Common.Ordinal, 
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            Assert.Throws<InvalidOperationException>(() => rs.SaveAs(
                new SaveResultsRequestParams(), 
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()), 
                null, null));
        }

        [Fact]
        public void SaveAsFailedExistingTaskInProgress()
        {
            // Setup:
            // ... Create a result set that has been executed
            ResultSet rs = new ResultSet(
                GetReader(Common.StandardTestDataSet, false, Common.StandardQuery),
                Common.Ordinal, Common.Ordinal,
                Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));

            // ... Insert a non-started task into the save as tasks
            rs.SaveTasks.AddOrUpdate(Common.OwnerUri, new Task(() => { }), (s, t) => null);

            // If: I attempt to save results with the same name as the non-completed task
            // Then: I should get an invalid operation exception
            var requestParams = new SaveResultsRequestParams {FilePath = Common.OwnerUri};
            Assert.Throws<InvalidOperationException>(() => rs.SaveAs(
                requestParams, GetMockFactory(GetMockWriter().Object, null), 
                null, null));
        }

        [Fact]
        public async Task SaveAsWithoutRowSelection()
        {
            // Setup:
            // ... Create a fake place to store data
            Dictionary<string, byte[]> mockFs = new Dictionary<string, byte[]>();

            // ... Create a mock reader/writer for reading the result
            IFileStreamFactory resultFactory = Common.GetFileStreamFactory(mockFs);

            // ... Create a result set with dummy data and read to the end
            ResultSet rs = new ResultSet(
                GetReader(Common.StandardTestDataSet, false, Common.StandardQuery),
                Common.Ordinal, Common.Ordinal,
                resultFactory);
            await rs.ReadResultToEnd(CancellationToken.None);

            // ... Create a mock writer for writing the save as file
            Mock<IFileStreamWriter> saveWriter = GetMockWriter();
            IFileStreamFactory saveFactory = GetMockFactory(saveWriter.Object, resultFactory.GetReader);

            // If: I attempt to save results and await completion
            rs.SaveAs(new SaveResultsRequestParams {FilePath = Common.OwnerUri}, saveFactory, null, null);
            Assert.True(rs.SaveTasks.ContainsKey(Common.OwnerUri));
            await rs.SaveTasks[Common.OwnerUri];

            // Then:
            // ... The task should have completed successfully
            Assert.Equal(TaskStatus.RanToCompletion, rs.SaveTasks[Common.OwnerUri].Status);

            // ... All the rows should have been written successfully
            saveWriter.Verify(
                w => w.WriteRow(It.IsAny<IList<DbCellValue>>(), It.IsAny<IList<DbColumnWrapper>>()),
                Times.Exactly(Common.StandardRows));
        }

        [Fact]
        public async Task SaveAsWithRowSelection()
        {
            // Setup:
            // ... Create a fake place to store data
            Dictionary<string, byte[]> mockFs = new Dictionary<string, byte[]>();

            // ... Create a mock reader/writer for reading the result
            IFileStreamFactory resultFactory = Common.GetFileStreamFactory(mockFs);

            // ... Create a result set with dummy data and read to the end
            ResultSet rs = new ResultSet(
                GetReader(Common.StandardTestDataSet, false, Common.StandardQuery),
                Common.Ordinal, Common.Ordinal,
                resultFactory);
            await rs.ReadResultToEnd(CancellationToken.None);

            // ... Create a mock writer for writing the save as file
            Mock<IFileStreamWriter> saveWriter = GetMockWriter();
            IFileStreamFactory saveFactory = GetMockFactory(saveWriter.Object, resultFactory.GetReader);

            // If: I attempt to save results that has a selection made
            var saveParams = new SaveResultsRequestParams
            {
                FilePath = Common.OwnerUri,
                RowStartIndex = 1,
                RowEndIndex = Common.StandardRows - 2,
                ColumnStartIndex = 0,                       // Column start/end doesn't matter, but are required to be
                ColumnEndIndex = 10                         // considered a "save selection"
            };
            rs.SaveAs(saveParams, saveFactory, null, null);
            Assert.True(rs.SaveTasks.ContainsKey(Common.OwnerUri));
            await rs.SaveTasks[Common.OwnerUri];

            // Then:
            // ... The task should have completed successfully
            Assert.Equal(TaskStatus.RanToCompletion, rs.SaveTasks[Common.OwnerUri].Status);

            // ... All the rows should have been written successfully
            saveWriter.Verify(
                w => w.WriteRow(It.IsAny<IList<DbCellValue>>(), It.IsAny<IList<DbColumnWrapper>>()),
                Times.Exactly(Common.StandardRows - 2));
        }

        private static Mock<IFileStreamWriter> GetMockWriter()
        {
            var mockWriter = new Mock<IFileStreamWriter>();
            mockWriter.Setup(w => w.WriteRow(It.IsAny<IList<DbCellValue>>(), It.IsAny<IList<DbColumnWrapper>>()));
            return mockWriter;
        }

        private static IFileStreamFactory GetMockFactory(IFileStreamWriter writer, Func<string, IFileStreamReader> readerGenerator)
        {
            var mockFactory = new Mock<IFileStreamFactory>();
            mockFactory.Setup(f => f.GetWriter(It.IsAny<string>()))
                .Returns(writer);
            mockFactory.Setup(f => f.GetReader(It.IsAny<string>()))
                .Returns(readerGenerator);
            return mockFactory.Object;
        }

        private static DbDataReader GetReader(TestResultSet[] dataSet, bool throwOnRead, string query)
        {
            var info = Common.CreateTestConnectionInfo(dataSet, throwOnRead);
            var connection = info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteReader();
        }
    }
}
