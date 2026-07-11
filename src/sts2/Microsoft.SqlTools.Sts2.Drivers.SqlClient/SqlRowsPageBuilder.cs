//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

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
        /// pass. Strings count UTF-16 char length (exact for ASCII, low for
        /// multibyte); binary counts base64 expansion. Typed vector cells and
        /// driver-truncated values estimate their real encoded size (D-0019) —
        /// the generic 24-byte fallback would under-count a 1,536-dimension
        /// vector (~8.3 KB encoded) by ~340x and defeat the page byte bound.
        /// </summary>
        public static long EstimateCellBytes(object? cell) => cell switch
        {
            null => 4,
            string s => s.Length + 2,
            byte[] b => ((long)b.Length * 4 + 2) / 3 + 2,
            bool => 5,
            Guid => 38,
            DateTime or DateTimeOffset => 36,
            TimeSpan => 20,
            decimal or double or float or long or int or short or byte => 20,
            char[] c => c.Length + 2,
            // base64 of the component bytes + the fixed tag fields
            Abstractions.DriverVectorValue v => ((long)v.ComponentBytes.Length * 4 + 2) / 3 + 128,
            // retained prefix (text verbatim, binary as base64) + wrapper facts
            Abstractions.DriverTruncatedValue t => t.Kind == "binary"
                ? ((long)(t.PrefixBytes?.Length ?? 0) * 4 + 2) / 3 + 128
                : (t.PrefixText?.Length ?? 0) + 128,
            _ => 24,
        };
    }
}
