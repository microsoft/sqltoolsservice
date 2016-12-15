//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class BatchTests
    {
        [Fact]
        public void BatchCreationTest()
        {
            // If I create a new batch...
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory(null));

            // Then: 
            // ... The text of the batch should be stored
            Assert.NotEmpty(batch.BatchText);

            // ... It should not have executed and no error
            Assert.False(batch.HasExecuted, "The query should not have executed.");

            // ... The results should be empty
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... The start line of the batch should be 0
            Assert.Equal(0, batch.Selection.StartLine);

            // ... It's ordinal ID should be what I set it to
            Assert.Equal(Common.Ordinal, batch.Id);

            // ... The summary should have the same info
            Assert.Equal(Common.Ordinal, batch.Summary.Id);
            Assert.Null(batch.Summary.ResultSetSummaries);
            Assert.Equal(0, batch.Summary.Selection.StartLine);
            Assert.NotEqual(default(DateTime).ToString("o"), batch.Summary.ExecutionStart); // Should have been set at construction
            Assert.Null(batch.Summary.ExecutionEnd);
            Assert.Null(batch.Summary.ExecutionElapsed);
        }

        [Fact]
        public async Task BatchExecuteNoResultSets()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            int resultSetCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // If I execute a query that should get no result sets
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                (b,m) => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(Common.CreateTestConnectionInfo(null, false)), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.Equal(1, batchStartCalls);
            Assert.Equal(1, batchEndCalls);
            Assert.Equal(0, resultSetCalls);

            // ... The batch and the summary should be correctly assigned
            ValidateBatch(batch, 0);
            ValidateBatchSummary(batch);

            // ... There should be a message for all results completing 
            Assert.Equal(1, messages.Count);
            Assert.All(messages, m => Assert.False(m.IsError));
        }

        [Fact]
        public async Task BatchExecuteOneResultSet()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            int resultSetCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // ... Build a data set to return
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false);

            // If I execute a query that should get one result set
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                (b,m) => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.Equal(1, batchStartCalls);
            Assert.Equal(1, batchEndCalls);
            Assert.Equal(1, resultSetCalls);

            // ... There should be exactly one result set
            ValidateBatch(batch, resultSets);
            ValidateBatchSummary(batch);

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, messages.Count);
            Assert.All(messages, m => Assert.False(m.IsError));
        }

        [Fact]
        public async Task BatchExecuteTwoResultSets()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            int resultSetCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // ... Build a data set to return
            const int resultSets = 2;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false);

            // If I execute a query that should get two result sets
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                (b,m) => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.Equal(1, batchStartCalls);
            Assert.Equal(1, batchEndCalls);
            Assert.Equal(2, resultSetCalls);

            // ... It should have executed without error
            ValidateBatch(batch, resultSets);
            ValidateBatchSummary(batch);

            // ... There should have been two messages, none with error
            Assert.Equal(1, messages.Count);
            Assert.All(messages, m => {Assert.False(m.IsError);});
        }

        [Fact]
        public async Task BatchExecuteInvalidQuery()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();



            // If I execute a batch that is invalid
            var ci = Common.CreateTestConnectionInfo(null, true);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                (b, m) => messages.Add(m),
                r => { throw new Exception("ResultSet callback was called when it should not have been."); });
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.Equal(1, batchStartCalls);
            Assert.Equal(1, batchEndCalls);

            // ... It should have executed without error
            ValidateBatch(batch, 0);
            ValidateBatchSummary(batch);

            // ... There should be plenty of messages for the error
            Assert.NotEmpty(messages);
            Assert.True(messages.Any(m => m.IsError));
        }

        [Fact]
        public async Task BatchExecuteExecuted()
        {
            // Setup: Build a data set to return
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false);

            // If I execute a batch
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Batch batch = new Batch(Common.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception
            BatchCallbackHelper(batch,
                b => { throw new Exception("Batch start callback should not have been called"); },
                b => { throw new Exception("Batch completion callback should not have been called"); },
                (b, m) => { throw new Exception("Message callback should not have been called"); },
                null);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => batch.Execute(GetConnection(ci), CancellationToken.None));

            // ... The data should still be available without error
            ValidateBatch(batch, resultSets);
            ValidateBatchSummary(batch);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void BatchExecuteNoSql(string query)
        {
            // If:
            // ... I create a batch that has an empty query
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentException>(() => new Batch(query, Common.SubsectionDocument, Common.Ordinal, Common.GetFileStreamFactory(null)));
        }

        [Fact]
        public void BatchNoBufferFactory()
        {
            // If:
            // ... I create a batch that has no file stream factory
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Batch("stuff", Common.SubsectionDocument, Common.Ordinal, null));
        }

        [Fact]
        public void BatchInvalidOrdinal()
        {
            // If:
            // ... I create a batch has has an ordinal less than 0
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => new Batch("stuff", Common.SubsectionDocument, -1, Common.GetFileStreamFactory(null)));
        }

        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }

        private static void ValidateBatch(Batch batch, int expectedResultSets)
        {
            // The batch should be executed
            Assert.True(batch.HasExecuted, "The query should have been marked executed.");

            // Result set list should never be null
            Assert.NotNull(batch.ResultSets);
            Assert.NotNull(batch.ResultSummaries);

            // Make sure the number of result sets matches
            Assert.Equal(expectedResultSets, batch.ResultSets.Count);
            Assert.Equal(expectedResultSets, batch.ResultSummaries.Length);
        }

        private static void ValidateBatchSummary(Batch batch)
        {
            BatchSummary batchSummary = batch.Summary;

            Assert.NotNull(batchSummary);
            Assert.Equal(batch.Id, batchSummary.Id);
            Assert.Equal(batch.ResultSets.Count, batchSummary.ResultSetSummaries.Length);
            Assert.Equal(batch.Selection, batchSummary.Selection);

            // Something other than default date is provided for start and end times
            Assert.True(DateTime.Parse(batchSummary.ExecutionStart) > default(DateTime));   
            Assert.True(DateTime.Parse(batchSummary.ExecutionEnd) > default(DateTime));
            Assert.NotNull(batchSummary.ExecutionElapsed);
        }

        private static void BatchCallbackHelper(Batch batch, Action<Batch> startCallback, Action<Batch> endCallback,
            Action<Batch, ResultMessage> messageCallback, Action<ResultSet> resultCallback)
        {
            // Setup the callback for batch start
            batch.BatchStart += b =>
            {
                startCallback?.Invoke(b);
                return Task.FromResult(0);
            };

            // Setup the callback for batch completion
            batch.BatchCompletion += b =>
            {
                endCallback?.Invoke(b);
                return Task.FromResult(0);
            };

            // Setup the callback for batch messages
            batch.BatchMessage += (b, m) =>
            {
                messageCallback?.Invoke(b, m);
                return Task.FromResult(0);
            };

            // Setup the result set completion callback
            batch.ResultSetCompletion += r =>
            {
                resultCallback?.Invoke(r);
                return Task.FromResult(0);
            };
        }
    }
}
