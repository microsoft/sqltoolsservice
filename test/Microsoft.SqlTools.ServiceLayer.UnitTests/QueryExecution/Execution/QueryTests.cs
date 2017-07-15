//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    public class QueryTests
    {

        [Fact]
        public void QueryCreationCorrect()
        {
            // If:
            // ... I create a query
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Constants.StandardQuery, ci, new QueryExecutionSettings(), fileStreamFactory);

            // Then:
            // ... I should get back two batches to execute that haven't been executed
            Assert.NotEmpty(query.QueryText);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);
        }

        [Fact]
        public void QueryExecuteNoConnectionInfo()
        {
            // If:
            // ... I create a query that has a null connection info
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Query("Some Query", null, new QueryExecutionSettings(), MemoryFileSystem.GetFileStreamFactory()));
        }

        [Fact]
        public void QueryExecuteNoSettings()
        {
            // If:
            // ... I create a query that has a null settings
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() =>
                new Query("Some query", Common.CreateTestConnectionInfo(null, false), null, MemoryFileSystem.GetFileStreamFactory()));
        }

        [Fact]
        public void QueryExecuteNoBufferFactory()
        {
            // If:
            // ... I create a query that has a null file stream factory
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() =>
                new Query("Some query", Common.CreateTestConnectionInfo(null, false), new QueryExecutionSettings(), null));
        }

        [Fact]
        public void QueryExecuteSingleBatch()
        {
            // Setup:
            // ... Keep track of how many times the callbacks were called
            int batchStartCallbacksReceived = 0;
            int batchCompleteCallbacksReceived = 0;
            int batchMessageCallbacksReceived = 0;

            // If:
            // ... I create a query from a single batch (without separator)
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Constants.StandardQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query,
                b => batchStartCallbacksReceived++,
                b => batchCompleteCallbacksReceived++,
                m => batchMessageCallbacksReceived++);

            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... There should be exactly 1 batch
            Assert.NotEmpty(query.Batches);
            Assert.Equal(1, query.Batches.Length);

            // ... The query should have completed successfully with one batch summary returned
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);

            // ... The batch callbacks should have been called precisely 1 time
            Assert.Equal(1, batchStartCallbacksReceived);
            Assert.Equal(1, batchCompleteCallbacksReceived);
            Assert.Equal(1, batchMessageCallbacksReceived);
        }

        [Fact]
        public async Task QueryExecuteSingleNoOpBatch()
        {
            // Setup: Keep track of all the messages received
            List<ResultMessage> messages = new List<ResultMessage>();

            // If:
            // ... I create a query from a single batch that does nothing
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Common.NoOpQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query,
                b => { throw new Exception("Batch startup callback should not have been called."); },
                b => { throw new Exception("Batch completion callback was called"); },
                m => messages.Add(m));

            // If:
            // ... I Then execute the query
            query.Execute();
            await query.ExecutionTask;

            // Then:
            // ... There should be no batches
            Assert.Equal(1, query.Batches.Length);

            // ... The query shouldn't have completed successfully
            Assert.False(query.HasExecuted);

            // ... The message callback should have been called 0 times
            Assert.Equal(0, messages.Count);
        }

        [Fact]
        public void QueryExecuteMultipleResultBatches()
        {
            // Setup:
            // ... Keep track of how many callbacks are received
            int batchStartCallbacksReceived = 0;
            int batchCompletedCallbacksReceived = 0;
            int batchMessageCallbacksReceived = 0;

            // If:
            // ... I create a query from two batches (with separator)
            ConnectionInfo ci = Common.CreateConnectedConnectionInfo(null, false);

            string queryText = string.Format("{0}\r\nGO\r\n{0}", Constants.StandardQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(queryText, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query,
                b => batchStartCallbacksReceived++,
                b => batchCompletedCallbacksReceived++,
                m => batchMessageCallbacksReceived++);

            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... I should get back a query with one batch (no op batch is not included)
            Assert.NotEmpty(query.Batches);
            Assert.Equal(2, query.Batches.Length);

            // ... The query should have completed successfully with two batch summaries returned
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(2, query.BatchSummaries.Length);

            // ... The batch start, complete, and message callbacks should have been called precisely 2 times
            Assert.Equal(2, batchStartCallbacksReceived);
            Assert.Equal(2, batchCompletedCallbacksReceived);
            Assert.Equal(2, batchMessageCallbacksReceived);
        }

        [Fact]
        public async Task QueryExecuteMultipleBatchesWithNoOp()
        {
            // Setup:
            // ... Keep track of how many times callbacks are called
            int batchStartCallbacksReceived = 0;
            int batchCompletionCallbacksReceived = 0;
            int batchMessageCallbacksReceived = 0;

            // If:
            // ... I create a query from a two batches (with separator)
            ConnectionInfo ci = Common.CreateConnectedConnectionInfo(null, false);
            string queryText = string.Format("{0}\r\nGO\r\n{1}", Constants.StandardQuery, Common.NoOpQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(queryText, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query, 
                b => batchStartCallbacksReceived++,
                b => batchCompletionCallbacksReceived++,
                m => batchMessageCallbacksReceived++);

            // .. I then execute the query
            query.Execute();
            await query.ExecutionTask;

            // Then:
            // ... I should get back a query with two batches
            Assert.NotEmpty(query.Batches);
            Assert.Equal(2, query.Batches.Length);

            // ... The query should have completed successfully
            Assert.True(query.HasExecuted);

            // ... The batch callbacks should have been called 2 times (for each no op batch)
            Assert.Equal(2, batchStartCallbacksReceived);
            Assert.Equal(2, batchCompletionCallbacksReceived);
            Assert.Equal(2, batchMessageCallbacksReceived);
        }

        [Fact]
        public async Task QueryExecuteMultipleNoOpBatches()
        {
            // Setup:
            // ... Keep track of how many messages were sent
            List<ResultMessage> messages = new List<ResultMessage>();

            // If:
            // ... I create a query from a two batches (with separator)
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            string queryText = string.Format("{0}\r\nGO\r\n{1}", Common.NoOpQuery, Common.NoOpQuery);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(queryText, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query,
                b => { throw new Exception("Batch start handler was called"); },
                b => { throw new Exception("Batch completed handler was called"); },
                m => messages.Add(m));

            // .. I then execute the query
            query.Execute();
            await query.ExecutionTask;

            // Then:
            // ... I should get back a query with no batches
            Assert.Equal(2, query.Batches.Length);

            // ... The query shouldn't have completed successfully
            Assert.False(query.HasExecuted);

            // ... The message callback should have been called exactly once
            Assert.Equal(0, messages.Count);
        }

        [Fact]
        public void QueryExecuteInvalidBatch()
        {
            // Setup:
            // ... Keep track of how many times a method is called
            int batchStartCallbacksReceived = 0;
            int batchCompletionCallbacksReceived = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // If:
            // ... I create a query from an invalid batch
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;

            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Common.InvalidQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            BatchCallbackHelper(query,
                b => batchStartCallbacksReceived++,
                b => batchCompletionCallbacksReceived++,
                m => messages.Add(m));

            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... I should get back a query with one batch
            Assert.NotEmpty(query.Batches);
            Assert.Equal(1, query.Batches.Length);

            // ... There should be an error on the batch
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);
            Assert.True(messages.Any(m => m.IsError));

            // ... The batch callbacks should have been called once
            Assert.Equal(1, batchStartCallbacksReceived);
            Assert.Equal(1, batchCompletionCallbacksReceived);
        }

        private static void BatchCallbackHelper(Query q, Action<Batch> startCallback, Action<Batch> endCallback,
            Action<ResultMessage> messageCallback)
        {
            // Setup the callback for batch start
            q.BatchStarted += b =>
            {
                startCallback?.Invoke(b);
                return Task.FromResult(0);
            };

            // Setup the callback for batch completion
            q.BatchCompleted += b =>
            {
                endCallback?.Invoke(b);
                return Task.FromResult(0);
            };

            // Setup the callback for batch messages
            q.BatchMessageSent += (m) =>
            {
                messageCallback?.Invoke(m);
                return Task.FromResult(0);
            };
        }
    }
}
