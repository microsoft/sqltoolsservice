//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using HostingMessage = Microsoft.SqlTools.Hosting.Protocol.Contracts.Message;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    public class TestMessageContents
    {
        public const string SomeFieldValue = "Some value";
        public const int NumberValue = 42;

        public string SomeField { get; set; }

        public int Number { get; set; }

        public TestMessageContents()
        {
            this.SomeField = SomeFieldValue;
            this.Number = NumberValue;
        }
    }

    public class JsonRpcMessageSerializerTests
    {
        private IMessageSerializer messageSerializer;

        private const string MessageId = "42";
        private const string MethodName = "testMethod";
        private static readonly JToken MessageContent = JToken.FromObject(new TestMessageContents());

        public JsonRpcMessageSerializerTests()
        {
            this.messageSerializer = new JsonRpcMessageSerializer();
        }

        [Test]
        public void SerializesRequestMessages()
        {
            var messageObj =
                this.messageSerializer.SerializeMessage(
                    HostingMessage.Request(
                        MessageId, 
                        MethodName, 
                        MessageContent));

            AssertMessageFields(
                messageObj,
                checkId: true,
                checkMethod: true,
                checkParams: true);
        }

        [Test]
        public void SerializesEventMessages()
        {
            var messageObj =
                this.messageSerializer.SerializeMessage(
                    HostingMessage.Event(
                        MethodName, 
                        MessageContent));

            AssertMessageFields(
                messageObj,
                checkMethod: true,
                checkParams: true);
        }

        [Test]
        public void SerializesResponseMessages()
        {
            var messageObj =
                this.messageSerializer.SerializeMessage(
                    HostingMessage.Response(
                        MessageId,
                        null,
                        MessageContent));

            AssertMessageFields(
                messageObj,
                checkId: true,
                checkResult: true);
        }

        [Test]
        public void SerializesResponseWithErrorMessages()
        {
            var messageObj =
                this.messageSerializer.SerializeMessage(
                   HostingMessage.ResponseError(
                        MessageId,
                        null,
                        MessageContent));

            AssertMessageFields(
                messageObj,
                checkId: true,
                checkError: true);
        }

        [Test]
        public async Task RequestContextSendResultPreservesNumericRequestId()
        {
            JObject requestObj = JObject.FromObject(new
            {
                jsonrpc = "2.0",
                id = 42,
                method = MethodName,
                @params = new TestMessageContents()
            });

            HostingMessage requestMessage = this.messageSerializer.DeserializeMessage(requestObj);
            JObject responseObj = await SendResponse(requestMessage);

            Assert.True(responseObj.TryGetValue("id", out JToken token));
            Assert.AreEqual(JTokenType.Integer, token.Type);
            Assert.AreEqual(42, token.ToObject<int>());
        }

        [Test]
        public async Task RequestContextSendResultPreservesStringRequestId()
        {
            JObject requestObj = JObject.FromObject(new
            {
                jsonrpc = "2.0",
                id = MessageId,
                method = MethodName,
                @params = new TestMessageContents()
            });

            HostingMessage requestMessage = this.messageSerializer.DeserializeMessage(requestObj);
            JObject responseObj = await SendResponse(requestMessage);

            Assert.True(responseObj.TryGetValue("id", out JToken token));
            Assert.AreEqual(JTokenType.String, token.Type);
            Assert.AreEqual(MessageId, token.ToString());
        }

        private static async Task<JObject> SendResponse(HostingMessage requestMessage)
        {
            using MemoryStream outputStream = new MemoryStream();
            MessageWriter messageWriter = new MessageWriter(outputStream, new JsonRpcMessageSerializer());
            RequestContext<TestMessageContents> requestContext =
                new RequestContext<TestMessageContents>(requestMessage, messageWriter);

            await requestContext.SendResult(new TestMessageContents());

            string output = Encoding.UTF8.GetString(outputStream.ToArray());
            int bodyStart = output.IndexOf("\r\n\r\n", StringComparison.Ordinal) + "\r\n\r\n".Length;
            return JObject.Parse(output.Substring(bodyStart));
        }

        private static void AssertMessageFields(
            JObject messageObj, 
            bool checkId = false,
            bool checkMethod = false,
            bool checkParams = false,
            bool checkResult = false, 
            bool checkError = false)
        {
            JToken token = null;

            Assert.True(messageObj.TryGetValue("jsonrpc", out token));
            Assert.AreEqual("2.0", token.ToString());

            if (checkId)
            {
                Assert.True(messageObj.TryGetValue("id", out token));
                Assert.AreEqual(MessageId, token.ToString());
            }

            if (checkMethod)
            {
                Assert.True(messageObj.TryGetValue("method", out token));
                Assert.AreEqual(MethodName, token.ToString());
            }

            if (checkError)
            {
                // TODO
            }
            else
            {
                string contentField = checkParams ? "params" : "result";
                Assert.True(messageObj.TryGetValue(contentField, out token));
                Assert.True(JToken.DeepEquals(token, MessageContent));
            }
        }
    }
}

