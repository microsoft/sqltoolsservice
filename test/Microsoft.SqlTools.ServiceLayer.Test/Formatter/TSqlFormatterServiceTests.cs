//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Formatter.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Formatter
{
    public class TSqlFormatterServiceTests : FormatterUnitTestsBase
    {
        private Mock<ServiceLayer.Workspace.Workspace> workspaceMock = new Mock<ServiceLayer.Workspace.Workspace>();
        private TextDocumentIdentifier textDocument;
        DocumentFormattingParams docFormatParams;

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
