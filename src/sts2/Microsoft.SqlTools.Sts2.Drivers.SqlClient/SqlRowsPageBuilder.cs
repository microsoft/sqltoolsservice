//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>
    /// Byte-aware page accumulation (QO-3, SPEC §7.5 options.pageBytes): a page
    /// completes when EITHER the row-count or the approximate-byte limit is
    /// reached, whichever comes first. A single row larger than the byte limit
    /// becomes its own one-row page — pages are never empty and a giant row is
    /// never silently dropped (per-cell bounding is the encoder's maxCellBytes
    /// job, SPEC §7.7). Byte accounting is a cheap wire-size approximation; the
    /// exact encoded size is measured at serialization (sts2.query.stats).
    /// </summary>
    public sealed class SqlRowsPageBuilder
    {
        private const long JavaScriptMaxSafeInteger = 9_007_199_254_740_991L;

        private readonly int pageRows;
        private readonly long pageBytes;
        private List<IReadOnlyList<object?>> rows;
        private long approxBytes;

        public SqlRowsPageBuilder(int pageRows, long pageBytes)
        {
            this.pageRows = Math.Max(1, pageRows);
            this.pageBytes = Math.Max(1, pageBytes);
            rows = new List<IReadOnlyList<object?>>(this.pageRows);
        }

        /// <summary>
        /// Adds one row; yields each page completed by the addition (at most two:
        /// a byte-limit pre-close of the current page, then the row's own page
        /// when the row alone reaches a limit).
        /// </summary>
        public IEnumerable<IReadOnlyList<IReadOnlyList<object?>>> Add(IReadOnlyList<object?> cells)
        {
            long rowBytes = EstimateRowBytes(cells);
            if (rows.Count > 0 && approxBytes + rowBytes > pageBytes)
            {
                yield return Take();
            }
            rows.Add(cells);
            approxBytes += rowBytes;
            if (rows.Count >= pageRows || approxBytes >= pageBytes)
            {
                yield return Take();
            }
        }

        /// <summary>The trailing partial page, or null when nothing is pending.</summary>
        public IReadOnlyList<IReadOnlyList<object?>>? Flush() => rows.Count > 0 ? Take() : null;

        private List<IReadOnlyList<object?>> Take()
        {
            List<IReadOnlyList<object?>> completed = rows;
            rows = new List<IReadOnlyList<object?>>(pageRows);
            approxBytes = 0;
            return completed;
        }

        private static long EstimateRowBytes(IReadOnlyList<object?> cells)
        {
            long total = 2; // row array brackets
            for (int i = 0; i < cells.Count; i++)
            {
                total += 1 + EstimateCellBytes(cells[i]);
            }
            return total;
        }

        /// <summary>
        /// Cheap per-cell wire-size approximation — no allocation, no encoding
        /// pass. Strings count their UTF-8 bytes plus JSON escaping; binary
        /// counts base64 expansion. Typed vector cells and
        /// driver-truncated values estimate their real encoded size (D-0019) —
        /// the generic 24-byte fallback would under-count a 1,536-dimension
        /// vector (~8.3 KB encoded) by ~340x and defeat the page byte bound.
        /// </summary>
        public static long EstimateCellBytes(object? cell) => cell switch
        {
            null => 4,
            string s => EstimateJsonStringBytes(s),
            // Runtime wire shape is {"$t":"binary","v":"..."}; include
            // conservative wrapper overhead in addition to exact base64 size.
            byte[] b => EstimateBase64Bytes(b.Length) + 32,
            bool => 5,
            Guid => 64,
            DateTime or DateTimeOffset => 80,
            TimeSpan => 64,
            decimal => 80,
            double or float => 40,
            // Unsafe Int64 values use {"$t":"int64","v":"..."}; price the
            // wrapper as well as the 20-digit decimal payload.
            long value when value is > JavaScriptMaxSafeInteger or < -JavaScriptMaxSafeInteger => 64,
            long => 20,
            int or short or byte => 12,
            char[] c => EstimateJsonStringBytes(c),
            // base64 of the component bytes + the fixed tag fields
            Abstractions.DriverVectorValue v => EstimateBase64Bytes(v.ComponentBytes.Length)
                + EstimateJsonStringBytes(v.BaseType) + EstimateJsonStringBytes(v.Encoding) + 128,
            Abstractions.DriverVectorUnavailableValue v => EstimateVectorUnavailableBytes(v),
            // base64 WKB + typed spatial tag fields (D-0020)
            Abstractions.DriverSpatialValue s => EstimateBase64Bytes(s.Wkb.Length)
                + EstimateJsonStringBytes(s.Kind) + 128,
            Abstractions.DriverSpatialUnavailableValue s => EstimateSpatialUnavailableBytes(s),
            // retained prefix (text verbatim, binary as base64) + every wrapper
            // fact, including the byte count and sha256 digest.
            Abstractions.DriverTruncatedValue t => EstimateTruncatedBytes(t),
            _ => 24,
        };

        private static long EstimateVectorUnavailableBytes(Abstractions.DriverVectorUnavailableValue value) =>
            128
            + EstimateJsonStringBytes(value.Reason)
            + (value.BaseType is null ? 0 : EstimateJsonStringBytes(value.BaseType))
            + (value.Dimensions is null ? 0 : 20);

        private static long EstimateSpatialUnavailableBytes(Abstractions.DriverSpatialUnavailableValue value) =>
            160
            + EstimateJsonStringBytes(value.Kind)
            + EstimateJsonStringBytes(value.Reason)
            + (value.Srid is null ? 0 : 20)
            + (value.SourceBytes is null ? 0 : 20);

        private static long EstimateTruncatedBytes(Abstractions.DriverTruncatedValue value) =>
            160 // object/property punctuation and conservative growth room
            + (value.Kind == "binary"
                ? EstimateBase64Bytes(value.PrefixBytes?.Length ?? 0) + 2
                : EstimateJsonStringBytes(value.PrefixText ?? string.Empty))
            + EstimateJsonStringBytes(value.Kind)
            + EstimateJsonStringBytes(value.DigestHex) + 7 // "sha256:" prefix
            + 20; // signed Int64 decimal byte count

        private static long EstimateBase64Bytes(int byteLength) =>
            ((long)byteLength + 2) / 3 * 4;

        private static long EstimateJsonStringBytes(ReadOnlySpan<char> value)
        {
            long total = 2; // JSON quotes
            foreach (char c in value)
            {
                // Match System.Text.Json's default encoder. Allowed Basic Latin
                // is one byte; every other UTF-16 code unit is safely bounded by
                // one six-byte \\uXXXX escape (astral scalars use two units).
                total += c < 128 && !JavaScriptEncoder.Default.WillEncode(c) ? 1 : 6;
            }
            return total;
        }
    }
}
