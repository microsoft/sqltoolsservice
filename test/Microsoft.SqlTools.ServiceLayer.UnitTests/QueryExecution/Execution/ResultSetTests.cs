// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    public class ResultSetTests
    {
        [Fact]
        public void ResultCreation()
        {
            // If:
            // ... I create a new result set with a valid db data reader
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());

            // Then:
            // ... There should not be any data read yet
            Assert.Null(resultSet.Columns);
            Assert.Equal(0, resultSet.RowCount);
            Assert.Equal(Common.Ordinal, resultSet.Id);

            // ... The summary should include the same info
            Assert.Null(resultSet.Summary.ColumnInfo);
            Assert.Equal(0, resultSet.Summary.RowCount);
            Assert.Equal(Common.Ordinal, resultSet.Summary.Id);
            Assert.Equal(Common.Ordinal, resultSet.Summary.BatchId);
        }

        [Fact]
        public async Task ReadToEndNullReader()
        {
            // If: I create a new result set with a null db data reader
            // Then: I should get an exception
            var fsf = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fsf);
            await Assert.ThrowsAsync<ArgumentNullException>(() => resultSet.ReadResultToEnd(null, CancellationToken.None));
        }

        /// <summary>
        /// Read to End test
        /// </summary>
        [Fact]
        public void ReadToEndSuccess()
        {
            // Setup: Create a results Available callback for result set
            //
            ResultSetSummary resultSummaryFromAvailableCallback = null;

            Task AvailableCallback(ResultSet r)
            {
                Debug.WriteLine($"available result notification sent, result summary was: {r.Summary}");
                resultSummaryFromAvailableCallback = r.Summary;
                return Task.CompletedTask;
            }

            // Setup: Create a results updated callback for result set
            //
            List<ResultSetSummary> resultSummariesFromUpdatedCallback = new List<ResultSetSummary>();

            Task UpdatedCallback(ResultSet r)
            {
                Debug.WriteLine($"updated result notification sent, result summary was: {r.Summary}");
                resultSummariesFromUpdatedCallback.Add(r.Summary);
                return Task.CompletedTask;
            }

            // Setup: Create a  results complete callback for result set
            //
            ResultSetSummary resultSummaryFromCompleteCallback = null;
            Task CompleteCallback(ResultSet r)
            {
                Debug.WriteLine($"Completed result notification sent, result summary was: {r.Summary}");
                resultSummaryFromCompleteCallback = r.Summary;
                return Task.CompletedTask;
            }

            // If:
            // ... I create a new resultset with a valid db data reader that has data
            // ... and I read it to the end
            DbDataReader mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);
            resultSet.ResultAvailable += AvailableCallback;
            resultSet.ResultUpdated += UpdatedCallback;
            resultSet.ResultCompletion += CompleteCallback;
            resultSet.ReadResultToEnd(mockReader, CancellationToken.None).Wait();

            Thread.Yield();
            resultSet.ResultAvailable -= AvailableCallback;
            resultSet.ResultUpdated -= UpdatedCallback;
            resultSet.ResultCompletion -= CompleteCallback;

            // Then:
            // ... The columns should be set
            // ... There should be rows to read back
            Assert.NotNull(resultSet.Columns);
            Assert.Equal(Common.StandardColumns, resultSet.Columns.Length);
            Assert.Equal(Common.StandardRows, resultSet.RowCount);

            // ... The summary should have the same info
            Assert.NotNull(resultSet.Summary.ColumnInfo);
            Assert.Equal(Common.StandardColumns, resultSet.Summary.ColumnInfo.Length);
            Assert.Equal(Common.StandardRows, resultSet.Summary.RowCount);

            // and:
            //
            VerifyReadResultToEnd(resultSet, resultSummaryFromAvailableCallback, resultSummaryFromCompleteCallback, resultSummariesFromUpdatedCallback);
        }

        [Theory]
        [MemberData(nameof(CallMethodWithoutReadingData))]
        public void CallMethodWithoutReading(Action<ResultSet> testMethod)
        {
            // Setup: Create a new result set with valid db data reader
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);

            // If: 
            // ... I have a result set that has not been read
            // ... and I attempt to call a method on it
            // Then: It should throw an exception
            Assert.ThrowsAny<Exception>(() => testMethod(resultSet));
        }

        public static IEnumerable<object> CallMethodWithoutReadingData
        {
            get
            {
                yield return new object[] {new Action<ResultSet>(rs => rs.GetSubset(0, 0).Wait())};
                yield return new object[] {new Action<ResultSet>(rs => rs.UpdateRow(0, null).Wait())};
                yield return new object[] {new Action<ResultSet>(rs => rs.AddRow(null).Wait())};
                yield return new object[] {new Action<ResultSet>(rs => rs.RemoveRow(0))};
                yield return new object[] {new Action<ResultSet>(rs => rs.GetRow(0))};
                yield return new object[] {new Action<ResultSet>(rs => rs.GetExecutionPlan().Wait())};
            }
        }

        void VerifyReadResultToEnd(ResultSet resultSet, ResultSetSummary resultSummaryFromAvailableCallback, ResultSetSummary resultSummaryFromCompleteCallback, List<ResultSetSummary> resultSummariesFromUpdatedCallback)
        {
            // ... The callback for result set available, update and completion callbacks should have been fired.
            //
            Assert.True(null != resultSummaryFromCompleteCallback, "completeResultSummary is null" + $"\r\n\t\tupdateResultSets: {string.Join("\r\n\t\t\t", resultSummariesFromUpdatedCallback)}");
            Assert.True(null != resultSummaryFromAvailableCallback, "availableResultSummary is null" + $"\r\n\t\tupdateResultSets: {string.Join("\r\n\t\t\t", resultSummariesFromUpdatedCallback)}");

            // insert availableResult at the top of the resultSummariesFromUpdatedCallback list as the available result set is the first update in that series.
            //
            resultSummariesFromUpdatedCallback.Insert(0, resultSummaryFromAvailableCallback);

            // ... The no of rows in available result set should be non-zero
            //
            // Assert.True(0 != resultSummaryFromAvailableCallback.RowCount, "availableResultSet RowCount is 0");

            // ... The final updateResultSet must have 'Complete' flag set to true
            //
            Assert.True(resultSummariesFromUpdatedCallback.Last().Complete,
                $"Complete Check failed.\r\n\t\t resultSummariesFromUpdatedCallback:{string.Join("\r\n\t\t\t", resultSummariesFromUpdatedCallback)}");

            // ... The no of rows in the final updateResultSet/AvailableResultSet should be equal to that in the Complete Result Set. 
            //
            Assert.True(resultSummaryFromCompleteCallback.RowCount == resultSummariesFromUpdatedCallback.Last().RowCount,
                 $"The row counts of the complete Result Set and Final update result set do not match"
                +$"\r\n\t\tcompleteResultSet: {resultSummaryFromCompleteCallback}"
                +$"\r\n\t\tupdateResultSets: {string.Join("\r\n\t\t\t", resultSummariesFromUpdatedCallback)}"
            );

            // ... RowCount should be in increasing order in updateResultSet callbacks
            //
            Parallel.ForEach(Partitioner.Create(0, resultSummariesFromUpdatedCallback.Count), (range) =>
            {
                int start = range.Item1 == 0 ? 1 : range.Item1;
                for (int i = start; i < range.Item2; i++)
                {
                    Assert.True(resultSummariesFromUpdatedCallback[i].RowCount >= resultSummariesFromUpdatedCallback[i - 1].RowCount,
                        $"Row Count of {i}th updateResultSet was smaller than that of the previous one"
                      + $"\r\n\t\tupdateResultSets: {string.Join("\r\n\t\t\t", resultSummariesFromUpdatedCallback)}"
                    );
                }
            });
        }

        /// <summary>
        /// Read to End Xml/JSon test
        /// </summary>
        /// <param name="forType"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("JSON")]
        [InlineData("XML")]
        public async Task ReadToEndForXmlJson(string forType)
        {
            // Setup:
            // ... Build a FOR XML or FOR JSON data set
            //
            DbColumn[] columns = {new TestDbColumn(string.Format("{0}_F52E2B61-18A1-11d1-B105-00805F49916B", forType))};
            object[][] rows = Enumerable.Repeat(new object[] {"test data"}, Common.StandardRows).ToArray();
            TestResultSet[] dataSets = {new TestResultSet(columns, rows) };

            // Setup: Create a results Available callback for result set
            //
            ResultSetSummary resultSummaryFromAvailableCallback = null;
            Task AvailableCallback(ResultSet r)
            {
                Debug.WriteLine($"available result notification sent, result summary was: {r.Summary}");
                resultSummaryFromAvailableCallback = r.Summary;
                return Task.CompletedTask;
            }


            // Setup: Create a results updated callback for result set
            //
            List<ResultSetSummary> resultSummariesFromUpdatedCallback = new List<ResultSetSummary>();

            Task UpdatedCallback(ResultSet r)
            {
                Debug.WriteLine($"updated result notification sent, result summary was: {r.Summary}");
                resultSummariesFromUpdatedCallback.Add(r.Summary);
                return Task.CompletedTask;
            }

            // Setup: Create a  results complete callback for result set
            //
            ResultSetSummary resultSummaryFromCompleteCallback = null;
            Task CompleteCallback(ResultSet r)
            {
                Debug.WriteLine($"Completed result notification sent, result summary was: {r.Summary}");
                Assert.True(r.Summary.Complete);
                resultSummaryFromCompleteCallback = r.Summary;
                return Task.CompletedTask;
            }

            // If:
            // ... I create a new result set with a valid db data reader that is FOR XML/JSON
            // ... and I read it to the end
            //
            DbDataReader mockReader = GetReader(dataSets, false, Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);
            resultSet.ResultAvailable += AvailableCallback;
            resultSet.ResultUpdated += UpdatedCallback;
            resultSet.ResultCompletion += CompleteCallback;
            var readResultTask = resultSet.ReadResultToEnd(mockReader, CancellationToken.None);
            await readResultTask;
            Debug.AutoFlush = true;
            Debug.Assert(readResultTask.IsCompletedSuccessfully, $"readResultTask did not Complete Successfully. Status: {readResultTask.Status}");
            Thread.Yield();
            resultSet.ResultAvailable -= AvailableCallback;
            resultSet.ResultUpdated -= UpdatedCallback;
            resultSet.ResultCompletion -= CompleteCallback;
            // Then:
            // ... There should only be one column
            // ... There should only be one row
            //
            Assert.Equal(1, resultSet.Columns.Length);
            Assert.Equal(1, resultSet.RowCount);


            // and:
            //
            VerifyReadResultToEnd(resultSet, resultSummaryFromAvailableCallback, resultSummaryFromCompleteCallback, resultSummariesFromUpdatedCallback);

            // If:
            // ... I attempt to read back the results
            // Then: 
            // ... I should only get one row
            //
            var task = resultSet.GetSubset(0, 10);
            task.Wait();
            var subset = task.Result;
            Assert.Equal(1, subset.RowCount);
        }

        [Theory]
        [InlineData(-1, 0)] // Too small start row
        [InlineData(20, 0)] // Too large start row
        [InlineData(0, -1)] // Negative row count
        public async Task GetSubsetInvalidParameters(int startRow, int rowCount)
        {
            // If:
            // ... I create a new result set with a valid db data reader
            // ... And execute the result
            DbDataReader mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... And attempt to get a subset with invalid parameters
            // Then:
            // ... It should throw an exception for an invalid parameter
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => resultSet.GetSubset(startRow, rowCount));
        }

        [Theory]
        [InlineData(0, 3)]     // Standard scenario, 3 rows should come back
        [InlineData(0, 20)]    // Asking for too many rows, 5 rows should come back
        [InlineData(1, 3)]     //  Asking for proper subset of rows from non-zero start
        [InlineData(1, 20)]    // Asking for too many rows at a non-zero start
        public async Task GetSubsetSuccess(int startRow, int rowCount)
        {
            // If:
            // ... I create a new result set with a valid db data reader
            // ... And execute the result set
            DbDataReader mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... And attempt to get a subset with valid number of rows
            ResultSetSubset subset = await resultSet.GetSubset(startRow, rowCount);

            // Then:
            // ... rows sub-array and RowCount field of the subset should match
            Assert.Equal(subset.RowCount, subset.Rows.Length);

            // Then:
            // ... There should be rows in the subset, either the number of rows or the number of
            //     rows requested or the number of rows in the result set, whichever is lower
            long availableRowsFromStart = resultSet.RowCount - startRow;
            Assert.Equal(Math.Min(availableRowsFromStart, rowCount), subset.RowCount);

            // ... The rows should have the same number of columns as the resultset
            Assert.Equal(resultSet.Columns.Length, subset.Rows[0].Length);
        }

        [Theory]
        [MemberData(nameof(RowInvalidParameterData))]
        public async Task RowInvalidParameter(Action<ResultSet> actionToPerform)
        {
            // If: I create a new result set and execute it
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileStreamFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // Then: Attempting to read an invalid row should fail
            Assert.ThrowsAny<Exception>(() => actionToPerform(resultSet));
        }

        public static IEnumerable<object> RowInvalidParameterData
        {
            get
            {
                foreach (var method in RowInvalidParameterMethods)
                {
                    yield return new object[] {new Action<ResultSet>(rs => method(rs, -1))};
                    yield return new object[] {new Action<ResultSet>(rs => method(rs, 100))};
                }
            }
        }

        public static IEnumerable<Action<ResultSet, long>> RowInvalidParameterMethods
        {
            get
            {
                yield return (rs, id) => rs.RemoveRow(id);
                yield return (rs, id) => rs.GetRow(id);
                yield return (rs, id) => rs.UpdateRow(id, null).Wait();
            }
        }

        [Fact]
        public async Task RemoveRowSuccess()
        {
            // Setup: Create a result set that has the standard data set on it
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // If: I delete a row from the result set
            resultSet.RemoveRow(0);

            // Then:
            // ... The row count should decrease
            // ... The last row should have moved up by 1
            Assert.Equal(Common.StandardRows - 1, resultSet.RowCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => resultSet.GetRow(Common.StandardRows - 1));
        }

        [Fact]
        public async Task AddRowNoRows()
        {
            // Setup: 
            // ... Create a standard result set with standard data
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... Create a mock reader that has no rows
            var emptyReader = GetReader(new[] {new TestResultSet(5, 0)}, false, Constants.StandardQuery);

            // If: I add a row with a reader that has no rows
            // Then: 
            // ... I should get an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => resultSet.AddRow(emptyReader));

            // ... The row count should not have changed
            Assert.Equal(Common.StandardRows, resultSet.RowCount);
        }

        [Fact]
        public async Task AddRowThrowsOnRead()
        {
            // Setup:
            // ... Create a standard result set with standard data
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... Create a mock reader that will throw on read
            var throwingReader = GetReader(new[] {new TestResultSet(5, 0)}, true, Constants.StandardQuery);
            
            // If: I add a row with a reader that throws on read
            // Then:
            // ... I should get an exception
            await Assert.ThrowsAnyAsync<DbException>(() => resultSet.AddRow(throwingReader));
            
            // ... The row count should not have changed
            Assert.Equal(Common.StandardRows, resultSet.RowCount); 
        }

        [Fact]
        public async Task AddRowSuccess()
        {
            // Setup: 
            // ... Create a standard result set with standard data
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... Create a mock reader that has one row
            object[] row = Enumerable.Range(0, Common.StandardColumns).Select(i => "QQQ").ToArray();
            IEnumerable<object[]> rows = new List<object[]>{ row };
            TestResultSet[] results = {new TestResultSet(TestResultSet.GetStandardColumns(Common.StandardColumns), rows)};
            var newRowReader = GetReader(results, false, Constants.StandardQuery);

            // If: I add a new row to the result set
            await resultSet.AddRow(newRowReader);

            // Then:
            // ... There should be a new row in the list of rows
            Assert.Equal(Common.StandardRows + 1, resultSet.RowCount);

            // ... The new row should be readable and all cells contain the test value
            Assert.All(resultSet.GetRow(Common.StandardRows), cell => Assert.Equal("QQQ", cell.RawObject));
        }

        [Fact]
        public async Task UpdateRowNoRows()
        {
            // Setup: 
            // ... Create a standard result set with standard data
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... Create a mock reader that has no rows
            var emptyReader = GetReader(new[] { new TestResultSet(5, 0) }, false, Constants.StandardQuery);

            // If: I add a row with a reader that has no rows
            // Then: 
            // ... I should get an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => resultSet.UpdateRow(0, emptyReader));

            // ... The row count should not have changed
            Assert.Equal(Common.StandardRows, resultSet.RowCount);
        }

        [Fact]
        public async Task UpdateRowSuccess()
        {
            // Setup: 
            // ... Create a standard result set with standard data
            var fileFactory = MemoryFileSystem.GetFileStreamFactory();
            var mockReader = GetReader(Common.StandardTestDataSet, false, Constants.StandardQuery);
            ResultSet resultSet = new ResultSet(Common.Ordinal, Common.Ordinal, fileFactory);
            await resultSet.ReadResultToEnd(mockReader, CancellationToken.None);

            // ... Create a mock reader that has one row
            object[] row = Enumerable.Range(0, Common.StandardColumns).Select(i => "QQQ").ToArray();
            IEnumerable<object[]> rows = new List<object[]> { row };
            TestResultSet[] results = { new TestResultSet(TestResultSet.GetStandardColumns(Common.StandardColumns), rows) };
            var newRowReader = GetReader(results, false, Constants.StandardQuery);

            // If: I add a new row to the result set
            await resultSet.UpdateRow(0, newRowReader);

            // Then:
            // ... There should be the same number of rows
            Assert.Equal(Common.StandardRows, resultSet.RowCount);

            // ... The new row should be readable and all cells contain the test value
            Assert.All(resultSet.GetRow(0), cell => Assert.Equal("QQQ", cell.RawObject));
        }

        private static DbDataReader GetReader(TestResultSet[] dataSet, bool throwOnRead, string query)
        {
            var info = Common.CreateTestConnectionInfo(dataSet, false, throwOnRead);
            var connection = info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails), null);
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteReader();
        }
    }
}
