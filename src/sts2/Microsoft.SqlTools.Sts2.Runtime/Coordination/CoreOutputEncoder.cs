//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Text.Json;
using Microsoft.SqlTools.Sts2.Core;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Coordination
{
    /// <summary>
    /// Maps a <see cref="CoreOutput"/> to its envelope fields. Shared by the live
    /// coordinator and the replayer so both produce byte-identical canonical payloads —
    /// replay verification (I7) compares digests of exactly this encoding.
    /// </summary>
    public static class CoreOutputEncoder
    {
        /// <summary>The envelope fields for one Core output, given the input envelope's type.</summary>
        public readonly record struct EncodedOutput(string Kind, string Type, string? Corr, JsonElement? Payload);

        /// <summary>Encodes one output deterministically.</summary>
        public static EncodedOutput Encode(CoreOutput output, string requestType)
        {
            ArgumentNullException.ThrowIfNull(output);
            return output switch
            {
                RpcResultOutput result => new EncodedOutput(EnvelopeKinds.RpcOutResult, requestType, result.Corr, result.Result),
                RpcErrorOutput error => new EncodedOutput(EnvelopeKinds.RpcOutError, requestType, error.Corr, EncodeErrorBody(error)),
                RpcNotifyOutput notify => new EncodedOutput(EnvelopeKinds.RpcOutNotify, notify.Method, null, notify.Params),
                EffectRequestOutput effect => new EncodedOutput(EnvelopeKinds.EffectRequest, effect.EffectName, effect.EffectId, effect.Args),
                DiagnosticOutput diagnostic => new EncodedOutput(EnvelopeKinds.Diagnostic, diagnostic.Name, null, diagnostic.Data),
                _ => throw new InvalidOperationException("Unknown CoreOutput type: " + output.GetType().Name),
            };
        }

        /// <summary>The JSON-RPC error body (SPEC §7.6 shape).</summary>
        public static JsonElement EncodeErrorBody(RpcErrorOutput error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return JsonDocument.Parse(string.Create(
                CultureInfo.InvariantCulture,
                $"{{\"code\":{error.JsonRpcCode},\"message\":{JsonSerializer.Serialize(error.Message)},\"data\":{{\"code\":{JsonSerializer.Serialize(error.DataCode)},\"retryable\":false,\"corr\":{JsonSerializer.Serialize(error.Corr)}}}}}")).RootElement;
        }
    }
}
