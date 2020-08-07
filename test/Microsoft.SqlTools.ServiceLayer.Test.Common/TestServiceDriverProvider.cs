//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Service class to execute SQL tools commands using the test driver or calling the service classed directly
    /// </summary>
    public sealed class TestServiceDriverProvider : IDisposable
    {
        private bool isRunning = false;
        private TestConnectionProfileService testConnectionService;

        public TestServiceDriverProvider()
        {
            Driver = new ServiceTestDriver(TestRunner.Instance.ExecutableFilePath);
            Driver.Start().Wait();
            this.isRunning = true;
        }

        public void Dispose()
        {
            if (this.isRunning)
            {
                WaitForExit();
            }
        }

        public void WaitForExit()
        {
            try
            {
                this.isRunning = false;
                Driver.Stop().Wait();
                Console.WriteLine("Successfully killed process.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while waiting for service exit: {e.Message}");
            }
        }

        /// <summary>
        /// The driver object used to read/write data to the service
        /// </summary>
        public ServiceTestDriver Driver
        {
            get;
            private set;
        }

        public TestConnectionProfileService TestConnectionService
        {
            get
            {
                return (testConnectionService = testConnectionService ?? TestConnectionProfileService.Instance);
            }
        }

        private object fileLock = new Object();

        /// <summary>
        /// Request a new connection to be created
        /// </summary>
        /// <returns>True if the connection completed successfully</returns>        
        public async Task<bool> Connect(string ownerUri, ConnectParams connectParams, int timeout = 15000)
        {
            connectParams.OwnerUri = ownerUri;
            var connectResult = await Driver.SendRequest(ConnectionRequest.Type, connectParams);
            if (connectResult)
            {
                var completeEvent = await Driver.WaitForEvent(ConnectionCompleteNotification.Type, timeout);
                return !string.IsNullOrEmpty(completeEvent.ConnectionId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///  Request a new connection to be created for given query
        /// </summary>
        public async Task<bool> ConnectForQuery(TestServerType serverType, string query, string ownerUri, string databaseName = null, int timeout = 15000)
        {
            if (!string.IsNullOrEmpty(query))
            {
                WriteToFile(ownerUri, query);
            }

            return await Connect(serverType, ownerUri, databaseName, timeout);
        }

        /// <summary>
        ///  Request a new connection to be created for url
        /// </summary>
        public async Task<bool> Connect(TestServerType serverType, string ownerUri, string databaseName = null, int timeout = 15000)
        {

            var connectParams = GetConnectionParameters(serverType, databaseName);

            bool connected = await Connect(ownerUri, connectParams, timeout);
            Assert.True(connected, "Connection is successful");
            Console.WriteLine($"Connection to {connectParams.Connection.ServerName} is successful");

            return connected;
        }


        /// <summary>
        /// Request a disconnect
        /// </summary>
        public async Task<bool> Disconnect(string ownerUri)
        {
            var disconnectParams = new DisconnectParams();
            disconnectParams.OwnerUri = ownerUri;

            var disconnectResult = await Driver.SendRequest(DisconnectRequest.Type, disconnectParams);
            return disconnectResult;
        }

        /// <summary>
        /// Request a cancel connect
        /// </summary>
        public async Task<bool> CancelConnect(string ownerUri)
        {
            var cancelParams = new CancelConnectParams();
            cancelParams.OwnerUri = ownerUri;

            return await Driver.SendRequest(CancelConnectRequest.Type, cancelParams);
        }

        /// <summary>
        /// Request a cancel connect
        /// </summary>
        public async Task<ListDatabasesResponse> ListDatabases(string ownerUri)
        {
            var listParams = new ListDatabasesParams();
            listParams.OwnerUri = ownerUri;

            return await Driver.SendRequest(ListDatabasesRequest.Type, listParams);
        }

        /// <summary>
        /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task<SubsetResult> RequestQueryExecuteSubset(SubsetParams subsetParams)
        {
            return await Driver.SendRequest(SubsetRequest.Type, subsetParams);
        }

        /// <summary>
        /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task RequestOpenDocumentNotification(DidOpenTextDocumentNotification openParams)
        {
            await Driver.SendEvent(DidOpenTextDocumentNotification.Type, openParams);
        }

        /// <summary>
        /// Request a configuration change notification
        /// </summary>
        public async Task RequestChangeConfigurationNotification(DidChangeConfigurationParams<SqlToolsSettings> configParams)
        {
            await Driver.SendEvent(DidChangeConfigurationNotification<SqlToolsSettings>.Type, configParams);
        }

        /// <summary>
        /// /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task RequestChangeTextDocumentNotification(DidChangeTextDocumentParams changeParams)
        {
            await Driver.SendEvent(DidChangeTextDocumentNotification.Type, changeParams);
        }

        /// <summary>
        /// Request completion item resolve to look-up additional info
        /// </summary>
        public async Task<CompletionItem> RequestResolveCompletion(CompletionItem item)
        {
            var result = await Driver.SendRequest(CompletionResolveRequest.Type, item);
            return result;
        }

        /// <summary>
        /// Returns database connection parameters for given server type
        /// </summary>
        public ConnectParams GetConnectionParameters(TestServerType serverType, string databaseName = null)
        {
            return TestConnectionService.GetConnectionParameters(serverType, databaseName);
        }

        /// <summary>
        /// Request a list of completion items for a position in a block of text
        /// </summary>
        public async Task<CompletionItem[]> RequestCompletion(string ownerUri, string text, int line, int character)
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
        /// Request a list of completion items for a position in a block of text
        /// </summary>
        public async Task RequestRebuildIntelliSense(string ownerUri)
        {
            var rebuildIntelliSenseParams = new RebuildIntelliSenseParams();
            rebuildIntelliSenseParams.OwnerUri = ownerUri;

            await Driver.SendEvent(RebuildIntelliSenseNotification.Type, rebuildIntelliSenseParams);
        }


        /// <summary>
        /// Request a a hover tooltop
        /// </summary>
        public async Task<Hover> RequestHover(string ownerUri, string text, int line, int character)
        {
            // Write the text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, text);
            }

            var completionParams = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = ownerUri },
                Position = new Position
                {
                    Line = line,
                    Character = character
                }
            };

            var result = await Driver.SendRequest(HoverRequest.Type, completionParams);
            return result;
        }

        /// <summary>
        /// Request definition( peek definition/go to definition) for a sql object in a sql string
        /// </summary>
        public async Task<Location[]> RequestDefinition(string ownerUri, string text, int line, int character)
        {
            // Write the text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, text);
            }

            var definitionParams = new TextDocumentPosition();
            definitionParams.TextDocument = new TextDocumentIdentifier();
            definitionParams.TextDocument.Uri = ownerUri;
            definitionParams.Position = new Position();
            definitionParams.Position.Line = line;
            definitionParams.Position.Character = character;

            // Send definition request
            var result = await Driver.SendRequest(DefinitionRequest.Type, definitionParams);
            return result;
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        public async Task<QueryCompleteParams> RunQueryAndWaitToComplete(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            // Write the query text to a backing file
            WriteToFile(ownerUri, query);

            return await RunQueryAndWaitToComplete(ownerUri, timeoutMilliseconds);
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        public async Task<QueryCompleteParams> RunQueryAndWaitToComplete(string ownerUri, int timeoutMilliseconds = 5000)
        {

            var queryParams = new ExecuteDocumentSelectionParams
            {
                OwnerUri = ownerUri,
                QuerySelection = null
            };

            var result = await Driver.SendRequest(ExecuteDocumentSelectionRequest.Type, queryParams);
            if (result != null)
            {
                var eventResult = await Driver.WaitForEvent(QueryCompleteEvent.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        public async Task<BatchEventParams> RunQueryAndWaitToStart(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            // Write the query text to a backing file
            WriteToFile(ownerUri, query);

            return await RunQueryAndWaitToStart(ownerUri, timeoutMilliseconds);
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        public async Task<BatchEventParams> RunQueryAndWaitToStart(string ownerUri, int timeoutMilliseconds = 5000)
        {
            var queryParams = new ExecuteDocumentSelectionParams
            {
                OwnerUri = ownerUri,
                QuerySelection = null
            };

            var result = await Driver.SendRequest(ExecuteDocumentSelectionRequest.Type, queryParams);
            if (result != null)
            {
                var eventResult = await Driver.WaitForEvent(BatchStartEvent.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }

        public async Task<SessionCreatedParameters> RequestObjectExplorerCreateSession(ConnectionDetails connectionDetails, int timeoutMilliseconds = 5000)
        {
            var result = await Driver.SendRequest(CreateSessionRequest.Type, connectionDetails);
            if (result != null)
            {
                var eventResult = await Driver.WaitForEvent(CreateSessionCompleteNotification.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }

        public async Task<ExpandResponse> RequestObjectExplorerExpand(ExpandParams expandParams, int timeoutMilliseconds = 5000)
        {
            var result = await Driver.SendRequest(ExpandRequest.Type, expandParams);
            if (result)
            {
                var eventResult = await Driver.WaitForEvent(ExpandCompleteNotification.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }

        public async Task<ScriptingResult> RequestScript(ScriptingParams scriptingParams, int timeoutMilliseconds = 5000)
        {
            var result = await Driver.SendRequest(ScriptingRequest.Type, scriptingParams);
            return result;
        }

        public async Task<CloseSessionResponse> RequestObjectExplorerCloseSession(CloseSessionParams closeSessionParams, int timeoutMilliseconds = 5000)
        {
            return await Driver.SendRequest(CloseSessionRequest.Type, closeSessionParams);
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI. This method only waits for the initial response from query
        /// execution (QueryExecuteResult). It is up to the caller to wait for the QueryCompleteEvent if they are interested.
        /// </summary>
        public async Task<ExecuteRequestResult> RunQueryAsync(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            WriteToFile(ownerUri, query);

            var queryParams = new ExecuteDocumentSelectionParams
            {
                OwnerUri = ownerUri,
                QuerySelection = null
            };

            return await Driver.SendRequest(ExecuteDocumentSelectionRequest.Type, queryParams);
        }

        public async Task RunQuery(TestServerType serverType, string databaseName, string query)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                await ConnectForQuery(serverType, query, queryTempFile.FilePath, databaseName);
                var queryResult = await CalculateRunTime(() => RunQueryAndWaitToComplete(queryTempFile.FilePath, query, 50000));
                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.BatchSummaries);

                await Disconnect(queryTempFile.FilePath);
            }
        }

        public static async Task RunTestIterations(Func<TestTimer, Task> testToRun, [CallerMemberName] string testName = "")
        {
            TestTimer timer = new TestTimer() { PrintResult = true };
            for (int i = 0; i < TestRunner.Instance.NumberOfRuns; i++)
            {
                Console.WriteLine("Iteration Number: " + i);
                try
                {
                    await testToRun(timer);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Iteration Failed: " + ex.Message);
                }
                Thread.Sleep(5000);
            }
            timer.Print(testName);
        }

        public async Task<T> CalculateRunTime<T>(Func<Task<T>> testToRun, TestTimer timer = null)
        {
            if (timer != null)
            {
                timer.Start();
            }
            T result = await testToRun();
            if (timer != null)
            {
                timer.End();
            }

            return result;
        }

        public async Task ExecuteWithTimeout(TestTimer timer, int timeout, Func<Task<bool>> repeatedCode,
            TimeSpan? delay = null, [CallerMemberName] string testName = "")
        {
            timer.Start();
            while (true)
            {
                if (await repeatedCode())
                {
                    timer.End();
                    break;
                }
                if (timer.TotalMilliSecondsUntilNow >= timeout)
                {
                    Assert.True(false, $"{testName} timed out after {timeout} milliseconds");
                    break;
                }
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }
            }
        }

        /// <summary>
        /// Request to cancel an executing query
        /// </summary>
        public async Task<QueryCancelResult> CancelQuery(string ownerUri)
        {
            var cancelParams = new QueryCancelParams { OwnerUri = ownerUri };

            var result = await Driver.SendRequest(QueryCancelRequest.Type, cancelParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as CSV
        /// </summary>
        public async Task<SaveResultRequestResult> SaveAsCsv(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = ownerUri,
                BatchIndex = batchIndex,
                ResultSetIndex = resultSetIndex,
                FilePath = filename
            };

            var result = await Driver.SendRequest(SaveResultsAsCsvRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as JSON
        /// </summary>
        public async Task<SaveResultRequestResult> SaveAsJson(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = ownerUri,
                BatchIndex = batchIndex,
                ResultSetIndex = resultSetIndex,
                FilePath = filename
            };

            var result = await Driver.SendRequest(SaveResultsAsJsonRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as XML
        /// </summary>
        public async Task<SaveResultRequestResult> SaveAsXml(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsXmlRequestParams
            {
                OwnerUri = ownerUri,
                BatchIndex = batchIndex,
                ResultSetIndex = resultSetIndex,
                FilePath = filename
            };

            var result = await Driver.SendRequest(SaveResultsAsXmlRequest.Type, saveParams);
            return result;
        }
        
        /// <summary>
        /// Request a subset of results from a query
        /// </summary>
        public async Task<SubsetResult> ExecuteSubset(string ownerUri, int batchIndex, int resultSetIndex, int rowStartIndex, int rowCount)
        {
            var subsetParams = new SubsetParams();
            subsetParams.OwnerUri = ownerUri;
            subsetParams.BatchIndex = batchIndex;
            subsetParams.ResultSetIndex = resultSetIndex;
            subsetParams.RowsStartIndex = rowStartIndex;
            subsetParams.RowsCount = rowCount;

            var result = await Driver.SendRequest(SubsetRequest.Type, subsetParams);
            return result;
        }

        public async Task<ScriptingListObjectsResult> ListScriptingObjects(ScriptingListObjectsParams parameters)
        {
            return await Driver.SendRequest(ScriptingListObjectsRequest.Type, parameters);
        }

        public async Task<ScriptingResult> Script(ScriptingParams parameters)
        {
            return await Driver.SendRequest(ScriptingRequest.Type, parameters);
        }

        public async Task<ScriptingCancelResult> CancelScript(string operationId)
        {
            return await Driver.SendRequest(ScriptingCancelRequest.Type, new ScriptingCancelParams { OperationId = operationId });
        }

        /// <summary>
        /// Waits for a message to be returned by the service
        /// </summary>
        /// <returns>A message from the service layer</returns>
        public async Task<MessageParams> WaitForMessage()
        {
            return await Driver.WaitForEvent(MessageEvent.Type);
        }

        public void WriteToFile(string ownerUri, string query)
        {
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, query);
            }
        }

        public bool TryGetEvent<T>(EventType<T> eventType, out T value)
        {
            value = default(T);

            try
            {
                Task<T> t = this.Driver.WaitForEvent(eventType, TimeSpan.Zero);
                value = t.Result;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void AssertEventNotQueued<T>(EventType<T> eventType)
        {
            T temp;
            if (TryGetEvent(eventType, out temp))
            {
                Assert.True(false, string.Format("Event of type {0} was found in the queue.", eventType.GetType().FullName, temp.ToString()));
            }
        }
    }
}
