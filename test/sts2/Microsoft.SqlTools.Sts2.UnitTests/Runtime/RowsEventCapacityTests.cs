//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Buffers;
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
    }
}
