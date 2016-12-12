// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamWriterTests
    {
        [Theory]
        [InlineData("Something\rElse")]
        [InlineData("Something\nElse")]
        [InlineData("Something\"Else")]
        [InlineData("Something,Else")]
        [InlineData("\tSomething")]
        [InlineData("Something\t")]
        [InlineData(" Something")]
        [InlineData("Something ")]
        [InlineData(" \t\r\n\",\r\n\"\r ")]
        public void EncodeCsvFieldShouldWrap(string field)
        {
            // If: I CSV encode a field that has forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field);

            // Then: It should wrap it in quotes
            Assert.True(Regex.IsMatch(output, "^\".*")
                && Regex.IsMatch(output, ".*\"$"));
        }

        [Theory]
        [InlineData("Something")]
        [InlineData("Something valid.")]
        [InlineData("Something\tvalid")]
        public void EncodeCsvFieldShouldNotWrap(string field)
        {
            // If: I CSV encode a field that does not have forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field);

            // Then: It should not wrap it in quotes
            Assert.False(Regex.IsMatch(output, "^\".*\"$"));
        }

        [Fact]
        public void EncodeCsvFieldReplace()
        {
            // If: I CSV encode a field that has a double quote in it,
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField("Some\"thing");

            // Then: It should be replaced with double double quotes
            Assert.Equal("\"Some\"\"thing\"", output);
        }

        [Fact]
        public void WriteRowWithoutColumnSelectionOrHeader()
        {
            // Setup: 
            // ... Create a request params that has no selection made
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams();
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "item1" },
                new DbCellValue { DisplayValue = "item2" }
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2"))
            };
            byte[] output = new byte[8192];

            // If: I write a row
            SaveAsCsvFileStreamWriter writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then: It should write one line with 2 items, comma delimited
            string outputString = Encoding.Unicode.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            Assert.Equal(1, lines.Length);
            string[] values = lines[0].Split(',');
            Assert.Equal(2, values.Length);
        }

        [Fact]
        public void WriteRowWithHeader()
        {
            // Setup:
            // ... Create a request params that has no selection made, headers should be printed
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                IncludeHeaders = true
            };
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "item1" },
                new DbCellValue { DisplayValue = "item2" }
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2"))
            };
            byte[] output = new byte[8192];

            // If: I write a row
            SaveAsCsvFileStreamWriter writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written two lines
            string outputString = Encoding.Unicode.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Equal(2, lines.Length);

            // ... It should have written a header line with two, comma separated names
            string[] headerValues = lines[0].Split(',');
            Assert.Equal(2, headerValues.Length);
            for (int i = 0; i < columns.Count; i++)
            {
                Assert.Equal(columns[i].ColumnName, headerValues[i]);
            }

            // Note: No need to check values, it is done as part of the previous test
        }

        [Fact]
        public void WriteRowWithColumnSelection()
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
                RowEndIndex = 10,
                IncludeHeaders = true       // Including headers to test both column selection logic
            };
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "item1" },
                new DbCellValue { DisplayValue = "item2" },
                new DbCellValue { DisplayValue = "item3" },
                new DbCellValue { DisplayValue = "item4" }
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
            {
                new DbColumnWrapper(new TestDbColumn("column1")),
                new DbColumnWrapper(new TestDbColumn("column2")),
                new DbColumnWrapper(new TestDbColumn("column3")),
                new DbColumnWrapper(new TestDbColumn("column4"))
            };
            byte[] output = new byte[8192];

            // If: I write a row
            SaveAsCsvFileStreamWriter writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written two lines
            string outputString = Encoding.Unicode.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Equal(2, lines.Length);

            // ... It should have written a header line with two, comma separated names
            string[] headerValues = lines[0].Split(',');
            Assert.Equal(2, headerValues.Length);
            for (int i = 1; i <= 2; i++)
            {
                Assert.Equal(columns[i].ColumnName, headerValues[i-1]);
            }

            // ... The second line should have two, comma separated values
            string[] dataValues = lines[1].Split(',');
            Assert.Equal(2, dataValues.Length);
            for (int i = 1; i <= 2; i++)
            {
                Assert.Equal(data[i].DisplayValue, dataValues[i-1]);
            }
        }
    }
}
