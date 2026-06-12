//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Buffers;
using System.Globalization;
using System.Text;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    internal enum FrameHeaderStatus
    {
        /// <summary>Not enough buffered bytes to finish the header block.</summary>
        NeedMoreData,

        /// <summary>Header parsed; caller must wait until the full payload is buffered.</summary>
        HeaderParsed,

        /// <summary>Declared payload exceeds the configured maximum (SPEC §6.1).</summary>
        OversizedFrame,

        /// <summary>The header block is unparseable; framing cannot continue.</summary>
        MalformedHeader,
    }

    /// <summary>Incremental Content-Length frame scanning (SPEC §6.1).</summary>
    internal static class JsonRpcFraming
    {
        /// <summary>Upper bound on a sane header block; beyond this the stream is degraded.</summary>
        internal const int MaxHeaderBytes = 16 * 1024;

        private static ReadOnlySpan<byte> HeaderTerminator => "\r\n\r\n"u8;

        /// <summary>
        /// Attempts to parse one frame header at the start of <paramref name="buffer"/>.
        /// Accepts <c>Content-Length:33</c>, <c>Content-Length: 33</c>, case-insensitive
        /// names, and additional headers such as <c>Content-Type</c>.
        /// </summary>
        internal static FrameHeaderStatus TryParseHeader(
            in ReadOnlySequence<byte> buffer,
            int maxFrameBytes,
            out int headerLength,
            out long contentLength)
        {
            headerLength = 0;
            contentLength = -1;

            var sequenceReader = new SequenceReader<byte>(buffer);
            if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> headerBlock, HeaderTerminator, advancePastDelimiter: true))
            {
                return buffer.Length > MaxHeaderBytes ? FrameHeaderStatus.MalformedHeader : FrameHeaderStatus.NeedMoreData;
            }

            headerLength = (int)sequenceReader.Consumed;
            if (headerLength > MaxHeaderBytes)
            {
                return FrameHeaderStatus.MalformedHeader;
            }

            string headerText = Encoding.ASCII.GetString(headerBlock.ToArray());
            foreach (string line in headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon <= 0)
                {
                    continue;
                }
                if (line.AsSpan(0, colon).Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    if (!long.TryParse(line.AsSpan(colon + 1).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out long parsed))
                    {
                        return FrameHeaderStatus.MalformedHeader;
                    }
                    contentLength = parsed;
                }
            }

            if (contentLength < 0)
            {
                return FrameHeaderStatus.MalformedHeader;
            }
            if (contentLength > maxFrameBytes)
            {
                return FrameHeaderStatus.OversizedFrame;
            }
            return FrameHeaderStatus.HeaderParsed;
        }

        /// <summary>Builds a canonical <c>Content-Length</c> framed message around <paramref name="payload"/>.</summary>
        internal static byte[] BuildFrame(ReadOnlySpan<byte> payload)
        {
            byte[] header = Encoding.ASCII.GetBytes(
                "Content-Length: " + payload.Length.ToString(CultureInfo.InvariantCulture) + "\r\n\r\n");
            byte[] frame = new byte[header.Length + payload.Length];
            header.CopyTo(frame);
            payload.CopyTo(frame.AsSpan(header.Length));
            return frame;
        }
    }
}
