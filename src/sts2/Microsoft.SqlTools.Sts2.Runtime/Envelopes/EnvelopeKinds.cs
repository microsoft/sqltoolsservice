//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.Sts2.Runtime.Envelopes
{
    /// <summary>The closed set of envelope kinds (SPEC §8.1). Extending it is a SPEC-CHANGE.</summary>
    public static class EnvelopeKinds
    {
        /// <summary>An inbound JSON-RPC request.</summary>
        public const string RpcInRequest = "rpc.in.request";

        /// <summary>An inbound JSON-RPC notification.</summary>
        public const string RpcInNotify = "rpc.in.notify";

        /// <summary>An outbound JSON-RPC result.</summary>
        public const string RpcOutResult = "rpc.out.result";

        /// <summary>An outbound JSON-RPC error.</summary>
        public const string RpcOutError = "rpc.out.error";

        /// <summary>An outbound JSON-RPC notification.</summary>
        public const string RpcOutNotify = "rpc.out.notify";

        /// <summary>An internal command.</summary>
        public const string Command = "cmd";

        /// <summary>An internal event.</summary>
        public const string Event = "evt";

        /// <summary>A request for the effect runner.</summary>
        public const string EffectRequest = "effect.req";

        /// <summary>A result observed by the effect runner.</summary>
        public const string EffectResponse = "effect.res";

        /// <summary>A timer firing.</summary>
        public const string TimerDue = "timer.due";

        /// <summary>A configuration change; increments configVersion.</summary>
        public const string ConfigChanged = "config.changed";

        /// <summary>A redacted state snapshot.</summary>
        public const string StateSnapshot = "state.snapshot";

        /// <summary>A metric sample.</summary>
        public const string Metric = "metric";

        /// <summary>A diagnostic.</summary>
        public const string Diagnostic = "diag";

        /// <summary>A lifecycle/control signal (for example lifecycle.shutdown).</summary>
        public const string Control = "control";

        private static readonly HashSet<string> ValidKinds = new(StringComparer.Ordinal)
        {
            RpcInRequest, RpcInNotify, RpcOutResult, RpcOutError, RpcOutNotify,
            Command, Event, EffectRequest, EffectResponse, TimerDue, ConfigChanged,
            StateSnapshot, Metric, Diagnostic, Control,
        };

        /// <summary>All valid kinds.</summary>
        public static IReadOnlyCollection<string> All => ValidKinds;

        /// <summary>Returns true when <paramref name="kind"/> is a member of the closed set.</summary>
        public static bool IsValid(string kind) => ValidKinds.Contains(kind);
    }
}
