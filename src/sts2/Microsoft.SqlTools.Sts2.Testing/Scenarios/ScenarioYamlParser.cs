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
            var queryScripts = new List<FakeQueryScript>();
            if (TryGet(root, "driver", out YamlNode? driver) && driver is YamlMappingNode driverMap)
            {
                if (TryGet(driverMap, "open", out YamlNode? open) && open is YamlSequenceNode openList)
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
                if (TryGet(driverMap, "query", out YamlNode? query) && query is YamlSequenceNode queryList)
                {
                    foreach (YamlNode scriptNode in queryList)
                    {
                        var scriptMap = (YamlMappingNode)scriptNode;
                        var querySteps = new List<FakeQueryStep>();
                        if (TryGet(scriptMap, "steps", out YamlNode? stepsSeq) && stepsSeq is YamlSequenceNode stepList2)
                        {
                            foreach (YamlNode stepNode in stepList2)
                            {
                                var map = (YamlMappingNode)stepNode;
                                querySteps.Add(new FakeQueryStep
                                {
                                    Type = GetScalar(map, "type") ?? throw new InvalidDataException(filePath + ": query step missing type"),
                                    ResultSetId = ParseInt(map, "resultSetId"),
                                    Columns = GetScalar(map, "columns") is string cols ? int.Parse(cols, CultureInfo.InvariantCulture) : 2,
                                    Rows = ParseInt(map, "rows"),
                                    EdgeValues = GetScalar(map, "edgeValues") == "true",
                                    Text = GetScalar(map, "text"),
                                    Number = ParseInt(map, "number"),
                                    Severity = ParseInt(map, "severity"),
                                    ErrorCode = GetScalar(map, "errorCode"),
                                    RowCount = ParseInt(map, "rowCount"),
                                    RowsAffected = ParseInt(map, "rowsAffected"),
                                    DelayMs = ParseInt(map, "delayMs"),
                                });
                            }
                        }
                        queryScripts.Add(new FakeQueryScript { Steps = querySteps });
                    }
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

            JsonNode? configNode = TryGet(root, "config", out YamlNode? config) ? ToJson(config!) : null;
            string rowCapture = "full";
            string sqlCapture = "text";
            JsonNode? limits = null;
            if (configNode is System.Text.Json.Nodes.JsonObject configObj)
            {
                rowCapture = configObj["rowCapture"]?.GetValue<string>() ?? "full";
                sqlCapture = configObj["sqlCapture"]?.GetValue<string>() ?? "text";
                if (configObj["maxConnections"] is not null)
                {
                    limits = new System.Text.Json.Nodes.JsonObject { ["maxConnections"] = configObj["maxConnections"]!.DeepClone() };
                }
            }

            return new ScenarioDefinition
            {
                Info = info,
                OpenBehaviors = openBehaviors,
                QueryScripts = queryScripts,
                Steps = steps,
                Invariants = invariants,
                ConfigLimits = limits,
                RowCapture = rowCapture,
                SqlCapture = sqlCapture,
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
            if (TryGet(step, "notify", out YamlNode? notifyNode) && notifyNode is YamlMappingNode notifyMap)
            {
                return new ScenarioStep
                {
                    Kind = "notify",
                    NotifyMethod = GetScalar(notifyMap, "method") ?? throw new InvalidDataException(filePath + ": notify missing method"),
                    Params = TryGet(notifyMap, "params", out YamlNode? np) ? ToJson(np!) : null,
                };
            }
            if (TryGet(step, "waitForNotify", out YamlNode? waitNode) && waitNode is YamlMappingNode waitMap)
            {
                return new ScenarioStep
                {
                    Kind = "waitForNotify",
                    NotifyMethod = GetScalar(waitMap, "method") ?? throw new InvalidDataException(filePath + ": waitForNotify missing method"),
                    NotifyCount = ParseInt(waitMap, "count", 1),
                };
            }
            if (TryGet(step, "assertNotify", out YamlNode? assertNode) && assertNode is YamlMappingNode assertMap)
            {
                return new ScenarioStep
                {
                    Kind = "assertNotify",
                    NotifyMethod = GetScalar(assertMap, "method") ?? throw new InvalidDataException(filePath + ": assertNotify missing method"),
                    NotifyCount = ParseInt(assertMap, "count", 1),
                    NotifyMatch = TryGet(assertMap, "match", out YamlNode? match) ? ToJson(match!) : null,
                    SettleMs = ParseInt(assertMap, "settleMs"),
                };
            }
            if (TryGet(step, "control", out YamlNode? controlNode) && controlNode is YamlMappingNode controlMap)
            {
                return new ScenarioStep
                {
                    Kind = "control",
                    ControlSignal = GetScalar(controlMap, "signal") ?? throw new InvalidDataException(filePath + ": control missing signal"),
                };
            }
            throw new InvalidDataException(filePath + ": unknown step kind");
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

        private static int ParseInt(YamlMappingNode map, string key, int fallback = 0) =>
            GetScalar(map, key) is string value ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
    }
}
