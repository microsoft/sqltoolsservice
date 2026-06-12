//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Contracts.Diagnostics;
using StreamJsonRpc;

namespace Microsoft.SqlTools.Sts2.Hosting
{
    /// <summary>M0 diagnostics surface. Grows the envelope-translating gateway in M1.</summary>
    internal sealed class DiagnosticsRpcTarget
    {
        private readonly string serviceVersion;

        internal DiagnosticsRpcTarget(string serviceVersion)
        {
            this.serviceVersion = serviceVersion;
        }

        [JsonRpcMethod("v2/diagnostics.ping", UseSingleObjectParameterDeserialization = true)]
        public DiagnosticsPingResult Ping(DiagnosticsPingParams? args = null) => new()
        {
            SpecVersion = Sts2WireConstants.SpecVersion,
            ServiceVersion = serviceVersion,
            Echo = args?.Echo,
            LatestJournalSeq = 0, // journal arrives in M1
            Health = "ok",
        };
    }
}
