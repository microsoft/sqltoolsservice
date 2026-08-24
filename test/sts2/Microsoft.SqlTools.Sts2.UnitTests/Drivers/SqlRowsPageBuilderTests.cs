//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.SqlClient;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// QO-3 byte-aware page construction: rows and bytes both bound a page,
    /// whichever limit is reached first; oversized single rows become one-row
    /// pages; nothing is dropped and no page is empty.
    /// </summary>
    public class SqlRowsPageBuilderTests
    {
        private static List<IReadOnlyList<IReadOnlyList<object?>>> Drain(
            SqlRowsPageBuilder builder, IEnumerable<IReadOnlyList<object?>> rows)
        {
            var pages = new List<IReadOnlyList<IReadOnlyList<object?>>>();
            foreach (IReadOnlyList<object?> row in rows)
            {
                pages.AddRange(builder.Add(row));
            }
            IReadOnlyList<IReadOnlyList<object?>>? tail = builder.Flush();
            if (tail is not null)
            {
                pages.Add(tail);
            }
            return pages;
        }

        private static IReadOnlyList<object?> Row(params object?[] cells) => cells;

        [Fact]
        public void RowLimitSplitsExactlyAsBefore()
        {
            var builder = new SqlRowsPageBuilder(pageRows: 3, pageBytes: long.MaxValue > int.MaxValue ? int.MaxValue : int.MaxValue);
            List<IReadOnlyList<IReadOnlyList<object?>>> pages = Drain(
                builder, Enumerable.Range(0, 7).Select(i => Row(i)));

            Assert.Equal(3, pages.Count);
            Assert.Equal(new[] { 3, 3, 1 }, pages.Select(p => p.Count));
            // No row lost, order preserved.
            Assert.Equal(
                Enumerable.Range(0, 7),
                pages.SelectMany(p => p).Select(r => (int)r[0]!));
        }

        [Fact]
        public void ByteLimitSplitsBeforeTheRowLimit()
        {
            // ~500 approx bytes per row (string length dominates); a 1000-byte page
            // fits one row plus change — every page closes on bytes, not the
            // 1000-row default.
            var builder = new SqlRowsPageBuilder(pageRows: 1000, pageBytes: 1000);
            string wide = new string('x', 480);
            List<IReadOnlyList<IReadOnlyList<object?>>> pages = Drain(
                builder, Enumerable.Range(0, 6).Select(_ => Row(wide)));

            Assert.True(pages.Count >= 3, $"expected byte-splitting, got {pages.Count} pages");
            Assert.All(pages, page => Assert.InRange(page.Count, 1, 2));
            Assert.Equal(6, pages.Sum(p => p.Count));
        }

        [Fact]
        public void OversizedSingleRowBecomesItsOwnPage()
        {
            var builder = new SqlRowsPageBuilder(pageRows: 100, pageBytes: 256);
            List<IReadOnlyList<IReadOnlyList<object?>>> pages = Drain(
                builder,
                new[]
                {
                    Row("small"),
                    Row(new string('y', 5000)), // alone exceeds pageBytes
                    Row("small-too"),
                });

            Assert.Equal(3, pages.Count);
            Assert.Single(pages[1]);
            Assert.Equal(5000, ((string)pages[1][0][0]!).Length);
            // The small trailing row still arrives.
            Assert.Equal("small-too", pages[2][0][0]);
        }

        [Fact]
        public void FlushReturnsTailOnceThenNothing()
        {
            var builder = new SqlRowsPageBuilder(pageRows: 10, pageBytes: 1 << 20);
            Assert.Empty(builder.Add(Row(1)).ToList());
            IReadOnlyList<IReadOnlyList<object?>>? tail = builder.Flush();
            Assert.NotNull(tail);
            Assert.Single(tail);
            Assert.Null(builder.Flush());
        }

        [Fact]
        public void EstimatorCoversCommonCellShapesWithoutAllocation()
        {
            Assert.Equal(4, SqlRowsPageBuilder.EstimateCellBytes(null));
            Assert.Equal(7, SqlRowsPageBuilder.EstimateCellBytes("hello"));
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(new byte[300]) >= 400); // base64 expansion
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(System.Guid.NewGuid()) >= 36);
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(123.45m) > 0);
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(new object()) > 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(300)]
        public void BinaryEstimateIncludesTypedWrapper(int byteLength)
        {
            byte[] value = new byte[byteLength];
            int encodedBytes = Encoding.UTF8.GetByteCount(
                WireValueEncoder.Encode(value)!.ToJsonString());

            Assert.True(
                SqlRowsPageBuilder.EstimateCellBytes(value) >= encodedBytes,
                $"estimate was below the {encodedBytes}-byte typed binary encoding");
        }

        [Theory]
        [InlineData("plain ASCII")]
        [InlineData("caf\u00e9")]
        [InlineData("emoji \ud83d\ude00")]
        [InlineData("quote \" slash \\")]
        [InlineData("control\u0001")]
        [InlineData("<script>&")]
        public void StringEstimateBoundsDefaultJsonEncoding(string value)
        {
            int encodedBytes = JsonSerializer.SerializeToUtf8Bytes(value).Length;
            Assert.True(
                SqlRowsPageBuilder.EstimateCellBytes(value) >= encodedBytes,
                $"estimate was below the {encodedBytes}-byte JSON encoding");
        }

        [Fact]
        public void TypedWrapperEstimatesBoundWireEncoding()
        {
            object[] values =
            [
                decimal.MinValue,
                long.MinValue,
                long.MaxValue,
                DateTime.MaxValue,
                DateTimeOffset.MaxValue,
                TimeSpan.MaxValue,
                Guid.Empty,
                double.NaN,
                new DriverVectorUnavailableValue
                {
                    Dimensions = 1536,
                    BaseType = "float16",
                    Reason = "unsupportedBaseType",
                },
                new DriverSpatialUnavailableValue
                {
                    Kind = "geometry",
                    Reason = "maxCellBytes",
                    Srid = 4326,
                    SourceBytes = long.MaxValue,
                },
                new DriverTruncatedValue
                {
                    Kind = "string",
                    PrefixText = new string('x', 4096),
                    TotalBytes = long.MaxValue,
                    DigestHex = new string('f', 64),
                },
                new DriverTruncatedValue
                {
                    Kind = "binary",
                    PrefixBytes = new byte[4096],
                    TotalBytes = long.MaxValue,
                    DigestHex = new string('f', 64),
                },
            ];

            foreach (object value in values)
            {
                int encodedBytes = Encoding.UTF8.GetByteCount(
                    WireValueEncoder.Encode(value)!.ToJsonString());
                Assert.True(
                    SqlRowsPageBuilder.EstimateCellBytes(value) >= encodedBytes,
                    $"{value.GetType().Name} estimate was below its {encodedBytes}-byte wire encoding");
            }
        }

        [Fact]
        public void VectorUnavailableSentinelsSplitAtTheWireByteBound()
        {
            var sentinel = new DriverVectorUnavailableValue
            {
                Dimensions = 1536,
                BaseType = "float16",
                Reason = "cellLimit",
            };
            int encodedBytes = Encoding.UTF8.GetByteCount(
                WireValueEncoder.Encode(sentinel)!.ToJsonString());
            var builder = new SqlRowsPageBuilder(pageRows: 100, pageBytes: encodedBytes * 2 - 1);

            List<IReadOnlyList<IReadOnlyList<object?>>> pages = Drain(
                builder,
                new[] { Row(sentinel), Row(sentinel), Row(sentinel) });

            Assert.Equal(3, pages.Count);
            Assert.All(pages, page => Assert.Single(page));
        }

        [Fact]
        public void UnsafeInt64WrappersSplitAtTheWireByteBound()
        {
            int encodedBytes = Encoding.UTF8.GetByteCount(
                WireValueEncoder.Encode(long.MaxValue)!.ToJsonString());
            var builder = new SqlRowsPageBuilder(pageRows: 100, pageBytes: encodedBytes * 2 - 1);

            List<IReadOnlyList<IReadOnlyList<object?>>> pages = Drain(
                builder,
                new[] { Row(long.MaxValue), Row(long.MaxValue), Row(long.MaxValue) });

            Assert.Equal(3, pages.Count);
            Assert.All(pages, page => Assert.Single(page));
        }
    }
}
