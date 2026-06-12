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

        /// <summary>
        /// Reads every envelope from a journal directory (or a manifest path) in segment
        /// order. Includes the active segment of a still-running journal.
        /// </summary>
        public static IEnumerable<Sts2Envelope> ReadAll(string directoryOrManifestPath)
        {
            string directory = File.Exists(directoryOrManifestPath)
                ? Path.GetDirectoryName(Path.GetFullPath(directoryOrManifestPath))!
                : directoryOrManifestPath;

            IEnumerable<string> segments = Directory
                .EnumerateFiles(directory, "journal-*-*.jsonl")
                .Order(StringComparer.Ordinal);

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
