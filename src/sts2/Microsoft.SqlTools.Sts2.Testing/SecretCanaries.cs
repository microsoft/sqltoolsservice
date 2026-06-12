//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>
    /// Known fake secrets planted by tests (SPEC §8.5). The canary scan greps every
    /// produced artifact for these values; any hit fails the I6 secret-safety invariant.
    /// </summary>
    public static class SecretCanaries
    {
        /// <summary>A fake SQL login password.</summary>
        public const string Password = "CANARY-pw-1f9b3c7d2e";

        /// <summary>A fake bearer token with a JWT-ish shape.</summary>
        public const string AccessToken = "eyJhbGciOiJIUzI1NiJ9.CANARY-at-5a8d0e.CANARYSIG";

        /// <summary>Every canary value the scanner looks for.</summary>
        public static IReadOnlyList<string> All { get; } = [Password, AccessToken];

        /// <summary>Returns the canaries found in <paramref name="content"/> (empty = clean).</summary>
        public static IReadOnlyList<string> FindIn(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            var found = new List<string>();
            foreach (string canary in All)
            {
                if (content.Contains(canary, StringComparison.Ordinal))
                {
                    found.Add(canary);
                }
            }
            return found;
        }

        /// <summary>Scans every file under <paramref name="directory"/>; returns "path: canary" hits.</summary>
        public static IReadOnlyList<string> ScanDirectory(string directory)
        {
            var hits = new List<string>();
            if (!Directory.Exists(directory))
            {
                return hits;
            }
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                foreach (string canary in FindIn(File.ReadAllText(file)))
                {
                    hits.Add(file + ": " + canary);
                }
            }
            return hits;
        }
    }
}
