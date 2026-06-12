//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    /// <summary>Configuration for <see cref="StdioMultiplexer"/>. Defaults match SPEC §11.2.</summary>
    public sealed record MultiplexerOptions
    {
        /// <summary>Maximum accepted frame size in bytes (<c>sts2.transport.maxFrameBytes</c>).</summary>
        public int MaxFrameBytes { get; init; } = 64 * 1024 * 1024;

        /// <summary>
        /// Bounded wait for the STS2 journal flush before an <c>exit</c> notification is
        /// forwarded to legacy (<c>sts2.runtime.exitFlushMs</c>).
        /// </summary>
        public int ExitFlushMilliseconds { get; init; } = 500;

        /// <summary>Time-to-live for outbound server-request id table entries.</summary>
        public TimeSpan OutboundRequestIdTtl { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>Clock used for id-table expiry; injectable for tests.</summary>
        public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

        /// <summary>
        /// Receives multiplexer diagnostics. Implementations must never write to stdout
        /// (SPEC §6.6). Invoked inline; keep it fast and non-throwing.
        /// </summary>
        public Action<MultiplexerDiagnostic>? DiagnosticListener { get; init; }
    }

    /// <summary>A structured multiplexer diagnostic (never written to stdout).</summary>
    public readonly record struct MultiplexerDiagnostic(string Code, string Message);

    /// <summary>Stable diagnostic codes emitted by <see cref="StdioMultiplexer"/>.</summary>
    public static class MultiplexerDiagnosticCodes
    {
        /// <summary>A frame's JSON payload could not be minimally parsed; raw bytes were forwarded to legacy.</summary>
        public const string MalformedPayload = "malformedPayload";

        /// <summary>The header block was unparseable; the multiplexer degraded to forwarding all inbound bytes to legacy.</summary>
        public const string MalformedHeader = "malformedHeader";

        /// <summary>A frame exceeded <see cref="MultiplexerOptions.MaxFrameBytes"/> and was forwarded raw to legacy.</summary>
        public const string OversizedFrame = "oversizedFrame";

        /// <summary>An inbound response id had no entry in the rewrite table; the frame went to legacy.</summary>
        public const string UnknownResponseId = "unknownResponseId";

        /// <summary>Traffic for the STS2 channel was synthesized or dropped because STS2 is dead.</summary>
        public const string Sts2Dead = "sts2Dead";

        /// <summary>The lifecycle sink threw or timed out.</summary>
        public const string LifecycleSinkError = "lifecycleSinkError";

        /// <summary>A pump loop failed unexpectedly.</summary>
        public const string PumpFailure = "pumpFailure";
    }
}
