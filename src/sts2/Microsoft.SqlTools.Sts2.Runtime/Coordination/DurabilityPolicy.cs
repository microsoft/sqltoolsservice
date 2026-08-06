//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>
    /// The single place that decides which envelopes force a journal flush (SPEC §6.2/§8.3,
    /// R020). A "checkpoint" envelope is durable on disk before the pump proceeds, so an
    /// abrupt process termination cannot lose it. The decision depends on both kind AND type
    /// because some notifications are terminal (e.g. <c>v2/query.complete</c>) while most are not.
    /// </summary>
    public static class DurabilityPolicy
    {
        /// <summary>True when an envelope of this kind/type must be flushed before dispatch.</summary>
        public static bool IsCheckpoint(string kind, string type) => kind switch
        {
            // Terminal request responses, diagnostics, lifecycle, and config changes.
            EnvelopeKinds.RpcOutResult => true,
            EnvelopeKinds.RpcOutError => true,
            EnvelopeKinds.Diagnostic => true,
            EnvelopeKinds.Control => true,
            EnvelopeKinds.ConfigChanged => true,

            // The one terminal notification: a query's single completion (I2). Without this,
            // query.complete could remain buffered despite being the terminal the client waits on.
            EnvelopeKinds.RpcOutNotify => type is "v2/query.complete" or "v2/fatal",

            _ => false,
        };
    }
}
