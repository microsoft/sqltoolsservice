//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class CellUpdateTests
    {
        [Fact]
        public void NullColumnTest()
        {
            // If: I attempt to create a CellUpdate with a null column
            // Then: I should get an exception thrown
            Assert.Throws<ArgumentNullException>(() => new CellUpdate(null, string.Empty));
        }

        [Fact]
        public void NullStringValueTest()
        {
            // If: I attempt to create a CellUpdate with a null string value
            // Then: I should get an exception thrown
            Assert.Throws<ArgumentNullException>(() => new CellUpdate(new CellUpdateTestDbColumn(null), null));
        }

        [Fact]
        public void NullStringTest()
        {
            // If: I attempt to create a CellUpdate to set it to NULL (with mixed cases)
            const string nullString = "NULL";
            DbColumn col = new CellUpdateTestDbColumn(typeof(string));
            CellUpdate cu = new CellUpdate(col, nullString);

            // Then: The value should be a DBNull and the string value should be the same as what
            //       was given
            Assert.IsType<DBNull>(cu.Value);
            Assert.Equal(DBNull.Value, cu.Value);
            Assert.Equal(nullString, cu.ValueAsString);
            Assert.Equal(col, cu.Column);
        }

        [Fact]
        public void NullTextStringTest()
        {
            // If: I attempt to create a CellUpdate with the text 'NULL' (with mixed case)
            DbColumn col = new CellUpdateTestDbColumn(typeof(string));
            CellUpdate cu = new CellUpdate(col, "'NULL'");

            // Then: The value should be NULL
            Assert.IsType<string>(cu.Value);
            Assert.Equal("NULL", cu.Value);
            Assert.Equal("'NULL'", cu.ValueAsString);
            Assert.Equal(col, cu.Column);
        }

        [Theory]
        [MemberData(nameof(ByteArrayTestParams))]
        public void ByteArrayTest(string strValue, byte[] expectedValue, string expectedString)
        {
            // If: I attempt to create a CellUpdate for a binary column
            DbColumn col = new CellUpdateTestDbColumn(typeof(byte[]));
            CellUpdate cu = new CellUpdate(col, strValue);

            // Then: The value should be a binary and should match the expected data
            Assert.IsType<byte[]>(cu.Value);
            Assert.Equal(expectedValue, cu.Value);
            Assert.Equal(expectedString, cu.ValueAsString);
            Assert.Equal(col, cu.Column);
        }

        public static IEnumerable<object> ByteArrayTestParams
        {
            get
            {
                // All zero tests
                yield return new object[] {"00000000", new byte[] {0x00}, "0x00"};                  // Base10 
                yield return new object[] {"0x000000", new byte[] {0x00, 0x00, 0x00}, "0x000000"};  // Base16
                yield return new object[] {"0x000", new byte[] {0x00, 0x00}, "0x0000"};             // Base16, odd

                // Single byte tests
                yield return new object[] {"50", new byte[] {0x32}, "0x32"};                        // Base10
                yield return new object[] {"050", new byte[] {0x32}, "0x32"};                       // Base10, leading zeros
                yield return new object[] {"0xF0", new byte[] {0xF0}, "0xF0"};                      // Base16
                yield return new object[] {"0x0F", new byte[] {0x0F}, "0x0F"};                      // Base16, leading zeros
                yield return new object[] {"0xF", new byte[] {0x0F}, "0x0F"};                       // Base16, odd

                // Two byte tests
                yield return new object[] {"1000", new byte[] {0x03, 0xE8}, "0x03E8"};              // Base10
                yield return new object[] {"01000", new byte[] {0x03, 0xE8}, "0x03E8"};             // Base10, leading zeros
                yield return new object[] {"0xF001", new byte[] {0xF0, 0x01}, "0xF001"};            // Base16
                yield return new object[] {"0x0F10", new byte[] {0x0F, 0x10}, "0x0F10"};            // Base16, leading zeros
                yield return new object[] {"0xF10", new byte[] {0x0F, 0x10}, "0x0F10"};             // Base16, odd

                // Three byte tests
                yield return new object[] {"100000", new byte[] {0x01, 0x86, 0xA0}, "0x0186A0"};    // Base10
                yield return new object[] {"0100000", new byte[] {0x01, 0x86, 0xA0}, "0x0186A0"};   // Base10, leading zeros
                yield return new object[] {"0x101010", new byte[] {0x10, 0x10, 0x10}, "0x101010"};  // Base16
                yield return new object[] {"0x010101", new byte[] {0x01, 0x01, 0x01}, "0x010101"};  // Base16, leading zeros
                yield return new object[] {"0x10101", new byte[] {0x01, 0x01, 0x01}, "0x010101"};   // Base16, odd

                // Four byte tests
                yield return new object[] {"20000000", new byte[] {0x01, 0x31, 0x2D, 0x00}, "0x01312D00"};      // Base10
                yield return new object[] {"020000000", new byte[] {0x01, 0x31, 0x2D, 0x00}, "0x01312D00"};     // Base10, leading zeros
                yield return new object[] {"0xF0F00101", new byte[] {0xF0, 0xF0, 0x01, 0x01}, "0xF0F00101"};    // Base16
                yield return new object[] {"0x0F0F1010", new byte[] {0x0F, 0x0F, 0x10, 0x10}, "0x0F0F1010"};    // Base16, leading zeros
                yield return new object[] {"0xF0F1010", new byte[] {0x0F, 0x0F, 0x10, 0x10}, "0x0F0F1010"};     // Base16, odd
            }
        }

        [Fact]
        public void ByteArrayInvalidFormatTest()
        {
            // If: I attempt to create a CellUpdate for a binary column
            // Then: It should throw an exception
            DbColumn col = new CellUpdateTestDbColumn(typeof(byte[]));
            Assert.Throws<FormatException>(() => new CellUpdate(col, "this is totally invalid"));
        }

        [Theory]
        [MemberData(nameof(BoolTestParams))]
        public void BoolTest(string input, bool output, string outputString)
        {
            // If: I attempt to create a CellUpdate for a boolean column
            DbColumn col = new CellUpdateTestDbColumn(typeof(bool));
            CellUpdate cu = new CellUpdate(col, input);

            // Then: The value should match what was expected
            Assert.IsType<bool>(cu.Value);
            Assert.Equal(output, cu.Value);
            Assert.Equal(outputString, cu.ValueAsString);
            Assert.Equal(col, cu.Column);
        }

        public static IEnumerable<object> BoolTestParams
        {
            get
            {
                yield return new object[] {"1", true, bool.TrueString};
                yield return new object[] {"0", false, bool.FalseString};
                yield return new object[] {bool.TrueString, true, bool.TrueString};
                yield return new object[] {bool.FalseString, false, bool.FalseString};
            }
        }

        [Fact]
        public void BoolInvalidFormatTest()
        {
            // If: I create a CellUpdate for a bool column and provide an invalid numeric value
            // Then: It should throw an exception
            DbColumn col = new CellUpdateTestDbColumn(typeof(bool));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CellUpdate(col, "12345"));
        }

        [Theory]
        [MemberData(nameof(RoundTripTestParams))]
        public void RoundTripTest(Type dbColType, object obj)
        {
            // Setup: Figure out the test string
            string testString = obj.ToString();

            // If: I attempt to create a CellUpdate for a GUID column
            DbColumn col = new CellUpdateTestDbColumn(dbColType);
            CellUpdate cu = new CellUpdate(col, testString);

            // Then: The value and type should match what we put in
            Assert.IsType(dbColType, cu.Value);
            Assert.Equal(obj, cu.Value);
            Assert.Equal(testString, cu.ValueAsString);
            Assert.Equal(col, cu.Column);
        }

        public static IEnumerable<object> RoundTripTestParams
        {
            get
            {
                yield return new object[] {typeof(Guid), Guid.NewGuid()};
                yield return new object[] {typeof(TimeSpan), new TimeSpan(0, 1, 20, 0, 123)};
                yield return new object[] {typeof(DateTime), new DateTime(2016, 04, 25, 9, 45, 0)};
                yield return new object[]
                {
                    typeof(DateTimeOffset),
                    new DateTimeOffset(2016, 04, 25, 9, 45, 0, TimeSpan.FromHours(8))
                };
                yield return new object[] {typeof(long), 1000L};
                yield return new object[] {typeof(decimal), new decimal(3.14)};
                yield return new object[] {typeof(int), 1000};
                yield return new object[] {typeof(short), (short) 1000};
                yield return new object[] {typeof(byte), (byte) 5};
                yield return new object[] {typeof(double), 3.14d};
                yield return new object[] {typeof(float), 3.14f};
            }
        }

        private class CellUpdateTestDbColumn : DbColumn
        {
            public CellUpdateTestDbColumn(Type dataType)
            {
                DataType = dataType;
            }
        }
    }
}
