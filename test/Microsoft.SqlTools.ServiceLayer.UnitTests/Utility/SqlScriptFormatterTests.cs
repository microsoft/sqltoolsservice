//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public class SqlScriptFormatterTests
    {
        #region Format Identifier Tests

        [Fact]
        public void FormatIdentifierNull()
        {
            // If: I attempt to format null as an identifier
            // Then: I should get an exception thrown
            Assert.Throws<ArgumentNullException>(() => SqlScriptFormatter.FormatIdentifier(null));
        }

        [Theory]
        [InlineData("test", "[test]")]          // No escape characters
        [InlineData("]test", "[]]test]")]       // Escape character at beginning
        [InlineData("te]st", "[te]]st]")]       // Escape character in middle
        [InlineData("test]", "[test]]]")]       // Escape character at end
        [InlineData("t]]est", "[t]]]]est]")]    // Multiple escape characters
        public void FormatIdentifierTest(string value, string expectedOutput)
        {
            // If: I attempt to format a value as an identifier
            string output = SqlScriptFormatter.FormatIdentifier(value);

            // Then: The output should match the expected output
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("test", "[test]")]                          // No splits, no escape characters
        [InlineData("test.test", "[test].[test]")]              // One split, no escape characters
        [InlineData("test.te]st", "[test].[te]]st]")]           // One split, one escape character
        [InlineData("test.test.test", "[test].[test].[test]")]  // Two splits, no escape characters
        public void FormatMultipartIdentifierTest(string value, string expectedOutput)
        {
            // If: I attempt to format a value as a multipart identifier
            string output = SqlScriptFormatter.FormatMultipartIdentifier(value);

            // Then: The output should match the expected output
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [MemberData(nameof(GetMultipartIdentifierArrays))]
        public void FormatMultipartIdentifierArrayTest(string expectedOutput, string[] splits)
        {
            // If: I attempt to format a value as a multipart identifier
            string output = SqlScriptFormatter.FormatMultipartIdentifier(splits);

            // Then: The output should match the expected output
            Assert.Equal(expectedOutput, output);
        }

        public static IEnumerable<object[]> GetMultipartIdentifierArrays
        {
            get
            {
                yield return new object[] {"[test]", new[] {"test"}};                                   // No splits, no escape characters
                yield return new object[] {"[test].[test]", new[] {"test", "test"}};                    // One split, no escape characters
                yield return new object[] {"[test].[te]]st]", new[] {"test", "te]st"}};                 // One split, one escape character
                yield return new object[] {"[test].[test].[test]", new[] {"test", "test", "test"}};     // Two splits, no escape characters
            }
        }

        #endregion

        #region FormatValue Tests

        [Fact]
        public void NullDbCellTest()
        {
            // If: I attempt to format a null db cell
            // Then: It should throw
            Assert.Throws<ArgumentNullException>(() => SqlScriptFormatter.FormatValue(null, new FormatterTestDbColumn(null)));
        }

        [Fact]
        public void NullDbColumnTest()
        {
            // If: I attempt to format a null db column
            // Then: It should throw
            Assert.Throws<ArgumentNullException>(() => SqlScriptFormatter.FormatValue(new DbCellValue(), null));
        }

        public void UnsupportedColumnTest()
        {
            // If: I attempt to format an unsupported datatype
            // Then: It should throw
            DbColumn column = new FormatterTestDbColumn("unsupported");
            Assert.Throws<ArgumentOutOfRangeException>(() => SqlScriptFormatter.FormatValue(new DbCellValue(), column));
        }

        [Fact]
        public void NullTest()
        {
            // If: I attempt to format a db cell that contains null
            // Then: I should get the null string back
            string formattedString = SqlScriptFormatter.FormatValue(new DbCellValue(), new FormatterTestDbColumn(null));
            Assert.Equal(SqlScriptFormatter.NullString, formattedString);
        }


        [Theory]
        [InlineData("BIGINT")]
        [InlineData("INT")]
        [InlineData("SMALLINT")]
        [InlineData("TINYINT")]
        public void IntegerNumericTest(string dataType)
        {
            // Setup: Build a column and cell for the integer type column
            DbColumn column = new FormatterTestDbColumn(dataType);
            DbCellValue cell = new DbCellValue { RawObject = (long)123 };

            // If: I attempt to format an integer type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a long
            Assert.Equal(cell.RawObject, long.Parse(output));
        }

        [Theory]
        [InlineData("MONEY", "MONEY", null, null)]
        [InlineData("SMALLMONEY", "SMALLMONEY", null, null)]
        [InlineData("NUMERIC", @"NUMERIC\(\d+, \d+\)", 18, 0)]
        [InlineData("DECIMAL", @"DECIMAL\(\d+, \d+\)", 18, 0)]
        public void DecimalTest(string dataType, string regex, int? precision, int? scale)
        {
            // Setup: Build a column and cell for the decimal type column
            DbColumn column = new FormatterTestDbColumn(dataType, precision, scale);
            DbCellValue cell = new DbCellValue { RawObject = 123.45m };

            // If: I attempt to format a decimal type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: It should match a something like CAST(123.45 AS MONEY)
            Regex castRegex = new Regex($@"CAST\([\d\.]+ AS {regex}", RegexOptions.IgnoreCase);
            Assert.True(castRegex.IsMatch(output));
        }

        [Fact]
        public void DoubleTest()
        {
            // Setup: Build a column and cell for the approx numeric type column
            DbColumn column = new FormatterTestDbColumn("FLOAT");
            DbCellValue cell = new DbCellValue { RawObject = 3.14159d };

            // If: I attempt to format a approx numeric type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a double
            Assert.Equal(cell.RawObject, double.Parse(output));
        }

        [Fact]
        public void FloatTest()
        {
            // Setup: Build a column and cell for the approx numeric type column
            DbColumn column = new FormatterTestDbColumn("REAL");
            DbCellValue cell = new DbCellValue { RawObject = (float)3.14159 };

            // If: I attempt to format a approx numeric type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a double
            Assert.Equal(cell.RawObject, float.Parse(output));
        }

        [Theory]
        [InlineData("SMALLDATETIME")]
        [InlineData("DATETIME")]
        [InlineData("DATETIME2")]
        [InlineData("DATE")]
        public void DateTimeTest(string dataType)
        {
            // Setup: Build a column and cell for the datetime type column
            DbColumn column = new FormatterTestDbColumn(dataType);
            DbCellValue cell = new DbCellValue { RawObject = DateTime.Now };

            // If: I attempt to format a datetime type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a datetime
            Regex dateTimeRegex = new Regex("N'(.*)'");
            DateTime outputDateTime;
            Assert.True(DateTime.TryParse(dateTimeRegex.Match(output).Groups[1].Value, out outputDateTime));
        }

        [Fact]
        public void DateTimeOffsetTest()
        {
            // Setup: Build a column and cell for the datetime offset type column
            DbColumn column = new FormatterTestDbColumn("DATETIMEOFFSET");
            DbCellValue cell = new DbCellValue { RawObject = DateTimeOffset.Now };

            // If: I attempt to format a datetime offset type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a datetime offset
            Regex dateTimeRegex = new Regex("N'(.*)'");
            DateTimeOffset outputDateTime;
            Assert.True(DateTimeOffset.TryParse(dateTimeRegex.Match(output).Groups[1].Value, out outputDateTime));
        }

        [Fact]
        public void TimeTest()
        {
            // Setup: Build a column and cell for the time type column
            DbColumn column = new FormatterTestDbColumn("TIME");
            DbCellValue cell = new DbCellValue { RawObject = TimeSpan.FromHours(12) };

            // If: I attempt to format a time type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be able to be converted back into a timespan
            Regex dateTimeRegex = new Regex("N'(.*)'");
            TimeSpan outputDateTime;
            Assert.True(TimeSpan.TryParse(dateTimeRegex.Match(output).Groups[1].Value, out outputDateTime));
        }

        [Theory]
        [InlineData("", "N''")] // Make sure empty string works
        [InlineData(" \t\r\n", "N' \t\r\n'")] // Test for whitespace
        [InlineData("some text \x9152", "N'some text \x9152'")] // Test unicode (UTF-8 and UTF-16)
        [InlineData("'", "N''''")] // Test with escaped character
        public void StringFormattingTest(string input, string expectedOutput)
        {
            // Setup: Build a column and cell for the string type column
            // NOTE: We're using VARCHAR because it's very general purpose.
            DbColumn column = new FormatterTestDbColumn("VARCHAR");
            DbCellValue cell = new DbCellValue { RawObject = input };

            // If: I attempt to format a string type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should be quoted and escaped properly
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("CHAR")]
        [InlineData("NCHAR")]
        [InlineData("VARCHAR")]
        [InlineData("TEXT")]
        [InlineData("NTEXT")]
        [InlineData("XML")]
        public void StringTypeTest(string datatype)
        {
            // Setup: Build a column and cell for the string type column
            DbColumn column = new FormatterTestDbColumn(datatype);
            DbCellValue cell = new DbCellValue { RawObject = "test string" };

            // If: I attempt to format a string type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should match the output string
            Assert.Equal("N'test string'", output);
        }

        [Theory]
        [InlineData("BINARY")]
        [InlineData("VARBINARY")]
        [InlineData("IMAGE")]
        public void BinaryTest(string datatype)
        {
            // Setup: Build a column and cell for the string type column
            DbColumn column = new FormatterTestDbColumn(datatype);
            DbCellValue cell = new DbCellValue
            {
                RawObject = new byte[] { 0x42, 0x45, 0x4e, 0x49, 0x53, 0x43, 0x4f, 0x4f, 0x4c }
            };

            // If: I attempt to format a string type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should match the output string
            Regex regex = new Regex("0x[0-9A-F]+", RegexOptions.IgnoreCase);
            Assert.True(regex.IsMatch(output));
        }

        [Fact]
        public void GuidTest()
        {
            // Setup: Build a column and cell for the string type column
            DbColumn column = new FormatterTestDbColumn("UNIQUEIDENTIFIER");
            DbCellValue cell = new DbCellValue { RawObject = Guid.NewGuid() };

            // If: I attempt to format a string type column
            string output = SqlScriptFormatter.FormatValue(cell, column);

            // Then: The output string should match the output string
            Regex regex = new Regex(@"N'[0-9A-F]{8}(-[0-9A-F]{4}){3}-[0-9A-F]{12}'", RegexOptions.IgnoreCase);
            Assert.True(regex.IsMatch(output));
        }

        #endregion

        [Theory]
        [InlineData("(0)", "0")]
        [InlineData("((0))", "0")]
        [InlineData("('')", "")]
        [InlineData("('stuff')", "stuff")]
        [InlineData("(N'')", "")]
        [InlineData("(N'stuff')", "stuff")]
        [InlineData("('''stuff')", "stuff")]
        [InlineData("(N'stu''''ff')", "stu''ff")]
        public void UnescapeTest(string input, string output)
        {
            Assert.Equal(output, SqlScriptFormatter.UnwrapLiteral(input));
        }

        private class FormatterTestDbColumn : DbColumn
        {
            public FormatterTestDbColumn(string dataType, int? precision = null, int? scale = null)
            {
                DataTypeName = dataType;
                NumericPrecision = precision;
                NumericScale = scale;
            }
        }
    }
}
