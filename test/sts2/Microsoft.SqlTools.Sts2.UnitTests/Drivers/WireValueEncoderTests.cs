//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// SPEC §7.7 type-encoding matrix, server-free. The values a SqlClient reader returns
    /// (decimal, DateTime, DateTimeOffset, Guid, byte[], non-finite floats) encode to the
    /// pinned wire form without a live engine.
    /// </summary>
    public class WireValueEncoderTests
    {
        private static string? Json(object? cell) => WireValueEncoder.Encode(cell)?.ToJsonString();

        [Fact]
        public void NullAndDbNullEncodeToJsonNull()
        {
            Assert.Null(WireValueEncoder.Encode(null));
            Assert.Null(WireValueEncoder.Encode(DBNull.Value));
        }

        [Fact]
        public void OversizedCellsAreTruncatedDeterministically() // R024
        {
            string big = new('x', 100);
            JsonNode wrapper = WireValueEncoder.Encode(big, maxCellBytes: 10)!;
            Assert.Equal("truncated", wrapper["$t"]!.GetValue<string>());
            Assert.Equal("string", wrapper["of"]!.GetValue<string>());
            Assert.Equal(100, wrapper["bytes"]!.GetValue<int>());
            Assert.Equal(new string('x', 10), wrapper["v"]!.GetValue<string>());

            // Binary truncates too; small cells and a 0/large limit pass through unchanged.
            byte[] blob = new byte[100];
            Assert.Equal("truncated", WireValueEncoder.Encode(blob, 10)!["$t"]!.GetValue<string>());
            Assert.Equal("\"abc\"", WireValueEncoder.Encode("abc", 10)!.ToJsonString());
            Assert.Equal("\"abc\"", WireValueEncoder.Encode("abc", 0)!.ToJsonString());

            // Multi-byte UTF-8 is cut on a char boundary (never a partial code unit).
            string unicode = new('é', 20); // 'é' = 2 UTF-8 bytes each
            string prefix = WireValueEncoder.Encode(unicode, 5)!["v"]!.GetValue<string>();
            Assert.Equal("éé", prefix); // 2 chars = 4 bytes <= 5, third would exceed
        }

        [Fact]
        public void LosslessNativesStayNative()
        {
            Assert.Equal("true", Json(true));
            Assert.Equal("42", Json(42L));
            Assert.Equal("7", Json(7));
            Assert.Equal("1.5", Json(1.5));
            Assert.Equal("\"abc\"", Json("abc"));
        }

        [Fact]
        public void DecimalIsAnInvariantTypedWrapper()
        {
            JsonNode node = WireValueEncoder.Encode(12.50m)!;
            Assert.Equal("decimal", node["$t"]!.GetValue<string>());
            Assert.Equal("12.50", node["v"]!.GetValue<string>()); // scale preserved
        }

        [Fact]
        public void DateTimeKindsRoundTripInvariant()
        {
            JsonNode dt = WireValueEncoder.Encode(new DateTime(2026, 6, 13, 1, 2, 3, DateTimeKind.Utc))!;
            Assert.Equal("datetime2", dt["$t"]!.GetValue<string>());
            Assert.StartsWith("2026-06-13T01:02:03", dt["v"]!.GetValue<string>());

            JsonNode dto = WireValueEncoder.Encode(new DateTimeOffset(2026, 6, 13, 1, 2, 3, TimeSpan.FromHours(5)))!;
            Assert.Equal("datetimeoffset", dto["$t"]!.GetValue<string>());
            Assert.Contains("+05:00", dto["v"]!.GetValue<string>());
        }

        [Fact]
        public void GuidAndBinaryUseTypedWrappers()
        {
            JsonNode guid = WireValueEncoder.Encode(Guid.Parse("00000000-0000-0000-0000-000000000001"))!;
            Assert.Equal("guid", guid["$t"]!.GetValue<string>());
            Assert.Equal("00000000-0000-0000-0000-000000000001", guid["v"]!.GetValue<string>());

            JsonNode binary = WireValueEncoder.Encode(new byte[] { 1, 2, 3, 4 })!;
            Assert.Equal("binary", binary["$t"]!.GetValue<string>());
            Assert.Equal("AQIDBA==", binary["v"]!.GetValue<string>());
        }

        [Fact]
        public void NonFiniteFloatsAreWrappedNotEmittedAsRawJson()
        {
            Assert.Equal("NaN", WireValueEncoder.Encode(double.NaN)!["v"]!.GetValue<string>());
            Assert.Equal("Infinity", WireValueEncoder.Encode(double.PositiveInfinity)!["v"]!.GetValue<string>());
            Assert.Equal("-Infinity", WireValueEncoder.Encode(double.NegativeInfinity)!["v"]!.GetValue<string>());
        }

        [Fact]
        public void TimeSpanEncodesAsTime()
        {
            JsonNode time = WireValueEncoder.Encode(new TimeSpan(1, 2, 3))!;
            Assert.Equal("time", time["$t"]!.GetValue<string>());
            Assert.Contains("01:02:03", time["v"]!.GetValue<string>());
        }

        [Fact]
        public void EncodingIsDeterministic()
        {
            Assert.Equal(Json(12.34m), Json(12.34m));
            Assert.Equal(Json(new byte[] { 9, 8, 7 }), Json(new byte[] { 9, 8, 7 }));
        }
    }
}
