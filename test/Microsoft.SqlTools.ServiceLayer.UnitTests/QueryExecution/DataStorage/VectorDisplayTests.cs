//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.Data.SqlTypes;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    /// <summary>
    /// Tests for SQL Server VECTOR data type display in query results.
    /// </summary>
    public class VectorDisplayTests
    {
        #region DbColumnWrapper recognition tests

        [Test]
        public void VectorColumn_IsVector_True()
        {
            var col = new DbColumnWrapper(new TestDbColumn { DataTypeName = "vector" });
            Assert.True(col.IsVector);
        }

        [Test]
        public void VectorColumn_IsNotBytes_IsNotChars_IsNotUdt()
        {
            var col = new DbColumnWrapper(new TestDbColumn { DataTypeName = "vector" });
            Assert.False(col.IsBytes);
            Assert.False(col.IsChars);
            Assert.False(col.IsUdt);
        }

        [Test]
        public void VectorColumn_IsNotLong()
        {
            // Vectors are bounded size — should not be treated as streaming long columns.
            var col = new DbColumnWrapper(new TestDbColumn { DataTypeName = "vector" });
            Assert.False(col.IsLong.HasValue && col.IsLong.Value);
        }

        [Test]
        public void VarbinaryColumn_IsNotVector()
        {
            // Regular varbinary must NOT be affected by the vector fix.
            var col = new DbColumnWrapper(new TestDbColumn { DataTypeName = "varbinary", ColumnSize = 100 });
            Assert.False(col.IsVector);
            Assert.True(col.IsBytes);
        }

        [Test]
        public void VectorColumn_CaseInsensitive()
        {
            // DataTypeName is lowercased in the constructor, so "VECTOR" should work too.
            var col = new DbColumnWrapper(new TestDbColumn { DataTypeName = "VECTOR" });
            Assert.True(col.IsVector);
        }

        #endregion

        #region VectorBytesToJsonString tests

        [Test]
        public void VectorBytesToJsonString_EmptyArray_ReturnsEmptyBrackets()
        {
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(new byte[0]);
            Assert.AreEqual("[]", result);
        }

        [Test]
        public void VectorBytesToJsonString_Null_ReturnsEmptyBrackets()
        {
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(null);
            Assert.AreEqual("[]", result);
        }

        [Test]
        public void VectorBytesToJsonString_ThreeFloats_NoHeader()
        {
            // [1.0, 2.0, 3.0] without header — 12 bytes of raw floats
            byte[] bytes = BuildRawFloatBytes(1.0f, 2.0f, 3.0f);
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(bytes);
            Assert.AreEqual("[1,2,3]", result);
        }

        [Test]
        public void VectorBytesToJsonString_ThreeFloats_WithHeader()
        {
            // [1.0, 2.0, 3.0] with 8-byte header (2 flags + 2 dims + 4 reserved)
            byte[] bytes = BuildHeaderedFloatBytes(1.0f, 2.0f, 3.0f);
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(bytes);
            Assert.AreEqual("[1,2,3]", result);
        }

        [Test]
        public void VectorBytesToJsonString_FractionalValues()
        {
            byte[] bytes = BuildRawFloatBytes(0.5f, -0.5f, 0.0f);
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(bytes);
            Assert.AreEqual("[0.5,-0.5,0]", result);
        }

        [Test]
        public void VectorBytesToJsonString_SingleElement()
        {
            byte[] bytes = BuildRawFloatBytes(42.0f);
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(bytes);
            Assert.AreEqual("[42]", result);
        }

        [Test]
        public void VectorBytesToJsonString_KnownHexBytes_ThreeFloats()
        {
            // 1.0f = 0x3F800000 (little-endian: 00 00 80 3F)
            // 2.0f = 0x40000000 (little-endian: 00 00 00 40)
            // 3.0f = 0x40400000 (little-endian: 00 00 40 40)
            byte[] bytes = new byte[]
            {
                0x00, 0x00, 0x80, 0x3F,
                0x00, 0x00, 0x00, 0x40,
                0x00, 0x00, 0x40, 0x40
            };
            string result = ServiceBufferFileStreamWriter.VectorBytesToJsonString(bytes);
            Assert.AreEqual("[1,2,3]", result);
        }

        #endregion

        #region ConvertVectorToDisplayString tests

        [Test]
        public void ConvertVectorToDisplayString_SqlBinary_ReturnsJsonArray()
        {
            byte[] floatBytes = BuildRawFloatBytes(1.0f, 2.0f, 3.0f);
            var sqlBinary = new SqlBinary(floatBytes);
            string result = ServiceBufferFileStreamWriter.ConvertVectorToDisplayString(sqlBinary);
            Assert.AreEqual("[1,2,3]", result);
        }

        [Test]
        public void ConvertVectorToDisplayString_ByteArray_ReturnsJsonArray()
        {
            byte[] floatBytes = BuildRawFloatBytes(0.5f, -0.5f);
            string result = ServiceBufferFileStreamWriter.ConvertVectorToDisplayString(floatBytes);
            Assert.AreEqual("[0.5,-0.5]", result);
        }

        [Test]
        public void ConvertVectorToDisplayString_SqlVectorFloat_ReturnsJsonArray()
        {
            // SqlClient returns SqlVector<float> for VECTOR columns — this is the primary code path.
            var vector = new SqlVector<float>(new float[] { 1.0f, 2.0f, 3.0f });
            string result = ServiceBufferFileStreamWriter.ConvertVectorToDisplayString(vector);
            Assert.AreEqual("[1,2,3]", result);
        }

        [Test]
        public void ConvertVectorToDisplayString_ArbitraryObject_UsesToString()
        {
            // Unknown future type falls back to ToString().
            var fakeVector = new FakeSqlVector("[10,20,30]");
            string result = ServiceBufferFileStreamWriter.ConvertVectorToDisplayString(fakeVector);
            Assert.AreEqual("[10,20,30]", result);
        }

        [Test]
        public void ConvertVectorToDisplayString_Null_ReturnsEmpty()
        {
            string result = ServiceBufferFileStreamWriter.ConvertVectorToDisplayString(null);
            Assert.AreEqual(string.Empty, result);
        }

        #endregion

        #region End-to-end write/read round-trip test

        [Test]
        public void VectorColumn_WriteRead_DisplaysJsonArray()
        {
            // Write a vector column value (as a JSON string) and read it back.
            // The writer calls WriteString; the reader (via NVarChar → SqlString → ReadString)
            // returns the JSON array as DisplayValue.
            const string expectedDisplay = "[1,2,3]";
            const long rowId = 1;

            byte[] storage = new byte[8192];
            var settings = new QueryExecutionSettings();

            using (var writer = new ServiceBufferFileStreamWriter(new MemoryStream(storage), settings))
            {
                writer.WriteString(expectedDisplay);
            }

            FileStreamReadResult result;
            using (var reader = new ServiceBufferFileStreamReader(new MemoryStream(storage), settings))
            {
                result = reader.ReadString(0, rowId);
            }

            Assert.AreEqual(expectedDisplay, result.Value.DisplayValue);
            Assert.AreEqual(expectedDisplay, result.Value.RawObject);
        }

        #endregion

        #region Helpers

        /// <summary>Returns raw float bytes (no header) for the given floats.</summary>
        private static byte[] BuildRawFloatBytes(params float[] values)
        {
            byte[] bytes = new byte[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, i * 4, 4);
            return bytes;
        }

        /// <summary>
        /// Returns float bytes prefixed with an 8-byte TDS-style header
        /// (2 bytes flags=0, 2 bytes dimension count, 4 bytes reserved=0).
        /// </summary>
        private static byte[] BuildHeaderedFloatBytes(params float[] values)
        {
            byte[] header = new byte[8];
            // bytes 2-3: dimension count as ushort, little-endian
            header[2] = (byte)(values.Length & 0xFF);
            header[3] = (byte)((values.Length >> 8) & 0xFF);

            byte[] payload = BuildRawFloatBytes(values);
            byte[] result = new byte[header.Length + payload.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(payload, 0, result, header.Length, payload.Length);
            return result;
        }

        /// <summary>
        /// A stub that mimics what SqlVector&lt;float&gt;.ToString() might return,
        /// to verify the fallback ToString() path in ConvertVectorToDisplayString.
        /// </summary>
        private sealed class FakeSqlVector
        {
            private readonly string _str;
            public FakeSqlVector(string str) { _str = str; }
            public override string ToString() => _str;
        }

        #endregion
    }
}
