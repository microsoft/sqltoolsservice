//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json.Linq;
using Xunit;
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
            Assert.Equal("2.0", token.ToString());

            if (checkId)
            {
                Assert.True(messageObj.TryGetValue("id", out token));
                Assert.Equal(MessageId, token.ToString());
            }

            if (checkMethod)
            {
                Assert.True(messageObj.TryGetValue("method", out token));
                Assert.Equal(MethodName, token.ToString());
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

