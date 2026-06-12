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
        /// except <c>kind</c> and <c>user</c> is a secret.
        /// </summary>
        public static JsonNode? Redact(JsonNode? payload, SecretSideTable sideTable)
        {
            ArgumentNullException.ThrowIfNull(sideTable);
            return RedactNode(payload, sideTable, underAuth: false);
        }

        private static JsonNode? RedactNode(JsonNode? node, SecretSideTable sideTable, bool underAuth)
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

                        redactedObject[key] = isSecret
                            ? JsonValue.Create(sideTable.Tokenize(value!.GetValue<string>()))
                            : RedactNode(value, sideTable, underAuth || valueIsAuthObject);
                    }
                    return redactedObject;

                case JsonArray array:
                    return new JsonArray(array.Select(item => RedactNode(item, sideTable, underAuth)).ToArray());

                case null:
                    return null;

                default:
                    return node.DeepClone();
            }
        }
    }
}
