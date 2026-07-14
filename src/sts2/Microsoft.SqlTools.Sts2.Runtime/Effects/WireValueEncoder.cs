//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                Write(writer, cell, maxCellBytes);
                writer.Flush();
            }
            return JsonNode.Parse(buffer.WrittenSpan);
        }

        /// <summary>
        /// Writes one cell directly to a UTF-8 stream. The query page hot path uses this
        /// form so it does not construct a second JsonNode graph before serialization.
        /// </summary>
        internal static void Write(Utf8JsonWriter writer, object? cell, int maxCellBytes)
        {
            ArgumentNullException.ThrowIfNull(writer);

            // A large value the DRIVER already pre-bounded by streaming (QO-4):
            // bytes/digest were computed over the full stream, so the wrapper
            // is emitted verbatim — only the retained prefix is re-capped by
            // the same rules as encoder-side truncation.
            if (cell is Abstractions.DriverTruncatedValue driverTruncated)
            {
                WriteDriverTruncated(writer, driverTruncated, maxCellBytes);
                return;
            }
            if (maxCellBytes > 0)
            {
                switch (cell)
                {
                    case string str when Encoding.UTF8.GetByteCount(str) > maxCellBytes:
                        WriteTruncatedString(writer, str, maxCellBytes);
                        return;
                    case byte[] bytes when bytes.Length > maxCellBytes:
                        WriteTruncatedBinary(writer, bytes, maxCellBytes);
                        return;
                }
            }
            Write(writer, cell);
        }

        /// <summary>SPEC §7.7 wrapper from driver-streamed truncation facts (QO-4).</summary>
        private static void WriteDriverTruncated(
            Utf8JsonWriter writer,
            Abstractions.DriverTruncatedValue value,
            int maxCellBytes)
        {
            int cap = maxCellBytes > 0 ? PrefixLength(maxCellBytes) : Sts2Defaults.TruncatedPrefixBytes;
            string prefix;
            if (value.Kind == "binary")
            {
                byte[] raw = value.PrefixBytes ?? [];
                prefix = Convert.ToBase64String(raw.Length > cap ? raw.AsSpan(0, cap) : raw);
                WriteTruncated(
                    writer,
                    "binary",
                    value.TotalBytes,
                    "sha256:" + value.DigestHex,
                    prefix);
            }
            else
            {
                prefix = value.PrefixText ?? string.Empty;
                int chars = Utf8PrefixCharLength(prefix, cap);
                WriteTruncated(
                    writer,
                    "string",
                    value.TotalBytes,
                    "sha256:" + value.DigestHex,
                    prefix.AsSpan(0, chars));
            }
        }

        /// <summary>Largest complete-code-point UTF-16 prefix within a UTF-8 byte budget.</summary>
        internal static int Utf8PrefixCharLength(string value, int maxBytes)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value);
            return Utf8PrefixCharLength(value, maxBytes, byteCount);
        }

        /// <summary>Byte-count-aware overload for callers already pricing the same value.</summary>
        internal static int Utf8PrefixCharLength(string value, int maxBytes, int utf8ByteCount)
        {
            if (maxBytes <= 0)
            {
                return 0;
            }
            // The common SQL path is ASCII or otherwise already within the
            // prefix budget. The runtime byte counter is vectorized and avoids
            // the substantially slower scalar-by-scalar walk in that case.
            if (utf8ByteCount <= maxBytes)
            {
                return value.Length;
            }
            int chars = 0;
            int bytes = 0;
            foreach (Rune rune in value.EnumerateRunes())
            {
                if (rune.Utf8SequenceLength > maxBytes - bytes)
                {
                    break;
                }
                bytes += rune.Utf8SequenceLength;
                chars += rune.Utf16SequenceLength;
            }
            return chars;
        }

        /// <summary>Encodes one cell. Null maps to JSON null.</summary>
        public static JsonNode? Encode(object? cell) => Encode(cell, maxCellBytes: 0);

        private static void Write(Utf8JsonWriter writer, object? cell)
        {
            switch (cell)
            {
                case null:
                case DBNull:
                    writer.WriteNullValue();
                    return;

                // Typed vector cells (D-0019, SPEC §7.7): emitted only for queries that
                // negotiated vectorEncoding=binary-v1 (the driver produces these values
                // only then). The payload field is "data", NEVER "v" — "v" would collide
                // with the generic {$t,v} scalar-wrapper handling in clients. Vectors are
                // never truncated: complete or an unavailable sentinel (the driver
                // enforces the cell bound with reason "cellLimit").
                case Abstractions.DriverVectorValue vector:
                    writer.WriteStartObject();
                    writer.WriteString("$t", "vector");
                    writer.WriteNumber("version", 1);
                    writer.WriteString("status", "ok");
                    writer.WriteNumber("dimensions", vector.Dimensions);
                    writer.WriteString("baseType", vector.BaseType);
                    writer.WriteString("encoding", vector.Encoding);
                    writer.WriteNumber("byteLength", vector.ComponentBytes.Length);
                    writer.WriteBase64String("data", vector.ComponentBytes);
                    writer.WriteEndObject();
                    return;
                case Abstractions.DriverVectorUnavailableValue unavailable:
                    WriteVectorUnavailable(writer, unavailable);
                    return;

                // D-0020: complete AsBinaryZM WKB is opt-in and provider-neutral.
                // Spatial values are complete or an honest cell-local sentinel.
                case Abstractions.DriverSpatialValue spatial:
                    writer.WriteStartObject();
                    writer.WriteString("$t", "spatial");
                    writer.WriteNumber("version", 1);
                    writer.WriteString("status", "ok");
                    writer.WriteString("kind", spatial.Kind);
                    writer.WriteString("encoding", "wkb");
                    writer.WriteNumber("srid", spatial.Srid);
                    writer.WriteNumber("wkbBytes", spatial.Wkb.Length);
                    writer.WriteBase64String("wkb", spatial.Wkb);
                    writer.WriteEndObject();
                    return;
                case Abstractions.DriverSpatialUnavailableValue spatialUnavailable:
                    WriteSpatialUnavailable(writer, spatialUnavailable);
                    return;

                // Lossless JSON natives.
                case bool b:
                    writer.WriteBooleanValue(b);
                    return;
                case long l:
                    writer.WriteNumberValue(l);
                    return;
                case int i:
                    writer.WriteNumberValue(i);
                    return;
                case short s:
                    writer.WriteNumberValue((int)s);
                    return;
                case byte by:
                    writer.WriteNumberValue((int)by);
                    return;
                case string str:
                    writer.WriteStringValue(str);
                    return;

                // Floating point: non-finite values are not representable in JSON, so wrap.
                case double d when double.IsFinite(d):
                    writer.WriteNumberValue(d);
                    return;
                case double d:
                    WriteNonFinite(writer, d);
                    return;
                case float f when float.IsFinite(f):
                    writer.WriteNumberValue((double)f);
                    return;
                case float f:
                    WriteNonFinite(writer, f);
                    return;

                // Lossy/ambiguous: typed wrappers with invariant string payloads.
                case decimal dec:
                    WriteWrapper(writer, "decimal", dec.ToString(CultureInfo.InvariantCulture));
                    return;
                case DateTime dt:
                    WriteWrapper(writer, "datetime2", dt.ToString("O", CultureInfo.InvariantCulture));
                    return;
                case DateTimeOffset dto:
                    WriteWrapper(writer, "datetimeoffset", dto.ToString("O", CultureInfo.InvariantCulture));
                    return;
                case TimeSpan ts:
                    WriteWrapper(writer, "time", ts.ToString("c", CultureInfo.InvariantCulture));
                    return;
                case Guid g:
                    WriteWrapper(writer, "guid", g.ToString("D", CultureInfo.InvariantCulture));
                    return;
                case byte[] bytes:
                    writer.WriteStartObject();
                    writer.WriteString("$t", "binary");
                    writer.WriteBase64String("v", bytes);
                    writer.WriteEndObject();
                    return;

                // A pre-built wire node (FakeDriver edge values) passes through.
                case JsonNode node:
                    node.WriteTo(writer);
                    return;

                // Provider-specific fallback: stable invariant string.
                default:
                    WriteWrapper(
                        writer,
                        "provider",
                        Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty);
                    return;
            }
        }

        private static void WriteWrapper(Utf8JsonWriter writer, string type, string value)
        {
            writer.WriteStartObject();
            writer.WriteString("$t", type);
            writer.WriteString("v", value);
            writer.WriteEndObject();
        }

        /// <summary>Honest vector sentinel (D-0019): stable reason, facts only when determinable.</summary>
        private static void WriteVectorUnavailable(
            Utf8JsonWriter writer,
            Abstractions.DriverVectorUnavailableValue value)
        {
            writer.WriteStartObject();
            writer.WriteString("$t", "vector");
            writer.WriteNumber("version", 1);
            writer.WriteString("status", "unavailable");
            writer.WriteString("reason", value.Reason);
            if (value.Dimensions is int dimensions)
            {
                writer.WriteNumber("dimensions", dimensions);
            }
            if (value.BaseType is string baseType)
            {
                writer.WriteString("baseType", baseType);
            }
            writer.WriteEndObject();
        }

        private static void WriteSpatialUnavailable(
            Utf8JsonWriter writer,
            Abstractions.DriverSpatialUnavailableValue value)
        {
            writer.WriteStartObject();
            writer.WriteString("$t", "spatial");
            writer.WriteNumber("version", 1);
            writer.WriteString("status", "unrenderable");
            writer.WriteString("kind", value.Kind);
            writer.WriteString("reason", value.Reason);
            if (value.Srid is int srid)
            {
                writer.WriteNumber("srid", srid);
            }
            if (value.SourceBytes is long sourceBytes)
            {
                writer.WriteNumber("sourceBytes", sourceBytes);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// The truncation-honesty wrapper (SPEC §7.7): <c>bytes</c> and <c>digest</c> describe
        /// the full value so the client can detect truncation and identify the original.
        /// </summary>
        private static void WriteTruncated(
            Utf8JsonWriter writer,
            string of,
            long bytes,
            string digest,
            ReadOnlySpan<char> prefix)
        {
            writer.WriteStartObject();
            writer.WriteString("$t", "truncated");
            writer.WriteString("of", of);
            writer.WriteNumber("bytes", bytes);
            writer.WriteString("digest", digest);
            writer.WriteString("v", prefix);
            writer.WriteEndObject();
        }

        /// <summary>Retained prefix bytes: the effective bound capped by <c>sts2.results.truncatedPrefixBytes</c>.</summary>
        private static int PrefixLength(int maxCellBytes) => Math.Min(maxCellBytes, Sts2Defaults.TruncatedPrefixBytes);

        /// <summary>Wraps an oversized string; the prefix never splits a UTF-8 code point.</summary>
        private static void WriteTruncatedString(
            Utf8JsonWriter writer,
            string value,
            int maxCellBytes)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            int len = PrefixLength(maxCellBytes); // < bytes.Length: the caller only truncates oversized values
            while (len > 0 && (bytes[len] & 0xC0) == 0x80) // don't cut a UTF-8 continuation byte
            {
                len--;
            }
            WriteTruncated(
                writer,
                "string",
                bytes.Length,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                Encoding.UTF8.GetString(bytes, 0, len));
        }

        /// <summary>Wraps an oversized binary value; the prefix bound applies to the raw bytes (pre-base64).</summary>
        private static void WriteTruncatedBinary(
            Utf8JsonWriter writer,
            byte[] bytes,
            int maxCellBytes) =>
            WriteTruncated(
                writer,
                "binary",
                bytes.Length,
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                Convert.ToBase64String(bytes.AsSpan(0, PrefixLength(maxCellBytes))));

        private static void WriteNonFinite(Utf8JsonWriter writer, double value) => WriteWrapper(
            writer,
            "double",
            double.IsNaN(value) ? "NaN" : double.IsPositiveInfinity(value) ? "Infinity" : "-Infinity");
    }
}
