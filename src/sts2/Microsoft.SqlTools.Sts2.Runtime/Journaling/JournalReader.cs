//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Journaling
{
    /// <summary>Reads journals back as ordered envelope streams. Tooling only; product code never rewrites journals.</summary>
    public static class JournalReader
    {
        private static readonly JsonSerializerOptions ManifestJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Parses a manifest file.</summary>
        public static JournalManifest ReadManifest(string manifestPath)
        {
            using FileStream stream = File.OpenRead(manifestPath);
            return JsonSerializer.Deserialize<JournalManifest>(stream, ManifestJsonOptions)
                ?? throw new InvalidDataException("Empty journal manifest: " + manifestPath);
        }

        /// <summary>Extracts the runId from a <c>journal-{runId}-{NNNN}.jsonl</c> segment file name, or null.</summary>
        private static string? RunIdOf(string fileName)
        {
            const string prefix = "journal-";
            const string suffix = ".jsonl";
            if (!fileName.StartsWith(prefix, StringComparison.Ordinal) || !fileName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return null;
            }
            string core = fileName[prefix.Length..^suffix.Length]; // {runId}-{NNNN}
            int dash = core.LastIndexOf('-');
            if (dash <= 0)
            {
                return null;
            }
            string index = core[(dash + 1)..];
            return index.Length == 4 && index.All(char.IsAsciiDigit) ? core[..dash] : null;
        }

        /// <summary>
        /// Reads every envelope from a journal directory (or a manifest path) in segment
        /// order, including the active segment of a still-running journal. Run isolation
        /// (R007): the directory must contain exactly ONE run — mixing runs (which would
        /// fabricate false gaps/divergence or leak data across bundles) is a loud error,
        /// not a silent concatenation. Use one directory per run (see Bootstrap).
        /// </summary>
        public static IEnumerable<Sts2Envelope> ReadAll(string directoryOrManifestPath)
        {
            string directory = File.Exists(directoryOrManifestPath)
                ? Path.GetDirectoryName(Path.GetFullPath(directoryOrManifestPath))!
                : directoryOrManifestPath;

            string[] segments = Directory
                .EnumerateFiles(directory, "journal-*-*.jsonl")
                .Order(StringComparer.Ordinal)
                .ToArray();

            var runs = segments
                .Select(s => RunIdOf(Path.GetFileName(s)))
                .Where(r => r is not null)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (runs.Length > 1)
            {
                throw new InvalidDataException(
                    $"Journal directory '{directory}' contains {runs.Length} runs ({string.Join(", ", runs)}); " +
                    "reads must be scoped to a single run. Use ReadRun(directory, runId) or one directory per run.");
            }

            return ReadSegments(segments);
        }

        /// <summary>Reads only the segments of <paramref name="runId"/> in a possibly-shared directory.</summary>
        public static IEnumerable<Sts2Envelope> ReadRun(string directory, string runId)
        {
            ArgumentException.ThrowIfNullOrEmpty(runId);
            string[] segments = Directory
                .EnumerateFiles(directory, "journal-" + runId + "-*.jsonl")
                .Order(StringComparer.Ordinal)
                .ToArray();
            return ReadSegments(segments);
        }

        private static IEnumerable<Sts2Envelope> ReadSegments(IReadOnlyList<string> segments)
        {
            foreach (string segment in segments)
            {
                using var reader = new StreamReader(
                    new FileStream(segment, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                while (reader.ReadLine() is string line)
                {
                    if (line.Length > 0)
                    {
                        yield return EnvelopeJsonCodec.DeserializeLine(line);
                    }
                }
            }
        }
    }
}
