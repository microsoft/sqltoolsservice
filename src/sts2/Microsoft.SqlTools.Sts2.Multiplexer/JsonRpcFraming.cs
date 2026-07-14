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

            bool parsedContentLength;
            if (headerBlock.IsSingleSegment)
            {
                parsedContentLength = TryParseContentLength(headerBlock.FirstSpan, out contentLength);
            }
            else
            {
                // Headers are capped at 16 KiB and normally fit in one segment. Preserve
                // the allocation-free path even for adversarial fragmentation.
                Span<byte> contiguousHeader = stackalloc byte[checked((int)headerBlock.Length)];
                headerBlock.CopyTo(contiguousHeader);
                parsedContentLength = TryParseContentLength(contiguousHeader, out contentLength);
            }

            if (!parsedContentLength)
            {
                return FrameHeaderStatus.MalformedHeader;
            }
            if (contentLength > maxFrameBytes)
            {
                return FrameHeaderStatus.OversizedFrame;
            }
            return FrameHeaderStatus.HeaderParsed;
        }

        private static bool TryParseContentLength(ReadOnlySpan<byte> header, out long contentLength)
        {
            contentLength = -1;
            int offset = 0;
            while (offset <= header.Length)
            {
                int relativeEnd = header[offset..].IndexOf("\r\n"u8);
                int end = relativeEnd < 0 ? header.Length : offset + relativeEnd;
                ReadOnlySpan<byte> line = header[offset..end];
                int colon = line.IndexOf((byte)':');
                if (colon > 0 && AsciiEqualsIgnoreCase(TrimAsciiWhitespace(line[..colon]), "Content-Length"u8))
                {
                    if (!TryParseNonNegativeInt64(TrimAsciiWhitespace(line[(colon + 1)..]), out contentLength))
                    {
                        return false;
                    }
                }

                if (relativeEnd < 0)
                {
                    break;
                }
                offset = end + 2;
            }

            return contentLength >= 0;
        }

        private static ReadOnlySpan<byte> TrimAsciiWhitespace(ReadOnlySpan<byte> value)
        {
            int start = 0;
            while (start < value.Length && value[start] is (byte)' ' or (byte)'\t')
            {
                start++;
            }

            int end = value.Length;
            while (end > start && value[end - 1] is (byte)' ' or (byte)'\t')
            {
                end--;
            }
            return value[start..end];
        }

        private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                byte a = left[i];
                byte b = right[i];
                if (a is >= (byte)'A' and <= (byte)'Z')
                {
                    a = (byte)(a + ((byte)'a' - (byte)'A'));
                }
                if (b is >= (byte)'A' and <= (byte)'Z')
                {
                    b = (byte)(b + ((byte)'a' - (byte)'A'));
                }
                if (a != b)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryParseNonNegativeInt64(ReadOnlySpan<byte> value, out long result)
        {
            result = 0;
            if (value.IsEmpty)
            {
                return false;
            }

            foreach (byte digit in value)
            {
                if (digit is < (byte)'0' or > (byte)'9')
                {
                    return false;
                }
                int numeric = digit - (byte)'0';
                if (result > (long.MaxValue - numeric) / 10)
                {
                    return false;
                }
                result = (result * 10) + numeric;
            }
            return true;
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
