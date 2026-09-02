//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    public class QueryErrorSelectionTests
    {
        [Test]
        public async Task QueryErrorIncludesAbsoluteErrorSelection()
        {
            const string queryText = "SELECT * FROM;";
            var executionSelection = new SelectionData(29, 0, 29, queryText.Length);

            // Execute invalid SQL through a live SqlClient connection so the error is handled by
            // the DbException path rather than by directly invoking the batch message handler.
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;
            IReadOnlyList<ResultMessage> messages = await ExecuteQuery(
                queryText,
                connInfo,
                executionSelection);

            ResultMessage error = messages.Single(message => message.IsError);
            Assert.That(error.ErrorSelection, Is.Not.Null);
            Assert.That(error.ErrorSelection.StartLine, Is.EqualTo(29));
            Assert.That(error.ErrorSelection.StartColumn, Is.Zero);
            Assert.That(error.ErrorSelection.EndLine, Is.EqualTo(29));
            Assert.That(error.ErrorSelection.EndColumn, Is.Zero);
        }

        [Test]
        public async Task StoredProcedureErrorDoesNotIncludeDocumentSelection()
        {
            const string createProcedure =
                "CREATE PROCEDURE #QueryErrorSelectionTest AS\nBEGIN\n    SELECT 1 / 0;\nEND";
            const string executeProcedure = "EXEC #QueryErrorSelectionTest;";
            ConnectionInfo connInfo = LiveConnectionHelper.InitLiveConnectionInfo().ConnectionInfo;

            IReadOnlyList<ResultMessage> createMessages = await ExecuteQuery(
                createProcedure,
                connInfo);
            Assert.That(createMessages.Any(message => message.IsError), Is.False);

            IReadOnlyList<ResultMessage> executeMessages = await ExecuteQuery(
                executeProcedure,
                connInfo,
                new SelectionData(29, 0, 29, executeProcedure.Length));

            ResultMessage error = executeMessages.Single(message => message.IsError);
            Assert.That(error.ErrorSelection, Is.Null);
        }

        private static async Task<IReadOnlyList<ResultMessage>> ExecuteQuery(
            string queryText,
            ConnectionInfo connInfo,
            SelectionData executionSelection = null)
        {
            var query = new Query(
                queryText,
                connInfo,
                new QueryExecutionSettings(),
                MemoryFileSystem.GetFileStreamFactory(),
                executionSelection: executionSelection);
            var messages = new List<ResultMessage>();
            foreach (Batch batch in query.Batches)
            {
                batch.BatchMessageSent += message =>
                {
                    messages.Add(message);
                    return Task.CompletedTask;
                };
            }

            query.Execute();
            await query.ExecutionTask;
            return messages;
        }
    }
}
