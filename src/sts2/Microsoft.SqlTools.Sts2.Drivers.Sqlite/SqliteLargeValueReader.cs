//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;
using SQLitePCL;

namespace Microsoft.SqlTools.Sts2.Drivers.Sqlite
{
    /// <summary>
    /// Bounded managed reads for SQLite TEXT/BLOB cells. SQLite owns the complete value in
    /// native statement memory; the adapter copies only a fitting value or the wire prefix.
    /// </summary>
    internal static class SqliteLargeValueReader
    {
        private const int HashChunkBytes = 32768;

        internal static object ReadText(
            sqlite3_stmt statement,
            int ordinal,
            int maxCellBytes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // For a TEXT storage-class value, sqlite3_column_blob exposes the same UTF-8
            // bytes as a borrowed native span. Unlike Microsoft.Data.Sqlite.GetString or
            // GetTextReader, this does not allocate the complete managed value first.
            ReadOnlySpan<byte> utf8 = raw.sqlite3_column_blob(statement, ordinal);
            if (maxCellBytes <= 0 || utf8.Length <= maxCellBytes)
            {
                return Encoding.UTF8.GetString(utf8);
            }

            int prefixBudget = Math.Min(maxCellBytes, Sts2Defaults.TruncatedPrefixBytes);
            int prefixLength = CompleteUtf8PrefixLength(utf8, prefixBudget);
            return new DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = Encoding.UTF8.GetString(utf8[..prefixLength]),
                TotalBytes = utf8.Length,
                DigestHex = Hash(utf8, cancellationToken),
            };
        }

        internal static object ReadBinary(
            sqlite3_stmt statement,
            int ordinal,
            int maxCellBytes,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadOnlySpan<byte> bytes = raw.sqlite3_column_blob(statement, ordinal);
            if (maxCellBytes <= 0 || bytes.Length <= maxCellBytes)
            {
                return bytes.ToArray();
            }

            int prefixLength = Math.Min(
                bytes.Length,
                Math.Min(maxCellBytes, Sts2Defaults.TruncatedPrefixBytes));
            return new DriverTruncatedValue
            {
                Kind = "binary",
                PrefixBytes = bytes[..prefixLength].ToArray(),
                TotalBytes = bytes.Length,
                DigestHex = Hash(bytes, cancellationToken),
            };
        }

        /// <summary>Largest prefix that does not split a UTF-8 continuation sequence.</summary>
        internal static int CompleteUtf8PrefixLength(ReadOnlySpan<byte> utf8, int maxBytes)
        {
            int length = Math.Min(utf8.Length, Math.Max(0, maxBytes));
            if (length == utf8.Length)
            {
                return length;
            }

            // If the first excluded byte is a continuation byte, the included suffix is
            // only part of its scalar. Rewind through continuation bytes and its lead byte.
            while (length > 0 && (utf8[length] & 0xC0) == 0x80)
            {
                length--;
            }
            return length;
        }

        private static string Hash(ReadOnlySpan<byte> value, CancellationToken cancellationToken)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (int offset = 0; offset < value.Length; offset += HashChunkBytes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                hash.AppendData(value.Slice(offset, Math.Min(HashChunkBytes, value.Length - offset)));
            }
            cancellationToken.ThrowIfCancellationRequested();
            return Convert.ToHexStringLower(hash.GetHashAndReset());
        }
    }
}
