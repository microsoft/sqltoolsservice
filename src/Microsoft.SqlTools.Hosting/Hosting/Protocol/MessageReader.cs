﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.Hosting.Protocol
{
    public class MessageReader
    {
        #region Private Fields

        public const int DefaultBufferSize = 8192;
        public const double BufferResizeTrigger = 0.25;

        private const int CR = 0x0D;
        private const int LF = 0x0A;
        private static readonly string[] NewLineDelimiters = { Environment.NewLine }; 

        private readonly Stream inputStream;
        private readonly IMessageSerializer messageSerializer;
        private readonly Encoding messageEncoding;

        private ReadState readState;
        private bool needsMoreData = true;
        private int readOffset;
        private int bufferEndOffset;
        private byte[] messageBuffer;

        private int expectedContentLength;
        private Dictionary<string, string> messageHeaders;

        private enum ReadState
        {
            Headers,
            Content
        }

        #endregion

        #region Constructors
        public MessageReader() {} // added for mocking MessageReader in UT

        public MessageReader(
            Stream inputStream,
            IMessageSerializer messageSerializer,
            Encoding messageEncoding = null)
        {
            Validate.IsNotNull("streamReader", inputStream);
            Validate.IsNotNull("messageSerializer", messageSerializer);

            this.inputStream = inputStream;
            this.messageSerializer = messageSerializer;

            this.messageEncoding = messageEncoding;
            if (messageEncoding == null)
            {
                this.messageEncoding = Encoding.UTF8;
            }

            this.messageBuffer = new byte[DefaultBufferSize];
        }

        #endregion

        #region Public Methods

        public virtual async Task<Message> ReadMessage() // mark as virtual for mocking MessageReader in UT
        {
            string messageContent = null;

            // Do we need to read more data or can we process the existing buffer?
            while (!this.needsMoreData || await this.ReadNextChunk())
            {
                // Clear the flag since we should have what we need now
                this.needsMoreData = false;

                // Do we need to look for message headers?
                if (this.readState == ReadState.Headers &&
                    !this.TryReadMessageHeaders())
                {
                    // If we don't have enough data to read headers yet, keep reading
                    this.needsMoreData = true;
                    continue;
                }

                // Do we need to look for message content?
                if (this.readState == ReadState.Content &&
                    !this.TryReadMessageContent(out messageContent))
                {
                    // If we don't have enough data yet to construct the content, keep reading
                    this.needsMoreData = true;
                    continue;
                }

                // We've read a message now, break out of the loop
                break;
            }

            // Now that we have a message, reset the buffer's state
            ShiftBufferBytesAndShrink(readOffset);

            // Get the JObject for the JSON content
            JsonReader messageReader = new JsonTextReader(new StringReader(messageContent));
            messageReader.DateParseHandling = DateParseHandling.None;
            JObject messageObject = JObject.Load(messageReader);

            // Return the parsed message
            return this.messageSerializer.DeserializeMessage(messageObject);
        }

        #endregion

        #region Private Methods

        private async Task<bool> ReadNextChunk()
        {
            // Do we need to resize the buffer?  See if less than 1/4 of the space is left.
            if (((double)(this.messageBuffer.Length - this.bufferEndOffset) / this.messageBuffer.Length) < 0.25)
            {
                // Double the size of the buffer
                Array.Resize(
                    ref this.messageBuffer, 
                    this.messageBuffer.Length * 2);
            }

            // Read the next chunk into the message buffer
            int readLength =
                await this.inputStream.ReadAsync(
                    this.messageBuffer,
                    this.bufferEndOffset,
                    this.messageBuffer.Length - this.bufferEndOffset);

            this.bufferEndOffset += readLength;

            if (readLength == 0)
            {
                // If ReadAsync returns 0 then it means that the stream was
                // closed unexpectedly (usually due to the client application
                // ending suddenly).  For now, just terminate the language
                // server immediately.
                // TODO: Provide a more graceful shutdown path
                throw new EndOfStreamException(SR.HostingUnexpectedEndOfStream);
            }

            return true;
        }

        private bool TryReadMessageHeaders()
        {
            int scanOffset = this.readOffset;

            // Scan for the final double-newline that marks the end of the header lines
            while (scanOffset + 3 < this.bufferEndOffset && 
                   (this.messageBuffer[scanOffset] != CR || 
                    this.messageBuffer[scanOffset + 1] != LF || 
                    this.messageBuffer[scanOffset + 2] != CR || 
                    this.messageBuffer[scanOffset + 3] != LF))
            {
                scanOffset++;
            }

            // Make sure we haven't reached the end of the buffer without finding a separator (e.g CRLFCRLF)
            if (scanOffset + 3 >= this.bufferEndOffset)
            {
                return false;
            }

            // Convert the header block into a array of lines
            var headers = Encoding.ASCII.GetString(this.messageBuffer, this.readOffset, scanOffset)
                .Split(NewLineDelimiters, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                // Read each header and store it in the dictionary
                this.messageHeaders = new Dictionary<string, string>();
                foreach (var header in headers)
                {
                    int currentLength = header.IndexOf(':');
                    if (currentLength == -1)
                    {
                        throw new ArgumentException(SR.HostingHeaderMissingColon);
                    }

                    var key = header.Substring(0, currentLength);
                    var value = header.Substring(currentLength + 1).Trim();
                    this.messageHeaders[key] = value;
                }

                // Parse out the content length as an int
                string contentLengthString;
                if (!this.messageHeaders.TryGetValue("Content-Length", out contentLengthString))
                {
                    throw new MessageParseException("", SR.HostingHeaderMissingContentLengthHeader);
                }

                // Parse the content length to an integer
                if (!int.TryParse(contentLengthString, out this.expectedContentLength))
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
            this.readOffset += scanOffset + 4;

            // Done reading headers, now read content
            this.readState = ReadState.Content;

            return true;
        }

        private bool TryReadMessageContent(out string messageContent)
        {
            messageContent = null;

            // Do we have enough bytes to reach the expected length?
            if ((this.bufferEndOffset - this.readOffset) < this.expectedContentLength)
            {
                return false;
            }

            // Convert the message contents to a string using the specified encoding
            messageContent = this.messageEncoding.GetString(
                this.messageBuffer,
                this.readOffset,
                this.expectedContentLength);

            readOffset += expectedContentLength;

            // Done reading content, now look for headers for the next message
            this.readState = ReadState.Headers;

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
