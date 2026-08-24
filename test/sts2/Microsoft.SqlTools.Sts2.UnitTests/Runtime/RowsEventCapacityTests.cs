//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    public class RowsEventCapacityTests
    {
        [Theory]
        [InlineData("plain ascii")]
        [InlineData("<i>xml & html-sensitive</i>")]
        [InlineData("quotes: \" and slash: \\")]
        [InlineData("unicode é 𐍈")]
        public void JsonStringEstimateIsAnUpperBoundForTheProductEncoder(string value)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStringValue(value);
                writer.Flush();
            }

            long estimate = DriverEffectRunner.EstimateJsonStringBytes(value, value.Length);
            Assert.True(estimate >= buffer.WrittenCount, $"estimate {estimate} < actual {buffer.WrittenCount}");
        }

        [Fact]
        public void XmlHeavyPrefixPricesDefaultHtmlEscaping()
        {
            string value = string.Concat(Enumerable.Repeat("<i>y</i>", 8192));
            long estimate = DriverEffectRunner.EstimateJsonStringBytes(value, value.Length);

            // Each angle bracket becomes a six-byte JSON escape. The old
            // chars+25% heuristic estimated only ~82 KiB for this ~224 KiB value.
            Assert.InRange(estimate, 220_000, 240_000);
        }

        [Fact]
        public void CellEstimateCoversFullAndTruncatedLargeValues()
        {
            string fullText = new string('x', 100_000);
            string xmlPrefix = string.Concat(Enumerable.Repeat("<i>y</i>", 8192));
            object[] cells =
            [
                fullText,
                new byte[100_000],
                new DriverTruncatedValue
                {
                    Kind = "string",
                    PrefixText = xmlPrefix,
                    TotalBytes = 2_000_000,
                    DigestHex = new string('0', 64),
                },
            ];

            foreach (object cell in cells)
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(buffer))
                {
                    WireValueEncoder.Write(writer, cell, Sts2Defaults.MaxCellBytes);
                    writer.Flush();
                }

                long estimate = DriverEffectRunner.EstimateCellJsonBytes(cell, Sts2Defaults.MaxCellBytes);
                Assert.True(estimate >= buffer.WrittenCount, $"estimate {estimate} < actual {buffer.WrittenCount}");
            }
        }

        [Fact]
        public void RowsCapacityIncludesLargestContiguousEscapeReservation()
        {
            string jsonPrefix = string.Concat(Enumerable.Repeat("{\"x\":\"y\"}", 7282))[..65536];
            string xmlPrefix = string.Concat(Enumerable.Repeat("<i>y</i>", 8192));
            IReadOnlyList<IReadOnlyList<object?>> rows =
            [
                new object?[]
                {
                    1,
                    Truncated(jsonPrefix),
                    Truncated(xmlPrefix),
                },
            ];
            int capacity = DriverEffectRunner.EstimateRowsEventCapacity(rows, Sts2Defaults.MaxCellBytes);
            var buffer = new TrackingBufferWriter(capacity);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                DriverEffectRunner.WriteRows(
                    writer,
                    rows,
                    Sts2Defaults.MaxCellBytes,
                    out _,
                    out _);
                writer.Flush();
            }

            Assert.True(
                capacity == buffer.Capacity,
                $"initial {capacity}, final capacity {buffer.Capacity}, written {buffer.WrittenCount}; "
                + string.Join("; ", buffer.Requests));
            Assert.True(capacity >= buffer.WrittenCount);
            Assert.InRange(capacity, 800_000, 1_000_000);

            static DriverTruncatedValue Truncated(string prefix) => new()
            {
                Kind = "string",
                PrefixText = prefix,
                TotalBytes = 2_000_000,
                DigestHex = new string('0', 64),
            };
        }

        [Fact]
        public void RowsCapacityAvoidsReplacementAcrossLargeBinaryWireForms()
        {
            byte[] payload = Enumerable.Range(0, Sts2Defaults.TruncatedPrefixBytes)
                .Select(i => (byte)i)
                .ToArray();
            IReadOnlyList<IReadOnlyList<object?>> rows =
            [
                new object?[]
                {
                    payload,
                    new DriverTruncatedValue
                    {
                        Kind = "binary",
                        PrefixBytes = payload,
                        TotalBytes = 2_000_000,
                        DigestHex = new string('0', 64),
                    },
                    new DriverVectorValue
                    {
                        Dimensions = payload.Length / sizeof(float),
                        BaseType = "float32",
                        Encoding = "f32le",
                        ComponentBytes = payload,
                    },
                    new DriverSpatialValue
                    {
                        Kind = "geometry",
                        Srid = 0,
                        Wkb = payload,
                    },
                },
            ];
            int capacity = DriverEffectRunner.EstimateRowsEventCapacity(rows, Sts2Defaults.MaxCellBytes);
            var buffer = new TrackingBufferWriter(capacity);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                DriverEffectRunner.WriteRows(
                    writer,
                    rows,
                    Sts2Defaults.MaxCellBytes,
                    out _,
                    out _);
                writer.Flush();
            }

            Assert.True(
                capacity == buffer.Capacity,
                $"initial {capacity}, final capacity {buffer.Capacity}, written {buffer.WrittenCount}; "
                + string.Join("; ", buffer.Requests));
            Assert.True(capacity >= buffer.WrittenCount);
        }

        private sealed class TrackingBufferWriter : IBufferWriter<byte>
        {
            private readonly ArrayBufferWriter<byte> inner;

            internal TrackingBufferWriter(int capacity)
            {
                inner = new ArrayBufferWriter<byte>(capacity);
            }

            internal int Capacity => inner.Capacity;
            internal int WrittenCount => inner.WrittenCount;
            internal List<string> Requests { get; } = [];

            public void Advance(int count) => inner.Advance(count);

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                int before = inner.Capacity;
                Memory<byte> memory = inner.GetMemory(sizeHint);
                Requests.Add($"hint={sizeHint}, written={inner.WrittenCount}, capacity={before}->{inner.Capacity}");
                return memory;
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                int before = inner.Capacity;
                Span<byte> span = inner.GetSpan(sizeHint);
                Requests.Add($"hint={sizeHint}, written={inner.WrittenCount}, capacity={before}->{inner.Capacity}");
                return span;
            }
        }
    }
}
