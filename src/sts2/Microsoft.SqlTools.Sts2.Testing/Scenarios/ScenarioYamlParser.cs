//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace Microsoft.SqlTools.Sts2.Testing.Scenarios
{
    /// <summary>Parses scenario YAML (SPEC §14.2, DEV-004) into the executable model.</summary>
    public static class ScenarioYamlParser
    {
        /// <summary>Parses one scenario file.</summary>
        public static ScenarioDefinition Parse(string filePath)
        {
            var yaml = new YamlStream();
            using (var reader = new StreamReader(filePath))
            {
                yaml.Load(reader);
            }
            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new InvalidDataException(filePath + ": not a YAML mapping");
            }

            ScenarioInfo info = ScenarioCatalog.LoadOne(filePath);

            var openBehaviors = new List<FakeOpenBehavior>();
            if (TryGet(root, "driver", out YamlNode? driver) && driver is YamlMappingNode driverMap
                && TryGet(driverMap, "open", out YamlNode? open) && open is YamlSequenceNode openList)
            {
                foreach (YamlNode item in openList)
                {
                    var map = (YamlMappingNode)item;
                    openBehaviors.Add(new FakeOpenBehavior
                    {
                        Outcome = GetScalar(map, "outcome") ?? "ok",
                        DelayMs = int.Parse(GetScalar(map, "delayMs") ?? "0", CultureInfo.InvariantCulture),
                    });
                }
            }

            var steps = new List<ScenarioStep>();
            if (TryGet(root, "steps", out YamlNode? stepsNode) && stepsNode is YamlSequenceNode stepList)
            {
                foreach (YamlNode item in stepList)
                {
                    steps.Add(ParseStep((YamlMappingNode)item, filePath));
                }
            }

            var invariants = new List<string>();
            if (TryGet(root, "expect", out YamlNode? expect) && expect is YamlMappingNode expectMap
                && TryGet(expectMap, "invariants", out YamlNode? inv) && inv is YamlSequenceNode invList)
            {
                invariants.AddRange(invList.Select(n => ((YamlScalarNode)n).Value!));
            }

            return new ScenarioDefinition
            {
                Info = info,
                OpenBehaviors = openBehaviors,
                Steps = steps,
                Invariants = invariants,
                ConfigLimits = TryGet(root, "config", out YamlNode? config) ? ToJson(config!) : null,
            };
        }

        private static ScenarioStep ParseStep(YamlMappingNode step, string filePath)
        {
            if (TryGet(step, "request", out YamlNode? request) && request is YamlMappingNode requestMap)
            {
                return new ScenarioStep
                {
                    Kind = "request",
                    Method = GetScalar(requestMap, "method") ?? throw new InvalidDataException(filePath + ": request step missing method"),
                    Params = TryGet(requestMap, "params", out YamlNode? p) ? ToJson(p!) : null,
                    Await = GetScalar(step, "await") != "false",
                    Label = GetScalar(step, "label"),
                    ExpectResult = TryGet(step, "expectResult", out YamlNode? er) ? ToJson(er!) : null,
                    ExpectError = ParseExpectedError(step),
                    Bind = ParseBind(step),
                };
            }
            if (TryGet(step, "awaitTerminal", out YamlNode? awaitNode) && awaitNode is YamlMappingNode awaitMap)
            {
                return new ScenarioStep
                {
                    Kind = "awaitTerminal",
                    Label = GetScalar(awaitMap, "label") ?? throw new InvalidDataException(filePath + ": awaitTerminal missing label"),
                    ExpectResult = TryGet(step, "expectResult", out YamlNode? er2) ? ToJson(er2!) : null,
                    ExpectError = ParseExpectedError(step),
                };
            }
            throw new InvalidDataException(filePath + ": step must be 'request' or 'awaitTerminal'");
        }

        private static ScenarioExpectedError? ParseExpectedError(YamlMappingNode step)
        {
            if (!TryGet(step, "expectError", out YamlNode? error) || error is not YamlMappingNode errorMap)
            {
                return null;
            }
            return new ScenarioExpectedError
            {
                DataCode = GetScalar(errorMap, "dataCode") ?? throw new InvalidDataException("expectError missing dataCode"),
                JsonRpcCode = GetScalar(errorMap, "jsonRpcCode") is string code
                    ? int.Parse(code, CultureInfo.InvariantCulture)
                    : null,
            };
        }

        private static IReadOnlyDictionary<string, string> ParseBind(YamlMappingNode step)
        {
            var bind = new Dictionary<string, string>(StringComparer.Ordinal);
            if (TryGet(step, "bind", out YamlNode? bindNode) && bindNode is YamlMappingNode bindMap)
            {
                foreach ((YamlNode key, YamlNode value) in bindMap.Children)
                {
                    bind[((YamlScalarNode)key).Value!] = ((YamlScalarNode)value).Value!;
                }
            }
            return bind;
        }

        /// <summary>Converts a YAML node to JSON, inferring scalars (numbers, booleans, null).</summary>
        internal static JsonNode? ToJson(YamlNode node) => node switch
        {
            YamlMappingNode mapping => new JsonObject(mapping.Children.Select(kv =>
                new KeyValuePair<string, JsonNode?>(((YamlScalarNode)kv.Key).Value!, ToJson(kv.Value)))),
            YamlSequenceNode sequence => new JsonArray(sequence.Select(ToJson).ToArray()),
            YamlScalarNode scalar => ScalarToJson(scalar),
            _ => throw new InvalidDataException("Unsupported YAML node type: " + node.GetType().Name),
        };

        private static JsonNode? ScalarToJson(YamlScalarNode scalar)
        {
            string? value = scalar.Value;
            if (scalar.Style is YamlDotNet.Core.ScalarStyle.SingleQuoted or YamlDotNet.Core.ScalarStyle.DoubleQuoted)
            {
                return JsonValue.Create(value);
            }
            return value switch
            {
                null or "null" or "~" => null,
                "true" => JsonValue.Create(true),
                "false" => JsonValue.Create(false),
                _ when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l) => JsonValue.Create(l),
                _ when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) => JsonValue.Create(d),
                _ => JsonValue.Create(value),
            };
        }

        private static bool TryGet(YamlMappingNode map, string key, out YamlNode? value)
        {
            foreach ((YamlNode k, YamlNode v) in map.Children)
            {
                if (k is YamlScalarNode scalar && scalar.Value == key)
                {
                    value = v;
                    return true;
                }
            }
            value = null;
            return false;
        }

        private static string? GetScalar(YamlMappingNode map, string key) =>
            TryGet(map, key, out YamlNode? value) && value is YamlScalarNode scalar ? scalar.Value : null;
    }
}
