//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Contracts;
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
        public void TruncatedWrapperCarriesFullValueDigest() // STS2-3 honesty metadata
        {
            string big = new('x', 100);
            JsonNode wrapper = WireValueEncoder.Encode(big, maxCellBytes: 10)!;
            string expected = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(big)));
            Assert.Equal(expected, wrapper["digest"]!.GetValue<string>());

            byte[] blob = new byte[100];
            JsonNode binaryWrapper = WireValueEncoder.Encode(blob, 10)!;
            Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(blob)),
                binaryWrapper["digest"]!.GetValue<string>());
            // The binary prefix decodes to the first 10 raw bytes.
            Assert.Equal(blob.AsSpan(0, 10).ToArray(), Convert.FromBase64String(binaryWrapper["v"]!.GetValue<string>()));
        }

        [Fact]
        public void DriverTruncatedValuesEmitTheWrapperVerbatimWithRecappedPrefix() // QO-4
        {
            // The driver streamed the full value: bytes/digest arrive as facts;
            // the encoder re-caps only the retained prefix.
            var text = new Abstractions.DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = new string('x', 100),
                TotalBytes = 5_000_000_000, // > int.MaxValue: long-safe on the wire
                DigestHex = "abc123",
            };
            JsonNode wrapper = WireValueEncoder.Encode(text, maxCellBytes: 10)!;
            Assert.Equal("truncated", wrapper["$t"]!.GetValue<string>());
            Assert.Equal("string", wrapper["of"]!.GetValue<string>());
            Assert.Equal(5_000_000_000, wrapper["bytes"]!.GetValue<long>());
            Assert.Equal("sha256:abc123", wrapper["digest"]!.GetValue<string>());
            Assert.Equal(new string('x', 10), wrapper["v"]!.GetValue<string>());

            // Multi-byte prefix re-cap never splits a code point.
            var unicode = new Abstractions.DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = new string('é', 20),
                TotalBytes = 999,
                DigestHex = "dd",
            };
            Assert.Equal("éé", WireValueEncoder.Encode(unicode, 5)!["v"]!.GetValue<string>());

            var astral = new Abstractions.DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = new string('a', 4094) + "𐍈" + new string('b', 10),
                TotalBytes = 9999,
                DigestHex = "ee",
            };
            Assert.Equal(
                new string('a', 4094),
                WireValueEncoder.Encode(astral, 4096)!["v"]!.GetValue<string>());

            var binary = new Abstractions.DriverTruncatedValue
            {
                Kind = "binary",
                PrefixBytes = new byte[100],
                TotalBytes = 123456,
                DigestHex = "ff00",
            };
            JsonNode binaryWrapper = WireValueEncoder.Encode(binary, 10)!;
            Assert.Equal("binary", binaryWrapper["of"]!.GetValue<string>());
            Assert.Equal(10, Convert.FromBase64String(binaryWrapper["v"]!.GetValue<string>()).Length);
        }

        [Fact]
        public void ExactBoundIsNotTruncatedAndOneOverIs() // STS2-3 boundary
        {
            string atBound = new('x', 64);
            Assert.Equal(JsonValueKind.String, WireValueEncoder.Encode(atBound, 64)!.GetValueKind());

            string oneOver = new('x', 65);
            JsonNode wrapper = WireValueEncoder.Encode(oneOver, 64)!;
            Assert.Equal("truncated", wrapper["$t"]!.GetValue<string>());
            Assert.Equal(65, wrapper["bytes"]!.GetValue<int>());
            Assert.Equal(64, Encoding.UTF8.GetByteCount(wrapper["v"]!.GetValue<string>()));

            byte[] blobAtBound = new byte[64];
            Assert.Equal("binary", WireValueEncoder.Encode(blobAtBound, 64)!["$t"]!.GetValue<string>());
            Assert.Equal("truncated", WireValueEncoder.Encode(new byte[65], 64)!["$t"]!.GetValue<string>());
        }

        [Fact]
        public void TruncationNeverSplitsACodePoint() // STS2-3 UTF-8 safety
        {
            // 4095 ASCII bytes then a 2-byte 'é' straddling the 4096 bound: the prefix
            // backs off to the code-point boundary and stays decodable/re-encodable.
            string cell = new string('a', 4095) + "é" + new string('b', 100);
            string prefix = WireValueEncoder.Encode(cell, 4096)!["v"]!.GetValue<string>();
            Assert.Equal(new string('a', 4095), prefix);

            // A 4-byte surrogate pair ('𐍈') at the bound is dropped whole, never halved.
            string astral = new string('a', 4094) + "𐍈" + new string('b', 100);
            string astralPrefix = WireValueEncoder.Encode(astral, 4096)!["v"]!.GetValue<string>();
            Assert.Equal(new string('a', 4094), astralPrefix);
            Assert.True(Encoding.UTF8.GetByteCount(astralPrefix) <= 4096);
        }

        [Fact]
        public void RetainedPrefixIsCappedByTruncatedPrefixBytes() // SPEC §7.7 / §11.2
        {
            // Bound above the pinned prefix cap: the wrapper keeps only the capped prefix
            // while bytes/digest still describe the full value.
            string big = new('x', Sts2Defaults.TruncatedPrefixBytes + 5000);
            JsonNode wrapper = WireValueEncoder.Encode(big, Sts2Defaults.TruncatedPrefixBytes + 1000)!;
            Assert.Equal("truncated", wrapper["$t"]!.GetValue<string>());
            Assert.Equal(Sts2Defaults.TruncatedPrefixBytes, Encoding.UTF8.GetByteCount(wrapper["v"]!.GetValue<string>()));
            Assert.Equal(big.Length, wrapper["bytes"]!.GetValue<int>());
        }

        [Fact]
        public void VectorCellsEncodeTypedWithDataFieldNeverV() // D-0019
        {
            float[] components = [0.1f, -2.5f, float.MaxValue, float.Epsilon];
            byte[] bytes = new byte[components.Length * 4];
            for (int i = 0; i < components.Length; i++)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                    bytes.AsSpan(i * 4, 4), BitConverter.SingleToInt32Bits(components[i]));
            }
            var cell = new Abstractions.DriverVectorValue
            {
                Dimensions = components.Length,
                BaseType = "float32",
                Encoding = "f32le",
                ComponentBytes = bytes,
            };

            JsonNode node = WireValueEncoder.Encode(cell)!;
            Assert.Equal("vector", node["$t"]!.GetValue<string>());
            Assert.Equal(1, node["version"]!.GetValue<int>());
            Assert.Equal("ok", node["status"]!.GetValue<string>());
            Assert.Equal(4, node["dimensions"]!.GetValue<int>());
            Assert.Equal("float32", node["baseType"]!.GetValue<string>());
            Assert.Equal("f32le", node["encoding"]!.GetValue<string>());
            Assert.Equal(16, node["byteLength"]!.GetValue<int>());
            // The payload field is "data" — "v" would collide with the generic
            // {$t,v} scalar-wrapper handling in clients.
            Assert.Null(node["v"]);
            Assert.Equal(bytes, Convert.FromBase64String(node["data"]!.GetValue<string>()));
        }

        [Fact]
        public void VectorCellsAreNeverTruncated() // D-0019: complete or unavailable
        {
            var cell = new Abstractions.DriverVectorValue
            {
                Dimensions = 100,
                BaseType = "float32",
                Encoding = "f32le",
                ComponentBytes = new byte[400],
            };
            // A bound far below the encoded size must not produce a truncated
            // wrapper — the driver enforces the bound with a typed sentinel.
            JsonNode node = WireValueEncoder.Encode(cell, maxCellBytes: 10)!;
            Assert.Equal("vector", node["$t"]!.GetValue<string>());
            Assert.Equal("ok", node["status"]!.GetValue<string>());
            Assert.Equal(400, Convert.FromBase64String(node["data"]!.GetValue<string>()).Length);
        }

        [Fact]
        public void VectorUnavailableSentinelCarriesReasonAndOptionalFacts() // D-0019
        {
            JsonNode bare = WireValueEncoder.Encode(
                new Abstractions.DriverVectorUnavailableValue { Reason = "unsupportedBaseType" })!;
            Assert.Equal("vector", bare["$t"]!.GetValue<string>());
            Assert.Equal(1, bare["version"]!.GetValue<int>());
            Assert.Equal("unavailable", bare["status"]!.GetValue<string>());
            Assert.Equal("unsupportedBaseType", bare["reason"]!.GetValue<string>());
            Assert.Null(bare["dimensions"]);
            Assert.Null(bare["baseType"]);
            Assert.Null(bare["data"]);

            JsonNode withFacts = WireValueEncoder.Encode(
                new Abstractions.DriverVectorUnavailableValue { Dimensions = 1536, BaseType = "float16", Reason = "cellLimit" })!;
            Assert.Equal(1536, withFacts["dimensions"]!.GetValue<int>());
            Assert.Equal("float16", withFacts["baseType"]!.GetValue<string>());
            Assert.Equal("cellLimit", withFacts["reason"]!.GetValue<string>());
        }

        [Fact]
        public void SpatialCellsEncodeCompleteWkbWithoutGenericVCollision() // D-0020
        {
            byte[] wkb = Convert.FromHexString("0101000000000000000000F03F0000000000000040");
            JsonNode node = WireValueEncoder.Encode(new Abstractions.DriverSpatialValue
            {
                Kind = "geometry",
                Srid = 4326,
                Wkb = wkb,
            })!;

            Assert.Equal("spatial", node["$t"]!.GetValue<string>());
            Assert.Equal(1, node["version"]!.GetValue<int>());
            Assert.Equal("ok", node["status"]!.GetValue<string>());
            Assert.Equal("geometry", node["kind"]!.GetValue<string>());
            Assert.Equal("wkb", node["encoding"]!.GetValue<string>());
            Assert.Equal(4326, node["srid"]!.GetValue<int>());
            Assert.Equal(wkb.Length, node["wkbBytes"]!.GetValue<int>());
            Assert.Equal(wkb, Convert.FromBase64String(node["wkb"]!.GetValue<string>()));
            Assert.Null(node["v"]);
        }

        [Fact]
        public void SpatialUnavailableSentinelCarriesOnlySafeFacts() // D-0020
        {
            JsonNode node = WireValueEncoder.Encode(new Abstractions.DriverSpatialUnavailableValue
            {
                Kind = "geography",
                Reason = "maxCellBytes",
                Srid = 4326,
                SourceBytes = 2_000_000,
            })!;
            Assert.Equal("unrenderable", node["status"]!.GetValue<string>());
            Assert.Equal("geography", node["kind"]!.GetValue<string>());
            Assert.Equal("maxCellBytes", node["reason"]!.GetValue<string>());
            Assert.Equal(4326, node["srid"]!.GetValue<int>());
            Assert.Equal(2_000_000, node["sourceBytes"]!.GetValue<long>());
            Assert.Null(node["wkb"]);
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
