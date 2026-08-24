//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>Machine-readable header of one scenario file (SPEC §14.2).</summary>
    public sealed record ScenarioInfo
    {
        /// <summary>Unique scenario name.</summary>
        public required string Name { get; init; }

        /// <summary>Tags (<c>query</c>, <c>cancel</c>, <c>dialect:neutral</c>, ...).</summary>
        public required IReadOnlyList<string> Tags { get; init; }

        /// <summary><c>stub</c> until the scenario runner executes it; then <c>active</c>.</summary>
        public required string Status { get; init; }

        /// <summary>Milestone in which the scenario becomes active.</summary>
        public required string Milestone { get; init; }

        /// <summary>Execution adapter: <c>fake</c>, <c>sqlite</c>, <c>sqlserver</c>, or <c>multiplexer</c>.</summary>
        public required string Adapter { get; init; }

        /// <summary>File the scenario was loaded from.</summary>
        public required string FilePath { get; init; }
    }

    /// <summary>
    /// Loads scenario headers for SCENARIO-MATRIX generation and corpus tests. Reads
    /// only the flat header fields; full YAML execution semantics arrive with the
    /// scenario runner (M2/M3).
    /// </summary>
    public static class ScenarioCatalog
    {
        /// <summary>Loads every <c>*.yaml</c> scenario under <paramref name="directory"/>, ordered by name.</summary>
        public static IReadOnlyList<ScenarioInfo> Load(string directory)
        {
            var scenarios = new List<ScenarioInfo>();
            foreach (string file in Directory.EnumerateFiles(directory, "*.yaml", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                scenarios.Add(LoadOne(file));
            }
            return scenarios.OrderBy(s => s.Name, StringComparer.Ordinal).ToArray();
        }

        /// <summary>Parses one scenario header.</summary>
        public static ScenarioInfo LoadOne(string filePath)
        {
            string? name = null, status = null, milestone = null, adapter = null;
            string[] tags = [];

            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                if (line.StartsWith('#') || line.Length == 0)
                {
                    continue;
                }
                int colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon <= 0)
                {
                    continue;
                }
                string key = line[..colon].Trim();
                string value = line[(colon + 1)..].Trim();
                switch (key)
                {
                    case "name": name = value; break;
                    case "status": status = value; break;
                    case "milestone": milestone = value; break;
                    case "adapter": adapter = value; break;
                    case "tags":
                        tags = value.TrimStart('[').TrimEnd(']')
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        break;
                    default:
                        break; // body fields are the runner's business (M2/M3)
                }
            }

            return new ScenarioInfo
            {
                Name = name ?? throw new InvalidDataException(filePath + ": missing 'name'"),
                Tags = tags,
                Status = status ?? throw new InvalidDataException(filePath + ": missing 'status'"),
                Milestone = milestone ?? throw new InvalidDataException(filePath + ": missing 'milestone'"),
                Adapter = adapter ?? throw new InvalidDataException(filePath + ": missing 'adapter'"),
                FilePath = filePath,
            };
        }
    }
}
