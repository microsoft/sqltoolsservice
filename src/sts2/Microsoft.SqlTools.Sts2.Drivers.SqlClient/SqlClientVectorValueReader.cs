//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.SqlTools.Sts2.Abstractions;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>
    /// Typed vector cell reader (D-0019): converts the provider's
    /// <see cref="SqlVector{T}"/> into the provider-neutral
    /// <see cref="DriverVectorValue"/> — explicit little-endian IEEE 754
    /// component bytes, deterministic across platforms (never
    /// MemoryMarshal without an endianness contract). Anything that is not a
    /// float32 vector degrades honestly: a string passes through as the JSON
    /// text representation; other provider values become
    /// <see cref="DriverVectorUnavailableValue"/> sentinels — never a partial
    /// vector, never a provider CLR type past the driver boundary.
    /// </summary>
    internal static class SqlClientVectorValueReader
    {
        /// <summary>Reads one non-null vector cell under SequentialAccess.</summary>
        internal static object Read(SqlDataReader reader, int ordinal, int maxCellBytes)
        {
            object value;
            try
            {
                value = reader.GetValue(ordinal);
            }
            catch (InvalidOperationException)
            {
                return new DriverVectorUnavailableValue { Reason = "decodeFailed" };
            }
            catch (NotSupportedException)
            {
                return new DriverVectorUnavailableValue { Reason = "decodeFailed" };
            }

            if (value is SqlVector<float> vector)
            {
                if (vector.IsNull)
                {
                    return DBNull.Value; // IsDBNull is checked first; defensive
                }
                ReadOnlySpan<float> components = vector.Memory.Span;
                if (components.Length != vector.Length)
                {
                    return new DriverVectorUnavailableValue
                    {
                        Dimensions = vector.Length,
                        BaseType = "float32",
                        Reason = "providerValueMismatch",
                    };
                }
                long byteLength = (long)components.Length * 4;
                if (maxCellBytes > 0 && byteLength > maxCellBytes)
                {
                    // Vectors are never truncated: complete or unavailable. The
                    // engine's 1998-dimension maximum (7992 bytes) sits far below
                    // the pinned 1 MiB bound; this is reachable only when a
                    // client lowered options.maxCellBytes.
                    return new DriverVectorUnavailableValue
                    {
                        Dimensions = components.Length,
                        BaseType = "float32",
                        Reason = "cellLimit",
                    };
                }
                byte[] bytes = new byte[byteLength];
                for (int i = 0; i < components.Length; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        bytes.AsSpan(i * 4, 4),
                        BitConverter.SingleToInt32Bits(components[i]));
                }
                return new DriverVectorValue
                {
                    Dimensions = components.Length,
                    BaseType = "float32",
                    Encoding = "f32le",
                    ComponentBytes = bytes,
                };
            }

            // Not a float32 vector (for example a preview float16 base type whose
            // provider representation differs): the JSON text representation is an
            // honest fallback when the provider hands one over; anything else is a
            // typed sentinel — v1 does not pretend to transport it.
            return value switch
            {
                string text => text,
                _ => new DriverVectorUnavailableValue { Reason = "unsupportedBaseType" },
            };
        }
    }
}
