//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.SqlProjects.References
{
    /// <summary>
    /// Caches the resolved database references of SQL projects.
    /// </summary>
    /// <remarks>
    /// Safe to use from any thread. Concurrent requests for the same project share one resolution, and requests
    /// for different projects never wait for each other. An entry is resolved again once any file it depends on
    /// changes: the .sqlproj, the restore output, a referenced dacpac or a referenced project's .sqlproj. Changes to
    /// a referenced project's scripts do not invalidate an entry, because a project reference is built from its
    /// scripts each time it is loaded. Resolution only reads files, so callers run it on a background thread.
    /// </remarks>
    internal sealed class ProjectReferenceCache
    {
        private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the cached references of the project at <paramref name="projectFilePath"/>,
        /// calling <paramref name="resolve"/> when there are none or they are out of date.
        /// </summary>
        public ResolvedProjectReferences GetOrResolve(string projectFilePath, Func<ResolvedProjectReferences> resolve)
        {
            ArgumentNullException.ThrowIfNull(projectFilePath);
            ArgumentNullException.ThrowIfNull(resolve);

            string key = Path.GetFullPath(projectFilePath);
            while (true)
            {
                Entry entry = entries.GetOrAdd(key, _ => new Entry(key, resolve));
                ResolvedProjectReferences result;
                try
                {
                    result = entry.Value;
                }
                catch
                {
                    // Do not cache failures; the next request resolves again.
                    entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
                    throw;
                }

                if (!entry.IsStale())
                {
                    return result;
                }

                // Only remove this exact entry, so a newer entry added by another thread is kept.
                entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
            }
        }

        /// <summary>
        /// Drops the cached references of a project, for example after its references were edited.
        /// </summary>
        public void Invalidate(string projectFilePath)
        {
            ArgumentNullException.ThrowIfNull(projectFilePath);
            entries.TryRemove(Path.GetFullPath(projectFilePath), out _);
        }

        private sealed class Entry
        {
            private readonly Lazy<ResolvedProjectReferences> value;

            // Last write time of every file the resolution depends on; DateTime.MinValue for a missing file.
            private IReadOnlyDictionary<string, DateTime> dependencies = new Dictionary<string, DateTime>();

            public Entry(string projectFilePath, Func<ResolvedProjectReferences> resolve)
            {
                value = new Lazy<ResolvedProjectReferences>(() =>
                {
                    // Record the project's own files before resolving, so an edit made during resolution is noticed.
                    var snapshot = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                    string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
                    Record(snapshot, projectFilePath);
                    Record(snapshot, ProjectAssets.GetAssetsFilePath(projectDirectory));

                    ResolvedProjectReferences resolved = resolve();
                    foreach (string path in resolved.References.Select(reference => reference.ResolvedPath).OfType<string>())
                    {
                        Record(snapshot, path);
                    }

                    dependencies = snapshot;
                    return resolved;
                }, LazyThreadSafetyMode.ExecutionAndPublication);
            }

            public ResolvedProjectReferences Value => value.Value;

            public bool IsStale() =>
                dependencies.Any(dependency => GetLastWriteTime(dependency.Key) != dependency.Value);

            private static void Record(Dictionary<string, DateTime> snapshot, string path)
            {
                if (!snapshot.ContainsKey(path))
                {
                    snapshot[path] = GetLastWriteTime(path);
                }
            }

            private static DateTime GetLastWriteTime(string path) =>
                File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
    }
}
