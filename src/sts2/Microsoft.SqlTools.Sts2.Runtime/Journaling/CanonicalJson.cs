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
        public static string DigestOf(JsonElement element) => DigestOfCanonicalBytes(Canonicalize(element));

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
    }
}
