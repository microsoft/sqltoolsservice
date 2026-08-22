//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>
    /// Digest-capture elision (SPEC §8.2, §8.4): row cells and SQL text are replaced by
    /// authoritative-digest wrappers BEFORE journaling and digest computation, so live
    /// and replayed digests match exactly (I7 holds in digest mode). The original
    /// fragments live in an in-memory side table and are substituted back at the wire
    /// and effect-runner edges — like the secret side table, they never serialize.
    /// <para>
    /// Keying by content digest is safe because the coordinator pump is single-threaded:
    /// each fragment is Wrapped (added) and then Substituted (removed) within one input's
    /// processing, before the next input is Wrapped, so two equal fragments are never live
    /// in the table at once. The coordinator clears the table on dispose to bound any
    /// fragment that was journaled but whose output Core ultimately suppressed.
    /// </para>
    /// </summary>
    internal static class CaptureElision
    {
        /// <summary>
        /// Elides capture-sensitive fields of an input payload per the active capture modes
        /// (read from Core state, so a runtime setCapture takes effect on the next envelope).
        /// </summary>
        internal static JsonElement? ElideInput(
            string rowCapture,
            string sqlCapture,
            string kind,
            string type,
            JsonElement? payload,
            ConcurrentDictionary<string, JsonElement> sideTable,
            ICollection<string>? addedKeys = null)
        {
            if (payload is not { ValueKind: JsonValueKind.Object } p)
            {
                return payload;
            }

            bool elideRows = rowCapture == "digest"
                && kind == Envelopes.EnvelopeKinds.EffectResponse
                && type == "driver.queryEvent"
                && p.TryGetProperty("eventType", out JsonElement et)
                && et.ValueKind == JsonValueKind.String
                && et.GetString() == "rows";
            bool elideSql = sqlCapture == "digest"
                && kind == Envelopes.EnvelopeKinds.RpcInRequest
                && type == "v2/query.execute";

            if (!elideRows && !elideSql)
            {
                return payload;
            }

            // Rewrite only the top-level sensitive property into a compact wrapper.
            // JsonNode.Parse/GetRawText/ToJsonString previously created a second DOM and
            // multiple payload-sized UTF-16 strings for every rows page.
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                foreach (JsonProperty property in p.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    string? fieldKind = elideRows && (property.Name is "rows" or "compact")
                        ? property.Name
                        : elideSql && property.Name == "sql"
                            ? "sql"
                            : null;
                    if (fieldKind is null)
                    {
                        property.Value.WriteTo(writer);
                    }
                    else
                    {
                        WriteWrapper(writer, fieldKind, property.Value, sideTable, addedKeys);
                    }
                }
                writer.WriteEndObject();
                writer.Flush();
            }
            return JsonDocument.Parse(buffer.WrittenMemory).RootElement;
        }

        /// <summary>Substitutes elided wrappers back to their original fragments (wire/effect edges).</summary>
        internal static JsonElement? Substitute(JsonElement? payload, ConcurrentDictionary<string, JsonElement> sideTable)
        {
            if (payload is not { ValueKind: JsonValueKind.Object } p || sideTable.IsEmpty)
            {
                return null; // nothing to substitute
            }

            var buffer = new ArrayBufferWriter<byte>(EstimateSubstitutionCapacity(p));
            bool substituted;
            using (var writer = new Utf8JsonWriter(buffer))
            {
                substituted = WriteWithSubstitution(writer, p, sideTable);
                writer.Flush();
            }
            return substituted ? JsonDocument.Parse(buffer.WrittenMemory).RootElement : null;
        }

        /// <summary>
        /// Builds a small serializer-ready object graph while preserving captured large
        /// fragments as their original <see cref="JsonElement"/> values. The RPC formatter
        /// can then write the final params once, without a restored payload-sized buffer and
        /// <see cref="JsonDocument"/> parse. Returns null when no wrapper was substituted.
        /// </summary>
        internal static object? SubstituteParameterObject(
            JsonElement? payload,
            ConcurrentDictionary<string, JsonElement> sideTable)
        {
            if (payload is not { ValueKind: JsonValueKind.Object } element || sideTable.IsEmpty)
            {
                return null;
            }
            object? result = BuildParameterValue(element, sideTable, out bool substituted);
            return substituted ? result : null;
        }

        private static object? BuildParameterValue(
            JsonElement element,
            ConcurrentDictionary<string, JsonElement> sideTable,
            out bool substituted)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("$redacted", out JsonElement redacted)
                    && redacted.ValueKind == JsonValueKind.True
                    && element.TryGetProperty("digest", out JsonElement digestElement)
                    && digestElement.ValueKind == JsonValueKind.String
                    && sideTable.TryRemove(digestElement.GetString()!, out JsonElement original))
                {
                    substituted = true;
                    return original;
                }

                substituted = false;
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    result[property.Name] = BuildParameterValue(
                        property.Value,
                        sideTable,
                        out bool propertySubstituted);
                    substituted |= propertySubstituted;
                }
                return result;
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                substituted = false;
                var result = new List<object?>(element.GetArrayLength());
                foreach (JsonElement item in element.EnumerateArray())
                {
                    result.Add(BuildParameterValue(item, sideTable, out bool itemSubstituted));
                    substituted |= itemSubstituted;
                }
                return result;
            }
            substituted = false;
            return element;
        }

        private static int EstimateSubstitutionCapacity(JsonElement element)
        {
            long estimate = 4096 + ReplacementBytes(element);
            return (int)Math.Clamp(estimate, 4096, Sts2Defaults.MaxFrameBytes);
        }

        private static long ReplacementBytes(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("$redacted", out JsonElement redacted)
                    && redacted.ValueKind == JsonValueKind.True
                    && element.TryGetProperty("bytes", out JsonElement bytes)
                    && bytes.TryGetInt64(out long replacementBytes))
                {
                    return Math.Max(0, replacementBytes);
                }
                long total = 0;
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    total += ReplacementBytes(property.Value);
                }
                return total;
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                long total = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    total += ReplacementBytes(item);
                }
                return total;
            }
            return 0;
        }

        private static bool WriteWithSubstitution(
            Utf8JsonWriter writer,
            JsonElement element,
            ConcurrentDictionary<string, JsonElement> sideTable)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    if (element.TryGetProperty("$redacted", out JsonElement redacted)
                        && redacted.ValueKind == JsonValueKind.True
                        && element.TryGetProperty("digest", out JsonElement digestElement)
                        && digestElement.ValueKind == JsonValueKind.String
                        && sideTable.TryRemove(digestElement.GetString()!, out JsonElement original))
                    {
                        original.WriteTo(writer);
                        return true;
                    }

                    bool objectSubstituted = false;
                    writer.WriteStartObject();
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        objectSubstituted |= WriteWithSubstitution(writer, property.Value, sideTable);
                    }
                    writer.WriteEndObject();
                    return objectSubstituted;

                case JsonValueKind.Array:
                    bool arraySubstituted = false;
                    writer.WriteStartArray();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        arraySubstituted |= WriteWithSubstitution(writer, item, sideTable);
                    }
                    writer.WriteEndArray();
                    return arraySubstituted;

                default:
                    element.WriteTo(writer);
                    return false;
            }
        }

        private static void WriteWrapper(
            Utf8JsonWriter writer,
            string fieldKind,
            JsonElement original,
            ConcurrentDictionary<string, JsonElement> sideTable,
            ICollection<string>? addedKeys)
        {
            // Rows/compact fragments are emitted by DriverEffectRunner's default
            // Utf8JsonWriter, so their string tokens already use the frozen
            // canonical escaping. SQL is client input and must take the general
            // decode/normalize path.
            CanonicalJson.DigestResult canonical = fieldKind is "rows" or "compact"
                ? CanonicalJson.DigestAndMeasureWriterOutput(original)
                : CanonicalJson.DigestAndMeasure(original);
            sideTable[canonical.Digest] = original;
            addedKeys?.Add(canonical.Digest);

            writer.WriteStartObject();
            writer.WriteBoolean("$redacted", true);
            writer.WriteString("kind", fieldKind);
            writer.WriteString("digest", canonical.Digest);
            writer.WriteNumber("bytes", canonical.Bytes);
            if (fieldKind == "rows" && original.ValueKind == JsonValueKind.Array)
            {
                writer.WriteNumber("rows", original.GetArrayLength());
            }
            else if (fieldKind == "compact" && original.ValueKind == JsonValueKind.Object
                && original.TryGetProperty("values", out JsonElement values)
                && values.ValueKind == JsonValueKind.Array)
            {
                writer.WriteNumber("rows", values.GetArrayLength());
            }
            writer.WriteEndObject();
        }
    }
}
