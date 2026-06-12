//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Immutable;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>
    /// Immutable Core state. At M1 this holds only the toy machine that proves the
    /// pump/journal/replay loop; connection and query state machines arrive in M2/M3.
    /// Never contains row cells, secrets, or live handles (SPEC §9.2).
    /// </summary>
    public sealed record CoreState
    {
        /// <summary>The empty initial state.</summary>
        public static CoreState Initial { get; } = new()
        {
            LastSeq = 0,
            ShuttingDown = false,
            ToyCounter = 0,
            PendingToyEffects = ImmutableSortedDictionary<string, string>.Empty,
        };

        /// <summary>Seq of the last envelope this state reflects.</summary>
        public required long LastSeq { get; init; }

        /// <summary>True after a lifecycle shutdown/exit signal.</summary>
        public required bool ShuttingDown { get; init; }

        /// <summary>Toy: number of <c>v2/toy.echo</c> requests handled.</summary>
        public required int ToyCounter { get; init; }

        /// <summary>Toy: effect id -> originating RPC corr, for in-flight toy effects.
        /// Sorted so state output ordering is deterministic (SPEC §9.3.5).</summary>
        public required ImmutableSortedDictionary<string, string> PendingToyEffects { get; init; }
    }
}
