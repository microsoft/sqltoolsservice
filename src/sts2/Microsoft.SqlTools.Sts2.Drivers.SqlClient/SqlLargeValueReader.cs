//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>
    /// Streaming readers for MAX-typed columns under SequentialAccess (QO-4):
    /// values within the cell bound come back as ordinary CLR values (encoder
    /// path unchanged); oversized values come back as
    /// <see cref="DriverTruncatedValue"/> — a bounded prefix plus the FULL
    /// value's byte count and sha256, both computed while streaming fixed-size
    /// chunks. Peak memory per cell is prefix + one chunk, never the value.
    /// </summary>
    internal static class SqlLargeValueReader
    {
        internal enum CellRead
        {
            Value,
            Text,
            Binary,
        }

        private const int ChunkChars = 8192;
        private const int ChunkBytes = 32768;

        /// <summary>MAX-typed (or unbounded) columns stream; everything else keeps GetValue.</summary>
        internal static CellRead[] ClassifyColumns(IReadOnlyList<ColumnInfo> columns)
        {
            var kinds = new CellRead[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                string type = columns[i].EngineType.ToLowerInvariant();
                bool unbounded = columns[i].Length is null or <= 0 or >= 1_073_741_823;
                kinds[i] = type switch
                {
                    "xml" => CellRead.Text,
                    "text" or "ntext" => CellRead.Text,
                    "varchar" or "nvarchar" or "json" when unbounded => CellRead.Text,
                    "image" => CellRead.Binary,
                    "varbinary" when unbounded => CellRead.Binary,
                    _ => CellRead.Value,
                };
            }
            return kinds;
        }

        /// <summary>Streams a character column: full string when within bound, else prefix + facts.</summary>
        internal static object ReadText(SqlDataReader reader, int ordinal, int maxCellBytes)
        {
            var prefix = new StringBuilder();
            long totalBytes = 0;
            bool capped = false;
            char[] chars = new char[ChunkChars];
            // Encoder carries state across chunks so a surrogate pair split at
            // a chunk boundary still hashes/counts the true UTF-8 bytes.
            Encoder utf8 = Encoding.UTF8.GetEncoder();
            byte[] byteScratch = new byte[Encoding.UTF8.GetMaxByteCount(ChunkChars)];
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long fieldOffset = 0;
            while (true)
            {
                long read = reader.GetChars(ordinal, fieldOffset, chars, 0, chars.Length);
                if (read <= 0)
                {
                    break;
                }
                fieldOffset += read;
                int byteCount = utf8.GetBytes(chars, 0, (int)read, byteScratch, 0, flush: false);
                totalBytes += byteCount;
                sha.AppendData(byteScratch, 0, byteCount);
                if (!capped)
                {
                    prefix.Append(chars, 0, (int)read);
                    // Slightly over-retain (≤ one chunk); the encoder re-caps
                    // the prefix UTF-8-boundary-safe at the effective bound.
                    capped = totalBytes > maxCellBytes;
                }
                if (read < chars.Length)
                {
                    break;
                }
            }
            int tail = utf8.GetBytes(chars, 0, 0, byteScratch, 0, flush: true);
            if (tail > 0)
            {
                totalBytes += tail;
                sha.AppendData(byteScratch, 0, tail);
            }
            if (!capped)
            {
                return prefix.ToString(); // fits: ordinary string, encoder path unchanged
            }
            return new DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = prefix.ToString(),
                TotalBytes = totalBytes,
                DigestHex = Convert.ToHexStringLower(sha.GetHashAndReset()),
            };
        }

        /// <summary>Streams a binary column: full bytes when within bound, else prefix + facts.</summary>
        internal static object ReadBinary(SqlDataReader reader, int ordinal, int maxCellBytes)
        {
            byte[] chunk = new byte[ChunkBytes];
            var prefix = new List<byte>(Math.Min(maxCellBytes, ChunkBytes));
            long totalBytes = 0;
            bool capped = false;
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long fieldOffset = 0;
            while (true)
            {
                long read = reader.GetBytes(ordinal, fieldOffset, chunk, 0, chunk.Length);
                if (read <= 0)
                {
                    break;
                }
                fieldOffset += read;
                totalBytes += read;
                sha.AppendData(chunk, 0, (int)read);
                if (!capped)
                {
                    int keep = (int)Math.Min(read, Math.Max(0, maxCellBytes - prefix.Count));
                    for (int i = 0; i < keep; i++)
                    {
                        prefix.Add(chunk[i]);
                    }
                    capped = totalBytes >= maxCellBytes && prefix.Count >= maxCellBytes;
                }
                if (read < chunk.Length)
                {
                    break;
                }
            }
            if (totalBytes <= maxCellBytes)
            {
                return prefix.ToArray(); // fits: ordinary byte[], encoder path unchanged
            }
            return new DriverTruncatedValue
            {
                Kind = "binary",
                PrefixBytes = prefix.ToArray(),
                TotalBytes = totalBytes,
                DigestHex = Convert.ToHexStringLower(sha.GetHashAndReset()),
            };
        }
    }
}
