//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class QueryTests
    {

        [Fact]
        public void QueryExecuteNoQueryText()
        {
            // If:
            // ... I create a query that has a null query text
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentException>(() =>
                new Query(null, Common.CreateTestConnectionInfo(null, false), new QueryExecutionSettings(), Common.GetFileStreamFactory(null)));
        }

        [Fact]
        public void QueryExecuteNoConnectionInfo()
        {
            // If:
            // ... I create a query that has a null connection info
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Query("Some Query", null, new QueryExecutionSettings(), Common.GetFileStreamFactory(null)));
        }

        [Fact]
        public void QueryExecuteNoSettings()
        {
            // If:
            // ... I create a query that has a null settings
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() =>
                new Query("Some query", Common.CreateTestConnectionInfo(null, false), null, Common.GetFileStreamFactory(null)));
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
            // ... Create a callback for batch completion
            int batchCallbacksReceived = 0;
            Batch.BatchAsyncEventHandler batchCallback = summary =>
            {
                batchCallbacksReceived++;
                return Task.CompletedTask;
            };

            // If:
            // ... I create a query from a single batch (without separator)
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(Common.StandardQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            query.BatchCompleted += batchCallback;

            // Then:
            // ... I should get a single batch to execute that hasn't been executed
            Assert.NotEmpty(query.QueryText);
            Assert.NotEmpty(query.Batches);
            Assert.Equal(1, query.Batches.Length);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);

            // If:
            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... The query should have completed successfully with one batch summary returned
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);

            // ... The batch callback should have been called precisely 1 time
            Assert.Equal(1, batchCallbacksReceived);
        }

        [Fact]
        public void QueryExecuteNoOpBatch()
        {
            // Setup:
            // ... Create a callback for batch completion
            Batch.BatchAsyncEventHandler batchCallback = summary =>
            {
                throw new Exception("Batch completion callback was called");
            };

            // If:
            // ... I create a query from a single batch that does nothing
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(Common.NoOpQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            query.BatchCompleted += batchCallback;

            // Then:
            // ... I should get no batches back
            Assert.NotEmpty(query.QueryText);
            Assert.Empty(query.Batches);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);

            // If:
            // ... I Then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... The query should have completed successfully with no batch summaries returned
            Assert.True(query.HasExecuted);
            Assert.Empty(query.BatchSummaries);
        }

        [Fact]
        public void QueryExecuteMultipleBatches()
        {
            // Setup:
            // ... Create a callback for batch completion
            int batchCallbacksReceived = 0;
            Batch.BatchAsyncEventHandler batchCallback = summary =>
            {
                batchCallbacksReceived++;
                return Task.CompletedTask;
            };

            // If:
            // ... I create a query from two batches (with separator)
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            string queryText = string.Format("{0}\r\nGO\r\n{0}", Common.StandardQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(queryText, ci, new QueryExecutionSettings(), fileStreamFactory);
            query.BatchCompleted += batchCallback;

            // Then:
            // ... I should get back two batches to execute that haven't been executed
            Assert.NotEmpty(query.QueryText);
            Assert.NotEmpty(query.Batches);
            Assert.Equal(2, query.Batches.Length);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);

            // If:
            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... The query should have completed successfully with two batch summaries returned
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(2, query.BatchSummaries.Length);

            // ... The batch callback should have been called precisely 2 times
            Assert.Equal(2, batchCallbacksReceived);
        }

        [Fact]
        public void QueryExecuteMultipleBatchesWithNoOp()
        {
            // Setup:
            // ... Create a callback for batch completion
            int batchCallbacksReceived = 0;
            Batch.BatchAsyncEventHandler batchCallback = summary =>
            {
                batchCallbacksReceived++;
                return Task.CompletedTask;
            };

            // If:
            // ... I create a query from a two batches (with separator)
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false);
            string queryText = string.Format("{0}\r\nGO\r\n{1}", Common.StandardQuery, Common.NoOpQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(queryText, ci, new QueryExecutionSettings(), fileStreamFactory);
            query.BatchCompleted += batchCallback;

            // Then:
            // ... I should get back one batch to execute that hasn't been executed
            Assert.NotEmpty(query.QueryText);
            Assert.NotEmpty(query.Batches);
            Assert.Equal(1, query.Batches.Length);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);

            // If:
            // .. I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // ... The query should have completed successfully with one batch summary returned
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);

            // ... The batch callback should have been called precisely 1 time
            Assert.Equal(1, batchCallbacksReceived);
        }

        [Fact]
        public void QueryExecuteInvalidBatch()
        {
            // Setup:
            // ... Create a callback for batch completion
            int batchCallbacksReceived = 0;
            Batch.BatchAsyncEventHandler batchCallback = summary =>
            {
                batchCallbacksReceived++;
                return Task.CompletedTask;
            };

            // If:
            // ... I create a query from an invalid batch
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(Common.InvalidQuery, ci, new QueryExecutionSettings(), fileStreamFactory);
            query.BatchCompleted += batchCallback;

            // Then:
            // ... I should get back a query with one batch not executed
            Assert.NotEmpty(query.QueryText);
            Assert.NotEmpty(query.Batches);
            Assert.Equal(1, query.Batches.Length);
            Assert.False(query.HasExecuted);
            Assert.Throws<InvalidOperationException>(() => query.BatchSummaries);

            // If:
            // ... I then execute the query
            query.Execute();
            query.ExecutionTask.Wait();

            // Then:
            // ... There should be an error on the batch
            Assert.True(query.HasExecuted);
            Assert.NotEmpty(query.BatchSummaries);
            Assert.Equal(1, query.BatchSummaries.Length);
            Assert.True(query.BatchSummaries[0].HasError);
            Assert.NotEmpty(query.BatchSummaries[0].Messages);

            // ... The batch callback should have been called once
            Assert.Equal(1, batchCallbacksReceived);
        }

    }
}
