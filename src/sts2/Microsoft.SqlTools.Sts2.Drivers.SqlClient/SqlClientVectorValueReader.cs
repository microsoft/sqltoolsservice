//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers.Binary;
using System.Text.Json;
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
    /// MemoryMarshal without an endianness contract). The default path converts
    /// provider vectors to invariant JSON-array text. Anything that cannot meet
    /// the negotiated typed contract becomes a
    /// <see cref="DriverVectorUnavailableValue"/> sentinel — never a partial
    /// vector, never a provider CLR type past the driver boundary.
    /// </summary>
    internal static class SqlClientVectorValueReader
    {
        /// <summary>Reads one non-null vector cell under SequentialAccess.</summary>
        internal static object Read(SqlDataReader reader, int ordinal, int maxCellBytes)
            => ConvertTyped(ReadProviderValue(reader, ordinal), maxCellBytes);

        /// <summary>Reads the default, non-negotiated JSON-text representation.</summary>
        internal static object ReadText(SqlDataReader reader, int ordinal)
            => ConvertText(ReadProviderValue(reader, ordinal));

        private static object ReadProviderValue(SqlDataReader reader, int ordinal)
        {
            try
            {
                return reader.GetValue(ordinal);
            }
            catch (InvalidOperationException)
            {
                return new DriverVectorUnavailableValue { Reason = "decodeFailed" };
            }
            catch (NotSupportedException)
            {
                return new DriverVectorUnavailableValue { Reason = "decodeFailed" };
            }
        }

        internal static object ConvertTyped(object value, int maxCellBytes)
        {
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
                int bufferLength = checked((int)byteLength);
                byte[] bytes = new byte[bufferLength];
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

            // Once vectorBinary is negotiated, returning a plain string would
            // violate the complete-or-unavailable typed contract.
            return value is DriverVectorUnavailableValue unavailable
                ? unavailable
                : new DriverVectorUnavailableValue { Reason = "unsupportedBaseType" };
        }

        internal static object ConvertText(object value)
        {
            if (value is SqlVector<float> vector)
            {
                if (vector.IsNull)
                {
                    return DBNull.Value;
                }
                if (vector.Memory.Length != vector.Length)
                {
                    return new DriverVectorUnavailableValue
                    {
                        Dimensions = vector.Length,
                        BaseType = "float32",
                        Reason = "providerValueMismatch",
                    };
                }

                // System.Text.Json uses invariant, round-trippable float output,
                // producing the JSON array promised by the default vector contract.
                return JsonSerializer.Serialize(vector.Memory.ToArray());
            }

            return value switch
            {
                string text => text,
                DriverVectorUnavailableValue unavailable => unavailable,
                _ => new DriverVectorUnavailableValue { Reason = "unsupportedBaseType" },
            };
        }
    }
}
