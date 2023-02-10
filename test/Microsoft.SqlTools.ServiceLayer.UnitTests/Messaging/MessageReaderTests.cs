﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Text;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class MessageReaderTests
    {

        private readonly IMessageSerializer messageSerializer;

        public MessageReaderTests()
        {
            this.messageSerializer = new V8MessageSerializer();
        }

        [Test]
        public void ReadsMessage()
        {
            MemoryStream inputStream = new MemoryStream();
            MessageReader messageReader = new MessageReader(inputStream, this.messageSerializer);

            // Write a message to the stream
            byte[] messageBuffer = this.GetMessageBytes(Common.TestEventString);
            inputStream.Write(this.GetMessageBytes(Common.TestEventString), 0, messageBuffer.Length);

            inputStream.Flush();
            inputStream.Seek(0, SeekOrigin.Begin);

            Message messageResult = messageReader.ReadMessage().Result;
            Assert.AreEqual("testEvent", messageResult.Method);

            inputStream.Dispose();
        }

        [Test]
        public void ReadsManyBufferedMessages()
        {
            MemoryStream inputStream = new MemoryStream();
            MessageReader messageReader =
                new MessageReader(
                    inputStream,
                    this.messageSerializer);

            // Get a message to use for writing to the stream
            byte[] messageBuffer = this.GetMessageBytes(Common.TestEventString);

            // How many messages of this size should we write to overflow the buffer?
            int overflowMessageCount =
                (int)Math.Ceiling(
                    (MessageReader.DefaultBufferSize * 1.5) / messageBuffer.Length);

            // Write the necessary number of messages to the stream
            for (int i = 0; i < overflowMessageCount; i++)
            {
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
            }

            inputStream.Flush();
            inputStream.Seek(0, SeekOrigin.Begin);

            // Read the written messages from the stream
            for (int i = 0; i < overflowMessageCount; i++)
            {
                Message messageResult = messageReader.ReadMessage().Result;
                Assert.AreEqual("testEvent", messageResult.Method);
            }

            inputStream.Dispose();
        }

        [Test]
        public void ReadMalformedMissingHeaderTest()
        {
            using (MemoryStream inputStream = new MemoryStream())
            {
                // If:
                // ... I create a new stream and pass it information that is malformed
                // ... and attempt to read a message from it
                MessageReader messageReader = new MessageReader(inputStream, messageSerializer);
                byte[] messageBuffer = Encoding.ASCII.GetBytes("This is an invalid header\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);

                Assert.ThrowsAsync<ArgumentException>(() => messageReader.ReadMessage(), "An exception should be thrown while reading");
            }
        }

        [Test]
        public void ReadMalformedContentLengthNonIntegerTest()
        {
            using (MemoryStream inputStream = new MemoryStream())
            {
                // If:
                // ... I create a new stream and pass it a non-integer content-length header
                // ... and attempt to read a message from it
                MessageReader messageReader = new MessageReader(inputStream, messageSerializer);
                byte[] messageBuffer = Encoding.ASCII.GetBytes("Content-Length: asdf\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);

                Assert.ThrowsAsync<MessageParseException>(() => messageReader.ReadMessage(), "An exception should be thrown while reading") ;
            }
        }

        [Test]
        public void ReadMissingContentLengthHeaderTest()
        {
            using (MemoryStream inputStream = new MemoryStream())
            {
                // If:
                // ... I create a new stream and pass it a a message without a content-length header
                // ... and attempt to read a message from it
                MessageReader messageReader = new MessageReader(inputStream, messageSerializer);
                byte[] messageBuffer = Encoding.ASCII.GetBytes("Content-Type: asdf\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);

                Assert.ThrowsAsync<MessageParseException>(() => messageReader.ReadMessage(), "An exception should be thrown while reading");
            }
        }

        [Test]
        public void ReadMalformedContentLengthTooShortTest()
        {
            using (MemoryStream inputStream = new MemoryStream())
            {
                // If:
                // ... Pass in an event that has an incorrect content length
                // ... And pass in an event that is correct
                MessageReader messageReader = new MessageReader(inputStream, messageSerializer);
                byte[] messageBuffer = Encoding.ASCII.GetBytes("Content-Length: 10\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                messageBuffer = Encoding.UTF8.GetBytes(Common.TestEventString);
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                messageBuffer = Encoding.ASCII.GetBytes("\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);

                Assert.ThrowsAsync<JsonReaderException>(() => messageReader.ReadMessage(), "The first read should fail with an exception while deserializing");

                Assert.ThrowsAsync<MessageParseException>(() => messageReader.ReadMessage(), "The second read should fail with an exception while reading headers");
            }
        }

        [Test]
        public void ReadMalformedThenValidTest()
        {
            // If:
            // ... I create a new stream and pass it information that is malformed
            // ... and attempt to read a message from it
            // ... Then pass it information that is valid and attempt to read a message from it
            using (MemoryStream inputStream = new MemoryStream())
            {
                MessageReader messageReader = new MessageReader(inputStream, messageSerializer);
                byte[] messageBuffer = Encoding.ASCII.GetBytes("This is an invalid header\r\n\r\n");
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                messageBuffer = GetMessageBytes(Common.TestEventString);
                inputStream.Write(messageBuffer, 0, messageBuffer.Length);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);

                Assert.ThrowsAsync<ArgumentException>(() => messageReader.ReadMessage(), "An exception should be thrown while reading the first one");

                // ... A test event should be successfully read from the second one
                Message messageResult = messageReader.ReadMessage().Result;
                Assert.NotNull(messageResult);
                Assert.AreEqual("testEvent", messageResult.Method);
            }
        }

        [Test]
        public void ReaderResizesBufferForLargeMessages()
        {
            MemoryStream inputStream = new MemoryStream();
            MessageReader messageReader =
                new MessageReader(
                    inputStream,
                    this.messageSerializer);

            // Get a message with content so large that the buffer will need
            // to be resized to fit it all.
            byte[] messageBuffer = this.GetMessageBytes(
                string.Format(
                    Common.TestEventFormatString,
                    new String('X', (int) (MessageReader.DefaultBufferSize*3))));

            inputStream.Write(messageBuffer, 0, messageBuffer.Length);
            inputStream.Flush();
            inputStream.Seek(0, SeekOrigin.Begin);

            Message messageResult = messageReader.ReadMessage().Result;
            Assert.AreEqual("testEvent", messageResult.Method);

            inputStream.Dispose();
        }

        [Test]
        public void ReaderDoesNotModifyDateStrings()
        {
            MemoryStream inputStream = new MemoryStream();
            MessageReader messageReader =
                new MessageReader(
                    inputStream,
                    this.messageSerializer);
            
            string dateString = "2018-04-27T18:33:55.870Z";

            // Get a message with content that is a date as a string
            byte[] messageBuffer = this.GetMessageBytes(
                string.Format(Common.TestEventFormatString, dateString));

            inputStream.Write(messageBuffer, 0, messageBuffer.Length);
            inputStream.Flush();
            inputStream.Seek(0, SeekOrigin.Begin);

            Message messageResult = messageReader.ReadMessage().Result;
            Assert.AreEqual(dateString, messageResult.Contents.Value<string>("someString"));

            inputStream.Dispose();
        }

        private byte[] GetMessageBytes(string messageString, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            byte[] messageBytes = Encoding.UTF8.GetBytes(messageString);
            byte[] headerBytes = Encoding.ASCII.GetBytes(string.Format(Constants.ContentLengthFormatString, messageBytes.Length));

            // Copy the bytes into a single buffer
            byte[] finalBytes = new byte[headerBytes.Length + messageBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, finalBytes, 0, headerBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, finalBytes, headerBytes.Length, messageBytes.Length);

            return finalBytes;
        }
    }
}
