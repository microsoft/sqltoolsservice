using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.DataStorage
{
    public class ReaderWriterPairTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(-10)]
        [InlineData(short.MaxValue)]    // Two byte number
        [InlineData(short.MinValue)]    // Negative two byte number
        public void Int16(short value)
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(short) + 1;

            // If:
            // ... I write an int16 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int writtenBytes = writer.WriteInt16(value).Result;
                Assert.Equal(valueLength, writtenBytes);
            }

            // ... And read the int16 back
            FileStreamReadResult<short> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadInt16(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(int) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteInt32(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<int> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadInt32(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(long) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteInt64(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<long> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadInt64(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Byte(byte value)
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(byte) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteByte(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<byte> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadByte(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
        }

        [Theory]
        [InlineData('a')]
        [InlineData('1')]
        [InlineData((char)0x9152)]  // Test something in the UTF-16 space
        public void Char(char value)
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(char) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteChar(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<char> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadChar(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Boolean(bool value)
        {
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(bool) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteBoolean(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<bool> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadBoolean(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(float) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteSingle(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<float> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadSingle(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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
            // Setup: Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
            const int valueLength = sizeof(double) + 1;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteDouble(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<double> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadDouble(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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

                // Setup: Create a mock file stream wrapper
                Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
                int valueLength = 4 + value.BinData.Length;

                // If:
                // ... I write an int32 to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    int bytesWritten = writer.WriteSqlDecimal(value).Result;
                    Assert.Equal(valueLength, bytesWritten);
                }

                // ... And read the int32 back
                FileStreamReadResult<SqlDecimal> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = reader.ReadSqlDecimal(0).Result;
                }

                // Then:
                // ... The values should be the same
                Assert.Equal(value, outValue.Value);
                Assert.Equal(valueLength, outValue.TotalLength);
                Assert.False(outValue.IsNull);

                // Cleanup: Close the wrapper
                mockWrapper.Close();
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
                // Setup: Create a mock file stream wrapper
                Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
                int valueLength = decimal.GetBits(value).Length*4 + 1;

                // If:
                // ... I write an int32 to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    int bytesWritten = writer.WriteDecimal(value).Result;
                    Assert.Equal(valueLength, bytesWritten);
                }

                // ... And read the int32 back
                FileStreamReadResult<decimal> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = reader.ReadDecimal(0).Result;
                }

                // Then:
                // ... The values should be the same
                Assert.Equal(value, outValue.Value);
                Assert.Equal(valueLength, outValue.TotalLength);
                Assert.False(outValue.IsNull);

                // Cleanup: Close the wrapper
                mockWrapper.Close();
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
                // Setup: Create a mock file stream wrapper
                Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
                const int valueLength = sizeof(long) + 1;

                // If:
                // ... I write an int32 to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    int bytesWritten = writer.WriteDateTime(value).Result;
                    Assert.Equal(valueLength, bytesWritten);
                }

                // ... And read the int32 back
                FileStreamReadResult<DateTime> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = reader.ReadDateTime(0).Result;
                }

                // Then:
                // ... The values should be the same
                Assert.Equal(value, outValue.Value);
                Assert.Equal(valueLength, outValue.TotalLength);
                Assert.False(outValue.IsNull);

                // Cleanup: Close the wrapper
                mockWrapper.Close();
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
                // Setup: Create a mock file stream wrapper
                Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();
                const int valueLength = (sizeof(long) + 1)*2;

                // If:
                // ... I write an int32 to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    int bytesWritten = writer.WriteDateTimeOffset(value).Result;
                    Assert.Equal(valueLength, bytesWritten);
                }

                // ... And read the int32 back
                FileStreamReadResult<DateTimeOffset> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = reader.ReadDateTimeOffset(0).Result;
                }

                // Then:
                // ... The values should be the same
                Assert.Equal(value, outValue.Value);
                Assert.Equal(valueLength, outValue.TotalLength);
                Assert.False(outValue.IsNull);

                // Cleanup: Close the wrapper
                mockWrapper.Close();
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
                // Setup: Create a mock file stream wrapper
                Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();

                // If:
                // ... I write an int32 to the writer
                using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
                {
                    writer.WriteTimeSpan(value).Wait();
                }

                // ... And read the int32 back
                FileStreamReadResult<TimeSpan> outValue;
                using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
                {
                    outValue = reader.ReadTimeSpan(0).Result;
                }

                // Then:
                // ... The values should be the same
                Assert.Equal(value, outValue.Value);
                Assert.Equal(sizeof(long) + 1, outValue.TotalLength);
                Assert.False(outValue.IsNull);

                // Cleanup: Close the wrapper
                mockWrapper.Close();
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
                Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteString(null)).Wait();
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
            // ... Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();

            // ... Generate the test value
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(values[i%values.Length]);
            }
            string value = sb.ToString();
            int valueLength = sizeof(char)*length + 5;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteString(value).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<string> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadString(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.Equal(value, outValue.Value);
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
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
                Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteBytes(null, 0)).Wait();
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
            // ... Create a mock file stream wrapper
            Common.InMemoryWrapper mockWrapper = new Common.InMemoryWrapper();

            // ... Generate the test value
            List<byte> sb = new List<byte>();
            for (int i = 0; i < length; i++)
            {
                sb.Add(values[i % values.Length]);
            }
            byte[] value = sb.ToArray();
            int valueLength = sizeof(byte)*length + 5;

            // If:
            // ... I write an int32 to the writer
            using (ServiceBufferFileStreamWriter writer = new ServiceBufferFileStreamWriter(mockWrapper, "abc", 10, 10))
            {
                int bytesWritten = writer.WriteBytes(value, length).Result;
                Assert.Equal(valueLength, bytesWritten);
            }

            // ... And read the int32 back
            FileStreamReadResult<byte[]> outValue;
            using (ServiceBufferFileStreamReader reader = new ServiceBufferFileStreamReader(mockWrapper, "abc"))
            {
                outValue = reader.ReadBytes(0).Result;
            }

            // Then:
            // ... The values should be the same
            Assert.True(value.SequenceEqual(outValue.Value));
            Assert.Equal(valueLength, outValue.TotalLength);
            Assert.False(outValue.IsNull);

            // Cleanup: Close the wrapper
            mockWrapper.Close();
        }
    }
}
