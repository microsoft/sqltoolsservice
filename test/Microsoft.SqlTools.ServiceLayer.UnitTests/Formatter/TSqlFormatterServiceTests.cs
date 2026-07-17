//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.LanguageService.Formatter;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.Formatter.Contracts;
using Microsoft.SqlTools.LanguageService.Formatter.ScriptDom;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Moq;
using NUnit.Framework;
using Range = Microsoft.SqlTools.LanguageService.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class TSqlFormatterServiceTests : FormatterUnitTestsBase
    {
        private Mock<Microsoft.SqlTools.LanguageService.Workspace.Workspace> workspaceMock;
        private TextDocumentIdentifier textDocument;
        DocumentFormattingParams docFormatParams;
        DocumentRangeFormattingParams rangeFormatParams;

        [SetUp]
        public void InitTSqlFormatterServiceTests()
        {
            InitFormatterUnitTestsBase();
            workspaceMock = new Mock<Microsoft.SqlTools.LanguageService.Workspace.Workspace>();
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
                Range = new Range()
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

        private void SetupLanguageService(bool skipFile = false)
        {
            LanguageServiceMock.Setup(x => x.ShouldSkipNonMssqlFile(It.IsAny<string>())).Returns(skipFile);
        }

        [Test]
        public async Task FormatDocumentShouldReturnSingleEdit()
        {
            // Given a document that we want to format
            SetupLanguageService();
            SetupScriptFile(defaultSqlContents);
            // When format document is called
            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    // Then expect a single edit to be returned and for it to match the standard formatting
                    Assert.AreEqual(1, edits.Length);
                    AssertFormattingEqual(formattedSqlContents, edits[0].NewText);
                }));
        }

        [Test]
        public async Task FormatDocumentShouldSkipNonMssqlFile()
        {
            // Given a non-MSSQL document
            SetupLanguageService(skipFile: true);
            SetupScriptFile(defaultSqlContents);
            // When format document is called
            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    // Then expect a single edit to be returned and for it to match the standard formatting
                    Assert.AreEqual(0, edits.Length);
                    LanguageServiceMock.Verify(x => x.ShouldSkipNonMssqlFile(docFormatParams.TextDocument.Uri), Times.Once);
                }));
        }

        [Test]
        public async Task PreviewFormatDocumentShouldReturnSingleEdit()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true,
                KeywordCasing = CasingOptions.Uppercase
            });
            SetupScriptFile("select 1 as value");

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(1, edits.Length);
                    StringAssert.Contains("SELECT", edits[0].NewText);
                    StringAssert.Contains(" AS ", edits[0].NewText);
                }));
        }

        [Test]
        public async Task PreviewFormatDocumentShouldReturnNoEditsForParseError()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true
            });
            SetupScriptFile("select from");

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(0, edits.Length);
                }));
        }

        [Test]
        public async Task PreviewFormatDocumentShouldReturnNoEditsWhenNoChangeNeeded()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true
            });
            ScriptDomFormatterResult formattedSql = new ScriptDomSqlFormatter().Format("select 1 as value", new FormatOptions());
            SetupScriptFile(formattedSql.FormattedText);

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(0, edits.Length);
                }));
        }

        [Test]
        public async Task PreviewFormatDocumentShouldUseFormattingTabSize()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true
            });
            docFormatParams.Options = new FormattingOptions { InsertSpaces = true, TabSize = 2 };
            SetupScriptFile("create table dbo.T (id int not null, name int null)");

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(1, edits.Length);
                    StringAssert.Contains(Environment.NewLine + "  ", edits[0].NewText);
                }));
        }

        [Test]
        public async Task PreviewFormatDocumentShouldUseEnvironmentLineEndings()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true
            });
            SetupScriptFile("create table dbo.T (id int not null,\nname int null)");

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(1, edits.Length);
                    StringAssert.Contains(Environment.NewLine, edits[0].NewText);
                    if (Environment.NewLine == "\r\n")
                    {
                        Assert.False(edits[0].NewText.Replace("\r\n", string.Empty).Contains("\n"));
                    }
                    else
                    {
                        Assert.False(edits[0].NewText.Contains("\r\n"));
                    }
                }));
        }

        [Test]
        public async Task PreviewFormatRangeShouldReturnNoEditsForPartialSelection()
        {
            SetupLanguageService();
            FormatterService.UpdateFormatterSettings(new FormatterSettings
            {
                EnablePreviewFormatter = true
            });
            SetupScriptFile(defaultSqlContents);

            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocRangeFormatRequest(rangeFormatParams, requestContext),
                verify: (edits =>
                {
                    Assert.AreEqual(0, edits.Length);
                }));
        }

        [Test]
        public async Task FormatRangeShouldReturnSingleEdit()
        {
            // Given a document that we want to format
            SetupLanguageService();
            SetupScriptFile(defaultSqlContents);
            // When format document is called
            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocRangeFormatRequest(rangeFormatParams, requestContext),
                verify: (edits =>
                {
                    // Then expect a single edit to be returned and for it to match the standard formatting
                    Assert.AreEqual(1, edits.Length);
                    AssertFormattingEqual(formattedSqlContents, edits[0].NewText);
                }));
        }

        [Test]
        public async Task FormatRangeShouldSkipNonMssqlFile()
        {
            // Given a non-MSSQL document
            SetupLanguageService(skipFile: true);
            SetupScriptFile(defaultSqlContents);
            // When format document is called
            await TestUtils.RunAndVerify<TextEdit[]>(
                test: (requestContext) => FormatterService.HandleDocRangeFormatRequest(rangeFormatParams, requestContext),
                verify: (edits =>
                {
                    // Then expect a single edit to be returned and for it to match the standard formatting
                    Assert.AreEqual(0, edits.Length);
                    LanguageServiceMock.Verify(x => x.ShouldSkipNonMssqlFile(docFormatParams.TextDocument.Uri), Times.Once);
                }));
        }


        [Test]
        public async Task FormatDocumentTelemetryShouldIncludeFormatTypeProperty()
        {
            await RunAndVerifyTelemetryTest(
                // Given a document that we want to format
                preRunSetup: () => SetupLanguageService(),
                // When format document is called
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    // Then expect a telemetry event to have been sent with the right format definition
                    Assert.NotNull(actualParams);
                    Assert.AreEqual(TelemetryEventNames.FormatCode, actualParams.Params.EventName);
                    Assert.AreEqual(TelemetryPropertyNames.DocumentFormatType, actualParams.Params.Properties[TelemetryPropertyNames.FormatType]);
                    AssertFormatterTelemetry(
                        actualParams,
                        TelemetryPropertyNames.LegacyFormatterImplementation,
                        TelemetryPropertyNames.FormatterOutcomeApplied);
                });
        }

        [Test]
        public async Task FormatRangeTelemetryShouldIncludeFormatTypeProperty()
        {
            await RunAndVerifyTelemetryTest(
                // Given a document that we want to format
                preRunSetup: () => SetupLanguageService(),
                // When format range is called
                test: (requestContext) => FormatterService.HandleDocRangeFormatRequest(rangeFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    // Then expect a telemetry event to have been sent with the right format definition
                    Assert.NotNull(actualParams);
                    Assert.AreEqual(TelemetryEventNames.FormatCode, actualParams.Params.EventName);
                    Assert.AreEqual(TelemetryPropertyNames.RangeFormatType, actualParams.Params.Properties[TelemetryPropertyNames.FormatType]);
                    AssertFormatterTelemetry(
                        actualParams,
                        TelemetryPropertyNames.LegacyFormatterImplementation,
                        TelemetryPropertyNames.FormatterOutcomeApplied);

                    // And expect range to have been correctly formatted
                    Assert.AreEqual(1, result.Length);
                    AssertFormattingEqual(formattedSqlContents, result[0].NewText);
                });
        }

        [Test]
        public async Task PreviewFormatDocumentTelemetryShouldIncludeScriptDomOutcome()
        {
            await RunAndVerifyTelemetryTest(
                preRunSetup: () =>
                {
                    SetupLanguageService();
                    FormatterService.UpdateFormatterSettings(new FormatterSettings
                    {
                        EnablePreviewFormatter = true
                    });
                },
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    Assert.AreEqual(1, result.Length);
                    AssertFormatterTelemetry(
                        actualParams,
                        TelemetryPropertyNames.ScriptDomFormatterImplementation,
                        TelemetryPropertyNames.FormatterOutcomeApplied);
                },
                scriptContents: "select 1 as value");
        }

        [Test]
        public async Task PreviewFormatDocumentTelemetryShouldIncludeParseFailureOutcome()
        {
            await RunAndVerifyTelemetryTest(
                preRunSetup: () =>
                {
                    SetupLanguageService();
                    FormatterService.UpdateFormatterSettings(new FormatterSettings
                    {
                        EnablePreviewFormatter = true
                    });
                },
                test: (requestContext) => FormatterService.HandleDocFormatRequest(docFormatParams, requestContext),
                verify: (result, actualParams) =>
                {
                    Assert.AreEqual(0, result.Length);
                    AssertFormatterTelemetry(
                        actualParams,
                        TelemetryPropertyNames.ScriptDomFormatterImplementation,
                        TelemetryPropertyNames.FormatterOutcomeParseFailed);
                    Assert.Greater(actualParams.Params.Measures[TelemetryMeasureNames.ParseErrorCount], 0);
                },
                scriptContents: "select from");
        }

        private async Task RunAndVerifyTelemetryTest(
            Action preRunSetup,
            Func<RequestContext<TextEdit[]>, Task> test,
            Action<TextEdit[], TelemetryParams> verify,
            string scriptContents = null)
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
            if (preRunSetup != null)
            {
                preRunSetup();
            }
            SetupScriptFile(scriptContents ?? defaultSqlContents);

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

        private static void AssertFormatterTelemetry(
            TelemetryParams actualParams,
            string expectedImplementation,
            string expectedOutcome)
        {
            Assert.AreEqual(expectedImplementation, actualParams.Params.Properties[TelemetryPropertyNames.FormatterImplementation]);
            Assert.AreEqual(expectedOutcome, actualParams.Params.Properties[TelemetryPropertyNames.FormatterOutcome]);
            Assert.GreaterOrEqual(actualParams.Params.Measures[TelemetryMeasureNames.FormatterDurationMs], 0);
            Assert.GreaterOrEqual(actualParams.Params.Measures[TelemetryMeasureNames.ParseErrorCount], 0);
        }

        public static async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Mock<RequestContext<T>> contextMock, Action verify)
        {
            await test(contextMock.Object);
            VerifyResult(contextMock, verify);
        }

        public static void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action verify)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            verify();
        }

        private static void AssertFormattingEqual(string expected, string actual)
        {
            if (expected != actual)
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
