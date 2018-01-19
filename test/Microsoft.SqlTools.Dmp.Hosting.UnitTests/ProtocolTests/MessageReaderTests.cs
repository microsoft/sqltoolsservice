//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.ProtocolTests
{
    public class MessageReaderTests
    {
        #region Construction Tests
        
        [Fact]
        public void CreateReaderNullStream()
        {
            // If: I create a message reader with a null stream reader
            // Then: It should throw
            Assert.Throws<ArgumentNullException>(() => new MessageReader(null));
        }

        [Fact]
        public void CreateReaderStandardEncoding()
        {
            // If: I create a message reader without including a message encoding
            var mr = new MessageReader(Stream.Null);
            
            // Then: The reader's encoding should be UTF8
            Assert.Equal(Encoding.UTF8, mr.MessageEncoding);
        }

        [Fact]
        public void CreateReaderNonStandardEncoding()
        {
            // If: I create a message reader with a specific message encoding
            var mr = new MessageReader(Stream.Null, Encoding.ASCII);
            
            // Then: The reader's encoding should be ASCII
            Assert.Equal(Encoding.ASCII, mr.MessageEncoding);
        }
        
        #endregion
        
        #region ReadMessage Tests

        [Theory]
        [InlineData(512)]    // Buffer size can fit everything in one read
        [InlineData(10)]     // Buffer size must use multiple reads to read the headers
        [InlineData(25)]     // Buffer size must use multiple reads to read the contents
        public async Task ReadMessageSingleRead(int bufferSize)
        {
            // Setup: Reader with a stream that has an entire message in it
            byte[] testBytes = Encoding.UTF8.GetBytes("Content-Length: 50\r\n\r\n{\"jsonrpc\": \"2.0\", \"method\":\"test\", \"params\":null}");
            using (Stream testStream = new MemoryStream(testBytes))
            {
                var mr = new MessageReader(testStream) {MessageBuffer = new byte[bufferSize]};
                
                // If: I reade a message with the reader
                var output = await mr.ReadMessage();
                
                // Then:
                // ... I should have a successful message read
                Assert.NotNull(output);
                
                // ... The reader should be back in header mode
                Assert.Equal(MessageReader.ReadState.Headers, mr.CurrentState);
                
                // ... The buffer should have been trimmed
                Assert.Equal(MessageReader.DefaultBufferSize, mr.MessageBuffer.Length);
            }
        }

        [Theory]
        [InlineData("Content-Type: application/json\r\n\r\n")] // Missing content-length header
        [InlineData("Content-Length: abc\r\n\r\n")]            // Content-length is not a number
        public async Task ReadMessageInvalidHeaders(string testString)
        {
            // Setup: Reader with a stream that has an invalid header in it
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            using (Stream testStream = new MemoryStream(testBytes))
            {
                var mr = new MessageReader(testStream) {MessageBuffer = new byte[20]};
                
                // If: I read a message with invalid headers
                // Then: ... I should get an exception
                await Assert.ThrowsAnyAsync<MessageParseException>(() => mr.ReadMessage());
                
                // ... The buffer should have been trashed (reset to it's original tiny size)
                Assert.Equal(MessageReader.DefaultBufferSize, mr.MessageBuffer.Length);
            }
        }

        [Fact]
        public async Task ReadMessageInvalidJson()
        {
            // Setup: Reader with a stream that has an invalid json message in it
            byte[] testBytes = Encoding.UTF8.GetBytes("Content-Length: 10\r\n\r\nabcdefghij");
            using (Stream testStream = new MemoryStream(testBytes))
            {
                // ... Buffer size is small to validate if the buffer has been trashed at the end
                var mr = new MessageReader(testStream) {MessageBuffer = new byte[20]};
                
                // If: I read a message with an invalid JSON in it
                // Then: 
                // ... I should get an exception
                await Assert.ThrowsAnyAsync<JsonException>(() => mr.ReadMessage());
                
                // ... The buffer should have been trashed (reset to it's original tiny size)
                Assert.Equal(MessageReader.DefaultBufferSize, mr.MessageBuffer.Length);
            }
        }

        [Fact]
        public async Task ReadMultipleMessages()
        {
            // Setup: Reader with a stream that has multiple messages in it
            const string testString = "Content-Length: 50\r\n\r\n{\"jsonrpc\": \"2.0\", \"method\":\"test\", \"params\":null}";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString + testString);
            using (Stream testStream = new MemoryStream(testBytes))
            {
                var mr = new MessageReader(testStream);
                
                // If:
                // ... I read a message
                var msg1 = await mr.ReadMessage();
                
                // ... And I read another message
                var msg2 = await mr.ReadMessage();
                
                // Then:
                // ... The messages should be real messages
                Assert.NotNull(msg1);
                Assert.NotNull(msg2);
            }
        }

        [Fact]
        public async Task ReadRecoverFromInvalidHeaderMessage()
        {
            // Setup: Reader with a stream that has incorrect message formatting
            const string testString = "Content-Type: application/json\r\n\r\n" +
                                      "Content-Length: 50\r\n\r\n{\"jsonrpc\": \"2.0\", \"method\":\"test\", \"params\":null}";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            using (Stream testStream = new MemoryStream(testBytes))
            {
                var mr = new MessageReader(testStream);
                
                // If: I read a message with invalid headers
                // Then: I should get an exception
                await Assert.ThrowsAnyAsync<MessageParseException>(() => mr.ReadMessage());
                
                // If: I read another, valid, message
                var msg = await mr.ReadMessage();
                
                // Then: I should have a valid message
                Assert.NotNull(msg);
            }
        }
        
        [Fact]
        public async Task ReadRecoverFromInvalidContentMessage()
        {
            // Setup: Reader with a stream that has incorrect message formatting
            const string testString = "Content-Length: 10\r\n\r\nabcdefghij" +
                                      "Content-Length: 50\r\n\r\n{\"jsonrpc\": \"2.0\", \"method\":\"test\", \"params\":null}";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            using (Stream testStream = new MemoryStream(testBytes))
            {
                var mr = new MessageReader(testStream);
                
                // If: I read a message with invalid content
                // Then: I should get an exception
                await Assert.ThrowsAnyAsync<JsonException>(() => mr.ReadMessage());
                
                // If: I read another, valid, message
                var msg = await mr.ReadMessage();
                
                // Then: I should have a valid message
                Assert.NotNull(msg);
            }
        }
        
        #endregion
    }
}