//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Dmp.Hosting.Utility;

namespace Microsoft.SqlTools.Dmp.Hosting.Protocol
{
    public class MessageReader
    {
        #region Private Fields

        public const int DefaultBufferSize = 8192;
        private const double BufferResizeTrigger = 0.25;

        private const int CR = 0x0D;
        private const int LF = 0x0A;
        private static readonly string[] NewLineDelimiters = { Environment.NewLine }; 

        private readonly Stream inputStream;

        private bool needsMoreData = true;
        private int readOffset;
        private int bufferEndOffset;

        private byte[] messageBuffer;

        private int expectedContentLength;
        private Dictionary<string, string> messageHeaders;
        
        internal enum ReadState
        {
            Headers,
            Content
        }

        #endregion

        #region Constructors

        public MessageReader(Stream inputStream,  Encoding messageEncoding = null)
        {
            Validate.IsNotNull("streamReader", inputStream);

            this.inputStream = inputStream;
            MessageEncoding = messageEncoding ?? Encoding.UTF8;

            messageBuffer = new byte[DefaultBufferSize];
        }

        #endregion
        
        #region Testable Properties

        internal byte[] MessageBuffer
        {
            get => messageBuffer;
            set => messageBuffer = value;
        }

        internal ReadState CurrentState { get; private set; }
        
        internal Encoding MessageEncoding { get; private set; }
        
        #endregion

        #region Public Methods

        public virtual async Task<Message> ReadMessage()
        {
            string messageContent = null;

            // Do we need to read more data or can we process the existing buffer?
            while (!needsMoreData || await ReadNextChunk())
            {
                // Clear the flag since we should have what we need now
                needsMoreData = false;

                // Do we need to look for message headers?
                if (CurrentState == ReadState.Headers && !TryReadMessageHeaders())
                {
                    // If we don't have enough data to read headers yet, keep reading
                    needsMoreData = true;
                    continue;
                }

                // Do we need to look for message content?
                if (CurrentState == ReadState.Content && !TryReadMessageContent(out messageContent))
                {
                    // If we don't have enough data yet to construct the content, keep reading
                    needsMoreData = true;
                    continue;
                }

                // We've read a message now, break out of the loop
                break;
            }

            // Now that we have a message, reset the buffer's state
            ShiftBufferBytesAndShrink(readOffset);

            // Return the parsed message
            return Message.Deserialize(messageContent);
        }

        #endregion

        #region Private Methods

        private async Task<bool> ReadNextChunk()
        {
            // Do we need to resize the buffer?  See if less than 1/4 of the space is left.
            if ((double)(messageBuffer.Length - bufferEndOffset) / messageBuffer.Length < BufferResizeTrigger)
            {
                // Double the size of the buffer
                Array.Resize(ref messageBuffer, messageBuffer.Length * 2);
            }

            // Read the next chunk into the message buffer
            int readLength =
                await inputStream.ReadAsync(messageBuffer, bufferEndOffset, messageBuffer.Length - bufferEndOffset);

            bufferEndOffset += readLength;

            if (readLength == 0)
            {
                // If ReadAsync returns 0 then it means that the stream was
                // closed unexpectedly (usually due to the client application
                // ending suddenly).  For now, just terminate the language
                // server immediately.
                throw new EndOfStreamException(SR.HostingUnexpectedEndOfStream);
            }

            return true;
        }

        private bool TryReadMessageHeaders()
        {
            int scanOffset = readOffset;

            // Scan for the final double-newline that marks the end of the header lines
            while (scanOffset + 3 < bufferEndOffset && 
                   (messageBuffer[scanOffset] != CR || 
                    messageBuffer[scanOffset + 1] != LF || 
                    messageBuffer[scanOffset + 2] != CR || 
                    messageBuffer[scanOffset + 3] != LF))
            {
                scanOffset++;
            }

            // Make sure we haven't reached the end of the buffer without finding a separator (e.g CRLFCRLF)
            if (scanOffset + 3 >= bufferEndOffset)
            {
                return false;
            }

            // Convert the header block into a array of lines
            var headers = Encoding.ASCII.GetString(messageBuffer, readOffset, scanOffset)
                .Split(NewLineDelimiters, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                // Read each header and store it in the dictionary
                messageHeaders = new Dictionary<string, string>();
                foreach (var header in headers)
                {
                    int currentLength = header.IndexOf(':');
                    if (currentLength == -1)
                    {
                        throw new ArgumentException(SR.HostingHeaderMissingColon);
                    }

                    var key = header.Substring(0, currentLength);
                    var value = header.Substring(currentLength + 1).Trim();
                    messageHeaders[key] = value;
                }

                // Parse out the content length as an int
                string contentLengthString;
                if (!messageHeaders.TryGetValue("Content-Length", out contentLengthString))
                {
                    throw new MessageParseException("", SR.HostingHeaderMissingContentLengthHeader);
                }

                // Parse the content length to an integer
                if (!int.TryParse(contentLengthString, out expectedContentLength))
                {
                    throw new MessageParseException("", SR.HostingHeaderMissingContentLengthValue);
                }
            }
            catch (Exception)
            {
                // The content length was invalid or missing. Trash the buffer we've read
                ShiftBufferBytesAndShrink(scanOffset + 4);
                throw;
            }

            // Skip past the headers plus the newline characters
            readOffset += scanOffset + 4;

            // Done reading headers, now read content
            CurrentState = ReadState.Content;

            return true;
        }

        private bool TryReadMessageContent(out string messageContent)
        {
            messageContent = null;

            // Do we have enough bytes to reach the expected length?
            if (bufferEndOffset - readOffset < expectedContentLength)
            {
                return false;
            }

            // Convert the message contents to a string using the specified encoding
            messageContent = MessageEncoding.GetString(messageBuffer, readOffset, expectedContentLength);

            readOffset += expectedContentLength;

            // Done reading content, now look for headers for the next message
            CurrentState = ReadState.Headers;

            return true;
        }

        private void ShiftBufferBytesAndShrink(int bytesToRemove)
        {
            // Create a new buffer that is shrunken by the number of bytes to remove
            // Note: by using Max, we can guarantee a buffer of at least default buffer size
            byte[] newBuffer = new byte[Math.Max(messageBuffer.Length - bytesToRemove, DefaultBufferSize)];

            // If we need to do shifting, do the shifting
            if (bytesToRemove <= messageBuffer.Length)
            {
                // Copy the existing buffer starting at the offset to remove
                Buffer.BlockCopy(messageBuffer, bytesToRemove, newBuffer, 0, bufferEndOffset - bytesToRemove);
            }

            // Make the new buffer the message buffer
            messageBuffer = newBuffer;

            // Reset the read offset and the end offset
            readOffset = 0;
            bufferEndOffset -= bytesToRemove;
        }

        #endregion
    }
}
