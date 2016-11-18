//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class LanguageServiceTests : TestBase
    {

        /// <summary>
        /// Validate hover tooltip scenarios
        /// </summary>
        [Fact]
        public async Task HoverTest()
        {
            try
            {
                string ownerUri = System.IO.Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";

                WriteToFile(ownerUri, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);
                  
                Thread.Sleep(500);

                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                Hover hover = await RequestHover(ownerUri, query, 0, 15);

                Assert.True(hover != null, "Hover tooltop is not null");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validation autocompletion suggestions scenarios
        /// </summary>
        [Fact]
        public async Task CompletionTest()
        {
            try
            {      
                string ownerUri = System.IO.Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";

                WriteToFile(ownerUri, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);
                  
                Thread.Sleep(500);

                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                CompletionItem[] completions = await RequestCompletion(ownerUri, query, 0, 15);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is not null and not empty");

                Thread.Sleep(50);

                CompletionItem item = await RequestResolveCompletion(completions[0]);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is not null and not empty");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validate diagnostic scenarios
        /// </summary>
        [Fact]
        public async Task DiagnosticsTests()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);

                string query = "SELECT *** FROM sys.objects";

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);
              
                Thread.Sleep(100);

                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position()
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
                        Uri = ownerUri
                    }
                };

                await RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(100);
        
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position()
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "t"
                };

                changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 3,
                        Uri = ownerUri
                    }
                };

                await RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(2500);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validate the configuration change event
        /// </summary>
        [Fact]
        public async Task ChangeConfigurationTest()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(500);             

                var settings = new SqlToolsSettings();
                settings.SqlTools.IntelliSense.EnableIntellisense = false;
                DidChangeConfigurationParams<SqlToolsSettings> configParams = new DidChangeConfigurationParams<SqlToolsSettings>()
                {
                    Settings = settings
                };

                await RequestChangeConfigurationNotification(configParams);

                Thread.Sleep(2000);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task NotificationIsSentAfterOnConnectionAutoCompleteUpdate()
        {
            try
            {
                // Connect
                string ownerUri = System.IO.Path.GetTempFileName();
                await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);

                // An event signalling that IntelliSense is ready should be sent shortly thereafter
                var readyParams = await Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.Equal(ownerUri, readyParams.OwnerUri);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task FunctionSignatureCompletionReturnsCorrectFunction()
        {
            string sqlText = "EXEC sys.fn_isrolemember ";
            string ownerUri = System.IO.Path.GetTempFileName();

            try
            {
                System.IO.File.WriteAllText(ownerUri, sqlText);

                // Connect
                await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);

                // Wait for intellisense to be ready
                var readyParams = await Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.Equal(ownerUri, readyParams.OwnerUri);

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
                var signatureHelp = await Driver.SendRequest(SignatureHelpRequest.Type, position);

                Assert.NotNull(signatureHelp);
                Assert.True(signatureHelp.ActiveSignature.HasValue);
                Assert.NotEmpty(signatureHelp.Signatures);

                var label = signatureHelp.Signatures[signatureHelp.ActiveSignature.Value].Label;
                Assert.NotNull(label);
                Assert.NotEmpty(label);
                Assert.True(label.Contains("fn_isrolemember"));

                await Disconnect(ownerUri);
            }
            finally
            {
                System.IO.File.Delete(ownerUri);
                WaitForExit();
            }
        }

        [Fact]
        public async Task FunctionSignatureCompletionReturnsCorrectParametersAtEachPosition()
        {
            string sqlText = "EXEC sys.fn_isrolemember 1, 'testing', 2";
            string ownerUri = System.IO.Path.GetTempFileName();

            try
            {
                System.IO.File.WriteAllText(ownerUri, sqlText);

                // Connect
                await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);

                // Wait for intellisense to be ready
                var readyParams = await Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.Equal(ownerUri, readyParams.OwnerUri);

                // Verify all parameters when the cursor is inside of parameters and at separator boundaries (,)
                await VerifyFunctionSignatureHelpParameter(ownerUri, 25, "fn_isrolemember", 0, "@mode int");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 26, "fn_isrolemember", 0, "@mode int");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 27, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 30, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 37, "fn_isrolemember", 1, "@login sysname");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 38, "fn_isrolemember", 2, "@tranpubid int");
                await VerifyFunctionSignatureHelpParameter(ownerUri, 39, "fn_isrolemember", 2, "@tranpubid int");

                await Disconnect(ownerUri);
            }
            finally
            {
                System.IO.File.Delete(ownerUri);
                WaitForExit();
            }
        }

        public async Task VerifyFunctionSignatureHelpParameter(
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
            var signatureHelp = await Driver.SendRequest(SignatureHelpRequest.Type, position);

            Assert.NotNull(signatureHelp);
            Assert.NotNull(signatureHelp.ActiveSignature);
            Assert.True(signatureHelp.ActiveSignature.HasValue);
            Assert.NotEmpty(signatureHelp.Signatures);

            var activeSignature = signatureHelp.Signatures[signatureHelp.ActiveSignature.Value];
            Assert.NotNull(activeSignature);

            var label = activeSignature.Label;
            Assert.NotNull(label);
            Assert.NotEmpty(label);
            Assert.True(label.Contains(expectedFunctionName));

            Assert.NotNull(signatureHelp.ActiveParameter);
            Assert.True(signatureHelp.ActiveParameter.HasValue);
            Assert.Equal(expectedParameterIndex, signatureHelp.ActiveParameter.Value);

            var parameter = activeSignature.Parameters[signatureHelp.ActiveParameter.Value];
            Assert.NotNull(parameter);
            Assert.NotNull(parameter.Label);
            Assert.NotEmpty(parameter.Label);
            Assert.Equal(expectedParameterName, parameter.Label);
        }
    }
}
