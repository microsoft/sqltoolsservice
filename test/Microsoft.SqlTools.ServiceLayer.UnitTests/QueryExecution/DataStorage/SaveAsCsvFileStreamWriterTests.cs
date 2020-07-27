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
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public class SaveAsCsvFileStreamWriterTests
    {
        [Test]
        public void EncodeCsvFieldShouldWrap(
            [Values("Something\rElse",
        "Something\nElse",
        "Something\"Else",
        "Something,Else",
        "\tSomething",
        "Something\t",
        " Something",
        "Something ",
        " \t\r\n\",\r\n\"\r ")] string field)
        {
            // If: I CSV encode a field that has forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field, ',', '\"');

            // Then: It should wrap it in quotes
            Assert.True(Regex.IsMatch(output, "^\".*")
                && Regex.IsMatch(output, ".*\"$"));
        }

        [Test]
        public void EncodeCsvFieldShouldNotWrap(
            [Values(
            "Something",
            "Something valid.",
            "Something\tvalid"
            )] string field)
        {
            // If: I CSV encode a field that does not have forbidden characters in it
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(field, ',', '\"');

            // Then: It should not wrap it in quotes
            Assert.False(Regex.IsMatch(output, "^\".*\"$"));
        }

        [Test]
        public void EncodeCsvFieldReplace()
        {
            // If: I CSV encode a field that has a double quote in it,
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField("Some\"thing", ',', '\"');

            // Then: It should be replaced with double double quotes
            Assert.AreEqual("\"Some\"\"thing\"", output);
        }

        [Test]
        public void EncodeCsvFieldNull()
        {
            // If: I CSV encode a null
            string output = SaveAsCsvFileStreamWriter.EncodeCsvField(null, ',', '\"');

            // Then: there should be a string version of null returned
            Assert.AreEqual("NULL", output);
        }

        [Test]
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
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual(1, lines.Length);
            string[] values = lines[0].Split(',');
            Assert.AreEqual(2, values.Length);
        }

        [Test]
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
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);

            // ... It should have written a header line with two, comma separated names
            string[] headerValues = lines[0].Split(',');
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 0; i < columns.Count; i++)
            {
                Assert.AreEqual(columns[i].ColumnName, headerValues[i]);
            }

            // Note: No need to check values, it is done as part of the previous test
        }

        [Test]
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
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);

            // ... It should have written a header line with two, comma separated names
            string[] headerValues = lines[0].Split(',');
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 1; i <= 2; i++)
            {
                Assert.AreEqual(columns[i].ColumnName, headerValues[i - 1]);
            }

            // ... The second line should have two, comma separated values
            string[] dataValues = lines[1].Split(',');
            Assert.AreEqual(2, dataValues.Length);
            for (int i = 1; i <= 2; i++)
            {
                Assert.AreEqual(data[i].DisplayValue, dataValues[i - 1]);
            }
        }

        [Test]
        public void WriteRowWithCustomDelimiters()
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
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            string[] lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);

            // ... It should have written a header line with two, pipe("|") separated names
            string[] headerValues = lines[0].Split('|');
            Assert.AreEqual(2, headerValues.Length);
            for (int i = 0; i < columns.Count; i++)
            {
                Assert.AreEqual(columns[i].ColumnName, headerValues[i]);
            }

            // Note: No need to check values, it is done as part of the previous tests
        }

        [Test]
        public void WriteRowsWithCustomLineSeperator()
        {
            // Setup:
            // ... Create a request params that has custom line seperator then this seperator should be used
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

            byte[] output;
            string outputString;
            string[] lines;
            SaveAsCsvFileStreamWriter writer;

            // If: I set default seperator and write a row
            requestParams.LineSeperator = null;
            output = new byte[8192];
            writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have splitten the lines by system's default line seperator
            outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            lines = outputString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);

            // If: I set \n (line feed) as seperator and write a row
            requestParams.LineSeperator = "\n";
            output = new byte[8192];
            writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have splitten the lines by \n
            outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            lines = outputString.Split(new[] { '\n' }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);

            // If: I set \r\n (carriage return + line feed) as seperator and write a row
            requestParams.LineSeperator = "\r\n";
            output = new byte[8192];
            writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have splitten the lines by \r\n
            outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            lines = outputString.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.AreEqual(2, lines.Length);
            
        }

        [Test]
        public void WriteRowWithCustomTextIdentifier()
        {
            // Setup:
            // ... Create a request params that has a text identifier set say single quotation marks("'") then this text identifier should be used
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams()
            {
                TextIdentifier = "\'",
                Delimiter = ";"
            };
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "item;1" },
                new DbCellValue { DisplayValue = "item,2" },
                new DbCellValue { DisplayValue = "item\"3" },
                new DbCellValue { DisplayValue = "item\'4" }
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
            // ... It should have splitten the columns by delimiter, embedded in text identifier when field contains delimiter or the text identifier
            string outputString = Encoding.UTF8.GetString(output).TrimEnd('\0', '\r', '\n');
            Assert.AreEqual("\'item;1\';item,2;item\"3;\'item\'\'4\'", outputString);
        }

        [Test]
        public void WriteRowWithCustomEncoding()
        {
            // Setup:
            // ... Create a request params that has custom delimiter say pipe("|") then this delimiter should be used
            // ... Create a set of data to write
            // ... Create a memory location to store the data
            var requestParams = new SaveResultsAsCsvRequestParams
            {
                Encoding = "Windows-1252"
            };
            List<DbCellValue> data = new List<DbCellValue>
            {
                new DbCellValue { DisplayValue = "ü" }
            };
            List<DbColumnWrapper> columns = new List<DbColumnWrapper>
            {
                new DbColumnWrapper(new TestDbColumn("column1"))
            };
            byte[] output = new byte[8192];

            // If: I write a row
            SaveAsCsvFileStreamWriter writer = new SaveAsCsvFileStreamWriter(new MemoryStream(output), requestParams);
            using (writer)
            {
                writer.WriteRow(data, columns);
            }

            // Then:
            // ... It should have written the umlaut using the encoding Windows-1252
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string outputString = Encoding.GetEncoding("Windows-1252").GetString(output).TrimEnd('\0', '\r', '\n');
            Assert.AreEqual("ü", outputString);

        }

    }
}
