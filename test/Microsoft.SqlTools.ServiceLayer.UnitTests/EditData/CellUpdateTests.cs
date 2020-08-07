//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.EditData
{
    public class CellUpdateTests
    {
        [Test]
        public void NullColumnTest()
        {
            // If: I attempt to create a CellUpdate with a null column
            // Then: I should get an exception thrown
            Assert.Throws<ArgumentNullException>(() => new CellUpdate(null, string.Empty));
        }

        [Test]
        public void NullStringValueTest()
        {
            // If: I attempt to create a CellUpdate with a null string value
            // Then: I should get an exception thrown
            Assert.Throws<ArgumentNullException>(() => new CellUpdate(GetWrapper<string>("ntext"), null));
        }

        [Test]
        public void NullStringAllowedTest()
        {
            // If: I attempt to create a CellUpdate to set it to NULL
            const string nullString = "NULL";
            DbColumnWrapper col = GetWrapper<string>("ntext");
            CellUpdate cu = new CellUpdate(col, nullString);

            // Then: The value should be a DBNull and the string value should be the same as what
            //       was given
            Assert.That(cu.Value, Is.InstanceOf<DBNull>());
            Assert.AreEqual(DBNull.Value, cu.Value);
            Assert.AreEqual(nullString, cu.ValueAsString);
            Assert.AreEqual(col, cu.Column);
        }
        
        [Test]
        public void NullStringNotAllowedTest()
        {
            // If: I attempt to create a cell update to set to null when its not allowed
            // Then: I should get an exception thrown
            Assert.Throws<InvalidOperationException>(() => new CellUpdate(GetWrapper<string>("ntext", false), "NULL"));
        }

        [Test]
        public void NullTextStringTest()
        {
            // If: I attempt to create a CellUpdate with the text 'NULL' (with mixed case)
            DbColumnWrapper col = GetWrapper<string>("ntext");
            CellUpdate cu = new CellUpdate(col, "'NULL'");

            // Then: The value should be NULL
            Assert.That(cu.Value, Is.InstanceOf<string>());
            Assert.AreEqual("NULL", cu.Value);
            Assert.AreEqual("'NULL'", cu.ValueAsString);
            Assert.AreEqual(col, cu.Column);
        }

        [Test]
        public void StringTooLongTest([Values("This is way too long", "TooLong")]string value)
        {
            // If: I attempt to create a CellUpdate to set it to a large string
            // Then: I should get an exception thrown
            DbColumnWrapper col = GetWrapper<string>("nvarchar", false, 6);
            Assert.Throws<InvalidOperationException>(() => new CellUpdate(col, value));
        }

        [Test]
        [TestCaseSource(nameof(ByteArrayTestParams))]
        public void ByteArrayTest(string strValue, byte[] expectedValue, string expectedString)
        {
            // If: I attempt to create a CellUpdate for a binary column
            DbColumnWrapper col = GetWrapper<byte[]>("binary");
            CellUpdate cu = new CellUpdate(col, strValue);

            // Then: The value should be a binary and should match the expected data
            Assert.That(cu.Value, Is.InstanceOf<byte[]>());
            Assert.AreEqual(expectedValue, cu.Value);
            Assert.AreEqual(expectedString, cu.ValueAsString);
            Assert.AreEqual(col, cu.Column);
        }

        public static IEnumerable<object[]> ByteArrayTestParams
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

        [Test]
        public void ByteArrayInvalidFormatTest()
        {
            // If: I attempt to create a CellUpdate for a binary column
            // Then: It should throw an exception
            DbColumnWrapper col = GetWrapper<byte[]>("binary");
            Assert.Throws<InvalidOperationException>(() => new CellUpdate(col, "this is totally invalid"));
        }

        [Test]
        [TestCaseSource(nameof(BoolTestParams))]
        public void BoolTest(string input, bool output, string outputString)
        {
            // If: I attempt to create a CellUpdate for a boolean column
            DbColumnWrapper col = GetWrapper<bool>("bit");
            CellUpdate cu = new CellUpdate(col, input);

            // Then: The value should match what was expected
            Assert.That(cu.Value, Is.InstanceOf<bool>());
            Assert.AreEqual(output, cu.Value);
            Assert.AreEqual(outputString, cu.ValueAsString);
            Assert.AreEqual(col, cu.Column);
        }

        private static IEnumerable<object[]> BoolTestParams
        {
            get
            {
                yield return new object[] {"1", true, bool.TrueString};
                yield return new object[] {"0", false, bool.FalseString};
                yield return new object[] {bool.TrueString, true, bool.TrueString};
                yield return new object[] {bool.FalseString, false, bool.FalseString};
            }
        }

        [Test]
        public void BoolInvalidFormatTest()
        {
            // If: I create a CellUpdate for a bool column and provide an invalid numeric value
            // Then: It should throw an exception
            DbColumnWrapper col = GetWrapper<bool>("bit");
            Assert.Throws<InvalidOperationException>(() => new CellUpdate(col, "12345"));
        }

        [Test]
        public void TimeSpanTooLargeTest([Values("24:00:00", "105:00:00")] string value)
        {
            // If: I create a cell update for a timespan column and provide a value that is over 24hrs
            // Then: It should throw an exception
            DbColumnWrapper col = GetWrapper<TimeSpan>("time");
            Assert.Throws<InvalidOperationException>(() => new CellUpdate(col, value));
        }

        /// <summary>
        /// Not using TestCaseSource because nUnit's test name generator
        /// doesn't like DbColumnWrapper objects as a source, due 
        /// to that class lacking a ToString override.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="obj"></param>
        [Test]
        public void RoundTripTest()
        {
            foreach (var inputs in RoundTripTestParams)
            {

                var col = (DbColumnWrapper)inputs[0];
                var obj = inputs[1];
                // Setup: Figure out the test string
                string testString = obj.ToString();

                // If: I attempt to create a CellUpdate
                CellUpdate cu = new CellUpdate(col, testString);

                // Then: The value and type should match what we put in
                Assert.That(cu.Value, Is.InstanceOf(col.DataType));
                Assert.AreEqual(obj, cu.Value);
                Assert.AreEqual(testString, cu.ValueAsString);
                Assert.AreEqual(col, cu.Column);
            }
        }

        public static IEnumerable<object[]> RoundTripTestParams
        {
            get
            {
                yield return new object[] {GetWrapper<Guid>("uniqueidentifier"), Guid.NewGuid()};
                yield return new object[] {GetWrapper<TimeSpan>("time"), new TimeSpan(0, 1, 20, 0, 123)};
                yield return new object[] {GetWrapper<DateTime>("datetime"), new DateTime(2016, 04, 25, 9, 45, 0)};
                yield return new object[]
                {
                    GetWrapper<DateTimeOffset>("datetimeoffset"),
                    new DateTimeOffset(2016, 04, 25, 9, 45, 0, TimeSpan.FromHours(8))
                };
                yield return new object[] {GetWrapper<long>("bigint"), 1000L};
                yield return new object[] {GetWrapper<decimal>("decimal"), new decimal(3.14)};
                yield return new object[] {GetWrapper<int>("int"), 1000};
                yield return new object[] {GetWrapper<short>("smallint"), (short) 1000};
                yield return new object[] {GetWrapper<byte>("tinyint"), (byte) 5};
                yield return new object[] {GetWrapper<double>("float"), 3.14d};
                yield return new object[] {GetWrapper<float>("real"), 3.14f};
            }
        }

        [Test]
        public void AsDbCellValue([Values]bool isNull)
        {
            // Setup: Create a cell update
            var value = isNull ? "NULL" : "foo";
            var col = GetWrapper<string>("NTEXT");
            CellUpdate cu = new CellUpdate(col, value);

            // If: I convert it to a DbCellvalue
            DbCellValue dbc = cu.AsDbCellValue;

            // Then:
            // ... It should not be null
            Assert.NotNull(dbc);

            // ... The display value should be the same as the value we supplied
            Assert.AreEqual(value, dbc.DisplayValue);

            // ... The null-ness of the value should be the same as what we supplied
            Assert.AreEqual(isNull, dbc.IsNull);

            // ... We don't care *too* much about the raw value, but we'll check it anyhow
            Assert.AreEqual(isNull ? (object)DBNull.Value : value, dbc.RawObject);
        }

        [Test]
        public void AsEditCellValue([Values]bool isNull)
        {
            // Setup: Create a cell update
            var value = isNull ? "NULL" : "foo";
            var col = GetWrapper<string>("NTEXT");
            CellUpdate cu = new CellUpdate(col, value);

            // If: I convert the cell update to an EditCell
            EditCell ec = cu.AsEditCell;

            // Then:
            // ... It should not be null
            Assert.NotNull(ec);

            // ... The display value should be the same as the value we supplied
            Assert.AreEqual(value, ec.DisplayValue);

            // ... The null-ness of the value should be the same as what we supplied
            Assert.AreEqual(isNull, ec.IsNull);

            // ... We don't care *too* much about the raw value, but we'll check it anyhow
            Assert.AreEqual(isNull ? (object)DBNull.Value : value, ec.RawObject);

            // ... The edit cell should be dirty
            Assert.True(ec.IsDirty);
        }

        private static DbColumnWrapper GetWrapper<T>(string dataTypeName, bool allowNull = true, int? colSize = null)
        {
            return new DbColumnWrapper(new CellUpdateTestDbColumn(typeof(T), dataTypeName, allowNull, colSize));
        }

        private class CellUpdateTestDbColumn : DbColumn
        {
            public CellUpdateTestDbColumn(Type dataType, string dataTypeName, bool allowNull = true, int? colSize = null)
            {
                AllowDBNull = allowNull;
                ColumnSize = colSize;
                DataType = dataType;
                DataTypeName = dataTypeName;
            }
        }
    }
}
