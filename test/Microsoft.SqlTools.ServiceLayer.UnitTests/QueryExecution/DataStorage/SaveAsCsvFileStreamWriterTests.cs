//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamWriterTests
    {
        [Test]
        public void Constructor_WithoutSelectionWithHeader_WritesHeaderWithAllColumns()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsCsvRequestParams { IncludeHeaders = true };
            var (columns, _) = GetTestValues(2);
            using var outputStream = new MemoryStream();
            byte[] output = new byte[8192];

            // If: I construct a CSV file writer
            var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns);
            writer.Dispose();

            // Then:
            // ... It should have written a line
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.AreEqual(1, lines.Length);

            // ... It should have written a header line with two comma separated names
            string[] headerValues = lines[0].Split(",");
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 0; i < columns.Length; i++)
            {
                Assert.AreEqual(columns[i].ColumnName, headerValues[i]);
            }
        }

        [Test]
        public void Constructor_WithSelectionWithHeader_WritesHeaderWithSelectedColumns()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                IncludeHeaders = true,
                ColumnStartIndex = 1,
                ColumnEndIndex = 2,
                RowStartIndex = 0,      // Including b/c it is required to be a "save selection"
                RowEndIndex = 10
            };
            var (columns, _) = GetTestValues(4);
            using var outputStream = new MemoryStream();
            byte[] output = new byte[8192];

            // If: I construct a CSV file writer
            var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns);
            writer.Dispose();

            // Then:
            // ... It should have written a line
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.AreEqual(1, lines.Length);

            // ... It should have written a header line with two comma separated names
            string[] headerValues = lines[0].Split(",");
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(columns[i + 1].ColumnName, headerValues[i]);
            }
        }

        [Test]
        public void Constructor_WithoutSelectionWithoutHeader_DoesNotWriteHeader()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should not be printed
            // ... Create a set of columns
            // --- Create a memory location to store the output
            var requestParams = new SaveResultsAsCsvRequestParams { IncludeHeaders = false };
            var (columns, _) = GetTestValues(2);
            byte[] output = new byte[8192];

            // If: I construct a CSV file writer
            var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns);
            writer.Dispose();

            // Then:
            // ... It not have written anything
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.IsEmpty(lines);
        }

        [TestCase("Something\rElse")]
        [TestCase("Something\nElse")]
        [TestCase("Something\"Else")]
        [TestCase("Something,Else")]
        [TestCase("\tSomething")]
        [TestCase("Something\t")]
        [TestCase(" Something")]
        [TestCase("Something ")]
        [TestCase(" \t\r\n\",\r\n\"\r ")]
        public void EncodeCsvField_DefaultControlCharacters_ShouldWrap(string field)
        {
            // Setup: Initialize a new CSV writer w/default separators
            using var writer = GetWriterForEncodingTests(null, null, null);

            // If: I CSV encode a field that has forbidden characters in it
            string output = writer.EncodeCsvField(field);

            // Then: It should wrap it in quotes
            Assert.True(Regex.IsMatch(output, "^\".*")); // Starts with "
            Assert.True(Regex.IsMatch(output, ".*\"$")); // Ends with "
        }

        [TestCase("Something")]
        [TestCase("Something valid.")]
        [TestCase("Something\tvalid")]
        public void EncodeCsvField_DefaultControlCharacters_ShouldNotWrap(string field)
        {
            // Setup: Initialize a new CSV writer w/default separators
            using var writer = GetWriterForEncodingTests(null, null, null);

            // If: I CSV encode a field that does not have forbidden characters in it
            string output = writer.EncodeCsvField(field);

            // Then: It should not wrap it in quotes
            Assert.False(Regex.IsMatch(output, "^\".*\"$"));
        }

        [TestCase(null, "Some\"thing", "\"Some\"\"thing\"")]
        [TestCase("*", "Some*thing", "*Some**thing*")]
        public void EncodeCsvField_EscapeIdentifier(string identifier, string input, string expectedOutput)
        {
            // Setup: Initialize a new CSV writer w/specified identifier
            using var writer = GetWriterForEncodingTests(null, identifier, null);

            // If: I CSV encode a field that has a double quote in it,
            string output = writer.EncodeCsvField(input);

            // Then: It should be replaced with double double quotes
            Assert.AreEqual(expectedOutput, output);
        }

        [TestCase("Something$$Else")]
        [TestCase("Something*Else")]
        [TestCase("Something|Else")]
        public void EncodeCsvField_CustomControlCharacters_ShouldWrap(string field)
        {
            // Setup: Initialize a new CSV writer w/non-default separators
            // ... `|ignored` as field delimiter
            // ... `*ignored` as text identifier
            // ... `$$` as new line
            using var writer = GetWriterForEncodingTests("|ignored", "*ignored", "$$");

            // If: I CSV encode a field
            string output = writer.EncodeCsvField(field);

            // Then: I should be wrapped in quotes
            Assert.True(Regex.IsMatch(output, "^\\*.*")); // Starts with *
            Assert.True(Regex.IsMatch(output, ".*\\*$")); // Ends with *
        }

        [Test]
        public void EncodeCsvField_Null()
        {
            // Setup: Initialize a new CSV writer w/ default control characters
            using var writer = GetWriterForEncodingTests(null, null, null);

            // If: I CSV encode a null
            string output = writer.EncodeCsvField(null);

            // Then: there should be a string version of null returned
            Assert.AreEqual("NULL", output);
        }

        [Test]
        public void WriteRow_WithoutColumnSelection()
        {
            // Setup:
            // ... Create a request params that has no selection made
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams();
            var (columns, data) = GetTestValues(2);
            byte[] output = new byte[8192];

            // If: I write a row
            using (var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns))
            {
                writer.WriteRow(data);
            }

            // Then: It should write one line with 2 items, comma delimited
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.AreEqual(1, lines.Length);

            string[] values = lines[0].Split(',');
            Assert.AreEqual(2, values.Length);
        }

        [Test]
        public void WriteRow_WithColumnSelection()
        {
            // Setup:
            // ... Create a request params that selects n-1 columns from the front and back
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                ColumnStartIndex = 1,
                ColumnEndIndex = 2,
                RowStartIndex = 0,          // Including b/c it is required to be a "save selection"
                RowEndIndex = 10
            };
            var (columns, data) = GetTestValues(4);
            byte[] output = new byte[8192];

            // If: I write a row
            SaveAsCsvFileStreamWriter writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns);
            using (writer)
            {
                writer.WriteRow(data);
            }

            // Then:
            // ... It should have written one line
            var lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.AreEqual(1, lines.Length);

            // ... The line should have two, comma separated values
            string[] dataValues = lines[0].Split(',');
            Assert.AreEqual(2, dataValues.Length);
            for (int i = 1; i <= 2; i++)
            {
                Assert.AreEqual(data[i].DisplayValue, dataValues[i - 1]);
            }
        }

        [Test]
        public void WriteRow_CustomDelimiter()
        {
            // Setup:
            // ... Create a request params that has custom delimiter say pipe("|") then this delimiter should be used
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                Delimiter = "|",
                IncludeHeaders = true
            };
            var (columns, data) = GetTestValues(2);
            byte[] output = new byte[8192];

            // If: I write a row
            using (var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns))
            {
                writer.WriteRow(data);
            }

            // Then:
            // ... It should have written two lines
            string[] lines = ParseWriterOutput(output, Environment.NewLine);
            Assert.AreEqual(2, lines.Length);

            // ... It should have written a header line with two, pipe("|") separated names
            string[] headerValues = lines[0].Split('|');
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 0; i < columns.Length; i++)
            {
                Assert.AreEqual(columns[i].ColumnName, headerValues[i]);
            }

            // Note: No need to check values, it is done as part of the previous tests
        }

        [Test]
        public void WriteRow_CustomLineSeparator()
        {
            // Setup:
            // ... Create a request params that has custom line separator
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                LineSeperator = "$$",
                IncludeHeaders = true
            };
            var (columns, data) = GetTestValues(2);
            byte[] output = new byte[8192];

            // If: I set write a row
            using (var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns))
            {
                writer.WriteRow(data);
            }

            // Then:
            // ... The lines should be split by the custom line separator
            var lines = ParseWriterOutput(output, "$$");
            Assert.AreEqual(2, lines.Length);
        }

        [Test]
        public void WriteRow_CustomEncoding()
        {
            // Setup:
            // ... Create a request params that uses a custom encoding
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                Encoding = "Windows-1252"
            };
            var data = new[] { new DbCellValue { DisplayValue = "ü" } };
            var columns = new[] { new DbColumnWrapper(new TestDbColumn("column1")) };
            byte[] output = new byte[8192];

            // If: I write a row
            using (var writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams, columns))
            {
                writer.WriteRow(data);
            }

            // Then:
            // ... It should have written the umlaut using the encoding Windows-1252
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string outputString = Encoding.GetEncoding("Windows-1252").GetString(output).TrimEnd('\0', '\r', '\n');
            Assert.AreEqual("ü", outputString);

        }

        private static (DbColumnWrapper[] columns, DbCellValue[] cells) GetTestValues(int columnCount)
        {
            var data = new DbCellValue[columnCount];
            var columns = new DbColumnWrapper[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                data[i] = new DbCellValue { DisplayValue = $"item{i}"};
                columns[i] = new DbColumnWrapper(new TestDbColumn($"column{i}"));
            }
            return (columns, data);
        }

        private static SaveAsCsvFileStreamWriter GetWriterForEncodingTests(string delimiter, string identifier, string lineSeparator)
        {
            var settings = new SaveResultsAsCsvRequestParams
            {
                Delimiter = delimiter,
                IncludeHeaders = false,
                LineSeperator = lineSeparator,
                TextIdentifier = identifier,
            };
            var mockStream = new Mock<Stream>();
            var mockColumns = Array.Empty<DbColumnWrapper>();
            return new SaveAsCsvFileStreamWriter(mockStream.Object, settings, mockColumns);
        }

        private static string[] ParseWriterOutput(byte[] output, string lineSeparator)
        {
            string outputString = Encoding.UTF8.GetString(output).Trim('\0');
            string[] lines = outputString.Split(new[] { lineSeparator }, StringSplitOptions.None);

            // Make sure the file ends with a new line and return all but the meaningful lines
            Assert.IsEmpty(lines[lines.Length - 1]);
            return lines.Take(lines.Length - 1).ToArray();
        }
    }
}
