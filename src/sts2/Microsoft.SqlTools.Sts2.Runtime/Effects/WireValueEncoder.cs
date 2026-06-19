//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

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
        /// Encodes one cell, truncating oversized string/binary values to at most
        /// <paramref name="maxCellBytes"/> bytes (SPEC §7.7 maxCellBytes, R024) so a single
        /// pathological cell cannot blow the memory/frame bound. Truncation is deterministic
        /// (UTF-8 boundary-safe), so replay reproduces the same digest. A truncated cell
        /// becomes <c>{"$t":"truncated","of":"string|binary","bytes":N,"v":&lt;prefix&gt;}</c>.
        /// </summary>
        public static JsonNode? Encode(object? cell, int maxCellBytes)
        {
            if (maxCellBytes > 0)
            {
                switch (cell)
                {
                    case string str when Encoding.UTF8.GetByteCount(str) > maxCellBytes:
                        return Truncated("string", Encoding.UTF8.GetByteCount(str), TruncateUtf8(str, maxCellBytes));
                    case byte[] bytes when bytes.Length > maxCellBytes:
                        return Truncated("binary", bytes.Length, Convert.ToBase64String(bytes.AsSpan(0, maxCellBytes)));
                }
            }
            return Encode(cell);
        }

        /// <summary>Encodes one cell. Null maps to JSON null.</summary>
        public static JsonNode? Encode(object? cell) => cell switch
        {
            null or DBNull => null,

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

        private static JsonObject Truncated(string of, int originalBytes, string prefix) => new()
        {
            ["$t"] = "truncated",
            ["of"] = of,
            ["bytes"] = originalBytes,
            ["v"] = prefix,
        };

        /// <summary>Truncates a string to at most <paramref name="maxBytes"/> UTF-8 bytes on a char boundary.</summary>
        private static string TruncateUtf8(string value, int maxBytes)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= maxBytes)
            {
                return value;
            }
            int len = maxBytes;
            while (len > 0 && (bytes[len] & 0xC0) == 0x80) // don't cut a UTF-8 continuation byte
            {
                len--;
            }
            return Encoding.UTF8.GetString(bytes, 0, len);
        }

        private static JsonObject NonFinite(double value) => Wrapper(
            "double",
            double.IsNaN(value) ? "NaN" : double.IsPositiveInfinity(value) ? "Infinity" : "-Infinity");
    }
}
