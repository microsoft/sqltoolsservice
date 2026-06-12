//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Runtime.Envelopes
{
    /// <summary>
    /// Deterministic single-line JSON codec for envelopes. Field order matches the SPEC
    /// §8.1 example; nullable fields are written explicitly as <c>null</c> so journal
    /// lines have a stable shape.
    /// </summary>
    public static class EnvelopeJsonCodec
    {
        /// <summary>The only envelope schema this codec accepts.</summary>
        public const string SchemaId = "sts2.envelope/1";

        /// <summary>Serializes one envelope to a single JSON line (no trailing newline).</summary>
        public static string SerializeLine(Sts2Envelope envelope)
        {
            ArgumentNullException.ThrowIfNull(envelope);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("schema", envelope.Schema);
                writer.WriteString("runId", envelope.RunId);
                writer.WriteNumber("seq", envelope.Seq);
                // UTC "Z" form per the SPEC §8.1 example (also avoids JSON-escaping of '+').
                writer.WriteString("ts", envelope.Ts.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteString("kind", envelope.Kind);
                WriteNullableString(writer, "sessionId", envelope.SessionId);
                WriteNullableString(writer, "corr", envelope.Corr);
                if (envelope.Cause is long cause)
                {
                    writer.WriteNumber("cause", cause);
                }
                else
                {
                    writer.WriteNull("cause");
                }
                writer.WriteString("type", envelope.Type);
                writer.WriteNumber("configVersion", envelope.ConfigVersion);
                writer.WriteString("digest", envelope.Digest);
                WriteNullableElement(writer, "payload", envelope.Payload);
                WriteNullableElement(writer, "payloadMeta", envelope.PayloadMeta);
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        /// <summary>Parses one journal line back into an envelope.</summary>
        public static Sts2Envelope DeserializeLine(string line)
        {
            ArgumentException.ThrowIfNullOrEmpty(line);
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            string schema = GetRequiredString(root, "schema");
            if (schema != SchemaId)
            {
                throw new InvalidDataException($"Unknown envelope schema '{schema}'; this codec reads '{SchemaId}'.");
            }
            string kind = GetRequiredString(root, "kind");
            if (!EnvelopeKinds.IsValid(kind))
            {
                throw new InvalidDataException($"Unknown envelope kind '{kind}'.");
            }

            return new Sts2Envelope
            {
                Schema = schema,
                RunId = GetRequiredString(root, "runId"),
                Seq = root.GetProperty("seq").GetInt64(),
                Ts = DateTimeOffset.Parse(GetRequiredString(root, "ts"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Kind = kind,
                SessionId = GetOptionalString(root, "sessionId"),
                Corr = GetOptionalString(root, "corr"),
                Cause = root.TryGetProperty("cause", out JsonElement causeElement) && causeElement.ValueKind == JsonValueKind.Number
                    ? causeElement.GetInt64()
                    : null,
                Type = GetRequiredString(root, "type"),
                ConfigVersion = root.GetProperty("configVersion").GetInt32(),
                Digest = GetRequiredString(root, "digest"),
                Payload = CloneOptionalElement(root, "payload"),
                PayloadMeta = CloneOptionalElement(root, "payloadMeta"),
            };
        }

        private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
        {
            if (value is null)
            {
                writer.WriteNull(name);
            }
            else
            {
                writer.WriteString(name, value);
            }
        }

        private static void WriteNullableElement(Utf8JsonWriter writer, string name, JsonElement? element)
        {
            if (element is null)
            {
                writer.WriteNull(name);
            }
            else
            {
                writer.WritePropertyName(name);
                element.Value.WriteTo(writer);
            }
        }

        private static string GetRequiredString(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()!
                : throw new InvalidDataException($"Envelope line is missing required string field '{name}'.");

        private static string? GetOptionalString(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static JsonElement? CloneOptionalElement(JsonElement root, string name) =>
            root.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null
                ? value.Clone()
                : null;
    }
}
