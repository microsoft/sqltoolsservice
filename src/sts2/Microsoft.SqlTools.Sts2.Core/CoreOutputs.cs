//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Immutable;
using System.Text.Json;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>Something Core wants done. Runtime journals each output, then acts on it.</summary>
    public abstract record CoreOutput;

    /// <summary>Send a JSON-RPC result for the request correlated by <paramref name="Corr"/>.</summary>
    public sealed record RpcResultOutput(string Corr, JsonElement Result) : CoreOutput;

    /// <summary>Send a JSON-RPC error (numeric code on the wire, stable identity in DataCode).</summary>
    public sealed record RpcErrorOutput(string Corr, int JsonRpcCode, string Message, string DataCode) : CoreOutput;

    /// <summary>Send a JSON-RPC notification.</summary>
    public sealed record RpcNotifyOutput(string Method, JsonElement Params) : CoreOutput;

    /// <summary>Run an effect; the response re-enters the coordinator as an <c>effect.res</c> envelope.</summary>
    public sealed record EffectRequestOutput(string EffectId, string EffectName, JsonElement Args, string? Corr) : CoreOutput;

    /// <summary>Journal a diagnostic.</summary>
    public sealed record DiagnosticOutput(string Name, JsonElement Data) : CoreOutput;

    /// <summary>
    /// Journal a configuration change (SPEC §8.4/§11.1). Core has already applied the new
    /// config to its state and bumped the version; this output records it in the trace so
    /// the change is replay-visible (I15) and the Runtime can enact the new capture mode.
    /// </summary>
    public sealed record ConfigChangedOutput(JsonElement Config) : CoreOutput;

    /// <summary>The reducer's verdict: the next state plus everything to do (SPEC §9.2).</summary>
    public sealed record CoreDecision(CoreState NewState, ImmutableArray<CoreOutput> Outputs)
    {
        /// <summary>A decision that changes state and does nothing else.</summary>
        public static CoreDecision StateOnly(CoreState newState) => new(newState, ImmutableArray<CoreOutput>.Empty);
    }
}
