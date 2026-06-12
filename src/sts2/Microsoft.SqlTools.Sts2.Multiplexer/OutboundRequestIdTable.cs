//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    internal enum ChannelKind
    {
        Legacy,
        Sts2,
    }

    /// <summary>
    /// Maps rewritten public ids of server-initiated requests back to their originating
    /// channel and exact original id representation (SPEC §6.3, I13). A plain
    /// id-to-channel table is insufficient because both channels can emit id 1.
    /// </summary>
    internal sealed class OutboundRequestIdTable
    {
        private sealed record Entry(ChannelKind Channel, string OriginalIdRawJson, DateTimeOffset CreatedAt);

        private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);
        private readonly TimeProvider clock;
        private readonly TimeSpan ttl;
        private long counter;

        internal OutboundRequestIdTable(TimeProvider clock, TimeSpan ttl)
        {
            this.clock = clock;
            this.ttl = ttl;
        }

        /// <summary>Registers an outbound request and returns its globally unique public id.</summary>
        internal string Register(ChannelKind channel, string originalIdRawJson)
        {
            PruneExpired();
            string publicId = "sts2mux-" + Interlocked.Increment(ref counter).ToString(CultureInfo.InvariantCulture);
            entries[publicId] = new Entry(channel, originalIdRawJson, clock.GetUtcNow());
            return publicId;
        }

        /// <summary>Consumes the entry for an inbound response; one shot, so duplicates miss.</summary>
        internal bool TryConsume(string publicId, out ChannelKind channel, out string originalIdRawJson)
        {
            channel = default;
            originalIdRawJson = string.Empty;
            if (!entries.TryRemove(publicId, out Entry? entry))
            {
                return false;
            }
            if (clock.GetUtcNow() - entry.CreatedAt > ttl)
            {
                return false; // expired entries behave like unknown ids
            }
            channel = entry.Channel;
            originalIdRawJson = entry.OriginalIdRawJson;
            return true;
        }

        internal void DropChannel(ChannelKind channel)
        {
            foreach ((string key, Entry entry) in entries)
            {
                if (entry.Channel == channel)
                {
                    entries.TryRemove(key, out _);
                }
            }
        }

        internal void Clear() => entries.Clear();

        private void PruneExpired()
        {
            DateTimeOffset now = clock.GetUtcNow();
            foreach ((string key, Entry entry) in entries)
            {
                if (now - entry.CreatedAt > ttl)
                {
                    entries.TryRemove(key, out _);
                }
            }
        }
    }
}
