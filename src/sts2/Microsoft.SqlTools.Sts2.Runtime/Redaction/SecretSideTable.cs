//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.SqlTools.Sts2.Runtime.Redaction
{
    /// <summary>
    /// In-memory store of real secret material keyed by SecretRef token (SPEC §8.5).
    /// Owned by Runtime; values are passed only to the effect runner. This table is
    /// never serialized, logged, exported, or exposed by diagnostics — its only public
    /// reflection surface is <see cref="Count"/>.
    /// </summary>
    public sealed class SecretSideTable
    {
        private readonly Lock gate = new();
        private readonly Dictionary<string, string> secretsByToken = new(StringComparer.Ordinal);
        private long counter;

        /// <summary>Number of live entries (safe to expose; used by health counters).</summary>
        public int Count
        {
            get
            {
                lock (gate)
                {
                    return secretsByToken.Count;
                }
            }
        }

        /// <summary>
        /// Stores <paramref name="secretValue"/> and returns its token:
        /// <c>secret:sha256:&lt;12-hex-prefix&gt;:&lt;counter&gt;</c>.
        /// </summary>
        public string Tokenize(string secretValue)
        {
            ArgumentNullException.ThrowIfNull(secretValue);
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(secretValue), hash);
            string prefix = Convert.ToHexStringLower(hash[..6]);
            long n = Interlocked.Increment(ref counter);
            string token = string.Create(CultureInfo.InvariantCulture, $"secret:sha256:{prefix}:{n}");
            lock (gate)
            {
                secretsByToken[token] = secretValue;
            }
            return token;
        }

        /// <summary>Resolves a token to its secret; effect-runner use only.</summary>
        public bool TryResolve(string token, out string secretValue)
        {
            lock (gate)
            {
                return secretsByToken.TryGetValue(token, out secretValue!);
            }
        }

        /// <summary>Removes one entry (open attempt completed or failed).</summary>
        public bool Remove(string token)
        {
            lock (gate)
            {
                return secretsByToken.Remove(token);
            }
        }

        /// <summary>Removes every entry in <paramref name="tokens"/> (session closed).</summary>
        public void RemoveAll(IEnumerable<string> tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);
            lock (gate)
            {
                foreach (string token in tokens)
                {
                    secretsByToken.Remove(token);
                }
            }
        }
    }
}
