//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class ReaderWriterPairTest
    {
        private static void VerifyReadWrite<T>(int valueLength, T value, Func<ServiceBufferFileStreamWriter, T, int> writeFunc, Func<ServiceBufferFileStreamReader, FileStreamReadResult<T>> readFunc)
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            try
            {
                // If:
                // ... I write a type T to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    int writtenBytes = writeFunc(writer, value);
                    Assert.Equal(valueLength, writtenBytes);
                }

                // ... And read the type T back
                FileStreamReadResult<T> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = readFunc(reader);
                }

                // Then:
                Assert.Equal(value, outValue.Value);
                Assert.Equal(valueLength, outValue.TotalLength);
                Assert.False(outValue.IsNull);
            }
            finally
            {
                // Cleanup: Close the wrapper
                mockWrapper.Close();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-10)]
        [InlineData(short.MaxValue)]    // Two byte number
        [InlineData(short.MinValue)]    // Negative two byte number
        public void Int16(short value)
        {
            VerifyReadWrite(sizeof(short) + 1, value, (writer, val) => writer.WriteInt16(val), reader => reader.ReadInt16(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-10)]
        [InlineData(short.MaxValue)]    // Two byte number
        [InlineData(short.MinValue)]    // Negative two byte number
        [InlineData(int.MaxValue)]      // Four byte number
        [InlineData(int.MinValue)]      // Negative four byte number
        public void Int32(int value)
        {
            VerifyReadWrite(sizeof(int) + 1, value, (writer, val) => writer.WriteInt32(val), reader => reader.ReadInt32(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-10)]
        [InlineData(short.MaxValue)]    // Two byte number
        [InlineData(short.MinValue)]    // Negative two byte number
        [InlineData(int.MaxValue)]      // Four byte number
        [InlineData(int.MinValue)]      // Negative four byte number
        [InlineData(long.MaxValue)]     // Eight byte number
        [InlineData(long.MinValue)]     // Negative eight byte number
        public void Int64(long value)
        {
            VerifyReadWrite(sizeof(long) + 1, value, (writer, val) => writer.WriteInt64(val), reader => reader.ReadInt64(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Byte(byte value)
        {
            VerifyReadWrite(sizeof(byte) + 1, value, (writer, val) => writer.WriteByte(val), reader => reader.ReadByte(0));
        }

        [Theory]
        [InlineData('a')]
        [InlineData('1')]
        [InlineData((char)0x9152)]  // Test something in the UTF-16 space
        public void Char(char value)
        {
            VerifyReadWrite(sizeof(char) + 1, value, (writer, val) => writer.WriteChar(val), reader => reader.ReadChar(0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Boolean(bool value)
        {
            VerifyReadWrite(sizeof(bool) + 1, value, (writer, val) => writer.WriteBoolean(val), reader => reader.ReadBoolean(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10.1)]
        [InlineData(-10.1)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void Single(float value)
        {
            VerifyReadWrite(sizeof(float) + 1, value, (writer, val) => writer.WriteSingle(val), reader => reader.ReadSingle(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10.1)]
        [InlineData(-10.1)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        public void Double(double value)
        {
            VerifyReadWrite(sizeof(double) + 1, value, (writer, val) => writer.WriteDouble(val), reader => reader.ReadDouble(0));
        }

        [Fact]
        public void SqlDecimalTest()
        {
            // Setup: Create some test values
            // NOTE: We are doing these here instead of InlineData because SqlDecimal values can't be written as constant expressions
            SqlDecimal[] testValues =
            {
                SqlDecimal.MaxValue, SqlDecimal.MinValue, new SqlDecimal(0x01, 0x01, true, 0, 0, 0, 0)
            };
            foreach (SqlDecimal value in testValues)
            {
                int valueLength = 4 + value.BinData.Length;
                VerifyReadWrite(valueLength, value, (writer, val) => writer.WriteSqlDecimal(val), reader => reader.ReadSqlDecimal(0));
            }
        }

        [Fact]
        public void Decimal()
        {
            // Setup: Create some test values
            // NOTE: We are doing these here instead of InlineData because Decimal values can't be written as constant expressions
            decimal[] testValues =
            {
                decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue
            };

            foreach (decimal value in testValues)
            {
                int valueLength = decimal.GetBits(value).Length*4 + 1;
                VerifyReadWrite(valueLength, value, (writer, val) => writer.WriteDecimal(val), reader => reader.ReadDecimal(0));
            }
        }

        [Fact]
        public void DateTimeTest()
        {
            // Setup: Create some test values
            // NOTE: We are doing these here instead of InlineData because DateTime values can't be written as constant expressions
            DateTime[] testValues = 
            {
                DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue
            };
            foreach (DateTime value in testValues)
            {
                VerifyReadWrite(sizeof(long) + 1, value, (writer, val) => writer.WriteDateTime(val), reader => reader.ReadDateTime(0));
            }
        }

        [Fact]
        public void DateTimeOffsetTest()
        {
            // Setup: Create some test values
            // NOTE: We are doing these here instead of InlineData because DateTimeOffset values can't be written as constant expressions
            DateTimeOffset[] testValues =
            {
                DateTimeOffset.Now, DateTimeOffset.UtcNow, DateTimeOffset.MinValue, DateTimeOffset.MaxValue
            };
            foreach (DateTimeOffset value in testValues)
            {
                VerifyReadWrite((sizeof(long) + 1)*2, value, (writer, val) => writer.WriteDateTimeOffset(val), reader => reader.ReadDateTimeOffset(0));
            }
        }

        [Fact]
        public void TimeSpanTest()
        {
            // Setup: Create some test values
            // NOTE: We are doing these here instead of InlineData because TimeSpan values can't be written as constant expressions
            TimeSpan[] testValues =
            {
                TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, TimeSpan.FromMinutes(60)
            };
            foreach (TimeSpan value in testValues)
            {
                VerifyReadWrite(sizeof(long) + 1, value, (writer, val) => writer.WriteTimeSpan(val), reader => reader.ReadTimeSpan(0));
            }
        }

        [Fact]
        public void StringNullTest()
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();

            // If:
            // ... I write null as a string to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                // Then:
                // ... I should get an argument null exception
                Assert.Throws<ArgumentNullException>(() => writer.WriteString(null));
            }
        }

        [Theory]
        [InlineData(0, null)]                             // Test of empty string
        [InlineData(1, new[] { 'j' })]
        [InlineData(1, new[] { (char)0x9152 })]
        [InlineData(100, new[] { 'j', (char)0x9152 })]    // Test alternating utf-16/ascii characters
        [InlineData(512, new[] { 'j', (char)0x9152 })]    // Test that requires a 4 byte length
        public void StringTest(int length, char[] values)
        {
            // Setup: 
            // ... Generate the test value
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(values[i%values.Length]);
            }
            string value = sb.ToString();
            int lengthLength = length == 0 || length > 255 ? 5 : 1;
            VerifyReadWrite(sizeof(char)*length + lengthLength, value, (writer, val) => writer.WriteString(value), reader => reader.ReadString(0));
        }

        [Fact]
        public void BytesNullTest()
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();

            // If:
            // ... I write null as a string to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                // Then:
                // ... I should get an argument null exception
                Assert.Throws<ArgumentNullException>(() => writer.WriteBytes(null, 0));
            }
        }

        [Theory]
        [InlineData(0, new byte[] { 0x00 })]                  // Test of empty byte[]
        [InlineData(1, new byte[] { 0x00 })]
        [InlineData(1, new byte[] { 0xFF })]
        [InlineData(100, new byte[] { 0x10, 0xFF, 0x00 })] 
        [InlineData(512, new byte[] { 0x10, 0xFF, 0x00 })]    // Test that requires a 4 byte length
        public void Bytes(int length, byte[] values)
        {
            // Setup: 
            // ... Generate the test value
            List<byte> sb = new List<byte>();
            for (int i = 0; i < length; i++)
            {
                sb.Add(values[i % values.Length]);
            }
            byte[] value = sb.ToArray();
            int lengthLength = length == 0 || length > 255 ? 5 : 1;
            int valueLength = sizeof(byte)*length + lengthLength;
            VerifyReadWrite(valueLength, value, (writer, val) => writer.WriteBytes(value, length), reader => reader.ReadBytes(0));
        }
    }
}
