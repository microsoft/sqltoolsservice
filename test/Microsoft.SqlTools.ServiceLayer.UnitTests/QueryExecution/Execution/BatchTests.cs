﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.Execution
{
    public class BatchTests
    {
        [Test]
        public void BatchCreationTest()
        {
            // If I create a new batch...
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());

            // Then: 
            // ... The text of the batch should be stored
            Assert.That(batch.BatchText, Is.Not.Empty);

            // ... It should not have executed and no error
            Assert.False(batch.HasExecuted, "The query should not have executed.");
            Assert.False(batch.HasError);

            // ... The results should be empty
            Assert.That(batch.ResultSets, Is.Empty);
            Assert.That(batch.ResultSummaries, Is.Empty);

            // ... The start line of the batch should be 0
            Assert.AreEqual(0, batch.Selection.StartLine);

            // ... It's ordinal ID should be what I set it to
            Assert.AreEqual(Common.Ordinal, batch.Id);

            // ... The summary should have the same info
            Assert.AreEqual(Common.Ordinal, batch.Summary.Id);
            Assert.Null(batch.Summary.ResultSetSummaries);
            Assert.AreEqual(0, batch.Summary.Selection.StartLine);
            Assert.That(batch.Summary.ExecutionStart, Is.Not.EqualTo(default(DateTime).ToString("o"))); // Should have been set at construction
            Assert.Null(batch.Summary.ExecutionEnd);
            Assert.Null(batch.Summary.ExecutionElapsed);
        }

        [Test]
        public async Task BatchExecuteNoResultSets()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            int resultSetCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // If I execute a query that should get no result sets
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(Common.CreateTestConnectionInfo(null, false, false)), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);
            Assert.AreEqual(0, resultSetCalls);

            // ... The batch and the summary should be correctly assigned
            ValidateBatch(batch, 0, false);
            ValidateBatchSummary(batch);
            ValidateMessages(batch, 1, messages);
        }

        [Test]
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
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false, false);

            // If I execute a query that should get one result set
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);
            Assert.AreEqual(1, resultSetCalls);

            // ... There should be exactly one result set
            ValidateBatch(batch, resultSets, false);
            ValidateBatchSummary(batch);
            ValidateMessages(batch, 1, messages);
        }

        [Test]
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
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false, false);

            // If I execute a query that should get two result sets
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);
            Assert.AreEqual(2, resultSetCalls);

            // ... It should have executed without error
            ValidateBatch(batch, resultSets, false);
            ValidateBatchSummary(batch);
            ValidateMessages(batch, 1, messages);
        }

        [Test]
        public async Task BatchExecuteMultiExecutions()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            int resultSetCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // ... Build a data set to return
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false, false);

            // If I execute a query that should get one result set, but execute it twice using "GO 2" syntax
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory, 2);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => resultSetCalls++);
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);
            Assert.AreEqual(2, resultSetCalls);

            // ... There should be exactly two result sets
            ValidateBatch(batch, 2, false);
            ValidateBatchSummary(batch);
            // ... And there should be an additional loop start in addition to the batch end message
            ValidateMessages(batch, 2, messages);
        }

        [Test]
        public async Task BatchExecuteInvalidQuery()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // If I execute a batch that is invalid
            var ci = Common.CreateTestConnectionInfo(null, true, false);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => { throw new Exception("ResultSet callback was called when it should not have been."); });
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);

            // ... It should have executed with error
            ValidateBatch(batch, 0, true);
            ValidateBatchSummary(batch);

            Assert.That(messages.Select(m => new { m.IsError, m.BatchId.Value }), Is.EqualTo(new[] { new { IsError = true, Value = batch.Id } }), "There should be one error message returned");
        }

        [Test]
        public async Task BatchExecuteInvalidQueryMultiExecutions()
        {
            // Setup: 
            // ... Keep track of callbacks being called
            int batchStartCalls = 0;
            int batchEndCalls = 0;
            List<ResultMessage> messages = new List<ResultMessage>();

            // If I execute a batch that is invalid, and if "GO 2" is added to execute more than once
            var ci = Common.CreateTestConnectionInfo(null, true, false);
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery + Environment.NewLine, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory, 2);
            BatchCallbackHelper(batch,
                b => batchStartCalls++,
                b => batchEndCalls++,
                m => messages.Add(m),
                r => { throw new Exception("ResultSet callback was called when it should not have been."); });
            await batch.Execute(GetConnection(ci), CancellationToken.None);

            // Then:
            // ... Callbacks should have been called the appropriate number of times
            Assert.AreEqual(1, batchStartCalls);
            Assert.AreEqual(1, batchEndCalls);

            // ... It should have executed with error
            ValidateBatch(batch, 0, true);
            ValidateBatchSummary(batch);

            // ... There should be two error messages returned and 4 info messages (loop start/end, plus 2 for ignoring the error)
            Assert.AreEqual(6, messages.Count);
            Assert.That(messages.Select(m => m.BatchId), Is.EquivalentTo(new[] { batch.Id, batch.Id, batch.Id, batch.Id, batch.Id, batch.Id }));
            Assert.That(messages.Select(m => m.IsError), Has.Exactly(2).True);
        }

        [Test]
        public async Task BatchExecuteExecuted()
        {
            // Setup: Build a data set to return
            const int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(Common.GetTestDataSet(resultSets), false, false);

            // If I execute a batch
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, fileStreamFactory);
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
                m => { throw new Exception("Message callback should not have been called"); },
                null);
            Assert.ThrowsAsync<InvalidOperationException>(
                () => batch.Execute(GetConnection(ci), CancellationToken.None));

            // ... The data should still be available without error
            ValidateBatch(batch, resultSets, false);
            ValidateBatchSummary(batch);
        }

        [Test]
        public void BatchExecuteNoSql([Values(null, "")]  string query)
        {
            // If:
            // ... I create a batch that has an empty query
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentException>(() => new Batch(query, Common.SubsectionDocument, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory()));
        }

        [Test]
        public void BatchNoBufferFactory()
        {
            // If:
            // ... I create a batch that has no file stream factory
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Batch("stuff", Common.SubsectionDocument, Common.Ordinal, null));
        }

        [Test]
        public void BatchInvalidOrdinal()
        {
            // If:
            // ... I create a batch has has an ordinal less than 0
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => new Batch("stuff", Common.SubsectionDocument, -1, MemoryFileSystem.GetFileStreamFactory()));
        }        

        [Test]
        public void StatementCompletedHandlerTest()
        {
            // If:
            // ... I call the StatementCompletedHandler
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());
            int messageCalls = 0;
            batch.BatchMessageSent += args =>
            {
                messageCalls++;
                return Task.FromResult(0);
            };

            // Then:
            // ... The message handler for the batch should havve been called twice
            batch.StatementCompletedHandler(null, new StatementCompletedEventArgs(1));
            Assert.True(messageCalls == 1);
            batch.StatementCompletedHandler(null, new StatementCompletedEventArgs(2));
            Assert.True(messageCalls == 2);
        }

        [Test]
        public async Task ServerMessageHandlerShowsErrorMessages()
        {
            // Set up the batch to track message calls
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());
            int errorMessageCalls = 0;
            int infoMessageCalls = 0;
            string actualMessage = null;
            batch.BatchMessageSent += args =>
            {
                if (args.IsError)
                {
                    errorMessageCalls++;
                }
                else
                {
                    infoMessageCalls++;
                }
                actualMessage = args.Message;
                return Task.CompletedTask;
            };

            // If I call the server message handler with an error message
            var errorMessage = "error message";
            await batch.HandleSqlErrorMessage(1, 15, 0, 1, string.Empty, errorMessage);

            // Then one error message call should be recorded
            Assert.AreEqual(1, errorMessageCalls);
            Assert.AreEqual(0, infoMessageCalls);

            // And the actual message should be a formatted version of the error message
            Assert.True(actualMessage.Length > errorMessage.Length);
        }

        [Test]
        public async Task ServerMessageHandlerShowsInfoMessages()
        {
            // Set up the batch to track message calls
            Batch batch = new Batch(Constants.StandardQuery, Common.SubsectionDocument, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());
            int errorMessageCalls = 0;
            int infoMessageCalls = 0;
            string actualMessage = null;
            batch.BatchMessageSent += args =>
            {
                if (args.IsError)
                {
                    errorMessageCalls++;
                }
                else
                {
                    infoMessageCalls++;
                }
                actualMessage = args.Message;
                return Task.CompletedTask;
            };

            // If I call the server message handler with an info message
            var infoMessage = "info message";
            await batch.HandleSqlErrorMessage(0, 0, 0, 1, string.Empty, infoMessage);

            // Then one info message call should be recorded
            Assert.AreEqual(0, errorMessageCalls);
            Assert.AreEqual(1, infoMessageCalls);

            // And the actual message should be the exact info message
            Assert.AreEqual(infoMessage, actualMessage);
        }
      
        private static DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails), null);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void ValidateBatch(Batch batch, int expectedResultSets, bool isError)
        {
            // The batch should be executed
            Assert.True(batch.HasExecuted, "The query should have been marked executed.");

            // Result set list should never be null
            Assert.NotNull(batch.ResultSets);
            Assert.NotNull(batch.ResultSummaries);

            // Make sure the number of result sets matches
            Assert.AreEqual(expectedResultSets, batch.ResultSets.Count);
            for (int i = 0; i < expectedResultSets; i++)
            {
                Assert.AreEqual(i, batch.ResultSets[i].Id);
            }
            Assert.AreEqual(expectedResultSets, batch.ResultSummaries.Length);

            // Make sure that the error state is set properly
            Assert.AreEqual(isError, batch.HasError);
        }

        private static void ValidateBatchSummary(Batch batch)
        {
            BatchSummary batchSummary = batch.Summary;

            Assert.NotNull(batchSummary);
            Assert.AreEqual(batch.Id, batchSummary.Id);
            Assert.AreEqual(batch.ResultSets.Count, batchSummary.ResultSetSummaries.Length);
            Assert.AreEqual(batch.Selection, batchSummary.Selection);
            Assert.AreEqual(batch.HasError, batchSummary.HasError);

            // Something other than default date is provided for start and end times
            Assert.True(DateTime.Parse(batchSummary.ExecutionStart) > default(DateTime));   
            Assert.True(DateTime.Parse(batchSummary.ExecutionEnd) > default(DateTime));
            Assert.NotNull(batchSummary.ExecutionElapsed);
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void ValidateMessages(Batch batch, int expectedMessages, IList<ResultMessage> messages)
        {
            // There should be equal number of messages to result sets
            Assert.AreEqual(expectedMessages, messages.Count);

            // No messages should be errors
            // All messages must have the batch ID
            Assert.That(messages.Select(m => new { m.IsError, m.BatchId.Value }), Has.All.EqualTo(new { IsError = false, Value = batch.Id }));
        }

        private static void BatchCallbackHelper(Batch batch, Action<Batch> startCallback, Action<Batch> endCallback,
            Action<ResultMessage> messageCallback, Action<ResultSet> resultCallback)
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
            batch.BatchMessageSent += (m) =>
            {
                messageCallback?.Invoke(m);
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
