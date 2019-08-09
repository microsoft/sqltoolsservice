//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        [Fact]
        public void ServiceInitialization()
        {
            try
            {
                TestServiceProvider serviceProvider = TestServiceProvider.Instance;
                Assert.NotNull(serviceProvider);
            }
            catch (System.ArgumentException)
            {

            }
            Assert.True(LanguageService.Instance.Context != null);
            Assert.True(LanguageService.Instance.ConnectionServiceInstance != null);
            Assert.True(LanguageService.Instance.CurrentWorkspaceSettings != null);
            Assert.True(LanguageService.Instance.CurrentWorkspace != null);
        }

        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        //[Fact]
        public void PrepopulateCommonMetadata()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var result = LiveConnectionHelper.InitLiveConnectionInfo("master", queryTempFile.FilePath);
                var connInfo = result.ConnectionInfo;

                ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };

                LanguageService.Instance.PrepopulateCommonMetadata(connInfo, scriptInfo, null);
            }
        }

        /// <summary>
        /// This test tests auto completion
        /// </summary>
        [Fact]
        public void AutoCompleteFindCompletions()
        {
            var result = GetLiveAutoCompleteTestObjects();

            result.TextDocumentPosition.Position.Character = 7;
            result.ScriptFile.Contents = "select ";

            var autoCompleteService = LanguageService.Instance;
            var completions = autoCompleteService.GetCompletionItems(
                result.TextDocumentPosition,
                result.ScriptFile,
                result.ConnectionInfo).Result;

            Assert.True(completions.Length > 0);
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// This test tests completion extension interface in following aspects
        /// 1. Loading a sample completion extension assembly
        /// 2. Initializing a completion extension implementation
        /// 3. Excuting an auto completion with extension enabled
        /// </summary>
        [Fact]
        public async void AutoCompleteWithExtension()
        {
            var result = GetLiveAutoCompleteTestObjects();

            result.TextDocumentPosition.Position.Character = 10;
            result.ScriptFile = ScriptFileTests.GetTestScriptFile("select * f");
            result.TextDocumentPosition.TextDocument.Uri = result.ScriptFile.FilePath;

            var autoCompleteService = LanguageService.Instance;
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<bool>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<bool>()))
                .Returns(Task.FromResult(true));
            requestContext.Setup(x => x.SendError(It.IsAny<string>(), 0))
                .Returns(Task.FromResult(true));

            //Create completion extension parameters
            var extensionParams = new CompletionExtensionParams()
            {
                AssemblyPath = Path.Combine(AssemblyDirectory, "Microsoft.SqlTools.Test.CompletionExtension.dll"),
                TypeName = "Microsoft.SqlTools.Test.CompletionExtension.CompletionExt",
                Properties = new Dictionary<string, object> { { "modelPath", "testModel" } }
            };

            //load and initialize completion extension, expect a success
            await autoCompleteService.HandleCompletionExtLoadRequest(extensionParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.IsAny<bool>()), Times.Once);
            requestContext.Verify(x => x.SendError(It.IsAny<string>(), 0), Times.Never);

            //Try to load the same completion extension second time, expect an error sent
            await autoCompleteService.HandleCompletionExtLoadRequest(extensionParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.IsAny<bool>()), Times.Once);
            requestContext.Verify(x => x.SendError(It.IsAny<string>(), 0), Times.Once);

            //Try to load the completion extension with new modified timestamp, expect a success
            var assemblyCopyPath = CopyFileWithNewModifiedTime(extensionParams.AssemblyPath);
            extensionParams = new CompletionExtensionParams()
            {
                AssemblyPath = assemblyCopyPath,
                TypeName = "Microsoft.SqlTools.Test.CompletionExtension.CompletionExt",
                Properties = new Dictionary<string, object> { { "modelPath", "testModel" } }
            };
            //load and initialize completion extension
            await autoCompleteService.HandleCompletionExtLoadRequest(extensionParams, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.IsAny<bool>()), Times.Exactly(2));
            requestContext.Verify(x => x.SendError(It.IsAny<string>(), 0), Times.Once);

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            autoCompleteService.ParseAndBind(result.ScriptFile, result.ConnectionInfo);
            scriptInfo.ConnectionKey = autoCompleteService.BindingQueue.AddConnectionContext(result.ConnectionInfo);

            //Invoke auto completion with extension enabled
            var completions = autoCompleteService.GetCompletionItems(
                result.TextDocumentPosition,
                result.ScriptFile,
                result.ConnectionInfo).Result;

            //Validate completion list is not empty
            Assert.True(completions != null && completions.Length > 0, "The completion list is null or empty!");
            //Validate the first completion item in the list is preselected
            Assert.True(completions[0].Preselect.HasValue && completions[0].Preselect.Value, "Preselect is not set properly in the first completion item by the completion extension!");
            //Validate the Command object attached to the completion item by the extension
            Assert.True(completions[0].Command != null && completions[0].Command.CommandStr == "vsintellicode.completionItemSelected", "Command is not set properly in the first completion item by the completion extension!");

            //clean up the temp file
            File.Delete(assemblyCopyPath);
        }

        /// <summary>
        /// Make a copy of a file and update the last modified time
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string CopyFileWithNewModifiedTime(string filePath)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            File.Copy(filePath, tempPath);
            File.SetLastWriteTimeUtc(tempPath, DateTime.UtcNow);
            return tempPath;
        }

        /// <summary>
        /// Verify that GetSignatureHelp returns not null when the provided TextDocumentPosition
        /// has an associated ScriptParseInfo and the provided query has a function that should
        /// provide signature help.
        /// </summary>
        [Fact]
        public async Task GetSignatureHelpReturnsNotNullIfParseInfoInitialized()
        {
            // When we make a connection to a live database
            Hosting.ServiceHost.SendEventIgnoreExceptions = true;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();

            // And we place the cursor after a function that should prompt for signature help
            string queryWithFunction = "EXEC sys.fn_isrolemember ";
            result.ScriptFile.Contents = queryWithFunction;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = result.ScriptFile.ClientFilePath
                },
                Position = new Position
                {
                    Line = 0,
                    Character = queryWithFunction.Length
                }
            };

            // If the SQL has already been parsed
            var service = LanguageService.Instance;
            await service.UpdateLanguageServiceOnConnection(result.ConnectionInfo);
            Thread.Sleep(2000);

            // We should get back a non-null ScriptParseInfo
            ScriptParseInfo parseInfo = service.GetScriptParseInfo(result.ScriptFile.ClientFilePath);
            Assert.NotNull(parseInfo);

            // And we should get back a non-null SignatureHelp
            SignatureHelp signatureHelp = service.GetSignatureHelp(textDocument, result.ScriptFile);
            Assert.NotNull(signatureHelp);
        }

        /// <summary>
        /// Test overwriting the binding queue context
        /// </summary>
        [Fact]
        public void OverwriteBindingContext()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();

            // add a new connection context
            var connectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(result.ConnectionInfo, overwrite: true);
            Assert.True(LanguageService.Instance.BindingQueue.BindingContextMap.ContainsKey(connectionKey));

            // cache the server connection
            var orgServerConnection = LanguageService.Instance.BindingQueue.BindingContextMap[connectionKey].ServerConnection;
            Assert.NotNull(orgServerConnection);

            // add a new connection context
            connectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(result.ConnectionInfo, overwrite: true);
            Assert.True(LanguageService.Instance.BindingQueue.BindingContextMap.ContainsKey(connectionKey));
            Assert.False(object.ReferenceEquals(LanguageService.Instance.BindingQueue.BindingContextMap[connectionKey].ServerConnection, orgServerConnection));
        }

        /// <summary>
        /// Verifies that clearing the Intellisense cache correctly refreshes the cache with new info from the DB.
        /// </summary>
        [Fact]
        public async Task RebuildIntellisenseCacheClearsScriptParseInfoCorrectly()
        {
            var testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, null, null, "LangSvcTest");
            try
            {
                var connectionInfoResult = LiveConnectionHelper.InitLiveConnectionInfo(testDb.DatabaseName);

                var langService = LanguageService.Instance;
                await langService.UpdateLanguageServiceOnConnection(connectionInfoResult.ConnectionInfo);
                var queryText = "SELECT * FROM dbo.";
                connectionInfoResult.ScriptFile.SetFileContents(queryText);

                var textDocumentPosition =
                    connectionInfoResult.TextDocumentPosition ??
                    new TextDocumentPosition()
                    {
                        TextDocument = new TextDocumentIdentifier
                        {
                            Uri = connectionInfoResult.ScriptFile.ClientFilePath
                        },
                        Position = new Position
                        {
                            Line = 0,
                            Character = queryText.Length
                        }
                    };

                // First check that we don't have any items in the completion list as expected
                var initialCompletionItems = await langService.GetCompletionItems(
                    textDocumentPosition, connectionInfoResult.ScriptFile, connectionInfoResult.ConnectionInfo);

                Assert.True(initialCompletionItems.Length == 0, $"Should not have any completion items initially. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");

                // Now create a table that should show up in the completion list
                testDb.RunQuery("CREATE TABLE dbo.foo(col1 int)");

                // And refresh the cache
                await langService.HandleRebuildIntelliSenseNotification(
                    new RebuildIntelliSenseParams() { OwnerUri = connectionInfoResult.ScriptFile.ClientFilePath },
                    new TestEventContext());

                // Now we should expect to see the item show up in the completion list
                var afterTableCreationCompletionItems = await langService.GetCompletionItems(
                    textDocumentPosition, connectionInfoResult.ScriptFile, connectionInfoResult.ConnectionInfo);

                Assert.True(afterTableCreationCompletionItems.Length == 1, $"Should only have a single completion item after rebuilding Intellisense cache. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");
                Assert.True(afterTableCreationCompletionItems[0].InsertText == "foo", $"Expected single completion item 'foo'. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");
            }
            finally
            {
                testDb.Cleanup();
            }
        }

        /// <summary>
        // This test validates switching off editor intellisesnse for now. 
        // Will change to better handling once we have specific SQLCMD intellisense in Language Service
        /// </summary>
        [Fact]
        public async Task HandleRequestToChangeToSqlcmdFile()
        {

            var scriptFile = new ScriptFile() { ClientFilePath = "HandleRequestToChangeToSqlcmdFile_" + DateTime.Now.ToLongDateString() + "_.sql" };

            try
            {
                // Prepare a script file
                scriptFile.SetFileContents("koko wants a bananas");
                File.WriteAllText(scriptFile.ClientFilePath, scriptFile.Contents);

                // Create a workspace and add file to it so that its found for intellense building
                var workspace = new ServiceLayer.Workspace.Workspace();
                var workspaceService = new WorkspaceService<SqlToolsSettings> { Workspace = workspace };
                var langService = new LanguageService() { WorkspaceServiceInstance = workspaceService }; 
                langService.CurrentWorkspace.GetFile(scriptFile.ClientFilePath);
                langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;

                // Add a connection to ensure the intellisense building works            
                ConnectionInfo connectionInfo = GetLiveAutoCompleteTestObjects().ConnectionInfo;
                langService.ConnectionServiceInstance.OwnerToConnectionMap.Add(scriptFile.ClientFilePath, connectionInfo);

                // Test SQL
                int countOfValidationCalls = 0;
                var eventContextSql = new Mock<SqlTools.Hosting.Protocol.EventContext>();
                eventContextSql.Setup(x => x.SendEvent(PublishDiagnosticsNotification.Type, It.Is<PublishDiagnosticsNotification>((notif) => ValidateNotification(notif, 2, ref countOfValidationCalls)))).Returns(Task.FromResult(new object()));
                await langService.HandleDidChangeLanguageFlavorNotification(new LanguageFlavorChangeParams
                {
                    Uri = scriptFile.ClientFilePath,
                    Language = LanguageService.SQL_LANG.ToLower(),
                    Flavor = "MSSQL"
                }, eventContextSql.Object);
                await langService.DelayedDiagnosticsTask; // to ensure completion and validation before moveing to next step

                // Test SQL CMD
                var eventContextSqlCmd = new Mock<SqlTools.Hosting.Protocol.EventContext>();
                eventContextSqlCmd.Setup(x => x.SendEvent(PublishDiagnosticsNotification.Type, It.Is<PublishDiagnosticsNotification>((notif) => ValidateNotification(notif, 0, ref countOfValidationCalls)))).Returns(Task.FromResult(new object()));
                await langService.HandleDidChangeLanguageFlavorNotification(new LanguageFlavorChangeParams
                {
                    Uri = scriptFile.ClientFilePath,
                    Language = LanguageService.SQL_CMD_LANG.ToLower(),
                    Flavor = "MSSQL"
                }, eventContextSqlCmd.Object);
                await langService.DelayedDiagnosticsTask;

                Assert.True(countOfValidationCalls == 2, $"Validation should be called 2 time but is called {countOfValidationCalls} times");
            }
            finally
            {
                if (File.Exists(scriptFile.ClientFilePath))
                {
                    File.Delete(scriptFile.ClientFilePath);
                }
            }
        }

        private bool ValidateNotification(PublishDiagnosticsNotification notif, int errors, ref int countOfValidationCalls)
        {
            countOfValidationCalls++;
            Assert.True(notif.Diagnostics.Length == errors, $"Notification errors {notif.Diagnostics.Length} are not as expected {errors}");
            return true;
        }
    }
}
