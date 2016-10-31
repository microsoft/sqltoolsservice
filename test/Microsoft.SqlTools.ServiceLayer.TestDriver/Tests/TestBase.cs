//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Base class for all test suites run by the test driver
    /// </summary>
    public class TestBase : IDisposable
    {
        public TestBase()
        {
            Driver = new ServiceTestDriver();
            Driver.Start().Wait();
        }

        public void Dispose()
        {
            Driver.Stop().Wait();
        }

        /// <summary>
        /// The driver object used to read/write data to the service
        /// </summary>
        public ServiceTestDriver Driver
        {
            get;
            private set;
        }

        private object fileLock = new Object();

        /// <summary>
        /// Request a new connection to be created
        /// </summary>
        /// <returns>True if the connection completed successfully</returns>
        protected async Task<bool> Connect(string ownerUri, ConnectParams connectParams)
        {
            connectParams.OwnerUri = ownerUri;
            var connectResult = await Driver.SendRequest(ConnectionRequest.Type, connectParams);
            if (connectResult)
            {
                var completeEvent = await Driver.WaitForEvent(ConnectionCompleteNotification.Type);
                return !string.IsNullOrEmpty(completeEvent.ConnectionId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Request a disconnect
        /// </summary>
        protected async Task<bool> Disconnect(string ownerUri)
        {
            var disconnectParams = new DisconnectParams();
            disconnectParams.OwnerUri = ownerUri;

            var disconnectResult = await Driver.SendRequest(DisconnectRequest.Type, disconnectParams);
            return disconnectResult;
        }

        /// <summary>
        /// Request a list of completion items for a position in a block of text
        /// </summary>
        protected async Task<CompletionItem[]> RequestCompletion(string ownerUri, string text, int line, int character)
        {
            // Write the text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, text);
            }

            var completionParams = new TextDocumentPosition();
            completionParams.TextDocument = new TextDocumentIdentifier();
            completionParams.TextDocument.Uri = ownerUri;
            completionParams.Position = new Position();
            completionParams.Position.Line = line;
            completionParams.Position.Character = character;

            var result = await Driver.SendRequest(CompletionRequest.Type, completionParams);
            return result;
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        protected async Task<QueryExecuteCompleteParams> RunQuery(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            // Write the query text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, query);
            }

            var queryParams = new QueryExecuteParams();
            queryParams.OwnerUri = ownerUri;
            queryParams.QuerySelection = null;

            var result = await Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
            if (result != null && string.IsNullOrEmpty(result.Messages))
            {
                var eventResult = await Driver.WaitForEvent(QueryExecuteCompleteEvent.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Request to cancel an executing query
        /// </summary>
        protected async Task<QueryCancelResult> CancelQuery(string ownerUri)
        {
            var cancelParams = new QueryCancelParams();
            cancelParams.OwnerUri = ownerUri;

            var result = await Driver.SendRequest(QueryCancelRequest.Type, cancelParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as CSV
        /// </summary>
        protected async Task<SaveResultRequestResult> SaveAsCsv(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsCsvRequestParams();
            saveParams.OwnerUri = ownerUri;
            saveParams.BatchIndex = batchIndex;
            saveParams.ResultSetIndex = resultSetIndex;
            saveParams.FilePath = filename;
            
            var result = await Driver.SendRequest(SaveResultsAsCsvRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as JSON
        /// </summary>
        protected async Task<SaveResultRequestResult> SaveAsJson(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsJsonRequestParams();
            saveParams.OwnerUri = ownerUri;
            saveParams.BatchIndex = batchIndex;
            saveParams.ResultSetIndex = resultSetIndex;
            saveParams.FilePath = filename;
            
            var result = await Driver.SendRequest(SaveResultsAsJsonRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request a subset of results from a query
        /// </summary>
        protected async Task<QueryExecuteSubsetResult> ExecuteSubset(string ownerUri, int batchIndex, int resultSetIndex, int rowStartIndex, int rowCount)
        {
            var subsetParams = new QueryExecuteSubsetParams();
            subsetParams.OwnerUri = ownerUri;
            subsetParams.BatchIndex = batchIndex;
            subsetParams.ResultSetIndex = resultSetIndex;
            subsetParams.RowsStartIndex = rowStartIndex;
            subsetParams.RowsCount = rowCount;

            var result = await Driver.SendRequest(QueryExecuteSubsetRequest.Type, subsetParams);
            return result;
        }
    }
}
