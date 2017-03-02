//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Formatter.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class TSqlFormatterServiceTests : FormatterUnitTestsBase
    {
        private Mock<ServiceLayer.Workspace.Workspace> workspaceMock = new Mock<ServiceLayer.Workspace.Workspace>();
        private TextDocumentIdentifier textDocument;
        DocumentFormattingParams docFormatParams;
        DocumentRangeFormattingParams rangeFormatParams;

        public TSqlFormatterServiceTests()
        {
            textDocument = new TextDocumentIdentifier
            {
                Uri = "script file"
            };
            docFormatParams = new DocumentFormattingParams()
            {
                TextDocument = textDocument,
                Options = new FormattingOptions() { InsertSpaces = true, TabSize = 4 }
            };
            rangeFormatParams = new DocumentRangeFormattingParams()
            {
                TextDocument = textDocument,
                Options = new FormattingOptions() { InsertSpaces = true, TabSize = 4 },
                Range = new ServiceLayer.Workspace.Contracts.Range()
                {
                    // From first "(" to last ")"
                    Start = new Position { Line = 0, Character = 16 },
                    End = new Position { Line = 0, Character = 56 }
                }
            };
        }

        private string defaultSqlContents = TestUtilities.NormalizeLineEndings(@"create TABLE T1 ( C1 int NOT NULL, C2 nvarchar(50) NULL)");
        // TODO fix bug where '\r' is appended
        private string formattedSqlContents = TestUtilities.NormalizeLineEndings(@"create TABLE T1
(
    C1 int NOT NULL,
    C2 nvarchar(50) NULL
)");
        
        [Fact]
        public async Task FormatDocumentShouldReturnSingleEdit()
        {
            // Given a document that we want to format
            SetupScriptFile(defaultSqlContents);
            // When format document is called
            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    // Then expect a single edit to be returned and for it to match the standard formatting
                    Assert.Equal(1, edits.Length);
                    AssertFormattingEqual(formattedSqlContents, edits[0].NewText);
                }));
        }

        [Fact]
        public async Task FormatDocumentTelemetryShouldIncludeFormatTypeProperty()
        {
            await RunAndVerifyTelemetryTest(
                // Given a document that we want to format
                preRunSetup: () => SetupScriptFile(defaultSqlContents),
                // When format document is called
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    // Then expect a telemetry event to have been sent with the right format definition
                    Assert.NotNull(actualParams);
                    Assert.Equal(TelemetryEventNames.FormatCode, actualParams.Params.EventName);
                    Assert.Equal(TelemetryPropertyNames.DocumentFormatType, actualParams.Params.Properties[TelemetryPropertyNames.FormatType]);
                });
        }

        [Fact]
        public async Task FormatRangeTelemetryShouldIncludeFormatTypeProperty()
        {
            await RunAndVerifyTelemetryTest(
                // Given a document that we want to format
                preRunSetup: () => SetupScriptFile(defaultSqlContents),
                // When format range is called
                test: (requestContext) => FormatterService.HandleDocRangeFormatRequest(rangeFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    // Then expect a telemetry event to have been sent with the right format definition
                    Assert.NotNull(actualParams);
                    Assert.Equal(TelemetryEventNames.FormatCode, actualParams.Params.EventName);
                    Assert.Equal(TelemetryPropertyNames.RangeFormatType, actualParams.Params.Properties[TelemetryPropertyNames.FormatType]);

                    // And expect range to have been correctly formatted
                    Assert.Equal(1, result.Length);
                    AssertFormattingEqual(formattedSqlContents, result[0].NewText);
                });
        }

        private async Task RunAndVerifyTelemetryTest(
            Action preRunSetup,
            Func<RequestContext<TextEdit[]>, Task> test,
            Action<TextEdit[], TelemetryParams> verify)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);
            TelemetryParams actualParams = null;
            TextEdit[] result = null;
            var contextMock = RequestContextMocks.Create<TextEdit[]>(r =>
            {
                result = r;
            })
            .AddErrorHandling(null)
            .AddEventHandling(TelemetryNotification.Type, (e, p) =>
            {
                actualParams = p;
                semaphore.Release();
            });

            // Given a document that we want to format
            SetupScriptFile(defaultSqlContents);

            // When format document is called
            await RunAndVerify<TextEdit[]>(
                test: test,
                contextMock: contextMock,
                verify: () =>
                {
                    // Wait for the telemetry notification to be processed on a background thread
                    semaphore.Wait(TimeSpan.FromSeconds(10));
                    verify(result, actualParams);
                });
        }

        public static async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Mock<RequestContext<T>> contextMock, Action verify)
        {
            await test(contextMock.Object);
            VerifyResult(contextMock, verify);
        }
        
        public static void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action verify)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Never);
            verify();
        }

        private static void AssertFormattingEqual(string expected, string actual)
        {
            if (string.Compare(expected, actual) != 0)
            {
                StringBuilder error = new StringBuilder();
                error.AppendLine("======================");
                error.AppendLine("Comparison failed:");
                error.AppendLine("==Expected==");
                error.AppendLine(expected);
                error.AppendLine("==Actual==");
                error.AppendLine(actual);
                Assert.False(false, error.ToString());
            }
        }

        private void SetupScriptFile(string fileContents)
        {
            WorkspaceServiceMock.SetupGet(service => service.Workspace).Returns(workspaceMock.Object);
            workspaceMock.Setup(w => w.GetFile(It.IsAny<string>())).Returns(CreateScriptFile(fileContents));
        }

        private ScriptFile CreateScriptFile(string content)
        {
            ScriptFile scriptFile = new ScriptFile()
            {
                Contents = content
            };
            return scriptFile;
        }
        

    }
}
