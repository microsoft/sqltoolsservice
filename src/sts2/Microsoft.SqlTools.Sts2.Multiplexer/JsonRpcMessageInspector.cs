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
        private const string QueryResultSetMethod = "v2/query.resultSet";
        private const string QueryRowsMethod = "v2/query.rows";
        private const string QueryMessageMethod = "v2/query.message";
        private const string QueryCompleteMethod = "v2/query.complete";
        private const string FatalMethod = "v2/fatal";

        internal static JsonRpcMessageInfo Inspect(ReadOnlySpan<byte> payload) =>
            InspectCore(payload, trustKnownServerNotifications: false);

        /// <summary>
        /// Inspects output from the trusted STS2/legacy hosts. StreamJsonRpc writes the
        /// method before the potentially large params object. Contract-defined server
        /// notifications never carry an id, so they can be classified without walking
        /// every nested result cell. Requests that present an id before their method keep
        /// the ordinary rewrite path.
        /// </summary>
        internal static JsonRpcMessageInfo InspectOutbound(ReadOnlySpan<byte> payload) =>
            InspectCore(payload, trustKnownServerNotifications: true);

        private static JsonRpcMessageInfo InspectCore(ReadOnlySpan<byte> payload, bool trustKnownServerNotifications)
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
                            if (trustKnownServerNotifications && TryGetKnownServerNotification(ref reader, out string? knownMethod))
                            {
                                return new JsonRpcMessageInfo(ParseFailed: false, knownMethod, hasId, idIsNull, idRawJson);
                            }
                            method = reader.GetString();
                            if (trustKnownServerNotifications && hasId)
                            {
                                // A server request has all routing facts once both id and
                                // method have been seen; params can remain unvisited.
                                return new JsonRpcMessageInfo(ParseFailed: false, method, hasId, idIsNull, idRawJson);
                            }
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

        private static bool TryGetKnownServerNotification(ref Utf8JsonReader reader, out string? method)
        {
            if (reader.ValueTextEquals("v2/query.rows"u8))
            {
                method = QueryRowsMethod;
                return true;
            }
            if (reader.ValueTextEquals("v2/query.resultSet"u8))
            {
                method = QueryResultSetMethod;
                return true;
            }
            if (reader.ValueTextEquals("v2/query.message"u8))
            {
                method = QueryMessageMethod;
                return true;
            }
            if (reader.ValueTextEquals("v2/query.complete"u8))
            {
                method = QueryCompleteMethod;
                return true;
            }
            if (reader.ValueTextEquals("v2/fatal"u8))
            {
                method = FatalMethod;
                return true;
            }

            method = null;
            return false;
        }
    }
}
