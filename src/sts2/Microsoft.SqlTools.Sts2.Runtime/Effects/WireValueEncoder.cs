//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Runtime.Effects
{
    /// <summary>
    /// Encodes a driver's CLR cell value to its wire form (SPEC §7.7): JSON natives where
    /// lossless and unambiguous, typed wrappers (<c>{"$t":...,"v":...}</c>) for lossy or
    /// ambiguous values. Invariant strings throughout (SPEC §2.11). Server-free and
    /// unit-tested so the type matrix is validated without a live engine.
    /// </summary>
    public static class WireValueEncoder
    {
        /// <summary>
        /// Encodes one cell, truncating oversized string/binary values at the effective
        /// bound <paramref name="maxCellBytes"/> (SPEC §7.7 maxCellBytes, R024/STS2-3) so a
        /// single pathological cell cannot blow the memory/frame bound. Truncation is never
        /// silent and is deterministic (UTF-8 boundary-safe), so replay reproduces the same
        /// digest. A truncated cell becomes
        /// <c>{"$t":"truncated","of":"string|binary","bytes":N,"digest":"sha256:...","v":&lt;prefix&gt;}</c>
        /// where <c>bytes</c>/<c>digest</c> describe the FULL value and the retained prefix
        /// is additionally capped by <c>sts2.results.truncatedPrefixBytes</c>.
        /// A non-positive bound disables truncation.
        /// </summary>
        public static JsonNode? Encode(object? cell, int maxCellBytes)
        {
            // A large value the DRIVER already pre-bounded by streaming (QO-4):
            // bytes/digest were computed over the full stream, so the wrapper
            // is emitted verbatim — only the retained prefix is re-capped by
            // the same rules as encoder-side truncation.
            if (cell is Abstractions.DriverTruncatedValue driverTruncated)
            {
                return DriverTruncated(driverTruncated, maxCellBytes);
            }
            if (maxCellBytes > 0)
            {
                switch (cell)
                {
                    case string str when Encoding.UTF8.GetByteCount(str) > maxCellBytes:
                        return TruncatedString(str, maxCellBytes);
                    case byte[] bytes when bytes.Length > maxCellBytes:
                        return TruncatedBinary(bytes, maxCellBytes);
                }
            }
            return Encode(cell);
        }

        /// <summary>SPEC §7.7 wrapper from driver-streamed truncation facts (QO-4).</summary>
        private static JsonObject DriverTruncated(Abstractions.DriverTruncatedValue value, int maxCellBytes)
        {
            int cap = maxCellBytes > 0 ? PrefixLength(maxCellBytes) : Sts2Defaults.TruncatedPrefixBytes;
            string prefix;
            if (value.Kind == "binary")
            {
                byte[] raw = value.PrefixBytes ?? [];
                prefix = Convert.ToBase64String(raw.Length > cap ? raw.AsSpan(0, cap) : raw);
            }
            else
            {
                byte[] raw = Encoding.UTF8.GetBytes(value.PrefixText ?? string.Empty);
                int len = Math.Min(cap, raw.Length);
                while (len > 0 && len < raw.Length && (raw[len] & 0xC0) == 0x80)
                {
                    len--;
                }
                prefix = Encoding.UTF8.GetString(raw, 0, len);
            }
            return new JsonObject
            {
                ["$t"] = "truncated",
                ["of"] = value.Kind == "binary" ? "binary" : "string",
                ["bytes"] = value.TotalBytes,
                ["digest"] = "sha256:" + value.DigestHex,
                ["v"] = prefix,
            };
        }

        /// <summary>Encodes one cell. Null maps to JSON null.</summary>
        public static JsonNode? Encode(object? cell) => cell switch
        {
            null or DBNull => null,

            // Typed vector cells (D-0019, SPEC §7.7): emitted only for queries that
            // negotiated vectorEncoding=binary-v1 (the driver produces these values
            // only then). The payload field is "data", NEVER "v" — "v" would collide
            // with the generic {$t,v} scalar-wrapper handling in clients. Vectors are
            // never truncated: complete or an unavailable sentinel (the driver
            // enforces the cell bound with reason "cellLimit").
            Abstractions.DriverVectorValue vector => new JsonObject
            {
                ["$t"] = "vector",
                ["version"] = 1,
                ["status"] = "ok",
                ["dimensions"] = vector.Dimensions,
                ["baseType"] = vector.BaseType,
                ["encoding"] = vector.Encoding,
                ["byteLength"] = vector.ComponentBytes.Length,
                ["data"] = Convert.ToBase64String(vector.ComponentBytes),
            },
            Abstractions.DriverVectorUnavailableValue unavailable => VectorUnavailable(unavailable),

            // D-0020: complete AsBinaryZM WKB is opt-in and provider-neutral.
            // Spatial values are complete or an honest cell-local sentinel.
            Abstractions.DriverSpatialValue spatial => new JsonObject
            {
                ["$t"] = "spatial",
                ["version"] = 1,
                ["status"] = "ok",
                ["kind"] = spatial.Kind,
                ["encoding"] = "wkb",
                ["srid"] = spatial.Srid,
                ["wkbBytes"] = spatial.Wkb.Length,
                ["wkb"] = Convert.ToBase64String(spatial.Wkb),
            },
            Abstractions.DriverSpatialUnavailableValue spatialUnavailable => SpatialUnavailable(spatialUnavailable),

            // Lossless JSON natives.
            bool b => JsonValue.Create(b),
            long l => JsonValue.Create(l),
            int i => JsonValue.Create(i),
            short s => JsonValue.Create((int)s),
            byte by => JsonValue.Create((int)by),
            string str => JsonValue.Create(str),

            // Floating point: non-finite values are not representable in JSON, so wrap.
            double d => double.IsFinite(d) ? JsonValue.Create(d) : NonFinite(d),
            float f => double.IsFinite(f) ? JsonValue.Create((double)f) : NonFinite(f),

            // Lossy/ambiguous: typed wrappers with invariant string payloads.
            decimal dec => Wrapper("decimal", dec.ToString(CultureInfo.InvariantCulture)),
            DateTime dt => Wrapper("datetime2", dt.ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset dto => Wrapper("datetimeoffset", dto.ToString("O", CultureInfo.InvariantCulture)),
            TimeSpan ts => Wrapper("time", ts.ToString("c", CultureInfo.InvariantCulture)),
            Guid g => Wrapper("guid", g.ToString("D", CultureInfo.InvariantCulture)),
            byte[] bytes => Wrapper("binary", Convert.ToBase64String(bytes)),

            // A pre-built wire node (FakeDriver edge values) passes through.
            JsonNode node => node.DeepClone(),

            // Provider-specific fallback: stable invariant string.
            _ => Wrapper("provider", Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty),
        };

        private static JsonObject Wrapper(string type, string value) => new()
        {
            ["$t"] = type,
            ["v"] = value,
        };

        /// <summary>Honest vector sentinel (D-0019): stable reason, facts only when determinable.</summary>
        private static JsonObject VectorUnavailable(Abstractions.DriverVectorUnavailableValue value)
        {
            var node = new JsonObject
            {
                ["$t"] = "vector",
                ["version"] = 1,
                ["status"] = "unavailable",
                ["reason"] = value.Reason,
            };
            if (value.Dimensions is int dimensions)
            {
                node["dimensions"] = dimensions;
            }
            if (value.BaseType is string baseType)
            {
                node["baseType"] = baseType;
            }
            return node;
        }

        private static JsonObject SpatialUnavailable(Abstractions.DriverSpatialUnavailableValue value)
        {
            var node = new JsonObject
            {
                ["$t"] = "spatial",
                ["version"] = 1,
                ["status"] = "unrenderable",
                ["kind"] = value.Kind,
                ["reason"] = value.Reason,
            };
            if (value.Srid is int srid)
            {
                node["srid"] = srid;
            }
            if (value.SourceBytes is long sourceBytes)
            {
                node["sourceBytes"] = sourceBytes;
            }
            return node;
        }

        /// <summary>
        /// The truncation-honesty wrapper (SPEC §7.7): <c>bytes</c> and <c>digest</c> describe
        /// the full value so the client can detect truncation and identify the original.
        /// </summary>
        private static JsonObject Truncated(string of, byte[] fullValue, string prefix) => new()
        {
            ["$t"] = "truncated",
            ["of"] = of,
            ["bytes"] = fullValue.Length,
            ["digest"] = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(fullValue)),
            ["v"] = prefix,
        };

        /// <summary>Retained prefix bytes: the effective bound capped by <c>sts2.results.truncatedPrefixBytes</c>.</summary>
        private static int PrefixLength(int maxCellBytes) => Math.Min(maxCellBytes, Sts2Defaults.TruncatedPrefixBytes);

        /// <summary>Wraps an oversized string; the prefix never splits a UTF-8 code point.</summary>
        private static JsonObject TruncatedString(string value, int maxCellBytes)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            int len = PrefixLength(maxCellBytes); // < bytes.Length: the caller only truncates oversized values
            while (len > 0 && (bytes[len] & 0xC0) == 0x80) // don't cut a UTF-8 continuation byte
            {
                len--;
            }
            return Truncated("string", bytes, Encoding.UTF8.GetString(bytes, 0, len));
        }

        /// <summary>Wraps an oversized binary value; the prefix bound applies to the raw bytes (pre-base64).</summary>
        private static JsonObject TruncatedBinary(byte[] bytes, int maxCellBytes) =>
            Truncated("binary", bytes, Convert.ToBase64String(bytes.AsSpan(0, PrefixLength(maxCellBytes))));

        private static JsonObject NonFinite(double value) => Wrapper(
            "double",
            double.IsNaN(value) ? "NaN" : double.IsPositiveInfinity(value) ? "Infinity" : "-Infinity");
    }
}
