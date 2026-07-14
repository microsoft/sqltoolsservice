//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Runtime.Journaling
{
    /// <summary>
    /// Canonical JSON for envelope digests (SPEC §8.2): UTF-8, object keys sorted by
    /// ordinal comparison at every depth, no insignificant whitespace, one escaping
    /// form for strings, number tokens preserved verbatim (wire-faithful, D-0007).
    /// FROZEN: changing canonicalization invalidates every existing journal digest;
    /// that is a SPEC-CHANGE, never a code cleanup.
    /// </summary>
    public static class CanonicalJson
    {
        internal readonly record struct DigestResult(string Digest, long Bytes);

        /// <summary>Returns the canonical UTF-8 bytes of <paramref name="element"/>.</summary>
        public static byte[] Canonicalize(JsonElement element)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false, SkipValidation = false }))
            {
                WriteCanonical(writer, element);
            }
            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>Returns <c>sha256:&lt;lowercase hex&gt;</c> of the canonical form.</summary>
        public static string DigestOf(JsonElement element) => DigestAndMeasure(element).Digest;

        /// <summary>
        /// Canonicalizes directly into an incremental SHA-256 sink. Envelope/capture hot
        /// paths need the digest and byte count, not an owned duplicate of the JSON bytes.
        /// </summary>
        internal static DigestResult DigestAndMeasure(JsonElement element)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var sink = new HashBufferWriter(hash);
            using (var writer = new Utf8JsonWriter(
                       sink,
                       new JsonWriterOptions { Indented = false, SkipValidation = false }))
            {
                WriteCanonical(writer, element);
                writer.Flush();
            }

            Span<byte> digest = stackalloc byte[32];
            if (!hash.TryGetHashAndReset(digest, out int bytesWritten) || bytesWritten != digest.Length)
            {
                throw new CryptographicException("Unable to finalize canonical JSON digest.");
            }
            return new DigestResult(
                "sha256:" + Convert.ToHexStringLower(digest),
                sink.BytesWritten);
        }

        /// <summary>Digests bytes that are already canonical.</summary>
        public static string DigestOfCanonicalBytes(ReadOnlySpan<byte> canonicalUtf8)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(canonicalUtf8, hash);
            return "sha256:" + Convert.ToHexStringLower(hash);
        }

        private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (JsonProperty property in element.EnumerateObject()
                                 .OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(property.Name);
                        WriteCanonical(writer, property.Value);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        WriteCanonical(writer, item);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    // Utf8JsonWriter's default escaper is deterministic, normalizing all
                    // wire escapings of the same string to one canonical form.
                    writer.WriteStringValue(element.GetString());
                    break;

                case JsonValueKind.Number:
                    // Verbatim token: parsing to double/decimal would silently change
                    // precision and break wire-faithfulness (D-0007).
                    writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;

                default:
                    throw new InvalidDataException($"Cannot canonicalize JsonValueKind.{element.ValueKind}.");
            }
        }

        /// <summary>
        /// Reuses one pooled scratch buffer and hashes every committed writer segment.
        /// No canonical payload-sized byte array survives the call.
        /// </summary>
        private sealed class HashBufferWriter : IBufferWriter<byte>, IDisposable
        {
            private readonly IncrementalHash hash;
            private byte[] buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);

            internal HashBufferWriter(IncrementalHash hash)
            {
                this.hash = hash;
            }

            internal long BytesWritten { get; private set; }

            public void Advance(int count)
            {
                if ((uint)count > (uint)buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (count == 0)
                {
                    return;
                }
                hash.AppendData(buffer.AsSpan(0, count));
                BytesWritten += count;
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                EnsureCapacity(sizeHint);
                return buffer;
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                EnsureCapacity(sizeHint);
                return buffer;
            }

            public void Dispose()
            {
                byte[] returned = buffer;
                buffer = [];
                ArrayPool<byte>.Shared.Return(returned);
            }

            private void EnsureCapacity(int sizeHint)
            {
                if (sizeHint <= buffer.Length)
                {
                    return;
                }
                byte[] replacement = ArrayPool<byte>.Shared.Rent(sizeHint);
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = replacement;
            }
        }
    }
}
