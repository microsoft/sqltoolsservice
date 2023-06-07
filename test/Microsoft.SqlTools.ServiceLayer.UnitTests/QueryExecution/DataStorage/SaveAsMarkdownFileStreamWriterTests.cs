//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public partial class SaveAsMarkdownFileStreamWriterTests
    {
        // Regex: Matches '|' not preceded by a '\'
        [GeneratedRegex("(?<!\\\\)\\|", RegexOptions.Compiled)]
        private static partial Regex GetUnescapedPipeRegex();

        [Test]
        public void Constructor_NullStream()
        {
            // Act
            TestDelegate action = () => _ = new SaveAsMarkdownFileStreamWriter(
                null,
                new SaveResultsAsMarkdownRequestParams(),
                Array.Empty<DbColumnWrapper>()
            );

            // Assert
            Assert.That(action, Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_NullColumns()
        {
            // Act
            TestDelegate action = () => _ = new SaveAsMarkdownFileStreamWriter(
                Stream.Null,
                new SaveResultsAsMarkdownRequestParams(),
                null
            );

            // Assert
            Assert.That(action, Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_WithoutSelectionWithHeader_WritesHeaderWithAllColumns()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsMarkdownRequestParams { IncludeHeaders = true };
            var (columns, _) = GetTestValues(2);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I construct a Markdown file writer
            using var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns);

            // Then:
            // ... It should have written a line
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(2), "Expected two lines of output");

            // ... It should have written a header line like |col1|col2|
            // ... It should have written a separator line like |---|---|
            ValidateLine(lines[0], columns.Select(c => c.ColumnName));
            ValidateLine(lines[1], Enumerable.Repeat("---", columns.Length));
        }

        [Test]
        public void Constructor_WithSelectionWithHeader_WritesHeaderWithSelectedColumns()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsMarkdownRequestParams
            {
                IncludeHeaders = true,
                ColumnStartIndex = 1,
                ColumnEndIndex = 2,
                RowStartIndex = 0,      // Including b/c it is required to be a "save selection"
                RowEndIndex = 10,
            };
            var (columns, _) = GetTestValues(4);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I construct a Markdown file writer
            using var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns);

            // Then:
            // ... It should have written a line
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(2), "Expected two lines of output");

            // ... It should have written a header line like |col1|col2|
            // ... It should have written a separator line like |---|---|
            ValidateLine(lines[0], columns.Skip(1).Take(2).Select(c => c.ColumnName));
            ValidateLine(lines[1], Enumerable.Repeat("---", 2));
        }

        [Test]
        public void Constructor_WithoutSelectionWithoutHeader_DoesNotWriteHeader()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should not be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsMarkdownRequestParams { IncludeHeaders = false };
            var (columns, _) = GetTestValues(2);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I construct a Markdown file writer
            using var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns);

            // Then:
            // ... It not have written anything
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines, Is.Empty);
        }

        [TestCase("Something\rElse")] // Contains carriage return
        [TestCase("Something\nElse")] // Contains line feed
        [TestCase("Something\r\nElse")] // Contains carriage return
        public void EncodeMarkdownField_ContainsNewlineCharacters_ShouldConvertToBr(string field)
        {
            // If: I Markdown encode a field that has a newline
            string output = SaveAsMarkdownFileStreamWriter.EncodeMarkdownField(field);

            // Then: It should replace it the newline character(s) with a <br />
            const string expected = "Something<br />Else";
            Assert.That(output, Is.EqualTo(expected));
        }

        [Test]
        public void EncodeMarkdownField_ContainsDelimiter_ShouldBeEscaped()
        {
            // If: I Markdown encode a field that has a pipe in it
            const string input = "|Something|Else|";
            string output = SaveAsMarkdownFileStreamWriter.EncodeMarkdownField(input);

            // Then: It should escape the pipe character
            const string expected = @"\|Something\|Else\|";
            Assert.AreEqual(expected, output);
        }

        // @TODO: Convert excess whitespace to &nbsp; on user choice
        // [TestCase("\tSomething")]       // Starts with tab
        // [TestCase("Something\t")]       // Ends with tab
        // [TestCase(" Something")]        // Starts with space
        // [TestCase("Something ")]        // Ends with space
        // [TestCase(" Something ")]       // Starts and ends with space
        // [TestCase("Something    else")] // Contains multiple consecutive spaces
        // public void EncodeMarkdownField_WhitespaceAtFrontOrBack_ShouldBeWrapped(string field)
        // {
        //     // Setup: Create MarkdownFileStreamWriter that specifies the text identifier and field separator
        //     var writer = GetWriterForEncodingTests(null, null, null);
        //
        //     // If: I Markdown encode a field that has forbidden characters in it
        //     string output = writer.EncodeMarkdownField(field);
        //
        //     // Then: It should wrap it in quotes
        //     Assert.True(Regex.IsMatch(output, "^\".*\"$", RegexOptions.Singleline));
        // }

        [Test]
        public void EncodeMarkdownField_ContainsHtmlEntityCharacters_ShouldConvertToHtmlEntities()
        {
            // If: I Markdown encode a field that has html entity characters in it
            const string input = "<<>>&®±ßüÁ";
            string output = SaveAsMarkdownFileStreamWriter.EncodeMarkdownField(input);

            // Then: The entity characters should be HTML encoded
            const string expected = "&lt;&lt;&gt;&gt;&amp;&#174;&#177;&#223;&#252;&#193;";
            Assert.That(output, Is.EqualTo(expected));
        }

        [Test]
        public void EncodeMarkdownField_Null()
        {
            // If: I Markdown encode a null
            string output = SaveAsMarkdownFileStreamWriter.EncodeMarkdownField(null);

            // Then: there should be a string version of null returned
            Assert.That(output, Is.EqualTo("NULL"));
        }

        [Test]
        public void WriteRow_WithoutColumnSelection()
        {
            // Setup:
            // ... Create a request params that has no selection made or header enabled
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsMarkdownRequestParams();
            var (columns, data) = GetTestValues(2);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I write a row
            using (var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns))
            {
                writer.WriteRow(data, columns);
            }

            // Then: It should write one line with the two cells
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(1), "Expected one line of output");

            ValidateLine(lines[0], data.Select(c => c.DisplayValue));
        }

        [Test]
        public void WriteRow_WithColumnSelection()
        {
            // Setup:
            // ... Create a request params that selects n-1 columns from the front and back
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsMarkdownRequestParams
            {
                ColumnStartIndex = 1,
                ColumnEndIndex = 2,
                RowStartIndex = 0,          // Including b/c it is required to be a "save selection"
                RowEndIndex = 10
            };
            var (columns, data) = GetTestValues(4);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I write a row
            using (var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns))
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written one line with the two cells written
            var lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(1), "Expected one line of output");

            ValidateLine(lines[0], data.Skip(1).Take(2).Select(c => c.DisplayValue));
        }

        [Test]
        public void WriteRow_EncodingTest()
        {
            // Setup:
            // ... Create default request params
            // ... Create a set of data to write that contains characters that should be encoded
            // ... Create a memory location to store the data
            // @TODO: Add case to test string for non-breaking spaces
            var requestParams = new SaveResultsAsMarkdownRequestParams();
            var columns = new[] { new DbColumnWrapper(new TestDbColumn("column")) };
            var data = new[] { new DbCellValue { DisplayValue = "|Something|\n|<<>>&|" } };
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I write a row
            using (var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns))
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written one line with the data properly encoded
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.That(lines.Length, Is.EqualTo(1), "Expected one line of output");

            ValidateLine(lines[0], new[] { "\\|Something\\|<br />\\|&lt;&lt;&gt;&gt;&amp;\\|" });
        }

        [Test]
        public void WriteRow_CustomLineSeparator()
        {
            // Setup:
            // ... Create a request params that has custom line separator
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsMarkdownRequestParams
            {
                LineSeparator = "$$",
                IncludeHeaders = true,
            };
            var (columns, data) = GetTestValues(2);
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I set write a row
            using (var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns))
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... The lines should be split by the custom line separator
            var lines = ParseWriterOutput(output, "$$");
            Assert.That(lines.Length, Is.EqualTo(3), "Expected three lines of output");

            // Note: Header output has been tested in constructor tests
        }

        [Test]
        public void WriteRow_CustomEncoding()
        {
            // Setup:
            // ... Create a request params that uses a custom encoding
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsMarkdownRequestParams { Encoding = "utf-16", };
            var data = new[] { new DbCellValue { DisplayValue = "ü" } };
            var columns = new[] { new DbColumnWrapper(new TestDbColumn("column1")) };
            byte[] output = new byte[8192];
            using var outputStream = new MemoryStream(output);

            // If: I write a row
            using (var writer = new SaveAsMarkdownFileStreamWriter(outputStream, requestParams, columns))
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written the umlaut as an HTML entity in utf-16le
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string outputString = Encoding.Unicode.GetString(output).TrimEnd('\0', '\r', '\n');
            Assert.That(outputString, Is.EqualTo("|&#252;|"));
        }

        private static (DbColumnWrapper[] columns, DbCellValue[] cells) GetTestValues(int columnCount)
        {
            var data = new DbCellValue[columnCount];
            var columns = new DbColumnWrapper[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                data[i] = new DbCellValue { DisplayValue = $"item{i}" };
                columns[i] = new DbColumnWrapper(new TestDbColumn($"column{i}"));
            }
            return (columns, data);
        }

        private static string[] ParseWriterOutput(byte[] output, string lineSeparator)
        {
            string outputString = Encoding.UTF8.GetString(output).Trim('\0');
            string[] lines = outputString.Split(lineSeparator);

            // Make sure the file ends with a new line and return all but the meaningful lines
            Assert.That(lines[^1], Is.Empty, "Output did not end with a newline");
            return lines.Take(lines.Length - 1).ToArray();
        }

        private static void ValidateLine(string line, IEnumerable<string> expectedCells)
        {
            string[] cells = GetUnescapedPipeRegex().Split(line);
            string[] expectedCellsArray = expectedCells as string[] ?? expectedCells.ToArray();
            Assert.That(cells.Length - 2, Is.EqualTo(expectedCellsArray.Length), "Wrong number of cells in output");

            Assert.That(cells[0], Is.Empty, "Row did not start with |");
            Assert.That(cells[^1], Is.Empty, "Row did not end with |");

            for (int i = 0; i < expectedCellsArray.Length; i++)
            {
                Assert.That(cells[i + 1], Is.EqualTo(expectedCellsArray[i]), "Wrong cell value");
            }
        }
    }
}
