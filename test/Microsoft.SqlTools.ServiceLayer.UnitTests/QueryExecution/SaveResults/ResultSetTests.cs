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
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.SaveResults
{
    public class ResultSetTests
    {
        [Test]
        public void SaveAs_NullParams()
        {
            // If: I attempt to save with a null set of params
            // Then: I should get a null argument exception
            ResultSet rs = new ResultSet(
                Common.Ordinal,
                Common.Ordinal,
                new Mock<IServiceBufferFileStreamFactory>().Object);
            Assert.Throws<ArgumentNullException>(() => rs.SaveAs(
                null,
                new Mock<ISaveAsFileStreamFactory>().Object,
                null,
                null));
        }

        [Test]
        public void SaveAs_NullFactory()
        {
            // If: I attempt to save with a null set of params
            // Then: I should get a null argument exception
            ResultSet rs = new ResultSet(
                Common.Ordinal,
                Common.Ordinal,
                new Mock<IServiceBufferFileStreamFactory>().Object);
            Assert.Throws<ArgumentNullException>(() => rs.SaveAs(
                new SaveResultsRequestParams(),
                null,
                null,
                null));
        }

        [Test]
        public void SaveAs_FailedIncomplete()
        {
            // If: I attempt to save a result set that hasn't completed execution
            // Then: I should get an invalid operation exception
            ResultSet rs = new ResultSet(
                Common.Ordinal,
                Common.Ordinal,
                new Mock<IServiceBufferFileStreamFactory>().Object);
            Assert.Throws<InvalidOperationException>(() => rs.SaveAs(
                new SaveResultsRequestParams(),
                new Mock<ISaveAsFileStreamFactory>().Object,
                null,
                null));
        }

        [Test]
        public void SaveAs_FailedExistingTaskInProgress()
        {
            // Setup:
            // ... Create a result set that has been executed
            ResultSet rs = new ResultSet(
                Common.Ordinal,
                Common.Ordinal,
                MemoryFileSystem.GetServiceBufferFileStreamFactory());

            // ... Insert a non-started task into the save as tasks
            rs.SaveTasks.AddOrUpdate(Constants.OwnerUri, new Task(() => { }), (_, _) => null);

            // ... Get a mock writer and factory factory
            var mockFactory = GetMockFactory(GetMockWriter().Object, null);

            // If: I attempt to save results with the same name as the non-completed task
            // Then: I should get an invalid operation exception
            var requestParams = new SaveResultsRequestParams {FilePath = Constants.OwnerUri};
            Assert.Throws<InvalidOperationException>(() => rs.SaveAs(requestParams, mockFactory, null, null));
        }

        [Test]
        public async Task SaveAs_WithoutRowSelection()
        {
            // Setup:
            // ... Create a mock reader/writer for reading the result
            var resultFactory = MemoryFileSystem.GetServiceBufferFileStreamFactory();

            // ... Create a result set with dummy data and read to the end
            ResultSet rs = new ResultSet(Common.Ordinal, Common.Ordinal, resultFactory);
            await rs.ReadResultToEnd(GetReader(Common.StandardTestDataSet, Constants.StandardQuery), CancellationToken.None);

            // ... Create a mock writer for writing the save as file
            var saveWriter = GetMockWriter();
            var saveFactory = GetMockFactory(saveWriter.Object, resultFactory.GetReader);

            // If:
            // ... I attempt to save results and await completion
            var requestParams = new SaveResultsRequestParams {FilePath = Constants.OwnerUri};
            rs.SaveAs(requestParams, saveFactory, null, null);

            // ... Make sure the save task was recorded and await its completion
            Assert.True(rs.SaveTasks.ContainsKey(Constants.OwnerUri));
            await rs.SaveTasks[Constants.OwnerUri];

            // Then:
            // ... The task should have completed successfully
            Assert.AreEqual(TaskStatus.RanToCompletion, rs.SaveTasks[Constants.OwnerUri].Status);

            // ... All the rows should have been written successfully
            saveWriter.Verify(
                w => w.WriteRow(It.IsAny<IList<DbCellValue>>()),
                Times.Exactly(Common.StandardRows));
        }

        [Test]
        public async Task SaveAs_WithRowSelection()
        {
            // Setup:
            // ... Create a mock reader/writer for reading the result
            IServiceBufferFileStreamFactory resultFactory = MemoryFileSystem.GetServiceBufferFileStreamFactory();

            // ... Create a result set with dummy data and read to the end
            ResultSet rs = new ResultSet(Common.Ordinal, Common.Ordinal, resultFactory);
            await rs.ReadResultToEnd(GetReader(Common.StandardTestDataSet, Constants.StandardQuery), CancellationToken.None);

            // ... Create a mock writer for writing the save as file
            var saveWriter = GetMockWriter();
            var saveFactory = GetMockFactory(saveWriter.Object, resultFactory.GetReader);

            // If: I attempt to save results that has a selection made
            var saveParams = new SaveResultsRequestParams
            {
                FilePath = Constants.OwnerUri,
                RowStartIndex = 1,
                RowEndIndex = Common.StandardRows - 2,
                ColumnStartIndex = 0,                       // Column start/end doesn't matter, but are required to be
                ColumnEndIndex = 10                         // considered a "save selection"
            };
            rs.SaveAs(saveParams, saveFactory, null, null);
            Assert.True(rs.SaveTasks.ContainsKey(Constants.OwnerUri));
            await rs.SaveTasks[Constants.OwnerUri];

            // Then:
            // ... The task should have completed successfully
            Assert.AreEqual(TaskStatus.RanToCompletion, rs.SaveTasks[Constants.OwnerUri].Status);

            // ... All the rows should have been written successfully
            saveWriter.Verify(
                w => w.WriteRow(It.IsAny<IList<DbCellValue>>()),
                Times.Exactly((int) (saveParams.RowEndIndex - saveParams.RowStartIndex  + 1)));
        }

        private static Mock<ISaveAsFileStreamWriter> GetMockWriter()
        {
            var mockWriter = new Mock<ISaveAsFileStreamWriter>();
            mockWriter.Setup(w => w.WriteRow(It.IsAny<IList<DbCellValue>>()));
            return mockWriter;
        }

        private static ISaveAsFileStreamFactory GetMockFactory(ISaveAsFileStreamWriter writer, Func<string, IFileStreamReader> readerGenerator)
        {
            var mockFactory = new Mock<ISaveAsFileStreamFactory>();
            mockFactory.Setup(f => f.GetWriter(It.IsAny<string>(), It.IsAny<IReadOnlyList<DbColumnWrapper>>()))
                .Returns(writer);
            mockFactory.Setup(f => f.GetReader(It.IsAny<string>()))
                .Returns(readerGenerator);
            return mockFactory.Object;
        }

        private static DbDataReader GetReader(TestResultSet[] dataSet, string query)
        {
            var info = Common.CreateTestConnectionInfo(dataSet, false, false);
            var connection = info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails), null);
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteReader();
        }
    }
}
