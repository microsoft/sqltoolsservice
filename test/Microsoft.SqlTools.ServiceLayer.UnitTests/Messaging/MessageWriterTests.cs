//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class MessageWriterTests
    {
        private readonly IMessageSerializer messageSerializer;

        public MessageWriterTests()
        {
            this.messageSerializer = new V8MessageSerializer();
        }

        [Test]
        public void SerializeMessageTest()
        {
            // serialize\deserialize a request
            var message = new Message();
            message.MessageType = MessageType.Request;
            message.Id = "id";            
            message.Method = "method";
            message.Contents = null;
            var serializedMessage = this.messageSerializer.SerializeMessage(message);
            Assert.NotNull(serializedMessage);
            var deserializedMessage = this.messageSerializer.DeserializeMessage(serializedMessage);
            Assert.AreEqual(message.Id, deserializedMessage.Id);

            // serialize\deserialize a response
            message.MessageType = MessageType.Response;
            serializedMessage = this.messageSerializer.SerializeMessage(message);
            Assert.NotNull(serializedMessage);
            deserializedMessage = this.messageSerializer.DeserializeMessage(serializedMessage);
            Assert.AreEqual(message.Id, deserializedMessage.Id);

            // serialize\deserialize a response with an error
            message.Error = JToken.FromObject("error");
            serializedMessage = this.messageSerializer.SerializeMessage(message);
            Assert.NotNull(serializedMessage);
            deserializedMessage = this.messageSerializer.DeserializeMessage(serializedMessage);
            Assert.AreEqual(message.Error, deserializedMessage.Error);

            // serialize\deserialize an unknown response type
            serializedMessage.Remove("type");
            serializedMessage.Add("type", JToken.FromObject("dontknowthisone"));
            Assert.AreEqual(MessageType.Unknown, this.messageSerializer.DeserializeMessage(serializedMessage).MessageType);
        }

        [Test]
        public async Task WritesMessage()
        {
            MemoryStream outputStream = new MemoryStream();
            MessageWriter messageWriter = new MessageWriter(outputStream, this.messageSerializer);

            // Write the message and then roll back the stream to be read
            // TODO: This will need to be redone!
            await messageWriter.WriteMessage(SqlTools.Hosting.Protocol.Contracts.Message.Event("testEvent", null));
            outputStream.Seek(0, SeekOrigin.Begin);

            string expectedHeaderString = string.Format(Constants.ContentLengthFormatString,
                Common.ExpectedMessageByteCount);

            byte[] buffer = new byte[128];
            await outputStream.ReadAsync(buffer, 0, expectedHeaderString.Length);

            Assert.AreEqual(
                expectedHeaderString,
                Encoding.ASCII.GetString(buffer, 0, expectedHeaderString.Length));

            // Read the message
            await outputStream.ReadAsync(buffer, 0, Common.ExpectedMessageByteCount);

            Assert.AreEqual(Common.TestEventString, 
                Encoding.UTF8.GetString(buffer, 0, Common.ExpectedMessageByteCount));

            outputStream.Dispose();
        }

    }
}
