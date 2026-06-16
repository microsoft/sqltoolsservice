//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>
    /// Digest-capture elision (SPEC §8.2, §8.4): row cells and SQL text are replaced by
    /// authoritative-digest wrappers BEFORE journaling and digest computation, so live
    /// and replayed digests match exactly (I7 holds in digest mode). The original
    /// fragments live in an in-memory side table and are substituted back at the wire
    /// and effect-runner edges — like the secret side table, they never serialize.
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
            ConcurrentDictionary<string, JsonElement> sideTable)
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

            JsonObject obj = JsonNode.Parse(p.GetRawText())!.AsObject();
            if (elideRows && obj["rows"] is JsonNode rows)
            {
                obj["rows"] = Wrap("rows", rows, sideTable);
            }
            if (elideSql && obj["sql"] is JsonNode sql)
            {
                obj["sql"] = Wrap("sql", sql, sideTable);
            }
            return JsonDocument.Parse(obj.ToJsonString()).RootElement;
        }

        /// <summary>Substitutes elided wrappers back to their original fragments (wire/effect edges).</summary>
        internal static JsonElement? Substitute(JsonElement? payload, ConcurrentDictionary<string, JsonElement> sideTable)
        {
            if (payload is not { ValueKind: JsonValueKind.Object } p || sideTable.IsEmpty
                || !p.GetRawText().Contains("\"$redacted\"", StringComparison.Ordinal))
            {
                return null; // nothing to substitute
            }

            JsonObject obj = JsonNode.Parse(p.GetRawText())!.AsObject();
            bool substituted = SubstituteNode(obj, sideTable);
            return substituted ? JsonDocument.Parse(obj.ToJsonString()).RootElement : null;
        }

        private static bool SubstituteNode(JsonObject obj, ConcurrentDictionary<string, JsonElement> sideTable)
        {
            bool any = false;
            foreach (string key in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(obj, kv => kv.Key)))
            {
                JsonNode? value = obj[key];
                if (value is JsonObject child)
                {
                    if (child["$redacted"] is JsonValue && child["digest"] is JsonValue digestValue
                        && sideTable.TryRemove(digestValue.GetValue<string>(), out JsonElement original))
                    {
                        obj[key] = JsonNode.Parse(original.GetRawText());
                        any = true;
                    }
                    else
                    {
                        any |= SubstituteNode(child, sideTable);
                    }
                }
            }
            return any;
        }

        private static JsonObject Wrap(string fieldKind, JsonNode original, ConcurrentDictionary<string, JsonElement> sideTable)
        {
            JsonElement element = JsonDocument.Parse(original.ToJsonString()).RootElement;
            byte[] canonical = CanonicalJson.Canonicalize(element);
            string digest = CanonicalJson.DigestOfCanonicalBytes(canonical);
            sideTable[digest] = element;

            var wrapper = new JsonObject
            {
                ["$redacted"] = true,
                ["kind"] = fieldKind,
                ["digest"] = digest,
                ["bytes"] = canonical.Length,
            };
            if (fieldKind == "rows" && element.ValueKind == JsonValueKind.Array)
            {
                wrapper["rows"] = element.GetArrayLength();
            }
            return wrapper;
        }
    }
}
