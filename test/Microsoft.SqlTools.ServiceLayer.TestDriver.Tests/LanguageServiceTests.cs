//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    [TestFixture]
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class LanguageServiceTests
    {

        /// <summary>
        /// Validate hover tooltip scenarios
        /// </summary>
        [Test]
        public async Task HoverTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string query = "SELECT * FROM sys.objects";

                testService.WriteToFile(queryTempFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testService.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);

                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(10000);

                Hover hover = await testService.RequestHover(queryTempFile.FilePath, query, 0, 15);

                Assert.True(hover != null, "Hover tooltop is null");

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validation autocompletion suggestions scenarios
        /// </summary>
        [Test]
        public async Task CompletionTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string query = "SELECT * FROM sys.objects";

                testService.WriteToFile(queryTempFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testService.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);

                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                CompletionItem[] completions = await testService.RequestCompletion(queryTempFile.FilePath, query, 0, 15);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is null or empty");

                Thread.Sleep(50);

                await testService.RequestResolveCompletion(completions[0]);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is null or empty");

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validate diagnostic scenarios
        /// </summary>
        [Test]
        public async Task DiagnosticsTests()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(500);

                string query = "SELECT *** FROM sys.objects";

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testService.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(100);

                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent
                {
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "z"
                };

                DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 2,
                        Uri = queryTempFile.FilePath
                    }
                };

                await testService.RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(100);

                contentChanges[0] = new TextDocumentChangeEvent
                {
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "t"
                };

                changeParams = new DidChangeTextDocumentParams
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier
                    {
                        Version = 3,
                        Uri = queryTempFile.FilePath
                    }
                };

                await testService.RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(2500);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Peek Definition/ Go to definition
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DefinitionTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string query = "SELECT * FROM sys.objects";
                int lineNumber = 0;
                int position = 23;

                testService.WriteToFile(queryTempFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testService.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);

                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);

                // Wait for intellisense to be ready
                var readyParams = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.True(connected, "Connection is successful");


                // Request definition for "objects"
                Location[] locations = await testService.RequestDefinition(queryTempFile.FilePath, query, lineNumber, position);

                Assert.True(locations != null, "Location is not null and not empty");
                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validate the configuration change event
        /// </summary>
        [Test]
        public async Task ChangeConfigurationTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(500);

                var settings = new SqlToolsSettings();
                settings.SqlTools.IntelliSense.EnableIntellisense = false;
                DidChangeConfigurationParams<SqlToolsSettings> configParams = new DidChangeConfigurationParams<SqlToolsSettings>()
                {
                    Settings = settings
                };

                await testService.RequestChangeConfigurationNotification(configParams);

                Thread.Sleep(2000);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task NotificationIsSentAfterOnConnectionAutoCompleteUpdate()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                // Connect
                await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);

                // An event signalling that IntelliSense is ready should be sent shortly thereafter
                var readyParams = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.AreEqual(queryTempFile.FilePath, readyParams.OwnerUri);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Test]
        public async Task FunctionSignatureCompletionReturnsEmptySignatureHelpObjectWhenThereAreNoMatches()
        {
            string sqlText = "EXEC sys.fn_not_a_real_function ";

            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string ownerUri = tempFile.FilePath;
                File.WriteAllText(ownerUri, sqlText);

                // Connect
                await testService.Connect(TestServerType.OnPrem, ownerUri);

                // Wait for intellisense to be ready
                var readyParams = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.AreEqual(ownerUri, readyParams.OwnerUri);

                // Send a function signature help Request
                var position = new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = ownerUri
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = sqlText.Length
                    }
                };
                var signatureHelp = await testService.Driver.SendRequest(SignatureHelpRequest.Type, position);

                Assert.NotNull(signatureHelp);
                Assert.False(signatureHelp.ActiveSignature.HasValue);
                Assert.Null(signatureHelp.Signatures);

                await testService.Disconnect(ownerUri);
            }
        }

        [Test]
        public async Task FunctionSignatureCompletionReturnsCorrectFunction()
        {
            string sqlText = "EXEC sys.fn_isrolemember ";

            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string ownerUri = tempFile.FilePath;

                // Connect
                await testService.ConnectForQuery(TestServerType.OnPrem, sqlText, ownerUri);

                // Wait for intellisense to be ready
                var readyParams = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.AreEqual(ownerUri, readyParams.OwnerUri);

                // Send a function signature help Request
                var position = new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = ownerUri
                    },
                    Position = new Position()
                    {
                        Line = 0,
                        Character = sqlText.Length
                    }
                };
                var signatureHelp = await testService.Driver.SendRequest(SignatureHelpRequest.Type, position);

                Assert.NotNull(signatureHelp);
                Assert.True(signatureHelp.ActiveSignature.HasValue);
                Assert.That(signatureHelp.Signatures, Is.Not.Empty, "signatureHelp.Signatures after SendRequest");

                var label = signatureHelp.Signatures[signatureHelp.ActiveSignature.Value].Label;
                Assert.That(label, Is.Not.Null.Or.Empty, "label");
                Assert.That(label, Contains.Substring("fn_isrolemember"), "label contents");

                await testService.Disconnect(ownerUri);
            }
        }

        [Test]
        public async Task FunctionSignatureCompletionReturnsCorrectParametersAtEachPosition()
        {
            string sqlText = "EXEC sys.fn_isrolemember 1, 'testing', 2";

            using (SelfCleaningTempFile tempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                string ownerUri = tempFile.FilePath;
                File.WriteAllText(ownerUri, sqlText);

                // Connect
                await testService.Connect(TestServerType.OnPrem, ownerUri);

                // Wait for intellisense to be ready
                var readyParams = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.AreEqual(ownerUri, readyParams.OwnerUri);

                // Verify all parameters when the cursor is inside of parameters and at separator boundaries (,)
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 25, "fn_isrolemember", 0, "@mode int");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 26, "fn_isrolemember", 0, "@mode int");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 27, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 30, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 37, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 38, "fn_isrolemember", 2, "@tranpubid int");
                await VerifyFunctionSignatureHelpParameter(testService, ownerUri, 39, "fn_isrolemember", 2, "@tranpubid int");

                await testService.Disconnect(ownerUri);
            }
        }

        public async Task VerifyFunctionSignatureHelpParameter(
            TestServiceDriverProvider TestService,
            string ownerUri,
            int character,
            string expectedFunctionName,
            int expectedParameterIndex,
            string expectedParameterName)
        {
            var position = new TextDocumentPosition()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = ownerUri
                },
                Position = new Position()
                {
                    Line = 0,
                    Character = character
                }
            };
            var signatureHelp = await TestService.Driver.SendRequest(SignatureHelpRequest.Type, position);

            Assert.NotNull(signatureHelp);
            Assert.NotNull(signatureHelp.ActiveSignature);
            Assert.True(signatureHelp.ActiveSignature.HasValue);
            Assert.That(signatureHelp.Signatures, Is.Not.Empty, "Signatures");

            var activeSignature = signatureHelp.Signatures[signatureHelp.ActiveSignature.Value];
            Assert.NotNull(activeSignature);

            var label = activeSignature.Label;
            Assert.That(label, Is.Not.Null.Or.Empty, "label");
            Assert.That(label, Contains.Substring(expectedFunctionName), "label contents");

            Assert.NotNull(signatureHelp.ActiveParameter);
            Assert.True(signatureHelp.ActiveParameter.HasValue);
            Assert.AreEqual(expectedParameterIndex, signatureHelp.ActiveParameter.Value);

            var parameter = activeSignature.Parameters[signatureHelp.ActiveParameter.Value];
            Assert.NotNull(parameter);
            Assert.That(parameter.Label, Is.Not.Null.Or.Empty, "parameter.Label");
            Assert.AreEqual(expectedParameterName, parameter.Label);
        }
    }
}
