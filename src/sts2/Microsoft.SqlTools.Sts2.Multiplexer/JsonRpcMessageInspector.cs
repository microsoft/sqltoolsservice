//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    /// <summary>
    /// Top-level routing facts about a JSON-RPC payload. <see cref="IdRawJson"/> preserves
    /// the exact original textual representation of the id (SPEC §6.3).
    /// </summary>
    internal readonly record struct JsonRpcMessageInfo(
        bool ParseFailed,
        string? Method,
        bool HasId,
        bool IdIsNull,
        string? IdRawJson);

    /// <summary>
    /// Minimal top-level inspection with <see cref="Utf8JsonReader"/>. Never materializes
    /// nested payloads (SPEC §6.1: routing must not deserialize full payloads).
    /// </summary>
    internal static class JsonRpcMessageInspector
    {
        internal static JsonRpcMessageInfo Inspect(ReadOnlySpan<byte> payload)
        {
            string? method = null;
            bool hasId = false;
            bool idIsNull = false;
            string? idRawJson = null;

            try
            {
                var reader = new Utf8JsonReader(payload);
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    return new JsonRpcMessageInfo(ParseFailed: true, null, false, false, null);
                }

                while (reader.Read())
                {
                    if (reader.CurrentDepth != 1 || reader.TokenType != JsonTokenType.PropertyName)
                    {
                        continue;
                    }

                    if (reader.ValueTextEquals("method"u8))
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            method = reader.GetString();
                        }
                        else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                        {
                            reader.Skip();
                        }
                    }
                    else if (reader.ValueTextEquals("id"u8))
                    {
                        reader.Read();
                        hasId = true;
                        long start = reader.TokenStartIndex;
                        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                        {
                            reader.Skip();
                        }
                        idIsNull = reader.TokenType == JsonTokenType.Null;
                        idRawJson = Encoding.UTF8.GetString(payload[(int)start..(int)reader.BytesConsumed]);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return new JsonRpcMessageInfo(ParseFailed: false, method, hasId, idIsNull, idRawJson);
            }
            catch (JsonException)
            {
                return new JsonRpcMessageInfo(ParseFailed: true, null, false, false, null);
            }
        }
    }
}
