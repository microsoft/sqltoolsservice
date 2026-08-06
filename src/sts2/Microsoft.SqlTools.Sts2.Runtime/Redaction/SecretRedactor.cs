//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.SqlTools.Sts2.Runtime.Redaction
{
    /// <summary>
    /// Tokenizes credential material in RPC payloads BEFORE envelopes are created
    /// (SPEC §8.5): the journal, Core, and every downstream consumer only ever see
    /// SecretRef tokens. Connection-string password scanning is added with the driver
    /// adapters (M2+); at M1 redaction is key-based.
    /// </summary>
    public static class SecretRedactor
    {
        private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "accessToken", "token",
        };

        private static readonly HashSet<string> AuthClearKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "kind", "user",
        };

        /// <summary>
        /// Returns a copy of <paramref name="payload"/> with secret string values replaced
        /// by side-table tokens. Under any object named <c>auth</c>, every string field
        /// except <c>kind</c> and <c>user</c> is a secret. When <paramref name="createdTokens"/>
        /// is supplied, every token minted for this payload is added to it so the caller can
        /// release them if the request is rejected before a driver consumes them (R004).
        /// </summary>
        public static JsonNode? Redact(JsonNode? payload, SecretSideTable sideTable, ICollection<string>? createdTokens = null)
        {
            ArgumentNullException.ThrowIfNull(sideTable);
            return RedactNode(payload, sideTable, underAuth: false, createdTokens);
        }

        private static JsonNode? RedactNode(JsonNode? node, SecretSideTable sideTable, bool underAuth, ICollection<string>? createdTokens)
        {
            switch (node)
            {
                case JsonObject obj:
                    var redactedObject = new JsonObject();
                    foreach ((string key, JsonNode? value) in obj)
                    {
                        bool valueIsAuthObject = string.Equals(key, "auth", StringComparison.OrdinalIgnoreCase);
                        bool isSecret = value is JsonValue
                            && value.GetValueKind() == JsonValueKind.String
                            && (SecretKeys.Contains(key) || (underAuth && !AuthClearKeys.Contains(key)));

                        if (isSecret)
                        {
                            string token = sideTable.Tokenize(value!.GetValue<string>());
                            createdTokens?.Add(token);
                            redactedObject[key] = JsonValue.Create(token);
                        }
                        else
                        {
                            redactedObject[key] = RedactNode(value, sideTable, underAuth || valueIsAuthObject, createdTokens);
                        }
                    }
                    return redactedObject;

                case JsonArray array:
                    return new JsonArray(array.Select(item => RedactNode(item, sideTable, underAuth, createdTokens)).ToArray());

                case null:
                    return null;

                default:
                    return node.DeepClone();
            }
        }
    }
}
